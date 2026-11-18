using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Views.Common;
using UnityEngine;

namespace PuzzleKingdoms.Views.Battle3D
{
    /// <summary>
    /// The 3D stand-in of one <see cref="Combatant"/> (PB-11): a coloured capsule with a weapon block, a tier ring
    /// and a world-space HP bar. It only displays state: position, hit flash, death. 1 cell = 1 metre.
    /// </summary>
    internal sealed class UnitActor
    {
        private const float FlashSeconds = 0.08f;
        private const float DeathSeconds = 0.6f;
        private const float TurnSpeed = 540f;
        private const float HpBarWidth = 0.9f;

        public readonly Combatant Combatant;
        public readonly GameObject Root;

        private readonly Renderer[] _bodyRenderers;
        private readonly Color[] _baseColors;
        private readonly Transform _hpRoot;
        private readonly Transform _hpFill;
        private readonly float _height;
        private Vector3 _previous;
        private Vector3 _current;
        private float _flashLeft;
        private float _deathAge = -1f;
        private float _heading;

        public UnitActor(Combatant combatant, Transform parent, int layer)
        {
            Combatant = combatant;
            Root = new GameObject($"Unit {combatant.Id}");
            Root.transform.SetParent(parent, false);

            Color color = Palette.For(combatant.Unit.Def);
            float tierScale = 1f + 0.1f * (combatant.Unit.Tier - 1);
            _height = 1.2f * tierScale;

            GameObject body = Primitive(PrimitiveType.Capsule, "Body", Root.transform, color);
            body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f) * tierScale;
            body.transform.localPosition = new Vector3(0f, 0.6f * tierScale, 0f);

            // Weapon block, by what the unit does: sword (melee), bow (ranged physical), orb (magic).
            UnitDef def = combatant.Unit.Def;
            GameObject weapon;
            if (def.DamageType == DamageType.Magic)
            {
                weapon = Primitive(PrimitiveType.Sphere, "Orb", Root.transform, Palette.Rally);
                weapon.transform.localScale = Vector3.one * 0.3f * tierScale;
                weapon.transform.localPosition = new Vector3(0.45f, 1.0f, 0.25f) * tierScale;
            }
            else if (def.AttackRange > 2f)
            {
                weapon = Primitive(PrimitiveType.Cube, "Bow", Root.transform, new Color(0.55f, 0.35f, 0.2f));
                weapon.transform.localScale = new Vector3(0.06f, 0.8f, 0.06f) * tierScale;
                weapon.transform.localPosition = new Vector3(0.45f, 0.7f, 0.25f) * tierScale;
            }
            else
            {
                weapon = Primitive(PrimitiveType.Cube, "Sword", Root.transform, new Color(0.85f, 0.88f, 0.92f));
                weapon.transform.localScale = new Vector3(0.08f, 0.08f, 0.8f) * tierScale;
                weapon.transform.localPosition = new Vector3(0.45f, 0.7f, 0.4f) * tierScale;
            }

            if (combatant.Unit.Tier > 1)
            {
                GameObject ring = Primitive(PrimitiveType.Cylinder, "Tier Ring", Root.transform, Palette.Gold);
                ring.transform.localScale = new Vector3(1.1f, 0.01f, 1.1f) * tierScale;
                ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            }

            _bodyRenderers = new[] { body.GetComponent<Renderer>(), weapon.GetComponent<Renderer>() };
            _baseColors = new[] { _bodyRenderers[0].material.color, _bodyRenderers[1].material.color };

            // HP bar: a dark quad with a coloured fill, turned towards the camera every frame.
            _hpRoot = new GameObject("HP").transform;
            _hpRoot.SetParent(Root.transform, false);
            _hpRoot.localPosition = new Vector3(0f, _height + 0.35f, 0f);
            GameObject back = Primitive(PrimitiveType.Quad, "Back", _hpRoot, new Color(0f, 0f, 0f, 1f));
            back.transform.localScale = new Vector3(HpBarWidth + 0.06f, 0.16f, 1f);
            GameObject fill = Primitive(PrimitiveType.Quad, "Fill", _hpRoot, combatant.Side == Side.Player ? Palette.Good : Palette.Bad);
            fill.transform.localScale = new Vector3(HpBarWidth, 0.1f, 1f);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            _hpFill = fill.transform;
            _hpRoot.gameObject.SetActive(false);

