using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Views.Common;
using UnityEngine;

namespace PuzzleKingdoms.Views.Battle3D
{
    /// <summary>
    /// The 3D battlefield of one battle (PB-11): ground, light, an orthographic camera and one <see cref="UnitActor"/>
    /// per combatant. It never runs the rules: <c>BattleScreen</c> steps the simulator and calls
    /// <see cref="OnStep"/> and <see cref="Render"/>. The camera draws into a <see cref="RenderTexture"/> that the
    /// battle screen shows inside its field, so the uGUI buttons stay on top and no scene is edited. The world
    /// sits far from the origin on its own layer so no other camera sees it.
    /// </summary>
    public sealed class BattleWorld3D
    {
        /// <summary>Layer used by every object of the battle world (only the battle camera draws it).</summary>
        public const int Layer = 31;
        public const float PitchDegrees = 45f;
        public const float YawDegrees = 0f;
        /// <summary>Width / height of the picture; the battle screen's field has the same shape.</summary>
        public const float Aspect = 0.72f;
        private const float MarginCells = 1.5f;
        private static readonly Vector3 Origin = new Vector3(3000f, 0f, 0f);
        private static readonly Color GroundColor = new Color32(0x4A, 0x6B, 0x4F, 0xFF);

        private sealed class Projectile
        {
            public GameObject Object;
            public Vector3 From;
            public Vector3 To;
            public float Age;
            public float Duration;
        }

        private readonly BattleSimulator _sim;
        private readonly GameObject _root;
        private readonly Camera _camera;
        private readonly List<UnitActor> _actors = new List<UnitActor>();
        private readonly List<Projectile> _projectiles = new List<Projectile>();

        private BattleWorld3D(BattleSimulator sim, int textureWidth, int textureHeight)
        {
            _sim = sim;
            _root = new GameObject("Battle3D");
            _root.transform.position = Origin;

            BuildGround();
            BuildLight();
            _camera = BuildCamera(sim.Rules, textureWidth, textureHeight);

            foreach (Combatant combatant in sim.Combatants)
            {
                _actors.Add(new UnitActor(combatant, _root.transform, Layer, sim.Rules.TicksPerSecond));
            }
        }

        public static BattleWorld3D Create(BattleSimulator sim, int textureWidth = 720, int textureHeight = 1000)
        {
            return new BattleWorld3D(sim, textureWidth, textureHeight);
        }

        public RenderTexture Texture => _camera.targetTexture;

        public Camera Camera => _camera;

