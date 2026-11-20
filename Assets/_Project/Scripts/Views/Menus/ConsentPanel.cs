using System;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// Asks before anything is collected (PB-29, KVKK / GDPR, D-047): play statistics, error reports and
    /// personalised ads, each its own switch. Switches start off the first time (no pre-ticked boxes);
    /// opened again from Settings they show the saved answer. "Allow all" or "Save my choice" closes it
    /// and reports the three answers.
    /// </summary>
    public sealed class ConsentPanel : MonoBehaviour
    {
        private Action<bool, bool, bool> _onAnswer;
        private bool _analytics;
        private bool _errors;
        private bool _ads;
        private Button _analyticsToggle;
        private Button _errorsToggle;
        private Button _adsToggle;

        public static ConsentPanel Create(Transform parent, GameSettings current, bool answeredBefore,
            Action<bool, bool, bool> onAnswer)
        {
            Image shade = Ui.Shade(parent, "ConsentPanel", 0.7f);
            var panel = shade.gameObject.AddComponent<ConsentPanel>();
            panel.Build(current, answeredBefore, onAnswer);
            return panel;
        }

        private void Build(GameSettings current, bool answeredBefore, Action<bool, bool, bool> onAnswer)
        {
            _onAnswer = onAnswer;
            _analytics = answeredBefore && current.analytics;
            _errors = answeredBefore && current.crashReports;
            _ads = answeredBefore && current.personalisedAds;

            Transform box = Ui.Card(transform, "Box", Palette.Panel, 0.06f, 0.18f, 0.94f, 0.82f).transform;
            Ui.Label(box, Loc.T("Help us improve the game"), 52, TextAnchor.MiddleLeft, Palette.Text,
                0.06f, 0.86f, 0.94f, 0.97f);
            Ui.Label(box, Loc.T("We would like to record how you play: which stages you win or lose, your moves and how long they take. Nothing that names you is collected, and you can change this any time in Settings."),
                38, TextAnchor.UpperLeft, Palette.Muted, 0.06f, 0.56f, 0.94f, 0.84f);

            _analyticsToggle = Row(box, Loc.T("Play statistics"), 0.45f, "Consent Analytics", () => _analytics = !_analytics);
            _errorsToggle = Row(box, Loc.T("Error reports"), 0.34f, "Consent Errors", () => _errors = !_errors);
            _adsToggle = Row(box, Loc.T("Personalised ads"), 0.23f, "Consent Ads", () => _ads = !_ads);

            Button all = Ui.Button(box, "Allow all", Ui.ButtonStyle.Primary, () => Answer(true, true, true),
                0.06f, 0.04f, 0.49f, 0.14f, 40);
            all.gameObject.name = "Allow All";
            Button save = Ui.Button(box, "Save my choice", Ui.ButtonStyle.Secondary, () => Answer(_analytics, _errors, _ads),
                0.51f, 0.04f, 0.94f, 0.14f, 40);
            save.gameObject.name = "Save Choice";
            Refresh();
        }

        private Button Row(Transform box, string text, float y, string name, Action flip)
        {
            Ui.Label(box, text, 44, TextAnchor.MiddleLeft, Palette.Text, 0.06f, y - 0.02f, 0.62f, y + 0.07f);
            Button toggle = Ui.Button(box, "", Ui.ButtonStyle.Secondary, () =>
            {
                flip();
                Refresh();
            }, 0.66f, y - 0.02f, 0.94f, y + 0.07f, 40);
            toggle.gameObject.name = name;
            return toggle;
        }

        private void Answer(bool analytics, bool errors, bool ads)
        {
            _onAnswer?.Invoke(analytics, errors, ads);
            Destroy(gameObject);
        }

        private void Refresh()
        {
            SetToggle(_analyticsToggle, _analytics);
            SetToggle(_errorsToggle, _errors);
            SetToggle(_adsToggle, _ads);
        }

        private static void SetToggle(Button button, bool on)
        {
            Ui.SetCaption(button, on ? "ON" : "OFF");
            button.image.color = on ? Palette.Good : Palette.Button;
        }
    }
}
