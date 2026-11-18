using System;
using PuzzleKingdoms.Data;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Geometry of the grey-box unit silhouettes. Pure math (no engine types) so it
    /// can be rendered and checked outside Unity. Space: x right, y up, both in
    /// [-1, 1]; the figure faces the viewer. <see cref="Shade"/> returns -1 for
    /// "transparent" or a brightness in [0, 1]; the view tints it with the unit
    /// color, so 1 is the plain unit color and lower values are darker details.
    /// </summary>
    public static class SilhouetteMath
    {
        public const float Transparent = -1f;
        private const float Body = 1f;
        private const float Light = 0.88f;
        private const float Dark = 0.6f;
        private const float Detail = 0.15f;

        public static float Shade(UnitLook look, float x, float y)
        {
            switch (look)
            {
                case UnitLook.Warrior: return Warrior(x, y);
                case UnitLook.Archer: return Archer(x, y);
                case UnitLook.Mage: return Mage(x, y);
                case UnitLook.GoblinGrunt: return GoblinGrunt(x, y);
                case UnitLook.GoblinArcher: return GoblinArcher(x, y);
                case UnitLook.GoblinBrute: return GoblinBrute(x, y);
                case UnitLook.GoblinChief: return GoblinChief(x, y);
                // PB-18 placeholders until the four Chapter 2 models exist.
                case UnitLook.ShieldGoblin: return GoblinBrute(x, y);
                case UnitLook.GoblinShaman: return GoblinArcher(x, y);
                case UnitLook.WolfRider: return GoblinGrunt(x, y);
                case UnitLook.GoblinBomber: return GoblinBrute(x, y);
                default: return Generic(x, y);
            }
        }

        // --- Looks ---------------------------------------------------------

        private static float Generic(float x, float y)
        {
            float s = Transparent;
            s = Paint(s, Capsule(x, y, -0.2f, -0.2f, -0.2f, -0.8f, 0.14f), Body);
            s = Paint(s, Capsule(x, y, 0.2f, -0.2f, 0.2f, -0.8f, 0.14f), Body);
            s = Paint(s, Poly(x, y, -0.4f, 0.4f, 0.4f, 0.4f, 0.3f, -0.3f, -0.3f, -0.3f), Body);
            s = Paint(s, Circle(x, y, 0f, 0.62f, 0.24f), Body);
            return s;
        }

        private static float Warrior(float x, float y)
        {
            float s = Transparent;
            s = Paint(s, Capsule(x, y, -0.13f, -0.2f, -0.15f, -0.85f, 0.11f), Body);
            s = Paint(s, Capsule(x, y, 0.13f, -0.2f, 0.15f, -0.85f, 0.11f), Body);
            s = Paint(s, Poly(x, y, -0.32f, 0.38f, 0.32f, 0.38f, 0.26f, -0.22f, -0.26f, -0.22f), Body);
            s = Paint(s, Capsule(x, y, 0.32f, 0.3f, 0.48f, -0.08f, 0.09f), Body);
            // Helmet with a visor slit and a crest.
            s = Paint(s, Poly(x, y, -0.05f, 0.76f, 0.05f, 0.76f, 0.08f, 0.98f, -0.08f, 0.98f), Dark);
            s = Paint(s, Circle(x, y, 0f, 0.6f, 0.23f), Dark);
            s = Paint(s, Rect(x, y, 0f, 0.56f, 0.13f, 0.03f), Detail);
            // Round shield on the left.
            s = Paint(s, Circle(x, y, -0.5f, 0.02f, 0.36f), Dark);
            s = Paint(s, Circle(x, y, -0.5f, 0.02f, 0.13f), Light);
            // Sword in the right hand.
            s = Paint(s, Capsule(x, y, 0.56f, -0.2f, 0.56f, 0.86f, 0.07f), Light);
            s = Paint(s, Capsule(x, y, 0.4f, -0.02f, 0.72f, -0.02f, 0.05f), Dark);
            return s;
        }

        private static float Archer(float x, float y)
        {
            float s = Transparent;
            s = Paint(s, Capsule(x, y, -0.3f, 0.5f, -0.16f, -0.05f, 0.07f), Dark); // quiver
            s = Paint(s, Capsule(x, y, -0.11f, -0.2f, -0.13f, -0.85f, 0.09f), Body);
            s = Paint(s, Capsule(x, y, 0.11f, -0.2f, 0.13f, -0.85f, 0.09f), Body);
            s = Paint(s, Poly(x, y, -0.26f, 0.38f, 0.26f, 0.38f, 0.2f, -0.22f, -0.2f, -0.22f), Body);
            s = Paint(s, Capsule(x, y, 0.26f, 0.3f, 0.42f, 0.1f, 0.07f), Body);
            // Hood with the face showing.
            s = Paint(s, Poly(x, y, -0.27f, 0.45f, 0.27f, 0.45f, 0f, 0.98f), Dark);
            s = Paint(s, Circle(x, y, 0f, 0.55f, 0.13f), Body);
            // Bow, string and arrow.
            s = Paint(s, Arc(x, y, 0.1f, 0.1f, 0.7f, 0.06f, -52f, 52f), Dark);
            s = Paint(s, Capsule(x, y, 0.53f, 0.65f, 0.53f, -0.45f, 0.015f), Light);
            s = Paint(s, Capsule(x, y, 0.1f, 0.1f, 0.8f, 0.1f, 0.025f), Light);
            return s;
        }

        private static float Mage(float x, float y)
        {
            float s = Transparent;
            // Staff with a glowing orb.
            s = Paint(s, Capsule(x, y, 0.62f, -0.9f, 0.62f, 0.6f, 0.05f), Dark);
            s = Paint(s, Circle(x, y, 0.62f, 0.74f, 0.16f), Light);
            s = Paint(s, Circle(x, y, 0.62f, 0.74f, 0.07f), Detail);
            // Robe, belt and sleeve.
            s = Paint(s, Poly(x, y, -0.26f, 0.36f, 0.26f, 0.36f, 0.5f, -0.9f, -0.5f, -0.9f), Body);
            s = Paint(s, Capsule(x, y, -0.34f, 0.0f, 0.34f, 0.0f, 0.04f), Dark);
            s = Paint(s, Capsule(x, y, 0.28f, 0.28f, 0.58f, 0.05f, 0.09f), Light);
            // Head with a pointed hat.
            s = Paint(s, Circle(x, y, 0f, 0.5f, 0.17f), Body);
            s = Paint(s, Poly(x, y, -0.22f, 0.62f, 0.22f, 0.62f, 0.14f, 1f), Dark);
            s = Paint(s, Capsule(x, y, -0.34f, 0.6f, 0.34f, 0.6f, 0.05f), Dark);
            return s;
        }

        private static float GoblinGrunt(float x, float y)
        {
            float s = Transparent;
            GoblinBody(ref s, x, y);
            // Wooden club.
            s = Paint(s, Capsule(x, y, 0.5f, -0.4f, 0.74f, 0.5f, 0.1f), Dark);
            s = Paint(s, Circle(x, y, 0.76f, 0.58f, 0.17f), Dark);
            return s;
        }

        private static float GoblinArcher(float x, float y)
        {
            float s = Transparent;
            GoblinBody(ref s, x, y);
            s = Paint(s, Arc(x, y, 0.1f, 0.0f, 0.6f, 0.055f, -52f, 52f), Dark);
            s = Paint(s, Capsule(x, y, 0.47f, 0.47f, 0.47f, -0.47f, 0.015f), Light);
            s = Paint(s, Capsule(x, y, 0.1f, 0.0f, 0.7f, 0.0f, 0.025f), Light);
            return s;
        }

        private static float GoblinBrute(float x, float y)
        {
            float s = Transparent;
            s = Paint(s, Capsule(x, y, -0.2f, -0.3f, -0.22f, -0.85f, 0.15f), Body);
            s = Paint(s, Capsule(x, y, 0.2f, -0.3f, 0.22f, -0.85f, 0.15f), Body);
            s = Paint(s, Poly(x, y, -0.5f, 0.42f, 0.5f, 0.42f, 0.4f, -0.3f, -0.4f, -0.3f), Body);
            s = Paint(s, Capsule(x, y, -0.5f, 0.35f, -0.62f, -0.1f, 0.12f), Body);
            s = Paint(s, Capsule(x, y, 0.5f, 0.35f, 0.62f, -0.1f, 0.12f), Body);
            // Plate armor and shoulder pads.
            s = Paint(s, Poly(x, y, -0.34f, 0.36f, 0.34f, 0.36f, 0.26f, -0.1f, -0.26f, -0.1f), Dark);
            s = Paint(s, Circle(x, y, -0.52f, 0.4f, 0.16f), Dark);
            s = Paint(s, Circle(x, y, 0.52f, 0.4f, 0.16f), Dark);
            // Small head with tusks and a heavy brow.
            s = Paint(s, Circle(x, y, 0f, 0.58f, 0.2f), Body);
            s = Paint(s, Rect(x, y, 0f, 0.62f, 0.18f, 0.03f), Detail);
            s = Paint(s, Poly(x, y, -0.1f, 0.46f, -0.05f, 0.46f, -0.08f, 0.34f), Light);
            s = Paint(s, Poly(x, y, 0.1f, 0.46f, 0.05f, 0.46f, 0.08f, 0.34f), Light);
            // Great axe.
            s = Paint(s, Capsule(x, y, 0.74f, -0.55f, 0.74f, 0.8f, 0.05f), Dark);
            s = Paint(s, Poly(x, y, 0.74f, 0.8f, 0.98f, 0.68f, 0.98f, 0.3f, 0.74f, 0.4f), Light);
            return s;
        }

        private static float GoblinChief(float x, float y)
        {
            float s = Transparent;
            // Cape behind the body.
            s = Paint(s, Poly(x, y, -0.5f, 0.3f, 0.5f, 0.3f, 0.62f, -0.85f, -0.62f, -0.85f), Dark);
            GoblinBody(ref s, x, y, 1.1f);
            // Crown.
            s = Paint(s, Poly(x, y, -0.2f, 0.74f, -0.2f, 0.98f, -0.1f, 0.84f, 0f, 1f, 0.1f, 0.84f, 0.2f, 0.98f, 0.2f, 0.74f), Light);
            // Heavy sword.
            s = Paint(s, Capsule(x, y, 0.64f, -0.4f, 0.64f, 0.84f, 0.09f), Light);
            s = Paint(s, Capsule(x, y, 0.42f, -0.2f, 0.86f, -0.2f, 0.06f), Dark);
            return s;
        }

        /// <summary>Hunched goblin: big head with long ears, small body, short legs.</summary>
        private static void GoblinBody(ref float s, float x, float y, float widen = 1f)
        {
            s = Paint(s, Capsule(x, y, -0.12f * widen, -0.4f, -0.14f * widen, -0.85f, 0.1f), Body);
            s = Paint(s, Capsule(x, y, 0.12f * widen, -0.4f, 0.14f * widen, -0.85f, 0.1f), Body);
            s = Paint(s, Poly(x, y, -0.24f * widen, 0.2f, 0.24f * widen, 0.2f, 0.2f * widen, -0.42f, -0.2f * widen, -0.42f), Body);
            s = Paint(s, Capsule(x, y, 0.22f * widen, 0.12f, 0.46f, -0.3f, 0.07f), Body);
            // Ears.
            s = Paint(s, Poly(x, y, -0.22f, 0.52f, -0.74f, 0.7f, -0.3f, 0.3f), Body);
            s = Paint(s, Poly(x, y, 0.22f, 0.52f, 0.74f, 0.7f, 0.3f, 0.3f), Body);
            // Head, eyes and a toothy grin.
            s = Paint(s, Circle(x, y, 0f, 0.46f, 0.28f), Body);
            s = Paint(s, Circle(x, y, -0.11f, 0.5f, 0.05f), Detail);
            s = Paint(s, Circle(x, y, 0.11f, 0.5f, 0.05f), Detail);
            s = Paint(s, Rect(x, y, 0f, 0.34f, 0.1f, 0.025f), Detail);
        }

        // --- Primitives ----------------------------------------------------

        private static float Paint(float current, bool inside, float shade) => inside ? shade : current;

        private static bool Circle(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx;
            float dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        }

        private static bool Rect(float x, float y, float cx, float cy, float halfWidth, float halfHeight) =>
            Math.Abs(x - cx) <= halfWidth && Math.Abs(y - cy) <= halfHeight;

        /// <summary>A thick line: every point within <paramref name="r"/> of the segment.</summary>
        private static bool Capsule(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float vx = x1 - x0;
            float vy = y1 - y0;
            float length2 = vx * vx + vy * vy;
            float t = length2 <= 0f ? 0f : ((x - x0) * vx + (y - y0) * vy) / length2;
            t = Math.Max(0f, Math.Min(1f, t));
            float dx = x - (x0 + vx * t);
            float dy = y - (y0 + vy * t);
            return dx * dx + dy * dy <= r * r;
        }

        /// <summary>Even-odd point-in-polygon test; points are x0, y0, x1, y1, ...</summary>
        private static bool Poly(float x, float y, params float[] pts)
        {
            bool inside = false;
            int n = pts.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = pts[2 * i], yi = pts[2 * i + 1];
                float xj = pts[2 * j], yj = pts[2 * j + 1];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>A curved band around (cx, cy), between two angles in degrees (0 = right, counter-clockwise).</summary>
        private static bool Arc(float x, float y, float cx, float cy, float radius, float halfThickness, float fromDegrees, float toDegrees)
        {
            float dx = x - cx;
            float dy = y - cy;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            if (Math.Abs(distance - radius) > halfThickness)
            {
                return false;
            }
            float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            return angle >= fromDegrees && angle <= toDegrees;
        }
    }
}
