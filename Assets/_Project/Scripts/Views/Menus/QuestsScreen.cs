using System;
using System.Collections;
using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Quests;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// The Quests tab (PB-24, D-060): the star chest on top, then the day's quests or the achievements, each
    /// with a progress bar, its reward and a Claim button when it is finished. Claiming pays gold at once.
    /// </summary>
    public sealed class QuestsScreen : MonoBehaviour
    {
        private static readonly Color Track = new Color(1f, 1f, 1f, 0.14f);

        private GameContent _content;
        private PlayerProfile _profile;
        private QuestBoard _board;
        private Action _onClaimed;
        private RectTransform _chest;
        private RectTransform _list;
        private RectTransform _tabBar;
        private Text _goldLabel;
        private bool _showAchievements;
        private string _justClaimed = "";

        public static QuestsScreen Create(Transform parent, GameContent content, PlayerProfile profile, QuestBoard board,
            Action onClaimed, Action<MainTab> onTab)
        {
            RectTransform root = Ui.Stretch(parent, "QuestsScreen");
            var screen = root.gameObject.AddComponent<QuestsScreen>();
            screen.Build(content, profile, board, onClaimed, onTab);
            return screen;
        }

        /// <summary>The Claim button of a quest (tests press it).</summary>
        public Button ClaimButton(string questId)
        {
            Transform row = _list.Find("Quest " + questId);
            Transform button = row != null ? row.Find("Button Claim") : null;
            return button != null ? button.GetComponent<Button>() : null;
        }

        /// <summary>The button that opens a star chest (tests press it).</summary>
        public Button ChestButton
        {
            get
            {
                Transform button = _chest != null ? _chest.Find("Chest Card/Button Open chest") : null;
                return button != null ? button.GetComponent<Button>() : null;
            }
        }

        /// <summary>Switches between the day's quests and the achievements.</summary>
        public void ShowTab(bool achievements)
        {
            _showAchievements = achievements;
            RebuildList();
        }

        private QuestRules Rules => _content.Quests != null ? _content.Quests.Rules : new QuestRules();

        /// <summary>How a quest reads: "Win 3 stages".</summary>
        public static string Describe(QuestDef quest)
        {
            switch (quest.Kind)
            {
                case QuestKind.WinStages: return Loc.F("Win {0} stages", quest.Target);
                case QuestKind.UseRally: return Loc.F("Use Rally {0} times", quest.Target);
                case QuestKind.RecruitUnits: return Loc.F("Recruit {0} units", quest.Target);
                case QuestKind.MergeUnits: return Loc.F("Merge units {0} times", quest.Target);
                case QuestKind.EarnGold: return Loc.F("Earn {0} gold", quest.Target);
                default: return Loc.F("Fight with a Tier 3 unit {0} times", quest.Target);
            }
        }

        private void Build(GameContent content, PlayerProfile profile, QuestBoard board, Action onClaimed, Action<MainTab> onTab)
        {
            _content = content;
            _profile = profile;
            _board = board;
            _onClaimed = onClaimed;
            Transform root = transform;
            Ui.Backdrop(root);

            Ui.Label(root, Loc.T("Quests"), 48, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.45f, 0.99f);
            Image coin = Ui.Box(root, "Coin", Palette.Gold, 0.5f, 0.945f, 0.545f, 0.98f);
            coin.sprite = GemShapes.Get(GemShape.Circle);
            coin.preserveAspect = true;
            coin.raycastTarget = false;
            _goldLabel = Ui.Label(root, profile.Gold.ToString(), 52, TextAnchor.MiddleLeft, Palette.Text, 0.56f, 0.935f, 0.97f, 0.99f);

            _chest = Ui.Rect(root, "Star Chest", 0.03f, 0.775f, 0.97f, 0.925f);
            _list = Ui.Rect(root, "Quest List", 0f, 0f, 1f, 1f);
            Ui.Button(root, "Daily", _showAchievements ? Palette.Button : Palette.Primary, () => ShowTab(false),
                0.03f, 0.705f, 0.49f, 0.762f, 34).gameObject.name = "Tab Daily";
            Ui.Button(root, "Achievements", _showAchievements ? Palette.Primary : Palette.Button, () => ShowTab(true),
                0.51f, 0.705f, 0.97f, 0.762f, 34).gameObject.name = "Tab Achievements";
            RebuildChest();
            RebuildList();
            _tabBar = TabBar.Add(root, MainTab.Quests, onTab);
        }

        // ---- the star chest ----

        private void RebuildChest()
        {
            Ui.Clear(_chest);
            int stars = _profile.TotalStars;
            QuestRules rules = Rules;
            int ready = _board.ChestsReady(stars);
            Image card = Ui.Card(_chest, "Chest Card", ready > 0 ? Color.Lerp(Palette.Panel, Palette.Gold, 0.18f) : Palette.Panel, 0f, 0f, 1f, 1f);
            RectTransform rect = card.rectTransform;
            Image icon = Ui.Box(rect, "Star", Palette.Gold, 0.03f, 0.22f, 0.15f, 0.82f);
            icon.sprite = GemShapes.Get(GemShape.Star);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Ui.Label(rect, Loc.T("Star chest"), 40, TextAnchor.MiddleLeft, Palette.Text, 0.18f, 0.55f, 0.7f, 0.95f);
            int into = _board.StarsTowardsNextChest(stars);
            Ui.Label(rect, Loc.F("{0} stars total, {1}/{2} to the next chest", stars, into, rules.StarsPerChest), 26,
                TextAnchor.MiddleLeft, Palette.Muted, 0.18f, 0.28f, 0.72f, 0.58f);
            Image track = Ui.Box(rect, "Chest Track", Track, 0.18f, 0.1f, 0.7f, 0.24f);
            track.sprite = UiStyle.Rounded;
            track.type = Image.Type.Sliced;
            track.raycastTarget = false;
            float fill = rules.StarsPerChest > 0 ? Mathf.Clamp01((float)into / rules.StarsPerChest) : 0f;
            Image bar = Ui.Box(track.transform, "Fill", Palette.Gold, 0f, 0f, Mathf.Max(0.02f, fill), 1f);
            bar.sprite = UiStyle.Rounded;
            bar.type = Image.Type.Sliced;
            bar.raycastTarget = false;

            if (ready > 0)
            {
                Button open = Ui.Button(rect, "Open chest", Palette.Good, OpenChest, 0.74f, 0.15f, 0.97f, 0.85f, 34);
                open.GetComponentInChildren<Text>().text = Loc.T("Open chest") + (ready > 1 ? " x" + ready : "") + "\n+" + rules.ChestGold;
                UiJuice.Pulse((RectTransform)open.transform, 0.05f, 4f);
            }
            else
            {
                Ui.Label(rect, "+" + rules.ChestGold, 40, TextAnchor.MiddleCenter, Palette.Muted, 0.74f, 0.15f, 0.97f, 0.85f);
            }
        }

        private void OpenChest()
        {
            if (_board.TryClaimChest(_profile.TotalStars, out int gold))
            {
                Paid(gold);
                RebuildChest();
            }
        }

        // ---- the list ----

        private void RebuildList()
        {
            Ui.Clear(_list);
            List<QuestStatus> quests = _showAchievements ? _board.Achievements() : _board.Daily();
            float top = 0.69f;
            float bottom = TabBar.Height + 0.012f;
            float gap = 0.008f;
            float height = quests.Count == 0 ? 0.1f : Mathf.Min(0.135f, (top - bottom - gap * (quests.Count - 1)) / quests.Count);
            for (int i = 0; i < quests.Count; i++)
            {
                float yMax = top - i * (height + gap);
                DrawQuest(quests[i], yMax - height, yMax, height);
            }
            if (quests.Count == 0)
            {
                Ui.Label(_list, Loc.T("No quests yet."), 36, TextAnchor.MiddleCenter, Palette.Muted, 0.03f, top - 0.1f, 0.97f, top);
            }
            RefreshTabLook();
        }

        private void RefreshTabLook()
        {
            // The two small tabs change colour with the choice.
            foreach (string name in new[] { "Tab Daily", "Tab Achievements" })
            {
                Transform tab = transform.Find(name);
                if (tab != null)
                {
                    bool selected = name == (_showAchievements ? "Tab Achievements" : "Tab Daily");
                    tab.GetComponent<Image>().color = selected ? Palette.Primary : Palette.Button;
                }
            }
        }

        private void DrawQuest(QuestStatus status, float yMin, float yMax, float height)
        {
            QuestDef quest = status.Def;
            bool fresh = quest.Id == _justClaimed;
            Color tint = status.CanClaim ? Color.Lerp(Palette.Panel, Palette.Good, 0.2f) : status.Claimed ? Color.Lerp(Palette.Panel, Color.black, 0.2f) : Palette.Panel;
            Image card = Ui.Card(_list, "Quest " + quest.Id, tint, 0.03f, yMin, 0.97f, yMax);
            RectTransform rect = card.rectTransform;

            Ui.Label(rect, Describe(quest), height > 0.1f ? 36 : 30, TextAnchor.MiddleLeft,
                status.Claimed ? Palette.Muted : Palette.Text, 0.04f, 0.5f, 0.64f, 0.95f);

            Image track = Ui.Box(rect, "Track", Track, 0.04f, 0.14f, 0.5f, 0.38f);
            track.sprite = UiStyle.Rounded;
            track.type = Image.Type.Sliced;
            track.raycastTarget = false;
            float fraction = Mathf.Clamp01((float)status.Progress / quest.Target);
            Image fill = Ui.Box(track.transform, "Fill", status.Complete ? Palette.Good : Palette.Primary, 0f, 0f, Mathf.Max(0.04f, fraction), 1f);
            fill.sprite = UiStyle.Rounded;
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            Ui.Label(rect, status.Progress + "/" + quest.Target, 26, TextAnchor.MiddleLeft, Palette.Muted, 0.52f, 0.1f, 0.66f, 0.42f);

            Image coin = Ui.Box(rect, "Coin", Palette.Gold, 0.66f, 0.62f, 0.7f, 0.9f);
            coin.sprite = GemShapes.Get(GemShape.Circle);
            coin.preserveAspect = true;
            coin.raycastTarget = false;
            Ui.Label(rect, quest.RewardGold.ToString(), 32, TextAnchor.MiddleLeft, Palette.Gold, 0.71f, 0.5f, 0.82f, 0.95f);

            if (status.Claimed)
            {
                Ui.Label(rect, Loc.T("Claimed"), 30, TextAnchor.MiddleCenter, Palette.Muted, 0.7f, 0.1f, 0.97f, 0.9f);
            }
            else if (status.CanClaim)
            {
                string id = quest.Id;
                Button claim = Ui.Button(rect, "Claim", Palette.Good, () => Claim(id), 0.7f, 0.1f, 0.97f, 0.9f, 34);
                claim.gameObject.name = "Button Claim";
                UiJuice.Pulse((RectTransform)claim.transform, 0.04f, 4f);
            }
            if (fresh)
            {
                StartCoroutine(Flash(card));
            }
        }

        private void Claim(string questId)
        {
            if (_board.TryClaim(questId, out int gold))
            {
                _justClaimed = questId;
                Paid(gold);
                RebuildList();
                _justClaimed = "";
            }
        }

        /// <summary>Pays a reward into the profile, plays a sound and refreshes the gold, the chest and the badge.</summary>
        private void Paid(int gold)
        {
            _profile.AddGold(gold);
            Sfx.Play(_content.Sounds != null ? _content.Sounds.UnitRecruited : null, 1.1f);
            _onClaimed?.Invoke();
            StartCoroutine(CountGold(_profile.Gold));
            UpdateBadge();
        }

        private int _shownGold = -1;

        private IEnumerator CountGold(int gold)
        {
            if (_shownGold < 0)
            {
                _shownGold = gold;
            }
            int from = _shownGold;
            return Tween.Run(0.5f, elapsed =>
            {
                _shownGold = Mathf.RoundToInt(Mathf.Lerp(from, gold, Tween.EaseOut(Tween.Progress(elapsed, 0.5f))));
                if (_goldLabel != null)
                {
                    _goldLabel.text = _shownGold.ToString();
                }
            });
        }

        private void UpdateBadge()
        {
            Transform tab = _tabBar != null ? _tabBar.Find("Tab Quests") : null;
            Transform dot = tab != null ? tab.Find("Badge") : null;
            if (dot == null)
            {
                return;
            }
            int count = _board.ClaimableCount(_profile.TotalStars);
            if (count <= 0)
            {
                Destroy(dot.gameObject);
            }
            else
            {
                dot.GetComponentInChildren<Text>().text = count.ToString();
            }
        }

        private static IEnumerator Flash(Image card)
        {
            Color from = Color.Lerp(card.color, Palette.Gold, 0.5f);
            Color to = card.color;
            return Tween.Run(0.6f, elapsed =>
            {
                if (card != null)
                {
                    card.color = Color.Lerp(from, to, Tween.EaseOut(Tween.Progress(elapsed, 0.6f)));
                }
            });
        }
    }
}