        /// <summary>Plays the events of one simulator tick: hit flashes and projectiles, then moves to the new state.</summary>
        public void OnStep(List<BattleEvent> events)
        {
            foreach (UnitActor actor in _actors)
            {
                actor.AfterTick();
            }
            if (events == null)
            {
                return;
            }
            foreach (BattleEvent e in events)
            {
                if (e.Type == BattleEventType.Died && e.SourceId >= 0 && e.SourceId < _actors.Count)
                {
                    PlaySound(Sfx.Bank?.UnitDied);
                    UnitActor dead = _actors[e.SourceId];
                    Vector3 at = dead.Position + Vector3.up * 0.4f;
                    if (dead.Combatant.Side == Side.Enemy)
                    {
                        // Goblin poof: a ring of dusty green-grey puffs, and a coin pops out (visual only).
                        Burst(at, new Color(0.62f, 0.66f, 0.5f), 10, 0.5f, 0.6f, 1.6f);
                        Burst(at + Vector3.up * 0.4f, Palette.Gold, 1, 0.32f, 0.9f, 0.5f, coin: true);
                    }
                    else
                    {
                        Burst(at, new Color(0.7f, 0.7f, 0.72f), 6, 0.4f, 0.5f, 1.1f);
                    }
                }
                if (e.Type == BattleEventType.RallyStarted)
                {
                    StartRally();
                    PlaySound(Sfx.Bank?.RallyStart);
                }
                else if (e.Type == BattleEventType.RallyEnded)
                {
                    EndRally();
                }
                if (e.Type == BattleEventType.RallyStrike && e.TargetId >= 0 && e.TargetId < _actors.Count)
                {
                    // A commander's Rally (PB-23): arrows fall as pale streaks, the fireball as a burst of fire.
                    UnitActor struck = _actors[e.TargetId];
                    struck.Flash();
                    struck.PlayHit();
                    Vector3 strikeAt = struck.Position + Vector3.up * 0.7f;
                    if (_sim.Variant.Kind == RallyKind.Fireball)
                    {
                        Burst(strikeAt, new Color(1f, 0.55f, 0.15f), 14, 0.34f, 0.7f, 1.8f);
                        Burst(strikeAt, new Color(1f, 0.85f, 0.3f), 8, 0.2f, 0.5f, 1.2f);
                    }
                    else
                    {
                        Burst(strikeAt + Vector3.up * 0.8f, new Color(0.92f, 0.88f, 0.7f), 5, 0.12f, 0.4f, 0.5f);
                        Burst(strikeAt, new Color(0.78f, 0.72f, 0.6f), 3, 0.2f, 0.4f, 0.8f);
                    }
                    continue;
                }
                if (e.Type != BattleEventType.Attack || e.TargetId < 0 || e.TargetId >= _actors.Count)
                {
                    continue;
                }
                _actors[e.TargetId].Flash();
                _actors[e.TargetId].PlayHit();
                {
                    // Hit feedback: dust on a melee hit, purple sparks on a magic hit.
                    UnitActor hitSource = e.SourceId >= 0 && e.SourceId < _actors.Count ? _actors[e.SourceId] : null;
                    Vector3 hitAt = _actors[e.TargetId].Position + Vector3.up * 0.6f;
                    if (hitSource != null && hitSource.Combatant.DamageType == DamageType.Magic)
                    {
                        Burst(hitAt, new Color(0.66f, 0.48f, 0.91f), 4, 0.18f, 0.35f, 1.2f);
                    }
                    else
                    {
                        Burst(hitAt, new Color(0.78f, 0.72f, 0.6f), 3, 0.22f, 0.4f, 0.9f);
                    }
                }
                UnitActor source = e.SourceId >= 0 && e.SourceId < _actors.Count ? _actors[e.SourceId] : null;
                source?.PlayAttack(_sim.Rally.IsActive ? 1f + _sim.Variant.AttackSpeedBonus : 1f);
                if (source != null)
                {
                    SoundBank bank = Sfx.Bank;
                    AudioClip clip = bank == null ? null
                        : source.Combatant.DamageType == DamageType.Magic ? bank.MagicCast
                        : source.Combatant.AttackRange > 2f ? bank.BowShot : bank.SwordHit;
                    PlaySound(clip);
                }
                if (source != null && source.Combatant.AttackRange > 2f && _projectiles.Count < 40)
                {
                    SpawnProjectile(source, _actors[e.TargetId]);
                }
            }
        }

        private sealed class Puff
        {
            public GameObject Object;
            public Vector3 Velocity;
            public float Age;
            public float Life;
            public float Size;
            public bool Coin;
        }

        private readonly List<Puff> _puffs = new List<Puff>();
        private const int MaxPuffs = 70;

        /// <summary>Little transient bits (dust, sparks, the goblin poof, a coin) that fly, shrink and vanish (PB-12).</summary>
        private void Burst(Vector3 at, Color color, int count, float size, float life, float spread, bool coin = false)
        {
            for (int i = 0; i < count && _puffs.Count < MaxPuffs; i++)
            {
                GameObject go = GameObject.CreatePrimitive(coin ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
                go.name = coin ? "Coin" : "Puff";
                go.transform.SetParent(_root.transform, false);
                go.GetComponent<Collider>().enabled = false;
                go.GetComponent<Renderer>().material.color = color;
                go.layer = Layer;
                go.transform.localPosition = at;
                Vector2 dir = UnityEngine.Random.insideUnitCircle * spread;
                _puffs.Add(new Puff
                {
                    Object = go,
                    Velocity = coin ? new Vector3(dir.x * 0.3f, 2.6f, dir.y * 0.3f) : new Vector3(dir.x, 0.6f + UnityEngine.Random.value * 0.8f, dir.y),
                    Life = life,
                    Size = size,
                    Coin = coin,
                });
            }
        }

        private void RenderPuffs(float deltaTime)
        {
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                Puff p = _puffs[i];
                p.Age += deltaTime;
                float t = Mathf.Clamp01(p.Age / p.Life);
                if (p.Coin)
                {
                    p.Velocity.y -= 7f * deltaTime;
                }
                p.Object.transform.localPosition += p.Velocity * deltaTime;
                float scale = p.Coin ? p.Size : p.Size * (1f - t * t);
                p.Object.transform.localScale = p.Coin ? new Vector3(scale, scale * 0.12f, scale) : Vector3.one * scale;
                if (p.Coin)
                {
                    p.Object.transform.Rotate(0f, 0f, 540f * deltaTime);
                }
                if (t >= 1f)
                {
                    DestroyObject(p.Object);
                    _puffs.RemoveAt(i);
                }
            }
        }

