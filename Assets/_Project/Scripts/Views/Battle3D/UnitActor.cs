using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Views.Common;
using UnityEngine;

namespace PuzzleKingdoms.Views.Battle3D
{
    /// <summary>
    /// The 3D stand-in of one <see cref="Combatant"/> (PB-11): the unit's model from the <see cref="BattleModelSet"/>
    /// (a coloured capsule with a weapon block when there is none), a tier ring and a world-space HP bar. It only
    /// displays state: position, hit flash, death. 1 cell = 1 metre.
    /// </summary>
    internal sealed class UnitActor
    {
        private const float FlashSeconds = 0.08f;
        private const float DeathSeconds = 0.6f;
        private const float TurnSpeed = 540f;
        private const float HpBarWidth = 0.9f;
        /// <summary>Colour multiplier of a hit flash on a textured model (values above 1 brighten the texture).</summary>
        private static readonly Color ModelFlash = new Color(2.6f, 2.6f, 2.6f, 1f);

        public readonly Combatant Combatant;
        public readonly GameObject Root;

        private readonly Renderer[] _bodyRenderers;
        private readonly Color[] _baseColors;
        private readonly Color[] _flashColors;
        private readonly Transform _hpRoot;
        private readonly Transform _hpFill;
        private readonly float _height;
        private Vector3 _previous;
        private Vector3 _current;
        private float _flashLeft;
        private float _deathAge = -1f;
        private float _heading;
        private readonly int _ticksPerSecond;
        private Animator _animator;
        private Transform _modelHolder;
        private float _attackClipLength = 1f;
        private float _hitCooldown;
        private float _smoothSpeed;

        /// <summary>A killed unit lies for this long (animated deaths), then sinks into the ground.</summary>
        private const float LieSeconds = 1.5f;
        private const float SinkSeconds = 0.5f;

