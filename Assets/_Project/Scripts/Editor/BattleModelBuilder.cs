using System.Collections.Generic;
using System.IO;
using PuzzleKingdoms.Data;
using UnityEditor;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// PB-11: builds the data of the 3D battle models from <c>Assets/_Project/Art/Characters/&lt;Name&gt;/</c> so it is
    /// reproducible instead of clicked by hand: one URP Lit material per model (with its base-colour texture), the
    /// <c>Data/BattleModels.asset</c> (<see cref="BattleModelSet"/>: look → model, material, upright rotation) and the
    /// reference to it in <c>GameContent</c>. The upright rotation comes from <c>ArtSpike.FacingCamera</c> (found by
    /// rendering), turned half a circle so the model faces +Z, the way a player unit looks into the battle.
    /// Menu: Puzzle Kingdoms → Build Battle Models. Batch mode:
    /// -executeMethod PuzzleKingdoms.EditorTools.BattleModelBuilder.Build
    /// </summary>
    public static class BattleModelBuilder
    {
        private const string SetPath = "Assets/_Project/Data/BattleModels.asset";
        private const string ContentPath = "Assets/_Project/Data/GameContent.asset";

        /// <summary>Model folder name → the look that uses it.</summary>
        private static readonly (string Name, UnitLook Look)[] Models =
        {
            ("Hero_Knight", UnitLook.Warrior),
            ("Hero_Archer", UnitLook.Archer),
            ("Hero_Mage", UnitLook.Mage),
            ("Goblin_Grunt", UnitLook.GoblinGrunt),
            ("Goblin_Archer", UnitLook.GoblinArcher),
            ("Goblin_Brute", UnitLook.GoblinBrute),
            ("Goblin_Chief", UnitLook.GoblinChief),
            // Chapter 2 (PB-18, D-051): static models for now, rigging comes with the animations.
            ("Goblin_Shield", UnitLook.ShieldGoblin),
            ("Goblin_Shaman", UnitLook.GoblinShaman),
            ("Goblin_WolfRider", UnitLook.WolfRider),
            ("Goblin_Bomber", UnitLook.GoblinBomber),
        };

        /// <summary>
        /// Euler angles that make a rigged model (a <c>_Rig.fbx</c>) stand up and face +Z, read from its own
        /// skeleton: the head bone is above the hips, the left toes point ahead of the left foot. Rounded to
        /// quarter turns because the bind pose is axis-aligned.
        /// </summary>
        private static Vector3 UprightFromSkeleton(GameObject rig)
        {
            GameObject instance = Object.Instantiate(rig);
            try
            {
                Transform Find(string bone)
                {
                    foreach (Transform t in instance.GetComponentsInChildren<Transform>())
                    {
                        if (t.name == "mixamorig:" + bone)
                        {
                            return t;
                        }
                    }
                    return null;
                }

                Transform hips = Find("Hips");
                Transform head = Find("Head");
                Transform foot = Find("LeftFoot");
                Transform toes = Find("LeftToeBase");
                if (hips == null || head == null || foot == null || toes == null)
                {
                    Debug.LogWarning($"[BattleModelBuilder] {rig.name}: bones Hips/Head/LeftFoot/LeftToeBase not found, no rotation.");
                    return Vector3.zero;
                }
                Vector3 up = (head.position - hips.position).normalized;
                Vector3 forward = Vector3.ProjectOnPlane(toes.position - foot.position, up).normalized;
                Vector3 euler = Quaternion.Inverse(Quaternion.LookRotation(forward, up)).eulerAngles;
                return new Vector3(Quarter(euler.x), Quarter(euler.y), Quarter(euler.z));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static float Quarter(float degrees) => Mathf.Round(degrees / 90f) * 90f % 360f;

        [MenuItem("Puzzle Kingdoms/Build Battle Models")]
        public static void Build()
        {
            var set = AssetDatabase.LoadAssetAtPath<BattleModelSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<BattleModelSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }

            var entries = new List<BattleModelSet.Entry>();
            foreach ((string name, UnitLook look) in Models)
            {
                string folder = $"{ArtSpike.CharactersRoot}{name}";

                // The rigged model (PB-12, Humanoid, animated by the shared controller) if there is one,
                // otherwise the static model (D-042).
                var rig = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{name}_Rig.fbx");
                var model = rig != null ? rig : AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{name}.fbx");
                if (model == null)
                {
                    Debug.LogWarning($"[BattleModelBuilder] {folder}/{name}.fbx not found, {look} stays a capsule.");
                    continue;
                }
                Vector3 upright;
                if (rig != null)
                {
                    upright = UprightFromSkeleton(rig);
                    // Checked by rendering: the skeleton rule is right for the Mage and the goblins, but the Knight
                    // and the Archer export with a different bind pose and came out facing the camera.
                    if (name == "Hero_Knight" || name == "Hero_Archer")
                    {
                        upright = Vector3.zero;
                    }
                    Debug.Log($"[BattleModelBuilder] {name} rig upright {upright}");
                }
                else
                {
                    Quaternion facing = ArtSpike.FacingCamera.TryGetValue(name, out Quaternion found) ? found : Quaternion.Euler(90f, 0f, 0f);
                    upright = (Quaternion.Euler(0f, 180f, 0f) * facing).eulerAngles;
                }
                entries.Add(new BattleModelSet.Entry
                {
                    look = look,
                    model = model,
                    material = BuildMaterial(folder, name),
                    uprightEuler = upright,
                });
            }

            var serialized = new SerializedObject(set);
            SerializedProperty list = serialized.FindProperty("entries");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("look").enumValueIndex = (int)entries[i].look;
                item.FindPropertyRelative("model").objectReferenceValue = entries[i].model;
                item.FindPropertyRelative("material").objectReferenceValue = entries[i].material;
                item.FindPropertyRelative("uprightEuler").vector3Value = entries[i].uprightEuler;
            }
            serialized.FindProperty("animator").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BattleAnimatorBuilder.ControllerPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);

            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
            var contentObject = new SerializedObject(content);
            contentObject.FindProperty("battleModels").objectReferenceValue = set;
            contentObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);

            AssetDatabase.SaveAssets();
            Debug.Log($"[BattleModelBuilder] {entries.Count} models in {SetPath}, linked in GameContent.");
        }

        private static Material BuildMaterial(string folder, string name)
        {
            string path = $"{folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{name}.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
