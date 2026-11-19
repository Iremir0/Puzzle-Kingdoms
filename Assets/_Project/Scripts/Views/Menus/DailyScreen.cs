using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// The Daily Challenge (PB-25, D-061): today's enemy army and rules, one attempt, your result and the
    /// last seven days. Everyone gets the same challenge on the same date.
    /// </summary>
    public sealed class DailyScreen : MonoBehaviour
    {
        public static DailyScreen Create(Transform parent, GameContent content, PlayerProfile profile, string today,
            StageDef challenge, Action onPlay, Action onBack)
        {
            RectTransform root = Ui.Stretch(parent, "DailyScreen");
            var screen = root.gameObject.AddComponent<DailyScreen>();
            screen.Build(content, profile, today, challenge, onPlay, onBack);
            return screen;
        }

        /// <summary>The Play button; null once today's attempt is used.</summary>
        public Button PlayButton
        {
            get
            {
                Transform button = transform.Find("Button Play");
                return button != null ? button.GetComponent<Button>() : null;
            }
        }

        private void Build(GameContent content, PlayerProfile profile, string today, StageDef challenge, Action onPlay, Action onBack)
        {
            Transform root = transform;
            Ui.Backdrop(root);
            Ui.Label(root, Loc.T("Daily Challenge"), 48, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.7f, 0.99f);
            Ui.Label(root, today, 34, TextAnchor.MiddleRight, Palette.Muted, 0.6f, 0.935f, 0.96f, 0.99f);

            // ---- today's challenge ----
            RectTransform card = Ui.Card(root, "Daily Card", Palette.Panel, 0.03f, 0.55f, 0.97f, 0.925f).rectTransform;
            Ui.Label(card, Loc.T("Today's enemy"), 34, TextAnchor.MiddleLeft, Palette.Muted, 0.05f, 0.82f, 0.95f, 0.97f);
            var groups = challenge.Enemies.GroupBy(e => e.Unit.DisplayName).Select(g => $"{Loc.T(g.Key)} x{g.Count()}");
            Text enemies = Ui.Label(card, string.Join("\n", groups), 36, TextAnchor.UpperLeft, Palette.Text, 0.05f, 0.42f, 0.95f, 0.82f);
            enemies.verticalOverflow = VerticalWrapMode.Overflow;
            string rules = Loc.F("{0} moves", challenge.Moves);
            if (challenge.StoneGems > 0)
            {
                rules += "  ·  " + Loc.F("{0} stones", challenge.StoneGems);
            }
            Ui.Label(card, rules, 34, TextAnchor.MiddleLeft, Palette.Text, 0.05f, 0.27f, 0.95f, 0.42f);
            string goal = StageBadges.GoalText(challenge);
            Ui.Label(card, goal.Length > 0 ? goal : Loc.T("Goal: defeat the army"), 32, TextAnchor.MiddleLeft, Palette.Gold, 0.05f, 0.14f, 0.95f, 0.27f);
            Ui.Label(card, Loc.F("Reward on a win: {0} gold", challenge.GoldReward), 30, TextAnchor.MiddleLeft, Palette.Muted, 0.05f, 0.02f, 0.95f, 0.14f);

            // ---- play, or today's result ----
            DailyResult done = profile.Daily.Result(today);
            if (done == null)
            {
                Button play = Ui.Button(root, "Play", Ui.ButtonStyle.Primary, onPlay, 0.1f, 0.43f, 0.9f, 0.52f, 52);
                UiJuice.Pulse((RectTransform)play.transform, 0.04f, 3f);
                Ui.Label(root, Loc.T("One attempt a day. Everyone plays the same challenge."), 28, TextAnchor.MiddleCenter, Palette.Muted,
                    0.05f, 0.385f, 0.95f, 0.43f);
            }
            else
            {
                RectTransform result = Ui.Card(root, "Daily Result", done.Won ? Color.Lerp(Palette.Panel, Palette.Good, 0.25f) : Color.Lerp(Palette.Panel, Palette.Bad, 0.2f),
                    0.03f, 0.4f, 0.97f, 0.53f).rectTransform;
                Ui.Label(result, done.Won ? Loc.T("Won") : Loc.T("Lost"), 44, TextAnchor.MiddleLeft, done.Won ? Palette.Good : Palette.Bad, 0.05f, 0.5f, 0.45f, 0.95f);
                Ui.Label(result, Loc.F("{0}% of the army left", done.HpPercent), 32, TextAnchor.MiddleLeft, Palette.Text, 0.05f, 0.08f, 0.6f, 0.5f);
                Ui.Label(result, Loc.F("{0} s", Mathf.RoundToInt(done.Ticks / (float)content.BattleRules.Rules.TicksPerSecond)), 36,
                    TextAnchor.MiddleRight, Palette.Muted, 0.6f, 0.2f, 0.95f, 0.8f);
                Ui.Label(root, Loc.T("Come back tomorrow for a new challenge."), 28, TextAnchor.MiddleCenter, Palette.Muted, 0.05f, 0.355f, 0.95f, 0.4f);
            }

            // ---- the last seven days ----
            RectTransform week = Ui.Card(root, "Daily Week", Palette.Panel, 0.03f, 0.1f, 0.97f, 0.34f).rectTransform;
            Ui.Label(week, Loc.T("Last 7 days"), 32, TextAnchor.MiddleLeft, Palette.Muted, 0.04f, 0.78f, 0.6f, 0.97f);
            DailyResult best = profile.Daily.Best();
            if (best != null)
            {
                Ui.Label(week, Loc.F("Best: {0}%", best.HpPercent), 30, TextAnchor.MiddleRight, Palette.Gold, 0.5f, 0.78f, 0.96f, 0.97f);
            }
            List<DailyResult> days = profile.Daily.Recent(today, 7);
            DateTime.TryParseExact(today, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
            for (int i = 0; i < days.Count; i++)
            {
                float x0 = 0.04f + i * 0.134f;
                DateTime day = date.AddDays(i - (days.Count - 1));
                DailyResult result = days[i];
                Color tint = result == null ? Palette.PanelLight : result.Won ? Palette.Good : Palette.Bad;
                RectTransform cell = Ui.Card(week, "Day " + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), tint, x0, 0.12f, x0 + 0.12f, 0.72f).rectTransform;
                Ui.Label(cell, day.Day.ToString(), 30, TextAnchor.MiddleCenter, Color.white, 0f, 0.5f, 1f, 1f);
                Ui.Label(cell, result == null ? "-" : result.HpPercent + "%", 24, TextAnchor.MiddleCenter, Color.white, 0f, 0f, 1f, 0.5f);
            }

            Ui.Button(root, "Back", Ui.ButtonStyle.Secondary, onBack, 0.3f, 0.02f, 0.7f, 0.085f, 40);
        }
    }
}
