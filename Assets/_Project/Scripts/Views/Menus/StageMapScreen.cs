using System;
using System.Collections.Generic;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Menus
{
    /// <summary>
    /// Chapter map (PB-13): a path through one node per stage, stars under each
    /// node, locked nodes greyed, a bouncing arrow over the stage to play next.
    /// Tapping a node opens its stage card (badges, enemy preview, Play). Node
    /// positions come from the stage assets (<see cref="StageDefinition.MapPosition"/>).
    /// </summary>
    public sealed class StageMapScreen : MonoBehaviour
    {
        public const float NodeSize = 150f;
        private const float ArrowBounce = 18f;
        private const float ArrowSpeed = 4f;
        private static readonly Color Land = Palette.Hex("2F4A3A");
        private static readonly Color PathColor = Palette.Hex("C9B47A");
        private static readonly Color Locked = Palette.Hex("4B5262");

        private GameContent _content;
        private PlayerProfile _profile;
        private Action<int> _onPlay;
        private RectTransform _map;
        private RectTransform _card;
        private RectTransform _arrow;
        private Vector2 _arrowHome;
        private readonly List<Button> _nodes = new List<Button>();

        /// <summary>Index of the stage to play next (the arrow), or -1 when everything has stars.</summary>
        public int CurrentStage { get; private set; }

        /// <summary>Index of the stage whose card is open.</summary>
        public int SelectedStage { get; private set; } = -1;

        public static StageMapScreen Create(Transform parent, GameContent content, PlayerProfile profile,
            Action<int> onPlay, Action onSettings = null)
        {
            RectTransform root = Ui.Stretch(parent, "StageMapScreen");
            var screen = root.gameObject.AddComponent<StageMapScreen>();
            screen.Build(content, profile, onPlay, onSettings);
            return screen;
        }

        /// <summary>The tappable node of a stage (tests and the tutorial use it).</summary>
        public RectTransform Node(int index) => (RectTransform)_nodes[index].transform;

        private void Build(GameContent content, PlayerProfile profile, Action<int> onPlay, Action onSettings)
        {
            _content = content;
            _profile = profile;
            _onPlay = onPlay;
            Transform root = transform;
            Ui.Box(root, "Background", Palette.Background);

            Ui.Label(root, Loc.T("Chapter 1"), 56, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.45f, 0.99f);
            Ui.Label(root, Loc.F("Gold {0}", profile.Gold), 44, TextAnchor.MiddleCenter, Palette.Text, 0.4f, 0.935f, 0.68f, 0.99f);
            if (onSettings != null)
            {
                Ui.Button(root, "Settings", Ui.ButtonStyle.Secondary, onSettings, 0.7f, 0.94f, 0.97f, 0.985f, 36);
            }

            _map = Ui.Box(root, "Map", Land, 0.03f, 0.33f, 0.97f, 0.925f).rectTransform;
            Ui.Label(_map, Loc.T("Chapter 2: Goblin Fort (coming later)"), 30, TextAnchor.UpperCenter, Palette.Muted,
                0f, 0.9f, 1f, 0.98f);

            IReadOnlyList<StageDefinition> stages = content.Campaign;
            List<string> order = content.CampaignOrder();
            CurrentStage = -1;
            for (int i = 0; i < stages.Count; i++)
            {
                if (profile.IsUnlocked(order, i) && profile.BestStars(stages[i].Id) == 0)
                {
                    CurrentStage = i;
                    break;
                }
            }

            for (int i = 0; i + 1 < stages.Count; i++)
            {
                DrawPath(stages[i].MapPosition, stages[i + 1].MapPosition, profile.IsUnlocked(order, i + 1));
            }
            for (int i = 0; i < stages.Count; i++)
            {
                _nodes.Add(DrawNode(i, stages[i], profile.IsUnlocked(order, i), profile.BestStars(stages[i].Id)));
            }
            if (CurrentStage >= 0)
            {
                _arrow = DrawArrow(stages[CurrentStage].MapPosition);
            }

            _card = Ui.Box(root, "Stage Card", Palette.Panel, 0.03f, 0.02f, 0.97f, 0.315f).rectTransform;
            int first = CurrentStage >= 0 ? CurrentStage : LastUnlocked(order);
            SelectStage(first);
        }

        private int LastUnlocked(List<string> order)
        {
            int last = 0;
            for (int i = 0; i < order.Count; i++)
            {
                if (_profile.IsUnlocked(order, i))
                {
                    last = i;
                }
            }
            return last;
        }

        private void Update()
        {
            if (_arrow != null)
            {
                _arrow.anchoredPosition = _arrowHome + Vector2.up * Mathf.Abs(Mathf.Sin(Time.unscaledTime * ArrowSpeed)) * ArrowBounce;
            }
        }

        /// <summary>Opens the card of stage <paramref name="index"/> (also for locked stages: they show why).</summary>
        public void SelectStage(int index)
        {
            IReadOnlyList<StageDefinition> stages = _content.Campaign;
            if (index < 0 || index >= stages.Count)
            {
                return;
            }
            SelectedStage = index;
            for (int i = 0; i < _nodes.Count; i++)
            {
                Outline outline = _nodes[i].GetComponent<Outline>();
                outline.enabled = i == index;
            }

            Ui.Clear(_card);
            StageDefinition stage = stages[index];
            StageDef def = stage.ToStageDef();
            bool unlocked = _profile.IsUnlocked(_content.CampaignOrder(), index);
            Ui.Label(_card, $"{index + 1}. {Loc.T(stage.DisplayName)}", 48, TextAnchor.MiddleLeft, Palette.Text,
                0.04f, 0.8f, 0.7f, 0.97f);
            AddStars(_card, _profile.BestStars(stage.Id), unlocked, 0.72f, 0.82f, 0.96f, 0.96f);
            StageBadges.AddRow(_card, def, !unlocked, 0.04f, 0.64f, 0.96f, 0.78f);
            string goal = StageBadges.GoalText(def); // PB-19
            if (goal.Length > 0)
            {
                Ui.Label(_card, goal, 30, TextAnchor.MiddleLeft, unlocked ? Palette.Gold : Palette.Muted, 0.42f, 0.52f, 0.96f, 0.62f);
            }
            new FormationGridView(_card, "Enemy Preview", true, false, 0.04f, 0.04f, 0.36f, 0.56f)
                .Show(Formation.ForStage(def));
            // After the grid, so the grid panel does not cover the tail of the caption.
            Ui.Label(_card, Loc.T("Enemy army"), 30, TextAnchor.UpperLeft, Palette.Muted, 0.04f, 0.04f, 0.3f, 0.6f);
            if (unlocked)
            {
                int play = index;
                Button button = Ui.Button(_card, "Play", Ui.ButtonStyle.Primary, () => _onPlay?.Invoke(play),
                    0.42f, 0.08f, 0.96f, 0.5f, 56);
                button.gameObject.name = "Play Button";
            }
            else
            {
                Ui.Label(_card, Loc.F("Locked: win stage {0} first.", index), 38, TextAnchor.MiddleCenter, Palette.Muted,
                    0.42f, 0.08f, 0.96f, 0.5f);
            }
        }

        /// <summary>A dotted path between two nodes, drawn as small squares.</summary>
        private void DrawPath(Vector2 from, Vector2 to, bool open)
        {
            RectTransform path = Ui.Rect(_map, "Path", 0f, 0f, 1f, 1f);
            // Dots only between the nodes' labels: from a bit after one node to a bit before the next.
            const int dots = 6;
            for (int i = 0; i < dots; i++)
            {
                Vector2 at = Vector2.Lerp(from, to, Mathf.Lerp(0.22f, 0.78f, i / (float)(dots - 1)));
                Image dot = Ui.Box(path, "Dot", open ? PathColor : Locked, at.x, at.y, at.x, at.y);
                dot.rectTransform.sizeDelta = new Vector2(22f, 22f);
                dot.sprite = GemShapes.Get(GemShape.Circle);
                dot.raycastTarget = false;
            }
        }

        private Button DrawNode(int index, StageDefinition stage, bool unlocked, int stars)
        {
            Vector2 at = stage.MapPosition;
            Color color = !unlocked ? Locked : stars > 0 ? Palette.Primary : Palette.Gold;
            Button node = Ui.Button(_map, (index + 1).ToString(), color, () => SelectStage(index), at.x, at.y, at.x, at.y, 64);
            node.gameObject.name = "Node " + (index + 1);
            var rect = (RectTransform)node.transform;
            rect.sizeDelta = new Vector2(NodeSize, NodeSize);
            node.image.sprite = GemShapes.Get(GemShape.Circle);
            Outline outline = node.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(5, 5);
            outline.enabled = false;
            if (!unlocked)
            {
                node.GetComponentInChildren<Text>().color = Palette.Muted;
            }

            // A plate behind name and stars, so the path dots never run through the text.
            Image plate = Ui.Box(_map, "Label Plate", new Color(0.08f, 0.13f, 0.1f, 0.85f),
                at.x - 0.19f, at.y - 0.135f, at.x + 0.19f, at.y - 0.055f);
            plate.raycastTarget = false;
            Ui.Label(_map, Loc.T(stage.DisplayName), 34, TextAnchor.UpperCenter, unlocked ? Palette.Text : Palette.Muted,
                at.x - 0.2f, at.y - 0.13f, at.x + 0.2f, at.y - 0.085f);
            AddStars(_map, stars, unlocked, at.x - 0.09f, at.y - 0.085f, at.x + 0.09f, at.y - 0.06f);
            return node;
        }

        /// <summary>Three star shapes, gold for each earned star.</summary>
        private static void AddStars(Transform parent, int stars, bool unlocked, float xMin, float yMin, float xMax, float yMax)
        {
            RectTransform row = Ui.Rect(parent, "Stars", xMin, yMin, xMax, yMax);
            for (int i = 0; i < 3; i++)
            {
                Image star = Ui.Box(row, "Star", i < stars ? Palette.Gold : new Color(1f, 1f, 1f, unlocked ? 0.35f : 0.15f),
                    i / 3f, 0f, (i + 1) / 3f, 1f);
                star.sprite = GemShapes.Get(GemShape.Star);
                star.preserveAspect = true;
                star.raycastTarget = false;
            }
        }

        /// <summary>A downward triangle above the node to play next; Update bounces it.</summary>
        private RectTransform DrawArrow(Vector2 at)
        {
            Image arrow = Ui.Box(_map, "Next Arrow", Color.white, at.x, at.y, at.x, at.y);
            arrow.sprite = GemShapes.Get(GemShape.Triangle);
            arrow.raycastTarget = false;
            RectTransform rect = arrow.rectTransform;
            rect.sizeDelta = new Vector2(60f, 60f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 180f);
            _arrowHome = new Vector2(0f, NodeSize * 0.5f + 50f);
            rect.anchoredPosition = _arrowHome;
            return rect;
        }
    }
}
