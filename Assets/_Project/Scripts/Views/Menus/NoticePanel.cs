using System;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// A message with one button over the current screen: a title, a few lines and "Close". Used when the save
    /// comes from a newer version of the game (D-067).
    /// </summary>
    public sealed class NoticePanel : MonoBehaviour
    {
        public static NoticePanel Create(Transform parent, string title, string body, Action onClose = null)
        {
            Image shade = Ui.Shade(parent, "NoticePanel", 0.7f);
            var panel = shade.gameObject.AddComponent<NoticePanel>();
            Transform box = Ui.Card(shade.transform, "Box", Palette.Panel, 0.08f, 0.33f, 0.92f, 0.67f).transform;
            Ui.Label(box, title, 52, TextAnchor.MiddleCenter, Palette.Gold, 0.05f, 0.76f, 0.95f, 0.96f);
            Ui.Label(box, body, 30, TextAnchor.MiddleCenter, Palette.Muted, 0.07f, 0.3f, 0.93f, 0.76f);
            Button close = Ui.Button(box, "Close", Ui.ButtonStyle.Primary, () =>
            {
                Destroy(shade.gameObject);
                onClose?.Invoke();
            }, 0.25f, 0.06f, 0.75f, 0.24f, 34);
            close.gameObject.name = "Close";
            return panel;
        }
    }
}
