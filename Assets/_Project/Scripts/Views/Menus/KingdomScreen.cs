using System;
using System.Collections;
using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Kingdom;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// The Kingdom (PB-22): one card per building with its level, what the next level adds and what it costs.
    /// Upgrading spends gold from the profile. Feel: the gold counts down, the card flashes, the icon pops,
    /// the new level pip pops in and a "+3 %" floats up; affordable upgrade buttons breathe a little.
    /// </summary>
    public sealed class KingdomScreen : MonoBehaviour
    {
        private const float CardTop = 0.905f;
        private const float CardHeight = 0.188f;
        private const float CardGap = 0.012f;
        private static readonly Color CardColor = Palette.Panel;
        private static readonly Color PipOff = new Color(1f, 1f, 1f, 0.14f);

        private GameContent _content;
        private PlayerProfile _profile;
        private KingdomRules _rules;
        private Action<string> _onUpgraded;
        private Text _goldLabel;
        private RectTransform _cards;
        private int _shownGold;
        private Coroutine _goldTween;
        private readonly List<RectTransform> _pulsing = new List<RectTransform>();

        /// <summary>Id of the card pressed last and the level it had before (effects play for it once).</summary>
        private string _justUpgraded = "";

        public static KingdomScreen Create(Transform parent, GameContent content, PlayerProfile profile,
            Action<string> onUpgraded, Action<MainTab> onTab)
        {
            RectTransform root = Ui.Stretch(parent, "KingdomScreen");
            var screen = root.gameObject.AddComponent<KingdomScreen>();
            screen.Build(content, profile, onUpgraded, onTab);
            return screen;
        }

        /// <summary>The Upgrade button of a building (tests press it).</summary>
        public Button UpgradeButton(string buildingId)
        {
            Transform card = _cards.Find("Card " + buildingId);
            Transform button = card != null ? card.Find("Button Upgrade") : null;
            return button != null ? button.GetComponent<Button>() : null;
        }

        /// <summary>The gold shown at the top (it counts towards the real value over a moment).</summary>
        public int ShownGold => _shownGold;

        private void Build(GameContent content, PlayerProfile profile, Action<string> onUpgraded, Action<MainTab> onTab)
        {
            _content = content;
            _profile = profile;
            _rules = content.KingdomRules != null ? content.KingdomRules.Rules : new KingdomRules();
            _onUpgraded = onUpgraded;
            Transform root = transform;
            Ui.Backdrop(root);

            Ui.Label(root, Loc.T("Kingdom"), 48, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.45f, 0.99f);
            Image coin = Ui.Box(root, "Coin", Palette.Gold, 0.5f, 0.945f, 0.545f, 0.98f);
            coin.sprite = GemShapes.Get(GemShape.Circle);
            coin.preserveAspect = true;
            coin.raycastTarget = false;
            _goldLabel = Ui.Label(root, profile.Gold.ToString(), 52, TextAnchor.MiddleLeft, Palette.Text, 0.56f, 0.935f, 0.97f, 0.99f);
            _shownGold = profile.Gold;

            _cards = Ui.Rect(root, "Cards", 0f, 0f, 1f, 1f);
            Rebuild();
            TabBar.Add(root, MainTab.Kingdom, onTab);
        }

        private void Rebuild()
        {
            Ui.Clear(_cards);
            _pulsing.Clear();
            for (int i = 0; i < _rules.Buildings.Count; i++)
            {
                float top = CardTop - i * (CardHeight + CardGap);
                DrawCard(_rules.Buildings[i], top - CardHeight, top);
            }
        }

        private UnitLook LookOf(BuildingRule building)
        {
            switch (building.UnitId)
            {
                case "warrior": return UnitLook.Warrior;
                case "archer": return UnitLook.Archer;
                case "mage": return UnitLook.Mage;
                default: return UnitLook.Generic;
            }
        }

        private string ClassName(BuildingRule building)
        {
            foreach (UnitDefinition unit in _content.RecruitableUnits)
            {
                if (unit.Def.Id == building.UnitId)
                {
                    return Loc.T(unit.Def.DisplayName);
                }
            }
            return building.UnitId;
        }

        /// <summary>"Warrior HP", "Archer attack speed", "Mage damage" or "Stage gold".</summary>
        private string StatName(BuildingRule building)
        {
            switch (building.Stat)
            {
                case BuildingStat.UnitHp: return Loc.F("{0} HP", ClassName(building));
                case BuildingStat.UnitAttackSpeed: return Loc.F("{0} attack speed", ClassName(building));
                case BuildingStat.UnitDamage: return Loc.F("{0} damage", ClassName(building));
                default: return Loc.T("Stage gold");
            }
        }

        private static string Percent(float fraction) => Mathf.RoundToInt(fraction * 100f).ToString();

        private void DrawCard(BuildingRule building, float yMin, float yMax)
        {
            int level = _profile.BuildingLevel(building.Id);
            bool fresh = building.Id == _justUpgraded;
            int cost = _rules.CostToUpgrade(building.Id, level);
            bool maxed = cost < 0;
            bool affordable = !maxed && cost <= _profile.Gold;

            Image card = Ui.Card(_cards, "Card " + building.Id, CardColor, 0.03f, yMin, 0.97f, yMax);
            RectTransform rect = card.rectTransform;

            // Icon on a round plate: the class silhouette, or a star for the Treasury.
            Image plate = Ui.Box(rect, "Icon Plate", level > 0 ? Palette.Primary : Palette.PanelLight, 0.03f, 0.14f, 0.2f, 0.86f);
            plate.sprite = GemShapes.Get(GemShape.Circle);
            plate.preserveAspect = true;
            plate.raycastTarget = false;
            if (building.Stat == BuildingStat.StageGold)
            {
                Image star = Ui.Box(plate.rectTransform, "Star", Palette.Gold, 0.2f, 0.2f, 0.8f, 0.8f);
                star.sprite = GemShapes.Get(GemShape.Star);
                star.preserveAspect = true;
                star.raycastTarget = false;
            }
            else
            {
                Image icon = UnitSilhouettes.AddIcon(plate.rectTransform, LookOf(building), 1);
                icon.color = Color.white;
            }

            Ui.Label(rect, Loc.T(building.DisplayName), 44, TextAnchor.MiddleLeft, Palette.Text, 0.23f, 0.6f, 0.72f, 0.94f);
            Ui.Label(rect, Loc.F("Level {0}/{1}", level, _rules.MaxLevel), 30, TextAnchor.MiddleRight,
                level > 0 ? Palette.Gold : Palette.Muted, 0.5f, 0.6f, 0.72f, 0.94f);

            // Level pips.
            RectTransform pips = Ui.Rect(rect, "Pips", 0.23f, 0.42f, 0.72f, 0.54f);
            for (int i = 0; i < _rules.MaxLevel; i++)
            {
                float width = 1f / _rules.MaxLevel;
                Image pip = Ui.Box(pips, "Pip " + (i + 1), i < level ? Palette.Gold : PipOff,
                    i * width + 0.006f, 0f, (i + 1) * width - 0.006f, 1f);
                pip.raycastTarget = false;
                if (fresh && i == level - 1)
                {
                    StartCoroutine(Pop(pip.rectTransform, 0.45f, 1.9f));
                }
            }

            // What it gives now and after the next level.
            float now = level * building.BonusPerLevel;
            string text = maxed
                ? Loc.F("{0} +{1}%", StatName(building), Percent(now))
                : Loc.F("{0} +{1}% → +{2}%", StatName(building), Percent(now), Percent(now + building.BonusPerLevel));
            Ui.Label(rect, text, 30, TextAnchor.MiddleLeft, Palette.Muted, 0.23f, 0.06f, 0.72f, 0.4f);

            if (maxed)
            {
                Ui.Label(rect, Loc.T("MAX"), 52, TextAnchor.MiddleCenter, Palette.Gold, 0.74f, 0.15f, 0.97f, 0.85f);
            }
            else
            {
                string id = building.Id;
                Button button = Ui.Button(rect, "Upgrade", affordable ? Palette.Primary : Palette.Button, () => Upgrade(id),
                    0.74f, 0.15f, 0.97f, 0.85f, 36);
                button.gameObject.name = "Button Upgrade";
                button.interactable = affordable;
                button.GetComponentInChildren<Text>().text = Loc.T("Upgrade") + "\n" + cost;
                if (!affordable)
                {
                    button.GetComponentInChildren<Text>().color = Palette.Muted;
                }
                else
                {
                    _pulsing.Add((RectTransform)button.transform);
                }
            }

            if (fresh)
            {
                StartCoroutine(Flash(card));
                StartCoroutine(Pop(plate.rectTransform, 0.4f, 1.18f));
                StartCoroutine(Float(rect, "+" + Percent(building.BonusPerLevel) + "%"));
            }
        }

        private void Upgrade(string buildingId)
        {
            if (!_profile.TryUpgradeBuilding(_rules, buildingId))
            {
                return;
            }
            _justUpgraded = buildingId;
            Sfx.Play(_content.Sounds != null ? _content.Sounds.UnitRecruited : null, 0.9f);
            _onUpgraded?.Invoke(buildingId);
            Rebuild();
            _justUpgraded = "";
            CountGoldTo(_profile.Gold);
        }

        private void CountGoldTo(int gold)
        {
            if (_goldTween != null)
            {
                StopCoroutine(_goldTween);
            }
            int from = _shownGold;
            _goldTween = StartCoroutine(Tween.Run(0.5f, elapsed =>
            {
                _shownGold = Mathf.RoundToInt(Mathf.Lerp(from, gold, Tween.EaseOut(Tween.Progress(elapsed, 0.5f))));
                _goldLabel.text = _shownGold.ToString();
            }));
        }

        private void Update()
        {
            // Affordable upgrade buttons breathe: slow, small, easy to ignore.
            float scale = 1f + 0.025f * Mathf.Sin(Time.unscaledTime * 3f);
            foreach (RectTransform button in _pulsing)
            {
                if (button != null)
                {
                    button.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private static IEnumerator Pop(RectTransform rect, float seconds, float peak)
        {
            return Tween.Run(seconds, elapsed =>
            {
                if (rect != null)
                {
                    float s = 1f + (peak - 1f) * Tween.Hump(Tween.Progress(elapsed, seconds));
                    rect.localScale = new Vector3(s, s, 1f);
                }
            });
        }

        private static IEnumerator Flash(Image card)
        {
            Color from = Color.Lerp(CardColor, Palette.Gold, 0.55f);
            return Tween.Run(0.6f, elapsed =>
            {
                if (card != null)
                {
                    card.color = Color.Lerp(from, CardColor, Tween.EaseOut(Tween.Progress(elapsed, 0.6f)));
                }
            });
        }

        private static IEnumerator Float(RectTransform card, string value)
        {
            Text label = Ui.Label(card, value, 56, TextAnchor.MiddleCenter, Palette.Gold, 0.74f, 0.3f, 0.97f, 0.9f);
            RectTransform rect = label.rectTransform;
            return Tween.Run(0.9f, elapsed =>
            {
                if (label == null)
                {
                    return;
                }
                float t = Tween.Progress(elapsed, 0.9f);
                rect.anchoredPosition = new Vector2(0f, Tween.EaseOut(t) * 70f);
                Color color = label.color;
                color.a = 1f - Tween.EaseIn(t);
                label.color = color;
            });
        }
    }
}
