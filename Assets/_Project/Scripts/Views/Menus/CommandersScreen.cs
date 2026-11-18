using System;
using System.Collections;
using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// The commanders (PB-23): the player's own pick of what Rally does. A card per commander with its
    /// colour crest, what its Rally does, and either "Choose", "Leading" or what to win to unlock it; the
    /// first card is the classic Rally, always available. Choosing flashes the card and pops its crest.
    /// </summary>
    public sealed class CommandersScreen : MonoBehaviour
    {
        private const float CardTop = 0.905f;
        private const float CardHeight = 0.2f;
        private const float CardGap = 0.012f;
        private static readonly Color CardColor = Palette.Panel;

        private GameContent _content;
        private PlayerProfile _profile;
        private Action<string> _onChosen;
        private RectTransform _cards;
        private List<Commander> _commanders;
        private string _justChosen = "\u0001";

        public static CommandersScreen Create(Transform parent, GameContent content, PlayerProfile profile,
            Action<string> onChosen, Action<MainTab> onTab)
        {
            RectTransform root = Ui.Stretch(parent, "CommandersScreen");
            var screen = root.gameObject.AddComponent<CommandersScreen>();
            screen.Build(content, profile, onChosen, onTab);
            return screen;
        }

        /// <summary>The Choose button of a commander ("" = the classic Rally); tests press it.</summary>
        public Button ChooseButton(string commanderId)
        {
            Transform card = _cards.Find("Card " + (commanderId == "" ? "classic" : commanderId));
            Transform button = card != null ? card.Find("Button Choose") : null;
            return button != null ? button.GetComponent<Button>() : null;
        }

        private void Build(GameContent content, PlayerProfile profile, Action<string> onChosen, Action<MainTab> onTab)
        {
            _content = content;
            _profile = profile;
            _onChosen = onChosen;
            _commanders = content.CommanderList();
            Transform root = transform;
            Ui.Box(root, "Background", Palette.Background);
            Ui.Label(root, Loc.T("Commanders"), 48, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.6f, 0.99f);
            Ui.Label(root, Loc.T("Win stages to earn commanders. One leads your army."), 28, TextAnchor.MiddleRight,
                Palette.Muted, 0.3f, 0.905f, 0.97f, 0.94f);
            _cards = Ui.Rect(root, "Cards", 0f, 0f, 1f, 1f);
            Rebuild();
            TabBar.Add(root, MainTab.Commanders, onTab);
        }

        private void Rebuild()
        {
            Ui.Clear(_cards);
            DrawCard(null, 0);
            for (int i = 0; i < _commanders.Count; i++)
            {
                DrawCard(_commanders[i], i + 1);
            }
            _justChosen = "\u0001";
        }

        private string StageName(string stageId)
        {
            foreach (StageDefinition stage in _content.Campaign)
            {
                if (stage.Id == stageId)
                {
                    return Loc.T(stage.DisplayName);
                }
            }
            return stageId;
        }

        private Color ColorOf(Commander commander)
        {
            foreach (CommanderDefinition definition in _content.Commanders)
            {
                if (definition.Id == commander.Id)
                {
                    return definition.Color;
                }
            }
            return Palette.Gold;
        }

        private static GemShape ShapeOf(RallyKind kind) =>
            kind == RallyKind.ShieldWall ? GemShape.Square : kind == RallyKind.ArrowRain ? GemShape.Triangle
            : kind == RallyKind.Fireball ? GemShape.Diamond : GemShape.Star;

        /// <summary>One line on what the Rally does, with its numbers.</summary>
        private static string Effect(RallyVariant v)
        {
            string seconds = Mathf.RoundToInt(v.DurationSeconds).ToString();
            switch (v.Kind)
            {
                case RallyKind.ShieldWall:
                    return Loc.F("{0} s: front row takes {1}% less damage", seconds, Mathf.RoundToInt(v.FrontRowDamageReduction * 100f));
                case RallyKind.ArrowRain:
                    return Loc.F("{0} s: {1} volleys, {2}% of max HP each, on the enemy back row", seconds, v.VolleyCount,
                        Mathf.RoundToInt(v.VolleyPercentMaxHp * 100f));
                case RallyKind.Fireball:
                    return Loc.F("One blast: {0}% of max HP to the densest enemy group", Mathf.RoundToInt(v.BlastPercentMaxHp * 100f));
                default:
                    return Loc.F("{0} s: +{1}% damage and attack speed", seconds, Mathf.RoundToInt(v.DamageBonus * 100f));
            }
        }

        private void DrawCard(Commander commander, int index)
        {
            bool classic = commander == null;
            string id = classic ? "" : commander.Id;
            bool unlocked = classic || _profile.IsUnlocked(commander);
            bool leading = _profile.CommanderId == id;
            RallyVariant variant = classic ? new RallyVariant() : commander.Variant;
            Color color = classic ? Palette.Gold : ColorOf(commander);
            float top = CardTop - index * (CardHeight + CardGap);
            float yMin = top - CardHeight;

            Image card = Ui.Box(_cards, "Card " + (classic ? "classic" : id), leading ? Palette.PanelLight : CardColor,
                0.03f, yMin, 0.97f, top);
            RectTransform rect = card.rectTransform;
            if (leading)
            {
                var outline = card.gameObject.AddComponent<Outline>();
                outline.effectColor = color;
                outline.effectDistance = new Vector2(5f, 5f);
            }

            // The crest: a round plate in the commander's colour with its shape.
            Image plate = Ui.Box(rect, "Crest", unlocked ? color : Palette.Button, 0.03f, 0.16f, 0.2f, 0.84f);
            plate.sprite = GemShapes.Get(GemShape.Circle);
            plate.preserveAspect = true;
            plate.raycastTarget = false;
            Image shape = Ui.Box(plate.rectTransform, "Shape", unlocked ? Color.white : Palette.Muted, 0.25f, 0.25f, 0.75f, 0.75f);
            shape.sprite = GemShapes.Get(ShapeOf(variant.Kind));
            shape.preserveAspect = true;
            shape.raycastTarget = false;

            string name = classic ? Loc.T("No commander") : Loc.T(commander.DisplayName);
            Ui.Label(rect, name, 44, TextAnchor.MiddleLeft, unlocked ? Palette.Text : Palette.Muted, 0.23f, 0.62f, 0.72f, 0.95f);
            string description = classic ? Loc.T("The classic Rally.") : Loc.T(commander.Description);
            Ui.Label(rect, description, 28, TextAnchor.UpperLeft, Palette.Muted, 0.23f, 0.34f, 0.72f, 0.64f);
            Ui.Label(rect, Effect(variant), 26, TextAnchor.UpperLeft, unlocked ? color : Palette.Muted, 0.23f, 0.04f, 0.72f, 0.36f);

            if (!unlocked)
            {
                Ui.Label(rect, Loc.F("Win {0} to unlock", StageName(commander.UnlockStageId)), 28, TextAnchor.MiddleCenter,
                    Palette.Muted, 0.74f, 0.15f, 0.97f, 0.85f);
            }
            else if (leading)
            {
                Ui.Label(rect, Loc.T("Leading"), 42, TextAnchor.MiddleCenter, Palette.Gold, 0.74f, 0.15f, 0.97f, 0.85f);
            }
            else
            {
                Button choose = Ui.Button(rect, "Choose", Palette.Primary, () => Choose(id), 0.74f, 0.2f, 0.97f, 0.8f, 38);
                choose.gameObject.name = "Button Choose";
            }

            if (id == _justChosen)
            {
                StartCoroutine(Flash(card, color));
                StartCoroutine(Pop(plate.rectTransform));
            }
        }

        private void Choose(string commanderId)
        {
            if (!_profile.TryChooseCommander(_commanders, commanderId))
            {
                return;
            }
            _justChosen = commanderId;
            Sfx.Play(_content.Sounds != null ? _content.Sounds.RallyCharge : null);
            _onChosen?.Invoke(commanderId);
            Rebuild();
            // Rebuild resets the marker before the next frame; the effects were started inside it.
        }

        private static IEnumerator Pop(RectTransform rect) =>
            Tween.Run(0.45f, elapsed =>
            {
                if (rect != null)
                {
                    float s = 1f + 0.25f * Tween.Hump(Tween.Progress(elapsed, 0.45f));
                    rect.localScale = new Vector3(s, s, 1f);
                }
            });

        private static IEnumerator Flash(Image card, Color color)
        {
            Color from = Color.Lerp(Palette.PanelLight, color, 0.6f);
            return Tween.Run(0.7f, elapsed =>
            {
                if (card != null)
                {
                    card.color = Color.Lerp(from, Palette.PanelLight, Tween.EaseOut(Tween.Progress(elapsed, 0.7f)));
                }
            });
        }
    }
}
