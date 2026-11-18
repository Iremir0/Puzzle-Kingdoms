using System.Collections.Generic;
using UnityEngine;

namespace PuzzleKingdoms.Views.Battle3D
{
    /// <summary>
    /// The look of the battlefield (D-055): a painted ground (mown grass, a trampled dirt arena, pebbles), trees,
    /// bushes, rocks, a fence and the two armies' banners around it. All made from code and primitives, no art
    /// files, the same every battle. Props stay at the sides and the far end; nothing stands in front of the
    /// player's back row.
    /// </summary>
    public static class BattleScenery
    {
        private const int TextureSize = 2048;
        /// <summary>World size of the ground texture (x by z); the quad is scaled to match.</summary>
        private const float GroundWidth = 18f;
        private const float GroundDepth = 22f;
        /// <summary>World size of a painted ground picture.</summary>
        private const float PaintedWidth = 10.5f;
        private const float PaintedDepth = 19f;
        private const float GatePaintedWidth = 8.9f;
        private const float GatePaintedDepth = 19.2f;
        private const float ArenaHalfWidth = 3.0f;
        private const float ArenaHalfDepth = 8.2f;

        private static Texture2D _groundTexture;
        private static readonly Dictionary<Color32, Material> Materials = new Dictionary<Color32, Material>();

        /// <param name="painted">A painted ground picture (PB-18 art pass); null builds the procedural ground with its props.</param>
        public static void Build(Transform root, int layer, Texture2D painted = null)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            Prepare(ground, root, layer);
            ground.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Material groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMaterial.SetFloat("_Smoothness", 0f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            if (painted != null)
            {
                // The picture already has the trees, fences and camps, so nothing else is put on it.
                bool fort = painted.name == "Battle_Gate"; // full-width fort picture: its own side walls show
                ground.transform.localScale = fort ? new Vector3(GatePaintedWidth, GatePaintedDepth, 1f) : new Vector3(PaintedWidth, PaintedDepth, 1f);
                groundMaterial.mainTexture = painted;
                // The picture is bright and the sun adds more: pull it down and a little toward grey so units stand out.
                groundMaterial.SetColor("_BaseColor", new Color(0.62f, 0.6f, 0.58f, 1f));
                return;
            }
            ground.transform.localScale = new Vector3(GroundWidth, GroundDepth, 1f);
            groundMaterial.mainTexture = GroundTexture();

            var random = new System.Random(20261118);
            ChalkLine(root, layer);
            Fence(root, layer, -1f);
            Fence(root, layer, 1f);
            SideProps(root, layer, random, -1f);
            SideProps(root, layer, random, 1f);
            FarProps(root, layer, random);
            NearProps(root, layer, random);
            Banner(root, layer, new Vector3(-2.7f, 0f, -7.6f), new Color32(0x3D, 0x7B, 0xD9, 0xFF));
            Banner(root, layer, new Vector3(2.7f, 0f, -7.6f), new Color32(0x3D, 0x7B, 0xD9, 0xFF));
            Banner(root, layer, new Vector3(-2.7f, 0f, 7.0f), new Color32(0x6E, 0x8F, 0x2E, 0xFF));
            Banner(root, layer, new Vector3(2.7f, 0f, 7.0f), new Color32(0x6E, 0x8F, 0x2E, 0xFF));
        }

        // ---- props ----

        private static void SideProps(Transform root, int layer, System.Random random, float side)
        {
            for (float z = -7.5f; z <= 9.5f; z += 1.9f)
            {
                float x = side * (3.9f + (float)random.NextDouble() * 0.7f);
                float zz = z + ((float)random.NextDouble() - 0.5f) * 0.8f;
                double pick = random.NextDouble();
                if (z < -6f)
                {
                    Bush(root, layer, new Vector3(x, 0f, zz), 0.7f + (float)random.NextDouble() * 0.3f);
                }
                else if (pick < 0.62)
                {
                    Tree(root, layer, new Vector3(x, 0f, zz), 0.85f + (float)random.NextDouble() * 0.5f, random);
                }
                else if (pick < 0.82)
                {
                    Bush(root, layer, new Vector3(x - side * 0.3f, 0f, zz), 0.8f);
                }
                else
                {
                    Rock(root, layer, new Vector3(x - side * 0.4f, 0f, zz), 0.55f + (float)random.NextDouble() * 0.35f, random);
                }
            }
        }