        public UnitActor(Combatant combatant, Transform parent, int layer, int ticksPerSecond)
        {
            _ticksPerSecond = Mathf.Max(1, ticksPerSecond);
            Combatant = combatant;
            Root = new GameObject($"Unit {combatant.Id}");
            Root.transform.SetParent(parent, false);

            UnitDef def = combatant.Unit.Def;
            float tierScale = 1f + 0.1f * (combatant.Unit.Tier - 1);
            if (UnitModels.TryGet(Palette.LookFor(def), out BattleModelSet.Entry entry))
            {
                _bodyRenderers = BuildModel(entry, tierScale);
                _baseColors = new Color[_bodyRenderers.Length];
                _flashColors = new Color[_bodyRenderers.Length];
                for (int i = 0; i < _bodyRenderers.Length; i++)
                {
                    _baseColors[i] = _bodyRenderers[i].material.color;
                    _flashColors[i] = ModelFlash;
                }
                _height = BattleModelSet.UnitHeightMetres * tierScale;
            }
            else
            {
                _bodyRenderers = BuildCapsule(def, Palette.For(def), tierScale);
                _baseColors = new[] { _bodyRenderers[0].material.color, _bodyRenderers[1].material.color };
                _flashColors = new[] { Color.white, Color.white };
                _height = 1.2f * tierScale;
            }

            if (combatant.Unit.Tier > 1)
            {
                GameObject ring = Primitive(PrimitiveType.Cylinder, "Tier Ring", Root.transform, Palette.Gold);
                ring.transform.localScale = new Vector3(1.1f, 0.01f, 1.1f) * tierScale;
                ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                if (combatant.Unit.Tier >= 3)
                {
                    // T3 glows (GDD §6): a bright ring and a soft gold emission on the body.
                    Glow(ring.GetComponent<Renderer>(), Palette.Gold * 1.6f);
                    foreach (Renderer part in _bodyRenderers)
                    {
                        Glow(part, Palette.Gold * 0.3f);
                    }
                }
            }

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

        /// <summary>Rally aura: a gold glow on the body while Rally lasts (PB-12).</summary>
        public void SetAura(bool on)
        {
            foreach (Renderer part in _bodyRenderers)
            {
                Material material = part.material;
                bool tier3 = Combatant.Unit.Tier >= 3;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", on ? Palette.Gold * 0.22f : (tier3 ? Palette.Gold * 0.3f : Color.black));
            }
        }

        /// <summary>The unit attacks: starts the Attack clip, squeezed into the attack interval (and faster in Rally).</summary>
        public void PlayAttack(float rallyFactor)
        {
            if (_animator == null)
            {
                return;
            }
            float interval = Mathf.Max(0.2f, Combatant.AttackInterval) / Mathf.Max(0.1f, rallyFactor);
            _animator.SetFloat("attackSpeed", Mathf.Clamp(_attackClipLength / (interval * 0.9f), 0.6f, 4f));
            _animator.SetTrigger("attack");
        }

        /// <summary>The unit is hit: a short stagger, at most once in a while so a stream of hits does not freeze it.</summary>
        public void PlayHit()
        {
            if (_animator != null && _hitCooldown <= 0f && Combatant.IsAlive)
            {
                _animator.SetTrigger("hit");
                _hitCooldown = 1.6f;
            }
        }

        /// <summary>Called after every simulator tick: keeps the last two positions to interpolate between.</summary>
        public void AfterTick()
        {
            _previous = _current;
            _current = WorldPosition();
            if (!Combatant.IsAlive && _deathAge < 0f)
            {
                _deathAge = 0f;
                _hpRoot.gameObject.SetActive(false);
                _animator?.SetTrigger("die");
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
            _hitCooldown -= deltaTime;
            if (_animator != null)
            {
                float speed = Vector3.Distance(_previous, _current) * _ticksPerSecond;
                _smoothSpeed = Mathf.MoveTowards(_smoothSpeed, speed, 8f * deltaTime);
                _animator.SetFloat("speed", _smoothSpeed);
                if (!Application.isPlaying)
                {
                    _animator.Update(deltaTime); // edit mode (the battle screenshots): nothing else ticks it
                }
            }

            Vector3 toTarget = lookAt.HasValue ? lookAt.Value - Root.transform.localPosition : Vector3.zero;
            float wanted = toTarget.sqrMagnitude > 0.01f
                ? Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg
                : (Combatant.Side == Side.Player ? 0f : 180f);
            _heading = Mathf.MoveTowardsAngle(_heading, wanted, TurnSpeed * deltaTime);
            Root.transform.rotation = Quaternion.Euler(0f, _heading, 0f);

            _flashLeft -= deltaTime;
            for (int i = 0; i < _bodyRenderers.Length; i++)
            {
                _bodyRenderers[i].material.color = _flashLeft > 0f ? _flashColors[i] : _baseColors[i];
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
            if (_animator != null)
            {
                // The Dead clip plays, the unit lies for a moment, then sinks into the ground and goes (PB-12).
                if (!Application.isPlaying)
                {
                    _animator.Update(deltaTime);
                }
                float sink = Mathf.Clamp01((_deathAge - LieSeconds) / SinkSeconds);
                _modelHolder.localPosition = Vector3.down * 0.6f * sink;
                if (sink >= 1f)
                {
                    Root.SetActive(false);
                }
                return;
            }

            float t = Mathf.Clamp01(_deathAge / DeathSeconds);
            Root.transform.localScale = new Vector3(1f + 0.3f * t, 1f - t, 1f + 0.3f * t);
            if (t >= 1f)
            {
                Root.SetActive(false);
            }
        }

        private Vector3 WorldPosition() => new Vector3(Combatant.Position.X, 0f, Combatant.Position.Y);

        /// <summary>
        /// The model of the set: stood up and turned to face +Z, scaled so its visible body is
        /// <see cref="BattleModelSet.UnitHeightMetres"/> tall (a Meshy export is in centimetres and lies on
        /// its back), feet on the ground, then the tier scale on top.
        /// </summary>
        private Renderer[] BuildModel(BattleModelSet.Entry entry, float tierScale)
        {
            Transform holder = new GameObject("Model").transform;
            holder.SetParent(Root.transform, false);
            GameObject model = Object.Instantiate(entry.model, holder, false);
            model.transform.localRotation = Quaternion.Euler(entry.uprightEuler);

            Renderer[] parts = model.GetComponentsInChildren<Renderer>();
            if (entry.material != null)
            {
                foreach (Renderer part in parts)
                {
                    part.sharedMaterial = entry.material;
                }
            }

            float height = Mathf.Max(0.1f, BoundsOf(parts).size.y);
            model.transform.localScale *= BattleModelSet.UnitHeightMetres / height; // multiply: imports carry a root scale
            Bounds fitted = BoundsOf(parts);
            model.transform.position += Vector3.up * (Root.transform.position.y - fitted.min.y);
            holder.localScale = Vector3.one * tierScale;
            _modelHolder = holder;

            // A rigged (Humanoid) model brings an Animator; the shared controller drives it (PB-12, D-043).
            Animator animator = model.GetComponent<Animator>() != null ? model.GetComponent<Animator>() : model.GetComponentInChildren<Animator>();
            RuntimeAnimatorController controller = UnitModels.Controller;
            if (animator != null && controller != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animator = animator;
                foreach (AnimationClip clip in controller.animationClips)
                {
                    if (clip != null && clip.name == "Attack")
                    {
                        _attackClipLength = clip.length;
                    }
                }
            }
            return parts;
        }

        private Renderer[] BuildCapsule(UnitDef def, Color color, float tierScale)
        {
            GameObject body = Primitive(PrimitiveType.Capsule, "Body", Root.transform, color);
            body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f) * tierScale;
            body.transform.localPosition = new Vector3(0f, 0.6f * tierScale, 0f);

            // Weapon block, by what the unit does: sword (melee), bow (ranged physical), orb (magic).
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
            return new[] { body.GetComponent<Renderer>(), weapon.GetComponent<Renderer>() };
        }

        /// <summary>Turns on the emission of this renderer's own material (URP Lit) with the given colour.</summary>
        private static void Glow(Renderer renderer, Color emission)
        {
            Material material = renderer.material;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

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
