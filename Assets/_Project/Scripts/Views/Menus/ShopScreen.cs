using System;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// The Shop tab (PB-30, D-064): free gold for watching an ad (a few a day) and the Supporter pack, which gives
    /// every ad reward without an ad. Nothing here sells power.
    /// </summary>
    public sealed class ShopScreen : MonoBehaviour
    {
        public sealed class Actions
        {
            public Action WatchGold;
            public Action BuySupporter;
            public Action Restore;
        }

        public sealed class Info
        {
            public int Gold;
            public bool Supporter;
            public int GoldAdsLeft;
            public int GoldPerAd;
            public bool AdsReady;
            public bool BillingAvailable;
            public string Price = "";
        }

        public static ShopScreen Create(Transform parent, Info info, Actions actions, Action<MainTab> onTab)
        {
            RectTransform root = Ui.Stretch(parent, "ShopScreen");
            var screen = root.gameObject.AddComponent<ShopScreen>();
            screen.Build(info, actions, onTab);
            return screen;
        }

        public Button GoldButton => Find("Button Free gold");

        public Button BuyButton => Find("Shop Supporter/Button Buy");

        private Button Find(string path)
        {
            Transform button = transform.Find(path) ?? transform.Find("Shop Gold/" + path);
            return button != null ? button.GetComponent<Button>() : null;
        }

        private void Build(Info info, Actions actions, Action<MainTab> onTab)
        {
            Transform root = transform;
            Ui.Backdrop(root);
            Ui.Label(root, Loc.T("Shop"), 48, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.45f, 0.99f);
            Ui.Label(root, Loc.F("Gold {0}", info.Gold), 40, TextAnchor.MiddleRight, Palette.Text, 0.5f, 0.935f, 0.96f, 0.99f);

            // ---- free gold ----
            RectTransform gold = Ui.Card(root, "Shop Gold", Palette.Panel, 0.03f, 0.7f, 0.97f, 0.92f).rectTransform;
            Ui.Label(gold, Loc.F("Free gold: +{0}", info.GoldPerAd), 44, TextAnchor.MiddleLeft, Palette.Text, 0.05f, 0.55f, 0.95f, 0.95f);
            Ui.Label(gold, info.GoldAdsLeft > 0 ? Loc.F("{0} left today", info.GoldAdsLeft) : Loc.T("Come back tomorrow"), 30,
                TextAnchor.MiddleLeft, Palette.Muted, 0.05f, 0.35f, 0.5f, 0.55f);
            bool canTake = info.GoldAdsLeft > 0 && (info.Supporter || info.AdsReady);
            if (canTake)
            {
                Button take = Ui.Button(gold, info.Supporter ? "Take" : "Watch an ad", Ui.ButtonStyle.Primary, actions.WatchGold, 0.3f, 0.08f, 0.95f, 0.4f, 38);
                take.gameObject.name = "Button Free gold";
            }
            else if (info.GoldAdsLeft > 0)
            {
                Ui.Label(gold, Loc.T("No ad available right now"), 30, TextAnchor.MiddleRight, Palette.Muted, 0.4f, 0.1f, 0.95f, 0.4f);
            }

            // ---- the Supporter pack ----
            RectTransform pack = Ui.Card(root, "Shop Supporter", info.Supporter ? Color.Lerp(Palette.Panel, Palette.Good, 0.2f) : Palette.Panel,
                0.03f, 0.37f, 0.97f, 0.68f).rectTransform;
            Ui.Label(pack, Loc.T("Supporter pack"), 44, TextAnchor.MiddleLeft, Palette.Gold, 0.05f, 0.75f, 0.95f, 0.97f);
            Ui.Label(pack, Loc.T("Every ad reward comes without watching an ad. It helps us make the game. Nothing else changes."), 30,
                TextAnchor.UpperLeft, Palette.Text, 0.05f, 0.32f, 0.95f, 0.75f);
            if (info.Supporter)
            {
                Ui.Label(pack, Loc.T("Thank you! You are a Supporter."), 36, TextAnchor.MiddleCenter, Palette.Good, 0.05f, 0.04f, 0.95f, 0.3f);
            }
            else if (info.BillingAvailable)
            {
                Button buy = Ui.Button(pack, Loc.F("Buy {0}", info.Price), Ui.ButtonStyle.Primary, actions.BuySupporter, 0.05f, 0.04f, 0.5f, 0.3f, 36);
                buy.gameObject.name = "Button Buy";
                Button restore = Ui.Button(pack, "Restore", Ui.ButtonStyle.Secondary, actions.Restore, 0.55f, 0.04f, 0.95f, 0.3f, 32);
                restore.gameObject.name = "Button Restore";
            }
            else
            {
                Ui.Label(pack, Loc.T("Not available on this device yet."), 30, TextAnchor.MiddleCenter, Palette.Muted, 0.05f, 0.04f, 0.95f, 0.3f);
            }

            Ui.Label(root, Loc.T("Power is never for sale: no ad and no purchase makes your army stronger."), 28, TextAnchor.UpperCenter,
                Palette.Muted, 0.06f, 0.26f, 0.94f, 0.36f);
            TabBar.Add(root, MainTab.Shop, onTab);
        }
    }
}