            SetLayer(Root, layer);
            _heading = combatant.Side == Side.Player ? 0f : 180f;
            _previous = _current = WorldPosition();
            Root.transform.localPosition = _current;
        }

        /// <summary>Head height in world space, for floating numbers.</summary>
        public Vector3 HeadPosition => Root.transform.TransformPoint(Vector3.up * (_height + 0.2f));

        /// <summary>Position inside the battle world (metres, 1 cell = 1 m).</summary>
        public Vector3 Position => Root.transform.localPosition;

        public bool IsDead => _deathAge >= 0f;

        public void Flash() => _flashLeft = FlashSeconds;

        /// <summary>Called after every simulator tick: keeps the last two positions to interpolate between.</summary>
        public void AfterTick()
        {
            _previous = _current;
            _current = WorldPosition();
            if (!Combatant.IsAlive && _deathAge < 0f)
            {
                _deathAge = 0f;
                _hpRoot.gameObject.SetActive(false);
            }
        }

        /// <summary>Snaps to the simulator state (after Skip, or the first frame).</summary>
        public void Snap()
        {
            _previous = _current = WorldPosition();
            Root.transform.localPosition = _current;
            if (!Combatant.IsAlive && _deathAge < 0f)
            {
                _deathAge = DeathSeconds;
                Root.SetActive(false);
            }
        }

        /// <param name="alpha">Fraction of the current tick that has passed (0..1).</param>
        /// <param name="lookAt">Where a unit with a target looks; null to face the enemy side.</param>
        public void Render(float alpha, float deltaTime, Vector3? lookAt, Quaternion cameraRotation)
        {
            if (_deathAge >= 0f)
            {
                RenderDeath(deltaTime);
                return;
            }

            Root.transform.localPosition = Vector3.Lerp(_previous, _current, alpha);

            Vector3 toTarget = lookAt.HasValue ? lookAt.Value - Root.transform.localPosition : Vector3.zero;
            float wanted = toTarget.sqrMagnitude > 0.01f
                ? Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg
                : (Combatant.Side == Side.Player ? 0f : 180f);
            _heading = Mathf.MoveTowardsAngle(_heading, wanted, TurnSpeed * deltaTime);
            Root.transform.rotation = Quaternion.Euler(0f, _heading, 0f);

            _flashLeft -= deltaTime;
            for (int i = 0; i < _bodyRenderers.Length; i++)
            {
                _bodyRenderers[i].material.color = _flashLeft > 0f ? Color.white : _baseColors[i];
            }

            bool hurt = Combatant.Hp < Combatant.MaxHp;
            _hpRoot.gameObject.SetActive(hurt);
            if (hurt)
            {
                float ratio = Mathf.Clamp01((float)Combatant.Hp / Combatant.MaxHp);
                _hpFill.localScale = new Vector3(HpBarWidth * ratio, 0.1f, 1f);
                _hpFill.localPosition = new Vector3(-(1f - ratio) * HpBarWidth * 0.5f, 0f, -0.01f);
                _hpRoot.rotation = cameraRotation;
            }
        }

        private void RenderDeath(float deltaTime)
        {
            _deathAge += deltaTime;
            float t = Mathf.Clamp01(_deathAge / DeathSeconds);
            Root.transform.localScale = new Vector3(1f + 0.3f * t, 1f - t, 1f + 0.3f * t);
            if (t >= 1f)
            {
                Root.SetActive(false);
            }
        }

        private Vector3 WorldPosition() => new Vector3(Combatant.Position.X, 0f, Combatant.Position.Y);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            go.GetComponent<Renderer>().material.color = color;
            return go;
        }

        private static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayer(child.gameObject, layer);
            }
        }
    }
}