        private static void FarProps(Transform root, int layer, System.Random random)
        {
            for (float x = -6.5f; x <= 6.5f; x += 2.1f)
            {
                Tree(root, layer, new Vector3(x + ((float)random.NextDouble() - 0.5f), 0f, 9.4f + (float)random.NextDouble() * 0.5f),
                    1.1f + (float)random.NextDouble() * 0.5f, random);
            }
        }

        private static void NearProps(Transform root, int layer, System.Random random)
        {
            // Low things only: a tall prop here would stand in front of the player's units.
            for (float x = -6f; x <= 6f; x += 2.4f)
            {
                if (Mathf.Abs(x) < 3.2f)
                {
                    continue;
                }
                Bush(root, layer, new Vector3(x, 0f, -9.2f), 0.7f);
            }
            Rock(root, layer, new Vector3(-4.2f, 0f, -8.6f), 0.6f, random);
            Rock(root, layer, new Vector3(4.4f, 0f, -8.8f), 0.5f, random);
        }

        private static void Tree(Transform root, int layer, Vector3 at, float scale, System.Random random)
        {
            var tree = new GameObject("Tree");
            Prepare(tree, root, layer);
            tree.transform.localPosition = at;
            float shade = 0.85f + (float)random.NextDouble() * 0.3f;
            Color32 leaves = new Color(0.16f * shade, 0.42f * shade, 0.2f * shade);
            Color32 leavesLight = new Color(0.24f * shade, 0.55f * shade, 0.26f * shade);
            Part(tree.transform, layer, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f) * scale,
                new Vector3(0.2f, 0.55f, 0.2f) * scale, new Color32(0x5A, 0x3E, 0x26, 0xFF));
            Part(tree.transform, layer, PrimitiveType.Sphere, new Vector3(0f, 1.35f, 0f) * scale, new Vector3(1.25f, 1f, 1.25f) * scale, leaves);
            Part(tree.transform, layer, PrimitiveType.Sphere, new Vector3(0.05f, 1.95f, 0f) * scale, new Vector3(0.95f, 0.85f, 0.95f) * scale, leavesLight);
            Part(tree.transform, layer, PrimitiveType.Sphere, new Vector3(0f, 2.45f, 0f) * scale, new Vector3(0.6f, 0.6f, 0.6f) * scale, leaves);
            Sway(tree, 2.2f, 0.7f, 0f);
        }

        /// <summary>Wind sway on a prop (see <see cref="SceneryMotion"/>); returns it so a caller can adjust.</summary>
        private static SceneryMotion Sway(GameObject go, float degrees, float speed, float yaw)
        {
            var motion = go.AddComponent<SceneryMotion>();
            motion.Degrees = degrees;
            motion.Speed = speed;
            motion.YawDegrees = yaw;
            return motion;
        }

