using System;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// Settings over the stage map (PB-14): sound effects, music, the speed battles
    /// start at, and "Reset progress" behind a second confirmation. Every change is
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
        private GameObject _confirm;

        public static SettingsPanel Create(Transform parent, GameSettings settings, string version,
            Action<GameSettings> onChanged, Action onResetProgress)
        {
            // A dimmed full-screen layer that swallows taps meant for the screen below.
            Image shade = Ui.Box(parent, "SettingsPanel", new Color(0f, 0f, 0f, 0.6f));
            var panel = shade.gameObject.AddComponent<SettingsPanel>();
            panel.Build(settings.Copy(), version, onChanged, onResetProgress);
            return panel;
        }

        private void Build(GameSettings settings, string version, Action<GameSettings> onChanged, Action onResetProgress)
        {
            _settings = settings;
            _onChanged = onChanged;
            _onResetProgress = onResetProgress;

            Transform box = Ui.Box(transform, "Box", Palette.Panel, 0.06f, 0.25f, 0.94f, 0.78f).transform;
            Ui.Label(box, "Settings", 60, TextAnchor.MiddleLeft, Palette.Text, 0.06f, 0.87f, 0.7f, 0.97f);
            Name(Ui.Button(box, "Close", Palette.Button, Close, 0.72f, 0.87f, 0.95f, 0.97f, 38), "Close");

            Row(box, "Sound effects", 0.73f);
            _sound = Name(Ui.Button(box, "", Palette.Button, ToggleSound, 0.66f, 0.71f, 0.94f, 0.81f, 40), "Sound Toggle");
            Row(box, "Music", 0.6f);
            _music = Name(Ui.Button(box, "", Palette.Button, ToggleMusic, 0.66f, 0.58f, 0.94f, 0.68f, 40), "Music Toggle");
            Row(box, "Battle speed", 0.47f);
            _speed1 = Name(Ui.Button(box, "1x", Palette.Button, () => SetSpeed(1), 0.66f, 0.45f, 0.79f, 0.55f, 40), "Speed 1x");
            _speed2 = Name(Ui.Button(box, "2x", Palette.Button, () => SetSpeed(2), 0.81f, 0.45f, 0.94f, 0.55f, 40), "Speed 2x");

            Name(Ui.Button(box, "Reset progress", Palette.Bad, AskReset, 0.06f, 0.22f, 0.94f, 0.33f, 40), "Reset Progress");
            Ui.Label(box, "Version " + version, 30, TextAnchor.MiddleCenter, Palette.Muted, 0f, 0.02f, 1f, 0.09f);

            // Second step of the reset: a box over the settings with Yes / Cancel.
            Image confirm = Ui.Box(transform, "Confirm Reset", Palette.Background, 0.1f, 0.38f, 0.9f, 0.62f);
            _confirm = confirm.gameObject;
            Ui.Label(confirm.transform, "Are you sure?\nThis deletes your stars and gold.", 44, TextAnchor.MiddleCenter,
                Palette.Text, 0.05f, 0.45f, 0.95f, 0.95f);
            Name(Ui.Button(confirm.transform, "Yes, delete", Palette.Bad, ConfirmReset, 0.05f, 0.08f, 0.48f, 0.38f, 40),
                "Yes Delete");
            Name(Ui.Button(confirm.transform, "Cancel", Palette.Button, () => _confirm.SetActive(false),
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
        }

        private static void SetToggle(Button button, bool on)
        {
            Ui.SetCaption(button, on ? "ON" : "OFF");
            button.image.color = on ? Palette.Good : Palette.Button;
        }
    }
}
