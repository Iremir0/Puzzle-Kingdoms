using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// PB-10 art spike: lays out candidate 3D models as a 4×4 army against an enemy
    /// army under the PB-11 battle camera and saves screenshots plus a triangle
    /// report to <c>Logs/ArtSpike/</c>. Models are read from
    /// <c>Assets/_Project/Sandbox/&lt;Pack&gt;/</c>, which git ignores, so nothing
    /// third-party is committed before the decision. The scene is never saved.
    /// Menu: Puzzle Kingdoms → Art Spike Screenshots. Batch mode:
    /// -executeMethod PuzzleKingdoms.EditorTools.ArtSpike.Capture
    /// </summary>
    public static class ArtSpike
    {
        public const string SandboxRoot = "Assets/_Project/Sandbox/";
        /// <summary>Approved, own-made models (Meshy Pro, private licence) live here, one folder per model, in git.</summary>
        public const string CharactersRoot = "Assets/_Project/Art/Characters/";
        public const string OutputFolder = "Logs/ArtSpike";
        private const int Width = 1080;
        private const int Height = 1920;
        private const float LaneSpacing = 1.6f;
        private const float RowSpacing = 1.9f;
        private const float FrontGap = 3.0f;

        /// <summary>One candidate pack: player models front to back row, enemy models front to back row.</summary>
        private sealed class Pack
        {
            public string Name;
            public string[] PlayerRows;
            public string[] EnemyRows;
            public Dictionary<string, string> Textures;
            /// <summary>Accessory parts to hide per model (packs ship every weapon variant switched on).</summary>
            public Dictionary<string, string[]> Hide = new Dictionary<string, string[]>();
        }

        /// <summary>Unit height in metres at tier 1, so one cell (1.6 m) holds one unit with room around it.</summary>
        private const float UnitHeight = 1.15f;

        private static readonly Pack[] Packs =
        {
            new Pack
            {
                Name = "KayKit",
                // Warrior, Archer (Rogue with crossbow stands in for the Ranger), Mage; enemies: skeletons for goblins.
                PlayerRows = new[] { "Meshy/Hero_Knight", "Meshy/Hero_Knight", "Meshy/Hero_Archer", "KayKit/Mage" },
                EnemyRows = new[] { "KayKit/Skeleton_Warrior", "KayKit/Skeleton_Minion", "KayKit/Skeleton_Rogue", "KayKit/Skeleton_Mage" },
                Textures = new Dictionary<string, string>
                {
                    { "Knight", "knight_texture" }, { "Rogue", "rogue_texture" }, { "Mage", "mage_texture" },
                    { "Skeleton_Warrior", "skeleton_texture" }, { "Skeleton_Minion", "skeleton_texture" },
                    { "Skeleton_Rogue", "skeleton_texture" }, { "Skeleton_Mage", "skeleton_texture" },
                },
                Hide = new Dictionary<string, string[]>
                {
                    // Warrior: one sword and the round shield.
                    { "Knight", new[] { "1H_Sword_Offhand", "Badge_Shield", "Rectangle_Shield", "Spike_Shield", "2H_Sword" } },
                    // Archer: the two-handed crossbow only.
                    { "Rogue", new[] { "Knife_Offhand", "1H_Crossbow", "Knife", "Throwable" } },
                    // Mage: the staff only.
                    { "Mage", new[] { "Spellbook", "Spellbook_open", "1H_Wand" } },
                },
            },
            new Pack
            {
                // D-041: our own Meshy models (Pro plan, private licence) against the KayKit stand-ins.
                // New downloads go into the git-ignored Sandbox/Meshy/; approved ones into Art/Characters/<Name>/. Missing models are skipped.
                Name = "Meshy",
                PlayerRows = new[] { "Meshy/Hero_Knight", "Meshy/Hero_Knight", "Meshy/Hero_Archer", "KayKit/Mage" },
                EnemyRows = new[] { "Meshy/Goblin_Brute", "Meshy/Goblin_Grunt", "Meshy/Goblin_Chief", "Meshy/Goblin_Archer" },
                Textures = new Dictionary<string, string>
                {
                    { "Knight", "knight_texture" }, { "Rogue", "rogue_texture" }, { "Mage", "mage_texture" },
                    // Meshy exports: the base colour PNG is saved next to the FBX under the same name.
                    { "Hero_Knight", "Hero_Knight" }, { "Hero_Archer", "Hero_Archer" }, { "Goblin_Grunt", "Goblin_Grunt" }, { "Goblin_Archer", "Goblin_Archer" },
                    { "Goblin_Brute", "Goblin_Brute" }, { "Goblin_Chief", "Goblin_Chief" },
                },
                Hide = new Dictionary<string, string[]>
                {
                    { "Knight", new[] { "1H_Sword_Offhand", "Badge_Shield", "Rectangle_Shield", "Spike_Shield", "2H_Sword" } },
                    { "Rogue", new[] { "Knife_Offhand", "1H_Crossbow", "Knife", "Throwable" } },
                    { "Mage", new[] { "Spellbook", "Spellbook_open", "1H_Wand" } },
                },
            },
        };

        [MenuItem("Puzzle Kingdoms/Art Spike Screenshots")]
        public static void Capture()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(OutputFolder);
            var report = new StringBuilder();
            foreach (Pack pack in Packs)
            {
                if (!pack.PlayerRows.Concat(pack.EnemyRows).Any(row => File.Exists(Locate(SandboxRoot, row, ".fbx"))))
                {
                    report.AppendLine($"{pack.Name}: no model found in {SandboxRoot} or {CharactersRoot}, skipped");
                    continue;
                }
                CapturePack(pack, SandboxRoot, report);
            }
            File.WriteAllText(Path.Combine(OutputFolder, "report.txt"), report.ToString());
            Debug.Log("[ArtSpike]\n" + report);
        }


        private static void CapturePack(Pack pack, string folder, StringBuilder report)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.5f);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(3f, 1f, 4f);
            ground.GetComponent<Renderer>().sharedMaterial = new Material(lit) { color = new Color32(0x4A, 0x6B, 0x4F, 0xFF) };

            report.AppendLine($"== {pack.Name} ==");
            var triangles = new Dictionary<string, int>();
            PlaceArmy(pack.PlayerRows, folder, pack, lit, unlit, false, triangles);
            PlaceArmy(pack.EnemyRows, folder, pack, lit, unlit, true, triangles);
            foreach (KeyValuePair<string, int> pair in triangles)
            {
                report.AppendLine($"{pair.Key}: {pair.Value} triangles (visible parts)");
            }

            var camera = new GameObject("Battle Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x1E, 0x22, 0x30, 0xFF);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;

            Shoot(camera, 35f, 0f, 9.5f, Path.Combine(OutputFolder, "warmup.png")); // the first render in batch mode can be garbage
            File.Delete(Path.Combine(OutputFolder, "warmup.png"));
            Shoot(camera, 45f, 0f, 7.5f, Path.Combine(OutputFolder, $"{pack.Name}-front.png"));
            Shoot(camera, 35f, 45f, 8f, Path.Combine(OutputFolder, $"{pack.Name}-iso45.png"));
            Shoot(camera, 45f, 0f, 3.6f, Path.Combine(OutputFolder, $"{pack.Name}-closeup.png"), new Vector3(0f, 0f, -FrontGap / 2f - RowSpacing));
            Shoot(camera, 30f, 0f, 1.6f, Path.Combine(OutputFolder, $"{pack.Name}-enemy-closeup.png"), new Vector3(-LaneSpacing * 1.5f, 0.6f, FrontGap / 2f));
        }

        /// <summary>A 4×4 army; tiers 1-3 across the lanes to show the GDD tier look (size, trim ring, glow).</summary>
        private static void PlaceArmy(string[] rows, string folder, Pack pack, Shader lit, Shader unlit, bool enemy,
            Dictionary<string, int> triangles)
        {
            for (int row = 0; row < rows.Length; row++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Locate(folder, rows[row], ".fbx"));
                if (model == null)
                {
                    Debug.LogWarning($"[ArtSpike] missing {rows[row]}.fbx (looked in {folder} and {CharactersRoot})");
                    continue;
                }
                Material material = MaterialFor(pack, folder, rows[row], lit);
                for (int lane = 0; lane < 4; lane++)
                {
                    int tier = enemy ? 1 : lane < 2 ? 1 : lane == 2 ? 2 : 3;
                    float z = (FrontGap / 2f + row * RowSpacing) * (enemy ? 1f : -1f);
                    float x = (lane - 1.5f) * LaneSpacing;
                    var unit = (GameObject)Object.Instantiate(model);
                    unit.name = $"{rows[row]} T{tier}";
                    unit.transform.position = new Vector3(x, 0f, z);
                    unit.transform.rotation = Quaternion.Euler(0f, enemy ? 180f : 0f, 0f);
                    Pose(unit, model);
                    if (pack.Hide.TryGetValue(Path.GetFileName(rows[row]), out string[] hidden))
                    {
                        foreach (Renderer part in unit.GetComponentsInChildren<Renderer>(true))
                        {
                            if (hidden.Contains(part.name))
                            {
                                part.gameObject.SetActive(false);
                            }
                        }
                    }
                    if (rows[row].StartsWith("Meshy/"))
                    {
                        StandUp(unit, Path.GetFileName(rows[row]), enemy);
                    }
                    float height = BodyHeight(unit, Path.GetFileName(rows[row]));
                    float tierScale = tier == 1 ? 1f : tier == 2 ? 1.1f : 1.2f;
                    // Multiply, not replace: some imports carry a root scale (Meshy: ×100 for centimetres).
                    unit.transform.localScale *= UnitHeight / height * tierScale;
                    Ground(unit);
                    int count = 0;
                    foreach (Renderer renderer in unit.GetComponentsInChildren<Renderer>())
                    {
                        if (material != null)
                        {
                            renderer.sharedMaterial = material;
                        }
                        Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                            : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                        for (int sub = 0; mesh != null && sub < mesh.subMeshCount; sub++)
                        {
                            count += (int)(mesh.GetIndexCount(sub) / 3);
                        }
                    }
                    triangles[Path.GetFileName(rows[row])] = count;
                    if (lane == 0)
                    {
                        Debug.Log($"[ArtSpike] {rows[row]} parts: " + string.Join(", ", unit.GetComponentsInChildren<Renderer>(true)
                            .Select(r => $"{r.name}({(r.enabled && r.gameObject.activeInHierarchy ? "on" : "off")}, {r.bounds.size.y:0.0}m)")));
                    }
                    if (tier >= 2)
                    {
                        TierRing(unit.transform.position, tier, unlit);
                    }
                }
            }
        }

        /// <summary>
        /// World rotation that makes a Meshy model stand up facing the camera (towards -Z). Meshy does not
        /// export every model with the same axes: found by rendering candidates side by side (PB-10).
        /// </summary>
        private static readonly Dictionary<string, Quaternion> FacingCamera = new Dictionary<string, Quaternion>
        {
            { "Goblin_Grunt", Quaternion.Euler(90f, 0f, 0f) },
            { "Hero_Knight", Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(-90f, 0f, 0f) },
            { "Hero_Archer", Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(-90f, 0f, 0f) },
        };

        /// <summary>Stands a Meshy model up; players face away from the camera, towards the enemy.</summary>
        private static void StandUp(GameObject unit, string name, bool enemy)
        {
            Quaternion rotation = FacingCamera.TryGetValue(name, out Quaternion known) ? known : Quaternion.Euler(90f, 0f, 0f);
            unit.transform.rotation = enemy ? rotation : Quaternion.Euler(0f, 180f, 0f) * rotation;
        }

        private static Bounds VisibleBounds(GameObject unit)
        {
            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(unit.transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Meshy exports lie on their back (Z-up) and face backwards, in every file:
        /// turn the root 90° about X so it stands, then 180° so it faces the camera.
        /// because Z-up exporters put the head at +Z.
        /// </summary>

        /// <summary>Moves the unit up or down so its lowest point touches the ground.</summary>
        private static void Ground(GameObject unit)
        {
            Bounds bounds = VisibleBounds(unit);
            unit.transform.position += Vector3.up * (unit.transform.position.y - bounds.min.y);
        }

        /// <summary>Height of the visible body (all active renderers) at scale 1.</summary>
        private static float BodyHeight(GameObject unit, string name)
        {
            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return 1f;
            }
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            Debug.Log($"[ArtSpike] {name} height {bounds.size.y:0.00} m, width {bounds.size.x:0.00} m, depth {bounds.size.z:0.00} m, centre {bounds.center}, root rot {unit.transform.rotation.eulerAngles}, renderers {renderers.Length}, types {string.Join("/", renderers.Select(r => r.GetType().Name + " lossy " + r.transform.lossyScale + " mesh " + ((r as SkinnedMeshRenderer)?.sharedMesh ?? r.GetComponent<MeshFilter>()?.sharedMesh)?.bounds.size))}");
            return Mathf.Max(0.1f, bounds.size.y);
        }

        /// <summary>Samples the first idle-like clip, so units stand in a pose instead of a T-pose.</summary>
        private static void Pose(GameObject unit, GameObject model)
        {
            string path = AssetDatabase.GetAssetPath(model);
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToArray();
            AnimationClip idle = clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("idle")) ?? clips.FirstOrDefault();
            if (idle != null)
            {
                idle.SampleAnimation(unit, idle.length * 0.3f);
            }
        }

        /// <summary>
        /// A URP material with the pack's atlas texture, or null to keep the materials the model
        /// was imported with (AI-made models bring their own textures).
        /// </summary>
        private static Material MaterialFor(Pack pack, string folder, string row, Shader lit)
        {
            if (!pack.Textures.TryGetValue(Path.GetFileName(row), out string texture))
            {
                return null;
            }
            var material = new Material(lit);
            string directory = Path.GetDirectoryName(Locate(folder, row, ".fbx"))?.Replace('\\', '/');
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{directory}/{texture}.png"));
            material.SetFloat("_Smoothness", 0.15f);
            return material;
        }

        /// <summary>
        /// Where a model is: first in the git-ignored sandbox (new downloads being tried), otherwise in
        /// <see cref="CharactersRoot"/>, where approved models live in git (one folder per model).
        /// </summary>
        private static string Locate(string sandbox, string row, string extension)
        {
            string tried = sandbox + row + extension;
            if (File.Exists(tried))
            {
                return tried;
            }
            string nested = $"{sandbox}{row}/{Path.GetFileName(row)}{extension}"; // a downloaded model in its own folder
            if (File.Exists(nested))
            {
                return nested;
            }
            string name = Path.GetFileName(row);
            string committed = $"{CharactersRoot}{name}/{name}{extension}";
            return File.Exists(committed) ? committed : tried;
        }

        /// <summary>Tier 2: a trim-coloured ring under the feet; tier 3: a brighter, larger, glowing ring.</summary>
        private static void TierRing(Vector3 at, int tier, Shader unlit)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.position = at + Vector3.up * 0.02f;
            float size = tier == 2 ? 1.0f : 1.25f;
            ring.transform.localScale = new Vector3(size, 0.01f, size);
            Color gold = new Color32(0xF4, 0xBE, 0x4F, 0xFF);
            ring.GetComponent<Renderer>().sharedMaterial = new Material(unlit)
            {
                color = tier == 2 ? new Color(gold.r, gold.g, gold.b, 1f) * 0.8f : gold,
            };
            if (tier == 3)
            {
                var glow = new GameObject("Glow").AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = gold;
                glow.range = 1.4f;
                glow.intensity = 1.2f;
                glow.transform.position = at + Vector3.up * 1.2f;
            }
        }

        private static void Shoot(Camera camera, float pitch, float yaw, float size, string file, Vector3? focus = null)
        {
            Vector3 target = focus ?? Vector3.zero;
            camera.orthographicSize = size;
            camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            camera.transform.position = target - camera.transform.forward * 40f;
            var texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = texture;
            camera.aspect = Width / (float)Height;
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(image);
        }
    }
}
