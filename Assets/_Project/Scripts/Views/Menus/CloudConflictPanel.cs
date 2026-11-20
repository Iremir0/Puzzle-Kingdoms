using System;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// "Two saves found" (PB-31, D-065): this phone and the cloud each have progress the other lacks, so the player
    /// chooses. Shows stars, stages and gold of both. Nothing is lost until the player picks.
    /// </summary>
    public sealed class CloudConflictPanel : MonoBehaviour
    {
        public static CloudConflictPanel Create(Transform parent, PlayerProfile.Snapshot local, PlayerProfile.Snapshot cloud,
            Action onKeepLocal, Action onUseCloud)
        {
            Image shade = Ui.Shade(parent, "CloudConflictPanel", 0.7f);
            var panel = shade.gameObject.AddComponent<CloudConflictPanel>();
            panel.Build(local, cloud, onKeepLocal, onUseCloud);
            return panel;
        }

        private void Build(PlayerProfile.Snapshot local, PlayerProfile.Snapshot cloud, Action onKeepLocal, Action onUseCloud)
        {
            Transform box = Ui.Card(transform, "Box", Palette.Panel, 0.06f, 0.25f, 0.94f, 0.75f).transform;
            Ui.Label(box, Loc.T("Two saves found"), 52, TextAnchor.MiddleCenter, Palette.Gold, 0.05f, 0.85f, 0.95f, 0.98f);
            Ui.Label(box, Loc.T("This phone and your cloud save each have progress the other lacks. Which one do you want to keep?"), 30,
                TextAnchor.MiddleCenter, Palette.Muted, 0.06f, 0.65f, 0.94f, 0.85f);
            Describe(box, Loc.T("This phone"), local, 0.04f, 0.48f, "Keep this phone", onKeepLocal);
            Describe(box, Loc.T("Cloud save"), cloud, 0.52f, 0.96f, "Use cloud save", onUseCloud);
        }

        private static void Describe(Transform box, string title, PlayerProfile.Snapshot save, float x0, float x1, string caption, Action choose)
        {
            (int stars, int stages, int gold) = SaveConflict.Summary(save);
            RectTransform card = Ui.Card(box, "Save " + title, Palette.PanelLight, x0, 0.08f, x1, 0.62f).rectTransform;
            Ui.Label(card, title, 36, TextAnchor.MiddleCenter, Palette.Text, 0.05f, 0.78f, 0.95f, 0.98f);
            Ui.Label(card, Loc.F("{0} stars", stars) + "\n" + Loc.F("{0} stages", stages) + "\n" + Loc.F("Gold {0}", gold), 30,
                TextAnchor.MiddleCenter, Palette.Muted, 0.05f, 0.38f, 0.95f, 0.78f);
            Button button = Ui.Button(card, caption, Ui.ButtonStyle.Primary, () =>
            {
                Destroy(box.parent.gameObject);
                choose();
            }, 0.05f, 0.04f, 0.95f, 0.34f, 28);
            button.gameObject.name = "Button " + caption;
        }
    }
}
