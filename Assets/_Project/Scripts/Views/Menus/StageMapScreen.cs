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
        private RectTransform _layer;
        private Text _chapterTitle;
        private int _chapter = 1;
        private bool _labels = true;
        private bool _fitted;
        private RectTransform _card;
        private RectTransform _arrow;
        private Vector2 _arrowHome;
        private readonly List<Button> _nodes = new List<Button>();

        /// <summary>Chapter whose map is showing (PB-18).</summary>
        public int Chapter => _chapter;

        /// <summary>Index of the stage to play next (the arrow), or -1 when everything has stars.</summary>
        public int CurrentStage { get; private set; }

        /// <summary>Index of the stage whose card is open.</summary>
        public int SelectedStage { get; private set; } = -1;

        public static StageMapScreen Create(Transform parent, GameContent content, PlayerProfile profile,
            Action<int> onPlay, Action onSettings = null, Action<MainTab> onTab = null)
        {
            RectTransform root = Ui.Stretch(parent, "StageMapScreen");
            var screen = root.gameObject.AddComponent<StageMapScreen>();
            screen.Build(content, profile, onPlay, onSettings, onTab);
            return screen;
        }

        /// <summary>The tappable node of a stage (tests and the tutorial use it).</summary>
        public RectTransform Node(int index) => (RectTransform)_nodes[index].transform;

        private void Build(GameContent content, PlayerProfile profile, Action<int> onPlay, Action onSettings, Action<MainTab> onTab)
        {
            _content = content;
            _profile = profile;
            _onPlay = onPlay;
            Transform root = transform;
            Ui.Backdrop(root);

            _chapterTitle = Ui.Label(root, Loc.T("Chapter 1"), 40, TextAnchor.MiddleLeft, Palette.Gold, 0.04f, 0.935f, 0.56f, 0.99f);
            _chapterTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Label(root, Loc.F("Gold {0}", profile.Gold), 40, TextAnchor.MiddleCenter, Palette.Text, 0.56f, 0.935f, 0.7f, 0.99f);
            if (onSettings != null)
            {
                Ui.Button(root, "Settings", Ui.ButtonStyle.Secondary, onSettings, 0.7f, 0.94f, 0.97f, 0.985f, 36);
            }

            _map = Ui.Card(root, "Map", Land, 0.03f, onTab != null ? 0.375f : 0.33f, 0.97f, 0.925f).rectTransform;
            _layer = Ui.Rect(_map, "Layer", 0f, 0f, 1f, 1f);

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

            _card = Ui.Card(root, "Stage Card", Palette.Panel, 0.03f, onTab != null ? TabBar.Height + 0.008f : 0.02f, 0.97f, onTab != null ? 0.36f : 0.315f).rectTransform;
            if (onTab != null)
            {
                TabBar.Add(root, MainTab.Map, onTab);
            }
            int first = stages.Count == 0 ? -1 : CurrentStage >= 0 ? CurrentStage : LastUnlocked(order);
            ShowChapter(first >= 0 ? stages[first].Chapter : 1);
            SelectStage(first);
        }

        /// <summary>First campaign index of a chapter, or -1 when the campaign has no such chapter.</summary>
        private int ChapterFirst(int chapter)
        {
            IReadOnlyList<StageDefinition> stages = _content.Campaign;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].Chapter == chapter)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>"2-3": chapter and place in the chapter, as the stage is called on the map and its card.</summary>
        private string StageLabel(int index)
        {
            StageDefinition stage = _content.Campaign[index];
            return $"{stage.Chapter}-{index - ChapterFirst(stage.Chapter) + 1}";
        }

        private static string ChapterTitle(int chapter) =>
            chapter == 1 ? Loc.T("Chapter 1")
            : chapter == 2 ? Loc.T("Chapter 2: Goblin Fort")
            : Loc.F("Chapter {0}", chapter);

        /// <summary>The chapter's painted ground behind the path and nodes, dimmed a little so the nodes read.</summary>
        private void AddPainting(int chapter)
        {
            Sprite art = ArtSprites.ChapterMap(chapter);
            if (art == null)
            {
                return;
            }
            if (_map.GetComponent<Mask>() == null)
            {
                _map.gameObject.AddComponent<Mask>().showMaskGraphic = true; // keeps the rounded corners
            }
            Image painting = Ui.Box(_layer, "Painting", Color.white);
            painting.sprite = art;
            painting.raycastTarget = false;
            Image dim = Ui.Box(_layer, "Dim", new Color(0f, 0f, 0f, 0.22f));
            dim.raycastTarget = false;
        }

        /// <summary>Draws the map of one chapter: its path, nodes, arrow and the buttons to the next and previous chapter.</summary>
        public void ShowChapter(int chapter)
        {
            _chapter = chapter;
            Ui.Clear(_layer);
            _nodes.Clear();
            _arrow = null;
            _chapterTitle.text = ChapterTitle(chapter);
            AddPainting(chapter);
            _fitted = false;
            int inChapter = 0;
            foreach (StageDefinition s in _content.Campaign)
            {
                inChapter += s.Chapter == chapter ? 1 : 0;
            }
            // A long chapter has no room for a name under every node: the names are on the stage card.
            _labels = inChapter <= 5;

            IReadOnlyList<StageDefinition> stages = _content.Campaign;
            List<string> order = _content.CampaignOrder();
            for (int i = 0; i + 1 < stages.Count; i++)
            {
                if (stages[i].Chapter == chapter && stages[i + 1].Chapter == chapter)
                {
                    DrawPath(stages[i].MapPosition, stages[i + 1].MapPosition, _profile.IsUnlocked(order, i + 1));
                }
            }
            for (int i = 0; i < stages.Count; i++)
            {
                _nodes.Add(stages[i].Chapter != chapter
                    ? null
                    : DrawNode(i, stages[i], _profile.IsUnlocked(order, i), _profile.BestStars(stages[i].Id)));
            }
            if (CurrentStage >= 0 && stages[CurrentStage].Chapter == chapter)
            {
                _arrow = DrawArrow(stages[CurrentStage].MapPosition);
            }

            if (chapter > 1 && ChapterFirst(chapter - 1) >= 0)
            {
                AddChapterButton(chapter - 1, true, 0.02f, 0.4f);
            }
            if (ChapterFirst(chapter + 1) >= 0)
            {
                AddChapterButton(chapter + 1, _profile.IsUnlocked(order, ChapterFirst(chapter + 1)), 0.44f, 0.98f);
            }
        }

        private void AddChapterButton(int chapter, bool open, float xMin, float xMax)
        {
            Button button = Ui.Button(_layer, chapter == 1 ? "Chapter 1" : "Chapter 2: Goblin Fort", Ui.ButtonStyle.Secondary,
                () => OpenChapter(chapter), xMin, 0.9f, xMax, 0.985f, 30);
            button.gameObject.name = "Chapter Button " + chapter;
            button.interactable = open;
        }

        /// <summary>Switches to a chapter's map and opens the card of the stage to play there.</summary>
        public void OpenChapter(int chapter)
        {
            int first = ChapterFirst(chapter);
            if (first < 0)
            {
                return;
            }
            ShowChapter(chapter);
            IReadOnlyList<StageDefinition> stages = _content.Campaign;
            bool current = CurrentStage >= 0 && stages[CurrentStage].Chapter == chapter;
            SelectStage(current ? CurrentStage : first);
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
            if (!_fitted && _map != null && _map.rect.height > 1f)
            {
                FitNodes();
            }
            if (_arrow != null)
            {
                _arrow.anchoredPosition = _arrowHome + Vector2.up * Mathf.Abs(Mathf.Sin(Time.unscaledTime * ArrowSpeed)) * ArrowBounce;
            }
        }

        /// <summary>On a short map (a wide window) the nodes shrink so the rows of a long chapter do not overlap.</summary>
        private void FitNodes()
        {
            _fitted = true;
            float size = Mathf.Min(NodeSize, _map.rect.height * 0.16f);
            foreach (Button node in _nodes)
            {
                if (node == null)
                {
                    continue;
                }
                ((RectTransform)node.transform).sizeDelta = new Vector2(size, size);
                Text number = node.GetComponentInChildren<Text>();
                number.resizeTextForBestFit = true;
                number.resizeTextMinSize = 16;
                number.resizeTextMaxSize = 64;
            }
            _arrowHome = new Vector2(0f, size * 0.5f + 0.33f * size);
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
            if (stages[index].Chapter != _chapter)
            {
                ShowChapter(stages[index].Chapter);
            }
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].GetComponent<Outline>().enabled = i == index;
                }
            }

            Ui.Clear(_card);
            StageDefinition stage = stages[index];
            StageDef def = stage.ToStageDef();
            bool unlocked = _profile.IsUnlocked(_content.CampaignOrder(), index);
            Ui.Label(_card, $"{StageLabel(index)}. {Loc.T(stage.DisplayName)}", 48, TextAnchor.MiddleLeft, Palette.Text,
                0.04f, 0.8f, 0.7f, 0.97f);
            AddStars(_card, _profile.BestStars(stage.Id), unlocked, 0.72f, 0.82f, 0.96f, 0.96f);
            StageBadges.AddRow(_card, def, !unlocked, 0.04f, 0.64f, 0.96f, 0.78f);
            string goal = StageBadges.GoalText(def); // PB-19
            if (goal.Length > 0)
            {
                Ui.Label(_card, goal, 30, TextAnchor.MiddleLeft, unlocked ? Palette.Gold : Palette.Muted, 0.42f, 0.52f, 0.96f, 0.62f);
            }
            new FormationGridView(_card, "Enemy Preview", true, false, 0.04f, 0.03f, 0.36f, 0.5f)
                .Show(Formation.ForStage(def));
            // After the grid, so the grid panel does not cover the tail of the caption.
            Ui.Label(_card, Loc.T("Enemy army"), 28, TextAnchor.MiddleLeft, Palette.Muted, 0.04f, 0.5f, 0.36f, 0.62f);
            if (unlocked)
            {
                int play = index;
                Button button = Ui.Button(_card, "Play", Ui.ButtonStyle.Primary, () => _onPlay?.Invoke(play),
                    0.42f, 0.08f, 0.96f, 0.5f, 56);
                button.gameObject.name = "Play Button";
            }
            else
            {
                Ui.Label(_card, Loc.F("Locked: win stage {0} first.", StageLabel(index - 1)), 38, TextAnchor.MiddleCenter, Palette.Muted,
                    0.42f, 0.08f, 0.96f, 0.5f);
            }
        }

        /// <summary>A dotted path between two nodes, drawn as small squares.</summary>
        private void DrawPath(Vector2 from, Vector2 to, bool open)
        {
            RectTransform path = Ui.Rect(_layer, "Path", 0f, 0f, 1f, 1f);
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
            Button node = Ui.Button(_layer, (index - ChapterFirst(stage.Chapter) + 1).ToString(), color, () => SelectStage(index), at.x, at.y, at.x, at.y, 64);
            node.gameObject.name = "Node " + (index + 1);
            var rect = (RectTransform)node.transform;
            rect.sizeDelta = new Vector2(NodeSize, NodeSize);
            node.image.sprite = GemShapes.Get(GemShape.Circle);
            Transform gloss = node.transform.Find("Gloss"); // the rectangular button gloss does not suit a round node
            if (gloss != null)
            {
                Destroy(gloss.gameObject);
            }
            Outline outline = node.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(5, 5);
            outline.enabled = false;
            if (!unlocked)
            {
                node.GetComponentInChildren<Text>().color = new Color(1f, 1f, 1f, 0.8f); // locked: dark node, but the number stays readable
            }

            if (!_labels)
            {
                AddStars(_layer, stars, unlocked, at.x - 0.07f, at.y - 0.105f, at.x + 0.07f, at.y - 0.075f);
                return node;
            }

            // A plate behind name and stars, so the path dots never run through the text.
            Image plate = Ui.Box(_layer, "Label Plate", new Color(0.08f, 0.13f, 0.1f, 0.85f),
                at.x - 0.19f, at.y - 0.135f, at.x + 0.19f, at.y - 0.055f);
            plate.raycastTarget = false;
            Ui.Label(_layer, Loc.T(stage.DisplayName), 34, TextAnchor.UpperCenter, unlocked ? Palette.Text : Palette.Muted,
                at.x - 0.2f, at.y - 0.13f, at.x + 0.2f, at.y - 0.085f);
            AddStars(_layer, stars, unlocked, at.x - 0.09f, at.y - 0.085f, at.x + 0.09f, at.y - 0.06f);
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
            Image arrow = Ui.Box(_layer, "Next Arrow", Color.white, at.x, at.y, at.x, at.y);
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