        private static void Bush(Transform root, int layer, Vector3 at, float scale)
        {
            var bush = new GameObject("Bush");
            Prepare(bush, root, layer);
            bush.transform.localPosition = at;
            Part(bush.transform, layer, PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0f) * scale, new Vector3(0.9f, 0.6f, 0.8f) * scale,
                new Color32(0x2F, 0x6E, 0x34, 0xFF));
            Part(bush.transform, layer, PrimitiveType.Sphere, new Vector3(0.3f, 0.22f, 0.1f) * scale, new Vector3(0.6f, 0.45f, 0.55f) * scale,
                new Color32(0x3A, 0x80, 0x3C, 0xFF));
            Sway(bush, 1.2f, 1.1f, 0f);
        }

        private static void Rock(Transform root, int layer, Vector3 at, float scale, System.Random random)
        {
            var rock = new GameObject("Rock");
            Prepare(rock, root, layer);
            rock.transform.localPosition = at;
            rock.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 180f, 0f);
            Part(rock.transform, layer, PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0f) * scale, new Vector3(1f, 0.6f, 0.8f) * scale,
                new Color32(0x84, 0x84, 0x8A, 0xFF));
            Part(rock.transform, layer, PrimitiveType.Sphere, new Vector3(0.35f, 0.12f, 0.1f) * scale, new Vector3(0.55f, 0.38f, 0.5f) * scale,
                new Color32(0x9A, 0x9A, 0xA0, 0xFF));
        }

        private static void Fence(Transform root, int layer, float side)
        {
            var fence = new GameObject("Fence");
            Prepare(fence, root, layer);
            Color32 wood = new Color32(0x7A, 0x56, 0x34, 0xFF);
            for (float z = -7.6f; z <= 7.6f; z += 1.6f)
            {
                Part(fence.transform, layer, PrimitiveType.Cube, new Vector3(side * 3.3f, 0.35f, z), new Vector3(0.12f, 0.7f, 0.12f), wood);
            }
            Part(fence.transform, layer, PrimitiveType.Cube, new Vector3(side * 3.3f, 0.52f, 0f), new Vector3(0.07f, 0.08f, 16.0f), wood);
            Part(fence.transform, layer, PrimitiveType.Cube, new Vector3(side * 3.3f, 0.28f, 0f), new Vector3(0.07f, 0.08f, 16.0f), wood);
        }

        private static void Banner(Transform root, int layer, Vector3 at, Color32 cloth)
        {
            var banner = new GameObject("Banner");
            Prepare(banner, root, layer);
            banner.transform.localPosition = at;
            Part(banner.transform, layer, PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(0.06f, 1.0f, 0.06f), new Color32(0x5A, 0x3E, 0x26, 0xFF));
            // The cloth hangs from a pivot at the pole so it can flutter around it.
            var pivot = new GameObject("Cloth");
            Prepare(pivot, banner.transform, layer);
            pivot.transform.localPosition = new Vector3(0.03f, 0f, 0f);
            Part(pivot.transform, layer, PrimitiveType.Cube, new Vector3(0.25f, 1.7f, 0f), new Vector3(0.5f, 0.55f, 0.03f), cloth);
            Sway(pivot, 0f, 0f, 0f).Axis = Vector3.up;
            SceneryMotion flutter = pivot.GetComponent<SceneryMotion>();
            flutter.Degrees = 14f;
            flutter.Speed = 3.2f;
            Part(banner.transform, layer, PrimitiveType.Sphere, new Vector3(0f, 2.05f, 0f), new Vector3(0.12f, 0.12f, 0.12f), new Color32(0xF4, 0xBE, 0x4F, 0xFF));
        }

        /// <summary>Scuffed chalk marks where the two armies meet, instead of a hard white line.</summary>
        private static void ChalkLine(Transform root, int layer)
        {
            var line = new GameObject("Chalk Line");
            Prepare(line, root, layer);
            Color32 chalk = new Color32(0xE8, 0xDF, 0xC8, 0xFF);
            for (float x = -2.6f; x <= 2.6f; x += 0.65f)
            {
                GameObject dash = Part(line.transform, layer, PrimitiveType.Quad, new Vector3(x, 0.012f, 0f), new Vector3(0.4f, 0.07f, 1f), chalk);
                dash.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        // ---- building blocks ----

        private static void Prepare(GameObject go, Transform parent, int layer)
        {
            go.transform.SetParent(parent, false);
            go.layer = layer;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private static GameObject Part(Transform parent, int layer, PrimitiveType type, Vector3 position, Vector3 scale, Color32 color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            Prepare(part, parent, layer);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            return part;
        }

        private static Material MaterialFor(Color32 color)
        {
            if (!Materials.TryGetValue(color, out Material material) || material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
                material.SetFloat("_Smoothness", 0.1f);
                Materials[color] = material;
            }
            return material;
        }

        // ---- the painted ground ----

        private static Texture2D GroundTexture()
        {
            if (_groundTexture != null)
            {
                return _groundTexture;
            }
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGB24, true)
            {
                name = "BattleGround",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
            };
            var pixels = new Color32[TextureSize * TextureSize];
            Color grassDark = new Color(0.27f, 0.43f, 0.23f);
            Color grassLight = new Color(0.37f, 0.55f, 0.28f);
            Color dirtDark = new Color(0.5f, 0.38f, 0.25f);
            Color dirtLight = new Color(0.64f, 0.52f, 0.36f);
            for (int py = 0; py < TextureSize; py++)
            {
                for (int px = 0; px < TextureSize; px++)
                {
                    // World position of this pixel: x to the right, z away from the camera.
                    float x = ((px + 0.5f) / TextureSize - 0.5f) * GroundWidth;
                    float z = ((py + 0.5f) / TextureSize - 0.5f) * GroundDepth;

                    float broad = Mathf.PerlinNoise(x * 0.35f + 11f, z * 0.35f + 7f);
                    float fine = Mathf.PerlinNoise(x * 2.4f + 3f, z * 2.4f + 19f);
                    float speck = Mathf.PerlinNoise(x * 11f + 5f, z * 11f + 2f);

                    // Mown stripes across the grass.
                    float stripe = Mathf.Sin(z * 1.6f) > 0f ? 0.05f : -0.03f;
                    Color grass = Color.Lerp(grassDark, grassLight, Mathf.Clamp01(broad * 0.7f + fine * 0.35f + stripe));
                    if (speck > 0.72f)
                    {
                        grass = Color.Lerp(grass, grassLight * 1.15f, 0.55f); // blades of light grass
                    }

                    // A trampled arena: a rounded box with a noisy, soft edge.
                    float edge = RoundBoxDistance(x, z, ArenaHalfWidth, ArenaHalfDepth, 1.6f) + (broad - 0.5f) * 0.9f + (fine - 0.5f) * 0.3f;
                    float dirtAmount = Smooth(0.5f, -0.35f, edge);
                    Color dirt = Color.Lerp(dirtDark, dirtLight, Mathf.Clamp01(broad * 0.6f + fine * 0.5f));
                    if (speck > 0.8f)
                    {
                        dirt = Color.Lerp(dirt, dirtDark * 0.75f, 0.6f); // pebbles and scuffs
                    }
                    else if (speck < 0.12f)
                    {
                        dirt = Color.Lerp(dirt, dirtLight * 1.08f, 0.5f);
                    }
                    // Worn grass between the two: a darker, muddier rim.
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(dirtAmount - 0.5f) * 2.4f);
                    Color color = Color.Lerp(grass, dirt, dirtAmount);
                    color = Color.Lerp(color, color * 0.82f, rim * 0.5f);

                    // Edges of the world fade to shade, so the picture has depth and no hard rim.
                    float outside = Mathf.Max(Mathf.Abs(x) / (GroundWidth * 0.5f), Mathf.Abs(z) / (GroundDepth * 0.5f));
                    color *= Mathf.Lerp(1f, 0.55f, Smooth(0.45f, 1f, outside));
                    pixels[py * TextureSize + px] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true);
            _groundTexture = texture;
            return texture;
        }

        /// <summary>0 below <paramref name="edge0"/>, 1 above <paramref name="edge1"/>, a smooth ramp between (the edges may be given in either order).</summary>
        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Signed distance to a box with rounded corners, centred on the origin.</summary>
        private static float RoundBoxDistance(float x, float z, float halfWidth, float halfDepth, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qz = Mathf.Abs(z) - (halfDepth - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qz, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qz), 0f) - radius;
        }
    }
}
