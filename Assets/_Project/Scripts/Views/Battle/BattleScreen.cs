using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Battle
{
    /// <summary>
    /// Grey-box 2D battle view. Advances <see cref="BattleSimulator"/> at its
    /// fixed tick rate (times the speed setting) and draws every combatant as a
    /// square with an HP bar. Player at the bottom, enemy at the top.
    /// </summary>
    public sealed class BattleScreen : MonoBehaviour
    {
        /// <summary>Battlefield height shown, in cells (front gap + 2 rows per side + margin).</summary>
        private const float FieldMarginCells = 1.5f;
        private const float UnitSizeCells = 0.75f;
        private const float FlashSeconds = 0.08f;
        private const float FloatSeconds = 0.6f;
        private const float EndDelaySeconds = 1.2f;
        private const int MaxTicksPerFrame = 10;

        private sealed class UnitView
        {
            public Combatant Combatant;
            public RectTransform Rect;
            public Image Body;
            public RectTransform HpFill;
            public GameObject HpBar;
            public Color BaseColor;
            public float FlashLeft;
        }

        private sealed class FloatingText
        {
            public Text Label;
            public float Age;
        }

        private BattleSimulator _sim;
        private Action _onFinished;
        private RectTransform _field;
        private float _spanY;
        private float _spanX;
        private readonly List<UnitView> _units = new List<UnitView>();
        private readonly List<FloatingText> _floating = new List<FloatingText>();
        private Text _header;
        private Text _clock;
        private Text _banner;
        private Image _rallyGlow;
        private Button _rally;
        private Button _speed;
        private Button _pause;
        private float _accumulator;
        private int _speedFactor = 1;
        private bool _paused;
        private float _endTimer = -1f;

        public BattleSimulator Simulator => _sim;

        public static BattleScreen Create(Transform parent, StageRun run, Action onFinished)
        {
            RectTransform root = Ui.Stretch(parent, "BattleScreen");
            var screen = root.gameObject.AddComponent<BattleScreen>();
            screen.Build(run, onFinished);
            return screen;
        }

        private void Build(StageRun run, Action onFinished)
        {
            _sim = run.Battle;
            _onFinished = onFinished;
            Transform root = transform;
            Ui.Box(root, "Background", Palette.Background);

            _header = Ui.Label(root, "", 44, TextAnchor.MiddleLeft, Palette.Text, 0.04f, 0.93f, 0.75f, 0.99f);
            _clock = Ui.Label(root, "", 44, TextAnchor.MiddleRight, Palette.Muted, 0.7f, 0.93f, 0.96f, 0.99f);

            RectTransform area = Ui.Rect(root, "Field Area", 0.03f, 0.14f, 0.97f, 0.92f);
            _field = Ui.Stretch(area, "Field");
            var fitter = _field.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 0.72f;
            Ui.Box(_field, "Ground", Palette.Panel);
            _rallyGlow = Ui.Box(_field, "Rally Glow", new Color(Palette.Gold.r, Palette.Gold.g, Palette.Gold.b, 0.12f));
            _rallyGlow.raycastTarget = false;
            _rallyGlow.enabled = false;
            Ui.Box(_field, "Midline", new Color(1f, 1f, 1f, 0.08f), 0f, 0.498f, 1f, 0.502f);

            BattleRules rules = _sim.Rules;
            _spanY = 2f * (rules.FrontGap / 2f + 2f * rules.RowSpacing + FieldMarginCells);
            _spanX = _spanY * fitter.aspectRatio;

            foreach (Combatant combatant in _sim.Combatants)
            {
                _units.Add(CreateUnit(combatant));
            }

            _banner = Ui.Label(root, "", 80, TextAnchor.MiddleCenter, Palette.Gold, 0f, 0.84f, 1f, 0.92f);

            _rally = Ui.Button(root, "", Palette.Gold, OnRally, 0.03f, 0.02f, 0.5f, 0.12f, 48);
            _rally.GetComponentInChildren<Text>().color = Palette.Background;
            _speed = Ui.Button(root, "1x", Palette.Button, OnSpeed, 0.52f, 0.02f, 0.67f, 0.12f);
            _pause = Ui.Button(root, "Pause", Palette.Button, OnPause, 0.69f, 0.02f, 0.84f, 0.12f, 38);
            Ui.Button(root, "Skip", Palette.Button, OnSkip, 0.86f, 0.02f, 0.97f, 0.12f, 38);
            RefreshHud();
        }

        private UnitView CreateUnit(Combatant combatant)
        {
            var view = new UnitView { Combatant = combatant, BaseColor = Palette.For(combatant.Unit.Def) };
            Image body = Ui.Box(_field, $"Unit {combatant.Id}", view.BaseColor);
            body.sprite = UnitSilhouettes.Get(Palette.LookFor(combatant.Unit.Def));
            body.preserveAspect = true;
            body.raycastTarget = false;
            view.Body = body;
            view.Rect = body.rectTransform;
            var outline = body.gameObject.AddComponent<Outline>();
            outline.effectColor = combatant.Side == Side.Player ? Palette.Text : Palette.Bad;
            outline.effectDistance = new Vector2(4, 4);
            if (combatant.Unit.Tier > 1)
            {
                // The silhouette shows the class; only the tier is written on it.
                Ui.Label(body.transform, "T" + combatant.Unit.Tier, 30, TextAnchor.LowerCenter, Color.white, 0f, -0.05f, 1f, 0.3f);
            }

            Image bar = Ui.Box(body.transform, "HP", new Color(0, 0, 0, 0.6f), 0f, 1.08f, 1f, 1.22f);
            bar.raycastTarget = false;
            Image fill = Ui.Box(bar.transform, "Fill", combatant.Side == Side.Player ? Palette.Good : Palette.Bad);
            fill.raycastTarget = false;
            view.HpBar = bar.gameObject;
            view.HpFill = fill.rectTransform;
            return view;
        }

        private void Update()
        {
            if (_endTimer >= 0f)
            {
                _endTimer -= Time.deltaTime;
                if (_endTimer < 0f)
                {
                    _endTimer = float.MaxValue; // fire once
                    _onFinished?.Invoke();
                }
            }
            else if (!_paused && !_sim.IsFinished)
            {
                float tick = 1f / _sim.Rules.TicksPerSecond;
                _accumulator += Time.deltaTime * _speedFactor;
                int steps = 0;
                while (_accumulator >= tick && !_sim.IsFinished && steps++ < MaxTicksPerFrame)
                {
                    _accumulator -= tick;
                    Play(_sim.Step());
                }
                if (_sim.IsFinished)
                {
                    OnBattleOver();
                }
            }
            Render(Time.deltaTime);
        }

        private void Play(List<BattleEvent> events)
        {
            foreach (BattleEvent e in events)
            {
                switch (e.Type)
                {
                    case BattleEventType.Attack:
                        UnitView target = _units[e.TargetId];
                        target.FlashLeft = FlashSeconds;
                        SpawnFloating(target, e.Amount.ToString());
                        break;
                    case BattleEventType.RallyStarted:
                        _banner.text = "RALLY!";
                        _banner.color = Palette.Gold;
                        break;
                    case BattleEventType.RallyEnded:
                        _banner.text = "";
                        break;
                }
            }
            RefreshHud();
        }

        private void Render(float deltaTime)
        {
            float cell = _field.rect.height / _spanY;
            foreach (UnitView view in _units)
            {
                Combatant c = view.Combatant;
                var anchor = new Vector2(0.5f + c.Position.X / _spanX, 0.5f + c.Position.Y / _spanY);
                view.Rect.anchorMin = anchor;
                view.Rect.anchorMax = anchor;
                view.Rect.sizeDelta = Vector2.one * (cell * UnitSizeCells);
                view.Rect.anchoredPosition = Vector2.zero;

                if (!c.IsAlive)
                {
                    view.Body.color = new Color(view.BaseColor.r, view.BaseColor.g, view.BaseColor.b, 0.18f);
                    view.HpBar.SetActive(false);
                    continue;
                }
                view.FlashLeft -= deltaTime;
                view.Body.color = view.FlashLeft > 0f ? Color.white : view.BaseColor;
                view.HpFill.anchorMax = new Vector2(Mathf.Clamp01((float)c.Hp / c.MaxHp), 1f);
            }

            for (int i = _floating.Count - 1; i >= 0; i--)
            {
                FloatingText text = _floating[i];
                text.Age += deltaTime;
                text.Label.rectTransform.anchoredPosition += new Vector2(0f, cell * deltaTime);
                text.Label.color = new Color(1f, 1f, 1f, 1f - text.Age / FloatSeconds);
                if (text.Age >= FloatSeconds)
                {
                    Destroy(text.Label.gameObject);
                    _floating.RemoveAt(i);
                }
            }
            _rallyGlow.enabled = _sim.Rally.IsActive;
        }

        private void SpawnFloating(UnitView target, string value)
        {
            if (_floating.Count > 40)
            {
                return;
            }
            Text label = Ui.Label(_field, value, 30, TextAnchor.MiddleCenter, Color.white);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = target.Rect.anchorMin;
            rect.anchorMax = target.Rect.anchorMax;
            rect.sizeDelta = new Vector2(120, 40);
            rect.anchoredPosition = new Vector2(0f, target.Rect.sizeDelta.y * 0.9f);
            _floating.Add(new FloatingText { Label = label });
        }

        private void RefreshHud()
        {
            _header.text = $"Allies {_sim.AliveCount(Side.Player)}  vs  Enemies {_sim.AliveCount(Side.Enemy)}";
            _clock.text = $"{(float)_sim.Tick / _sim.Rules.TicksPerSecond:0.0} s";
            Rally rally = _sim.Rally;
            Ui.SetCaption(_rally, rally.IsActive
                ? $"Rally {(float)rally.TicksLeft / _sim.Rules.TicksPerSecond:0.0}s"
                : $"Rally ({rally.Charges})");
            _rally.interactable = rally.CanActivate && !_sim.IsFinished;
        }

        private void OnRally()
        {
            if (_sim.ActivateRally())
            {
                RefreshHud();
            }
        }

        private void OnSpeed()
        {
            _speedFactor = _speedFactor == 1 ? 2 : 1;
            Ui.SetCaption(_speed, $"{_speedFactor}x");
        }

        private void OnPause()
        {
            _paused = !_paused;
            Ui.SetCaption(_pause, _paused ? "Play" : "Pause");
        }

        /// <summary>Resolves the rest of the battle instantly (no further Rally).</summary>
        public void OnSkip()
        {
            while (!_sim.IsFinished)
            {
                _sim.Step();
            }
            RefreshHud();
            OnBattleOver();
        }

        private void OnBattleOver()
        {
            if (_endTimer >= 0f)
            {
                return;
            }
            RefreshHud();
            BattleResult result = _sim.Result;
            _banner.text = result.Victory ? "Victory!" : (result.TimedOut ? "Time up" : "Defeat");
            _banner.color = result.Victory ? Palette.Gold : Palette.Bad;
            _endTimer = EndDelaySeconds;
        }
    }
}