        private int _soundsThisTick;
        private float _lastSoundTime;

        /// <summary>Plays a battle sound, at most 3 per frame-burst and with a little pitch variety, so 32 units do not become noise.</summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip == null || !Application.isPlaying)
            {
                return;
            }
            if (Time.unscaledTime != _lastSoundTime)
            {
                _lastSoundTime = Time.unscaledTime;
                _soundsThisTick = 0;
            }
            if (_soundsThisTick++ < 3)
            {
                Sfx.Play(clip, UnityEngine.Random.Range(0.92f, 1.08f));
            }
        }

        private const float RingSeconds = 0.9f;
        private const float ShakeSeconds = 0.2f;
        private GameObject _rallyRing;
        private float _ringAge = -1f;
        private float _shakeLeft;
        private Vector3 _cameraRest;

        /// <summary>Rally begins (PB-12): a gold ring spreads from the player army, allies glow, the camera shakes briefly.</summary>
        private void StartRally()
        {
            Vector3 centre = Vector3.zero;
            int count = 0;
            foreach (UnitActor actor in _actors)
            {
                if (actor.Combatant.Side == Side.Player && actor.Combatant.IsAlive)
                {
                    centre += actor.Position;
                    count++;
                    actor.SetAura(true);
                }
            }
            centre = count > 0 ? centre / count : Vector3.zero;
            if (_rallyRing == null)
            {
                // A thin ring (a looped line on the ground), so it does not hide the units it passes.
                _rallyRing = new GameObject("Rally Ring");
                _rallyRing.transform.SetParent(_root.transform, false);
                _rallyRing.layer = Layer;
                var line = _rallyRing.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                const int points = 64;
                line.positionCount = points;
                for (int i = 0; i < points; i++)
                {
                    float angle = i * Mathf.PI * 2f / points;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.5f);
                }
                line.widthMultiplier = 0.22f;
                line.alignment = LineAlignment.TransformZ;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = line.endColor = Palette.Gold;
            }
            _rallyRing.transform.localPosition = new Vector3(centre.x, 0.03f, centre.z);
            _rallyRing.SetActive(true);
            _ringAge = 0f;
            _shakeLeft = ShakeSeconds;
            _cameraRest = _camera.transform.localPosition;
        }

        private void EndRally()
        {
            foreach (UnitActor actor in _actors)
            {
                actor.SetAura(false);
            }
        }

        private void RenderRally(float deltaTime)
        {
            if (_ringAge >= 0f && _rallyRing != null)
            {
                _ringAge += deltaTime;
                float t = Mathf.Clamp01(_ringAge / RingSeconds);
                float diameter = Mathf.Lerp(1f, 14f, t);
                _rallyRing.transform.localScale = new Vector3(diameter, 1f, diameter);
                _rallyRing.GetComponent<LineRenderer>().widthMultiplier = 0.22f;
                Color fade = Palette.Gold;
                fade.a = 1f - t * 0.8f;
                _rallyRing.GetComponent<LineRenderer>().startColor = fade;
                _rallyRing.GetComponent<LineRenderer>().endColor = fade;
                if (t >= 1f)
                {
                    _rallyRing.SetActive(false);
                    _ringAge = -1f;
                }
            }
            if (_shakeLeft > 0f)
            {
                _shakeLeft -= deltaTime;
                float strength = 0.12f * Mathf.Clamp01(_shakeLeft / ShakeSeconds);
                Vector3 jitter = new Vector3(Mathf.Sin(_shakeLeft * 90f), Mathf.Cos(_shakeLeft * 110f), 0f) * strength;
                _camera.transform.localPosition = _cameraRest + _camera.transform.localRotation * jitter;
                if (_shakeLeft <= 0f)
                {
                    _camera.transform.localPosition = _cameraRest;
                }
            }
        }

        /// <summary>Skip: jump to the final state without playing anything.</summary>
        public void SnapToSimulator()
        {
            foreach (UnitActor actor in _actors)
            {
                actor.Snap();
            }
            ClearProjectiles();
        }

        /// <param name="alpha">Fraction of the current tick that has passed (0..1).</param>
        public void Render(float alpha, float deltaTime)
        {
            RenderRally(deltaTime);
            RenderPuffs(deltaTime);
            Quaternion cameraRotation = _camera.transform.rotation;
            foreach (UnitActor actor in _actors)
            {
                Vector3? lookAt = null;
                int targetId = actor.Combatant.TargetId;
                if (actor.Combatant.IsAlive && targetId >= 0 && targetId < _actors.Count && _actors[targetId].Combatant.IsAlive)
                {
                    lookAt = _actors[targetId].Position;
                }
                actor.Render(alpha, deltaTime, lookAt, cameraRotation);
            }

            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = _projectiles[i];
                p.Age += deltaTime;
                float t = Mathf.Clamp01(p.Age / p.Duration);
                p.Object.transform.localPosition = Vector3.Lerp(p.From, p.To, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.4f;
                if (t >= 1f)
                {
                    DestroyObject(p.Object);
                    _projectiles.RemoveAt(i);
                }
            }
        }

        /// <summary>Position on the picture (0..1 in both axes) above a unit's head, for floating numbers.</summary>
        public Vector2 ViewportPointAbove(int combatantId)
        {
            if (combatantId < 0 || combatantId >= _actors.Count)
            {
                return new Vector2(0.5f, 0.5f);
            }
            Vector3 point = _camera.WorldToViewportPoint(_actors[combatantId].HeadPosition);
            return new Vector2(point.x, point.y);
        }

        public void Dispose()
        {
            ClearProjectiles();
            if (_camera != null && _camera.targetTexture != null)
            {
                _camera.targetTexture.Release();
                DestroyObject(_camera.targetTexture);
            }
            DestroyObject(_root);
        }

        private void SpawnProjectile(UnitActor from, UnitActor to)
        {
            bool magic = from.Combatant.DamageType == DamageType.Magic;
            GameObject go = GameObject.CreatePrimitive(magic ? PrimitiveType.Sphere : PrimitiveType.Cube);
            go.name = magic ? "Orb" : "Arrow";
            go.transform.SetParent(_root.transform, false);
            go.GetComponent<Collider>().enabled = false;
            go.GetComponent<Renderer>().material.color = magic ? Palette.Rally : new Color(0.9f, 0.85f, 0.6f);
            go.transform.localScale = magic ? Vector3.one * 0.25f : new Vector3(0.05f, 0.05f, 0.5f);
            go.layer = Layer;
            Vector3 start = from.Position + Vector3.up * 0.8f;
            Vector3 end = to.Position + Vector3.up * 0.8f;
            if (!magic)
            {
                go.transform.rotation = Quaternion.LookRotation(end - start);
            }
            float distance = Vector3.Distance(start, end);
            _projectiles.Add(new Projectile { Object = go, From = start, To = end, Duration = Mathf.Max(0.12f, distance / 16f) });
        }

        private void ClearProjectiles()
        {
            foreach (Projectile p in _projectiles)
            {
                DestroyObject(p.Object);
            }
            _projectiles.Clear();
            foreach (Puff puff in _puffs)
            {
                DestroyObject(puff.Object);
            }
            _puffs.Clear();
        }

        private void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            ground.transform.SetParent(_root.transform, false);
            ground.GetComponent<Collider>().enabled = false;
            ground.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(40f, 40f, 1f);
            ground.GetComponent<Renderer>().material.color = GroundColor;
            ground.layer = Layer;

            // A faint line across the middle where the two armies meet.
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
            line.name = "Midline";
            line.transform.SetParent(_root.transform, false);
            line.GetComponent<Collider>().enabled = false;
            line.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            line.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            line.transform.localScale = new Vector3(40f, 0.06f, 1f);
            line.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 1f) * 0.9f;
            line.layer = Layer;
        }

        private void BuildLight()
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(_root.transform, false);
            go.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << Layer;
            go.layer = Layer;
        }

        private Camera BuildCamera(BattleRules rules, int width, int height)
        {
            var go = new GameObject("Battle Camera");
            go.transform.SetParent(_root.transform, false);
            go.layer = Layer;
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Background;
            camera.cullingMask = 1 << Layer;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;

            // Fit the whole field: depth is squeezed by sin(pitch), plus head room for the units.
            float spanY = 2f * (rules.FrontGap / 2f + (Formation.Size - 1) * rules.RowSpacing + MarginCells);
            camera.orthographicSize = spanY / 2f * Mathf.Sin(PitchDegrees * Mathf.Deg2Rad) + 0.9f;

            Quaternion rotation = Quaternion.Euler(PitchDegrees, YawDegrees, 0f);
            go.transform.rotation = rotation;
            go.transform.position = Origin + new Vector3(0f, 0.5f, 0f) + rotation * Vector3.back * 30f;

            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "Battle3D" };
            texture.Create();
            camera.targetTexture = texture;
            return camera;
        }

        private static void DestroyObject(Object obj)
        {
            if (obj == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
