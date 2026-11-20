using System;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// Settings over the stage map (PB-14): sound effects, music, the speed battles
    /// start at, the language (PB-32), and "Reset progress" behind a second confirmation. Every change is
    /// reported at once through <c>onChanged</c>, which saves it.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        private GameSettings _settings;
        private Action<GameSettings> _onChanged;
        private Action _onResetProgress;
        private Button _sound;
        private Button _music;
        private Button _speed1;
        private Button _speed2;
        private Button _english;
        private Button _turkish;
        private GameObject _confirm;

        /// <summary>The cloud save row of the settings (PB-31): whether a cloud exists here, who is signed in, what the button does.</summary>
        public sealed class CloudInfo
        {
            public bool Available;
            public bool SignedIn;
            public string Account = "";
            public Action Toggle;
        }

        private Button _cloudButton;

        /// <summary>Updates the cloud button after signing in or out.</summary>
        public void SetCloud(bool signedIn, string account) =>
            Ui.SetCaption(_cloudButton, CloudCaption(signedIn, account));

        private static string CloudCaption(bool signedIn, string account) =>
            signedIn ? (string.IsNullOrEmpty(account) ? Loc.T("Signed in") : account) + " · " + Loc.T("Sign out") : Loc.T("Sign in");

        public static SettingsPanel Create(Transform parent, GameSettings settings, string version,
            Action<GameSettings> onChanged, Action onResetProgress, Action onResetTutorial = null, Action onPrivacy = null,
            Action onReplays = null, CloudInfo cloud = null)
        {
            // A dimmed full-screen layer that swallows taps meant for the screen below.
            Image shade = Ui.Shade(parent, "SettingsPanel", 0.6f);
            var panel = shade.gameObject.AddComponent<SettingsPanel>();
            panel.Build(settings.Copy(), version, onChanged, onResetProgress, onResetTutorial, onPrivacy, onReplays, cloud);
            return panel;
        }

        private void Build(GameSettings settings, string version, Action<GameSettings> onChanged, Action onResetProgress,
            Action onResetTutorial, Action onPrivacy, Action onReplays, CloudInfo cloud)
        {
            _settings = settings;
            _onChanged = onChanged;
            _onResetProgress = onResetProgress;

            Transform box = Ui.Card(transform, "Box", Palette.Panel, 0.06f, 0.2f, 0.94f, 0.83f).transform;
            Ui.Label(box, Loc.T("Settings"), 60, TextAnchor.MiddleLeft, Palette.Text, 0.06f, 0.89f, 0.7f, 0.98f);
            Name(Ui.Button(box, "Close", Ui.ButtonStyle.Secondary, Close, 0.72f, 0.89f, 0.95f, 0.98f, 38), "Close");
            if (onReplays != null)
            {
                Name(Ui.Button(box, "Replays", Ui.ButtonStyle.Secondary, onReplays, 0.4f, 0.89f, 0.7f, 0.98f, 34), "Replays");
            }

            Row(box, Loc.T("Sound effects"), 0.76f);
            _sound = Name(Ui.Button(box, "", Ui.ButtonStyle.Secondary, ToggleSound, 0.66f, 0.74f, 0.94f, 0.83f, 40), "Sound Toggle");
            Row(box, Loc.T("Music"), 0.66f);
            _music = Name(Ui.Button(box, "", Ui.ButtonStyle.Secondary, ToggleMusic, 0.66f, 0.64f, 0.94f, 0.725f, 40), "Music Toggle");
            Row(box, Loc.T("Battle speed"), 0.56f);
            _speed1 = Name(Ui.Button(box, "1x", Ui.ButtonStyle.Secondary, () => SetSpeed(1), 0.66f, 0.54f, 0.79f, 0.625f, 40), "Speed 1x");
            _speed2 = Name(Ui.Button(box, "2x", Ui.ButtonStyle.Secondary, () => SetSpeed(2), 0.81f, 0.54f, 0.94f, 0.625f, 40), "Speed 2x");
            // Each language button shows the language's own name, whatever language is on (PB-32).
            Row(box, Loc.T("Language"), 0.46f);
            _english = Name(Ui.Button(box, Languages.NativeName(Language.English), Ui.ButtonStyle.Secondary,
                () => SetLanguage(Language.English), 0.5f, 0.44f, 0.71f, 0.525f, 40), "Language en");
            _turkish = Name(Ui.Button(box, Languages.NativeName(Language.Turkish), Ui.ButtonStyle.Secondary,
                () => SetLanguage(Language.Turkish), 0.73f, 0.44f, 0.94f, 0.525f, 40), "Language tr");

            // Reset tutorial and Data and privacy (PB-29) share a row; either takes the whole row alone.
            float split = onResetTutorial != null && onPrivacy != null ? 0.49f : 0.94f;
            if (onResetTutorial != null)
            {
                Button resetTutorial = null;
                resetTutorial = Name(Ui.Button(box, "Reset tutorial", Ui.ButtonStyle.Secondary, () =>
                {
                    onResetTutorial();
                    Ui.SetCaption(resetTutorial, "Tips will show again");
                    resetTutorial.interactable = false;
                }, 0.06f, 0.34f, split, 0.425f, 40), "Reset Tutorial");
            }
            if (onPrivacy != null)
            {
                Name(Ui.Button(box, "Data and privacy", Ui.ButtonStyle.Secondary, onPrivacy,
                    onResetTutorial != null ? 0.51f : 0.06f, 0.34f, 0.94f, 0.425f, 40), "Privacy");
            }
            if (cloud != null && cloud.Available)
            {
                Row(box, Loc.T("Cloud save"), 0.26f);
                _cloudButton = Name(Ui.Button(box, CloudCaption(cloud.SignedIn, cloud.Account), Ui.ButtonStyle.Secondary,
                    cloud.Toggle, 0.5f, 0.24f, 0.94f, 0.325f, 30), "Cloud Button");
            }
            Name(Ui.Button(box, "Reset progress", Ui.ButtonStyle.Danger, AskReset, 0.06f, 0.14f, 0.94f, 0.225f, 40), "Reset Progress");
            Ui.Label(box, Loc.F("Version {0}", version), 30, TextAnchor.MiddleCenter, Palette.Muted, 0f, 0.02f, 1f, 0.09f);

            // Second step of the reset: a box over the settings with Yes / Cancel.
            Image confirm = Ui.Box(transform, "Confirm Reset", Palette.Background, 0.1f, 0.38f, 0.9f, 0.62f);
            _confirm = confirm.gameObject;
            Ui.Label(confirm.transform, Loc.T("Are you sure?\nThis deletes your stars and gold."), 44, TextAnchor.MiddleCenter,
                Palette.Text, 0.05f, 0.45f, 0.95f, 0.95f);
            Name(Ui.Button(confirm.transform, "Yes, delete", Ui.ButtonStyle.Danger, ConfirmReset, 0.05f, 0.08f, 0.48f, 0.38f, 40),
                "Yes Delete");
            Name(Ui.Button(confirm.transform, "Cancel", Ui.ButtonStyle.Secondary, () => _confirm.SetActive(false),
                0.52f, 0.08f, 0.95f, 0.38f, 40), "Cancel Reset");
            _confirm.SetActive(false);

            Refresh();
        }

        public GameSettings Settings => _settings.Copy();

        private static void Row(Transform box, string text, float y) =>
            Ui.Label(box, text, 44, TextAnchor.MiddleLeft, Palette.Text, 0.06f, y - 0.02f, 0.62f, y + 0.08f);

        private static Button Name(Button button, string name)
        {
            button.gameObject.name = name;
            return button;
        }

        private void ToggleSound()
        {
            _settings.sound = !_settings.sound;
            Changed();
        }

        private void ToggleMusic()
        {
            _settings.music = !_settings.music;
            Changed();
        }

        private void SetSpeed(int speed)
        {
            _settings.battleSpeed = speed;
            Changed();
        }

        /// <summary>Saves the choice; GameApp then redraws the map and this panel in the new language.</summary>
        private void SetLanguage(Language language)
        {
            _settings.language = Languages.Code(language);
            Changed();
        }

        private void Changed()
        {
            Refresh();
            _onChanged?.Invoke(_settings.Copy());
        }

        private void AskReset() => _confirm.SetActive(true);

        private void ConfirmReset()
        {
            _confirm.SetActive(false);
            _onResetProgress?.Invoke();
            Close();
        }

        private void Close() => Destroy(gameObject);

        private void Refresh()
        {
            SetToggle(_sound, _settings.sound);
            SetToggle(_music, _settings.music);
            _speed1.image.color = _settings.BattleSpeed == 1 ? Palette.Primary : Palette.Button;
            _speed2.image.color = _settings.BattleSpeed == 2 ? Palette.Primary : Palette.Button;
            Language shown = _settings.LanguageFor(Loc.SystemIsTurkish);
            _english.image.color = shown == Language.English ? Palette.Primary : Palette.Button;
            _turkish.image.color = shown == Language.Turkish ? Palette.Primary : Palette.Button;
        }

        private static void SetToggle(Button button, bool on)
        {
            Ui.SetCaption(button, on ? "ON" : "OFF");
            button.image.color = on ? Palette.Good : Palette.Button;
        }
    }
}
