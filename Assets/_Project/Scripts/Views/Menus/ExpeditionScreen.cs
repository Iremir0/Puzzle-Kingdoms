using System;
using System.Collections.Generic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// The expedition (PB-26, D-062): the path of battles, the army with its wounds, and either the Start button,
    /// the three rewards to pick from, or the end of the run. One screen for all four states.
    /// </summary>
    public sealed class ExpeditionScreen : MonoBehaviour
    {
        public sealed class Actions
        {
            public Action Start;
            public Action<int> Pick;
            public Action GiveUp;
            public Action Collect;
            public Action Back;
        }

        public static ExpeditionScreen Create(Transform parent, Expedition expedition, PlayerProfile profile, Actions actions)
        {
            RectTransform root = Ui.Stretch(parent, "ExpeditionScreen");
            var screen = root.gameObject.AddComponent<ExpeditionScreen>();
            screen.Build(expedition, profile, actions);
            return screen;
        }

        /// <summary>The reward cards (tests press them), in the order offered.</summary>
        public Button OfferButton(int index)
        {
            Transform card = transform.Find("Offer " + index);
            return card != null ? card.GetComponent<Button>() : null;
        }

        public Button StartButton
        {
            get
            {
                Transform button = transform.Find("Button Start battle");
                return button != null ? button.GetComponent<Button>() : null;
            }
        }

        public Button CollectButton
        {
            get
            {
                Transform button = transform.Find("Button Collect");
                return button != null ? button.GetComponent<Button>() : null;
            }
        }

        /// <summary>How a reward reads in a card.</summary>
        public static string Describe(ExpeditionReward reward, IReadOnlyList<UnitDef> classes)
        {
            switch (reward.Kind)
            {
                case ExpeditionRewardKind.Recruit:
                    UnitDef def = null;
                    foreach (UnitDef candidate in classes)
                    {
                        def = candidate.Id == reward.UnitId ? candidate : def;
                    }
                    return Loc.F("Recruit {0} {1}", reward.Amount, def != null ? Loc.T(def.DisplayName) : reward.UnitId);
                case ExpeditionRewardKind.HealAll: return Loc.T("Heal the whole army");
                case ExpeditionRewardKind.Promote: return Loc.T("Promote your weakest unit");
                default: return Loc.F("{0} extra Rally in the next battle", reward.Amount);
            }
        }

        private void Build(Expedition expedition, PlayerProfile profile, Actions actions)
        {
            Transform root = transform;
            Ui.Backdrop(root);
            Ui.Label(root, Loc.T("Expedition"), 48, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.6f, 0.99f);
            Ui.Label(root, Loc.F("Best: {0}", profile.BestExpeditionDepth) + " / " + expedition.Rules.Steps, 32, TextAnchor.MiddleRight, Palette.Muted,
                0.5f, 0.935f, 0.96f, 0.99f);

            // ---- the path ----
            RectTransform path = Ui.Card(root, "Expedition Path", Palette.Panel, 0.03f, 0.8f, 0.97f, 0.925f).rectTransform;
            int steps = expedition.Rules.Steps;
            for (int i = 0; i < steps; i++)
            {
                float width = 0.9f / steps;
                float x0 = 0.05f + i * width;
                bool done = i < expedition.Step || expedition.Phase == ExpeditionPhase.Won;
                bool current = i == expedition.Step && !expedition.IsOver;
                bool boss = i == steps - 1;
                Color tint = done ? Palette.Good : current ? Palette.Gold : Palette.PanelLight;
                RectTransform node = Ui.Card(path, "Step " + (i + 1), tint, x0 + width * 0.08f, 0.2f, x0 + width * 0.92f, 0.8f).rectTransform;
                Ui.Label(node, boss ? Loc.T("Boss") : (i + 1).ToString(), boss ? 30 : 44, TextAnchor.MiddleCenter,
                    current || done ? Palette.Background : Palette.Muted, 0f, 0f, 1f, 1f);
            }

            // ---- the army ----
            RectTransform army = Ui.Card(root, "Expedition Army", Palette.Panel, 0.03f, 0.43f, 0.97f, 0.79f).rectTransform;
            Ui.Label(army, Loc.F("Army: {0} units", expedition.Army.Count), 32, TextAnchor.MiddleLeft, Palette.Muted, 0.04f, 0.88f, 0.96f, 0.99f);
            if (expedition.Army.Count == 0)
            {
                Ui.Label(army, Loc.T("Your army is built in the puzzle before each battle."), 30, TextAnchor.MiddleCenter, Palette.Muted,
                    0.06f, 0.2f, 0.94f, 0.8f);
            }
            const int columns = 6;
            const int maxShown = 24;
            for (int i = 0; i < expedition.Army.Count && i < maxShown; i++)
            {
                ArmyUnit unit = expedition.Army[i];
                int col = i % columns;
                int row = i / columns;
                float x0 = 0.03f + col * 0.158f;
                float yTop = 0.86f - row * 0.215f;
                RectTransform cell = Ui.Card(army, "Unit " + i, Color.Lerp(Palette.PanelLight, Palette.For(unit.Def), 0.35f),
                    x0, yTop - 0.19f, x0 + 0.145f, yTop).rectTransform;
                Ui.Label(cell, Loc.T(unit.Def.DisplayName).Substring(0, 1) + unit.Tier, 34, TextAnchor.MiddleCenter, Color.white, 0f, 0.35f, 1f, 1f);
                Image track = Ui.Box(cell, "Hp Track", new Color(0f, 0f, 0f, 0.45f), 0.1f, 0.1f, 0.9f, 0.28f);
                Ui.Box(track.transform, "Hp", unit.HpFraction > 0.5f ? Palette.Good : unit.HpFraction > 0.25f ? Palette.Gold : Palette.Bad,
                    0f, 0f, Mathf.Max(0.04f, unit.HpFraction), 1f).raycastTarget = false;
            }
            if (expedition.Army.Count > maxShown)
            {
                Ui.Label(army, "+" + (expedition.Army.Count - maxShown), 28, TextAnchor.MiddleRight, Palette.Muted, 0.7f, 0.88f, 0.96f, 0.99f);
            }

            // ---- what to do now ----
            switch (expedition.Phase)
            {
                case ExpeditionPhase.Reward:
                    BuildOffers(root, expedition, actions);
                    break;
                case ExpeditionPhase.Won:
                case ExpeditionPhase.Lost:
                    BuildEnd(root, expedition, actions);
                    break;
                default:
                    Button start = Ui.Button(root, "Start battle", Ui.ButtonStyle.Primary, actions.Start, 0.1f, 0.3f, 0.9f, 0.4f, 50);
                    UiJuice.Pulse((RectTransform)start.transform, 0.04f, 3f);
                    Ui.Label(root, expedition.IsBossStep
                        ? Loc.T("The boss waits at the end of the road.")
                        : Loc.F("Battle {0} of {1}: a puzzle first, then the war camp.", expedition.Step + 1, steps),
                        28, TextAnchor.MiddleCenter, Palette.Muted, 0.05f, 0.24f, 0.95f, 0.3f);
                    break;
            }
            if (!expedition.IsOver)
            {
                Ui.Button(root, "Give up", Ui.ButtonStyle.Secondary, actions.GiveUp, 0.55f, 0.03f, 0.97f, 0.1f, 36);
            }
            Ui.Button(root, "Back", Ui.ButtonStyle.Secondary, actions.Back, 0.03f, 0.03f, 0.45f, 0.1f, 36);
        }

        private static void BuildOffers(Transform root, Expedition expedition, Actions actions)
        {
            Ui.Label(root, Loc.T("Pick a reward"), 40, TextAnchor.MiddleCenter, Palette.Gold, 0.05f, 0.385f, 0.95f, 0.43f);
            for (int i = 0; i < expedition.Offers.Count; i++)
            {
                int index = i;
                ExpeditionReward offer = expedition.Offers[i];
                float yMax = 0.375f - i * 0.09f;
                Button card = Ui.Button(root, Describe(offer, expedition.Rules.PlayerClasses), Palette.Button, () => actions.Pick(index),
                    0.05f, yMax - 0.08f, 0.95f, yMax, 36);
                card.gameObject.name = "Offer " + i;
            }
        }

        private static void BuildEnd(Transform root, Expedition expedition, Actions actions)
        {
            bool won = expedition.Phase == ExpeditionPhase.Won;
            RectTransform card = Ui.Card(root, "Expedition Result", won ? Color.Lerp(Palette.Panel, Palette.Good, 0.25f) : Color.Lerp(Palette.Panel, Palette.Bad, 0.2f),
                0.03f, 0.2f, 0.97f, 0.42f).rectTransform;
            Ui.Label(card, won ? Loc.T("Expedition complete!") : Loc.T("The expedition ends here."), 44, TextAnchor.MiddleCenter,
                won ? Palette.Good : Palette.Bad, 0.04f, 0.55f, 0.96f, 0.95f);
            Ui.Label(card, Loc.F("Battles won: {0}", expedition.Step) + "   +" + expedition.GoldEarned, 36, TextAnchor.MiddleCenter, Palette.Text,
                0.04f, 0.1f, 0.96f, 0.55f);
            Ui.Button(root, "Collect", Ui.ButtonStyle.Primary, actions.Collect, 0.1f, 0.115f, 0.9f, 0.19f, 44);
        }
    }
}
