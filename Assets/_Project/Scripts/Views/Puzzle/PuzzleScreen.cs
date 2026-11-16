using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Puzzle
{
    /// <summary>
    /// Grey-box puzzle screen: the enemy formation on top, recruitment meters,
    /// the 6×6 board. Plays the swap through <see cref="StageRun.Puzzle"/> and
    /// animates the returned steps (compute, then animate).
    /// </summary>
    public sealed class PuzzleScreen : MonoBehaviour
    {
        private const float SwapSeconds = 0.12f;
        private const float FlashSeconds = 0.14f;
        private const float ClearSeconds = 0.08f;
        private const float FillSeconds = 0.16f;
        private const float RejectSeconds = 0.2f;
        /// <summary>Idle time before a valid move is pointed out (GDD §5 hint).</summary>
        private const float HintDelaySeconds = 5f;
        private static readonly Color StoneTint = new Color(0.68f, 0.68f, 0.72f, 1f);

        private StageRun _run;
        private Action _onDone;
        private Image[,] _cells;
        private Text[,] _tags;
        private Image[,] _marks;
        private Image[,] _stoneMarks;
        private Outline[,] _outlines;
        private Text _moves;
        private Text _status;
        private Text _bonus;
        private Button _continue;
        private readonly List<(string key, RectTransform fill, Text label)> _meters =
            new List<(string, RectTransform, Text)>();
        private GridPos? _selected;
        private float _idleSeconds;
        private (GridPos a, GridPos b)? _hint;

        /// <summary>True while a move is being animated; input is ignored.</summary>
        public bool IsBusy { get; private set; }

        public static PuzzleScreen Create(Transform parent, StageRun run, Action onDone)
        {
            RectTransform root = Ui.Stretch(parent, "PuzzleScreen");
            var screen = root.gameObject.AddComponent<PuzzleScreen>();
            screen.Build(run, onDone);
            return screen;
        }

        private void Build(StageRun run, Action onDone)
        {
            _run = run;
            _onDone = onDone;
            Transform root = transform;
            Ui.Box(root, "Background", Palette.Background);

            Ui.Label(root, _run.Stage.DisplayName, 52, TextAnchor.MiddleLeft, Palette.Text, 0.04f, 0.93f, 0.7f, 0.99f);
            _moves = Ui.Label(root, "", 52, TextAnchor.MiddleRight, Palette.Gold, 0.6f, 0.93f, 0.96f, 0.99f);

            Ui.Box(root, "Enemy Panel", Palette.Panel, 0.03f, 0.75f, 0.97f, 0.925f);
            Ui.Label(root, "Enemy army", 36, TextAnchor.UpperLeft, Palette.Muted, 0.06f, 0.75f, 0.4f, 0.915f);
            StageBadges.AddPanelLine(root, _run.Stage);
            new FormationGridView(root, "Enemy Formation", true, false, 0.35f, 0.755f, 0.65f, 0.92f)
                .Show(Formation.ForStage(_run.Stage));

            BuildMeters(root);
            BuildBoard(root);

            _bonus = Ui.Label(root, "", 64, TextAnchor.MiddleCenter, Palette.Gold, 0f, 0.1f, 1f, 0.15f);
            _status = Ui.Label(root, "Swap two neighbouring gems. Tap or swipe.", 36, TextAnchor.MiddleCenter,
                Palette.Muted, 0.04f, 0.02f, 0.96f, 0.1f);
            _continue = Ui.Button(root, "To the War Camp", Palette.Primary, () => _onDone?.Invoke(),
                0.2f, 0.02f, 0.8f, 0.09f);
            _continue.gameObject.SetActive(false);

            Render(_run.Puzzle.Board.ToGrid(), _run.Puzzle.Board.ToSpecialGrid(), _run.Puzzle.Board.ToStoneGrid());
            RefreshHud();
        }

        private void BuildMeters(Transform root)
        {
            IReadOnlyList<string> keys = _run.Puzzle.GemRewardKeys;
            float width = 0.94f / keys.Count;
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                float left = 0.03f + i * width;
                Image bar = Ui.Box(root, "Meter " + key, Palette.Panel, left + 0.01f, 0.68f, left + width - 0.01f, 0.735f);
                Image fill = Ui.Box(bar.transform, "Fill", Palette.ForGem(key, StageRun.RallyKey), 0, 0, 0, 1);
                Text label = Ui.Label(bar.transform, "", 36, TextAnchor.MiddleCenter, Palette.Text);
                _meters.Add((key, fill.rectTransform, label));
            }
        }

        private void BuildBoard(Transform root)
        {
            BoardModel board = _run.Puzzle.Board;
            RectTransform area = Ui.Rect(root, "Board Area", 0.03f, 0.16f, 0.97f, 0.665f);
            RectTransform square = Ui.Stretch(area, "Board");
            var fitter = square.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)board.Width / board.Height;
            Ui.Box(square, "Board Background", Palette.Panel);

            _cells = new Image[board.Width, board.Height];
            _tags = new Text[board.Width, board.Height];
            _marks = new Image[board.Width, board.Height];
            _stoneMarks = new Image[board.Width, board.Height];
            _outlines = new Outline[board.Width, board.Height];
            float w = 1f / board.Width;
            float h = 1f / board.Height;
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    float top = 1f - y * h;
                    Image cell = Ui.Box(square, $"Gem {x},{y}", Color.clear,
                        x * w + 0.008f, top - h + 0.008f, (x + 1) * w - 0.008f, top - 0.008f);
                    cell.preserveAspect = true;
                    cell.gameObject.AddComponent<GemCell>().Init(this, new GridPos(x, y));
                    _cells[x, y] = cell;
                    _tags[x, y] = Ui.Label(cell.transform, "", 56, TextAnchor.MiddleCenter, new Color(0, 0, 0, 0.55f));
                    // Half of a stone gem is rock (D-032): drawn over the gem, so the other half keeps its color.
                    Image stone = Ui.Box(cell.transform, "Stone", StoneTint);
                    stone.preserveAspect = true;
                    stone.raycastTarget = false;
                    stone.enabled = false;
                    _stoneMarks[x, y] = stone;
                    Image mark = Ui.Box(cell.transform, "Special", Color.white);
                    mark.preserveAspect = true;
                    mark.raycastTarget = false;
                    mark.enabled = false;
                    _marks[x, y] = mark;
                    Outline outline = cell.gameObject.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(7, 7);
                    outline.enabled = false;
                    _outlines[x, y] = outline;
                }
            }
        }

        private void Update()
        {
            if (IsBusy || _run.Puzzle.IsFinished)
            {
                return;
            }
            _idleSeconds += Time.deltaTime;
            if (_hint == null && _idleSeconds >= HintDelaySeconds
                && _run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b))
            {
                _hint = (a, b);
            }
            if (_hint != null)
            {
                // Pulse the two hinted gems' outlines.
                float alpha = 0.35f + 0.65f * Mathf.PingPong(Time.time * 2f, 1f);
                foreach (GridPos cell in new[] { _hint.Value.a, _hint.Value.b })
                {
                    if (_selected == cell)
                    {
                        continue;
                    }
                    Outline outline = _outlines[cell.X, cell.Y];
                    outline.effectColor = new Color(Palette.Gold.r, Palette.Gold.g, Palette.Gold.b, alpha);
                    outline.enabled = true;
                }
            }
        }

        private void ClearHint()
        {
            _idleSeconds = 0f;
            if (_hint == null)
            {
                return;
            }
            foreach (GridPos cell in new[] { _hint.Value.a, _hint.Value.b })
            {
                _outlines[cell.X, cell.Y].enabled = _selected == cell;
                _outlines[cell.X, cell.Y].effectColor = Palette.Highlight;
            }
            _hint = null;
        }

        // --- Input (called by GemCell) -------------------------------------

        public void OnCellPressed(GridPos cell)
        {
            if (IsBusy || _run.Puzzle.IsFinished)
            {
                return;
            }
            ClearHint();
            if (_selected == null || _selected.Value == cell)
            {
                Select(_selected == null ? cell : (GridPos?)null);
            }
            else if (BoardModel.AreAdjacent(_selected.Value, cell))
            {
                GridPos from = _selected.Value;
                Select(null);
                PlaySwap(from, cell);
            }
            else
            {
                Select(cell);
            }
        }

        public void OnCellSwiped(GridPos cell, GridPos direction)
        {
            if (IsBusy || _run.Puzzle.IsFinished)
            {
                return;
            }
            Select(null);
            GridPos target = cell + direction;
            if (_run.Puzzle.Board.IsInside(target))
            {
                PlaySwap(cell, target);
            }
        }

        /// <summary>Plays a swap as if the player made it (also used by tests and the auto-play button).</summary>
        public bool PlaySwap(GridPos a, GridPos b)
        {
            if (IsBusy)
            {
                return false;
            }
            ClearHint();
            int[,] before = _run.Puzzle.Board.ToGrid();
            SpecialKind[,] beforeSpecials = _run.Puzzle.Board.ToSpecialGrid();
            bool[,] beforeStones = _run.Puzzle.Board.ToStoneGrid();
            MoveResult result = _run.Puzzle.PlaySwap(a, b);
            StartCoroutine(result.IsValid ? Animate(before, beforeSpecials, beforeStones, result) : Reject(a, b));
            return result.IsValid;
        }

        private void Select(GridPos? cell)
        {
            if (_selected != null)
            {
                _outlines[_selected.Value.X, _selected.Value.Y].enabled = false;
            }
            _selected = cell;
            if (cell != null)
            {
                Outline outline = _outlines[cell.Value.X, cell.Value.Y];
                outline.effectColor = Palette.Highlight;
                outline.enabled = true;
            }
        }

        // --- Playback ------------------------------------------------------

        private IEnumerator Animate(int[,] before, SpecialKind[,] beforeSpecials, bool[,] beforeStones, MoveResult result)
        {
            IsBusy = true;
            foreach (MoveReplay.Frame frame in MoveReplay.Frames(before, result, beforeSpecials, beforeStones))
            {
                switch (frame.Kind)
                {
                    case MoveReplay.FrameKind.Swapped:
                        Render(frame.Grid, frame.Specials, frame.Stones);
                        yield return new WaitForSeconds(SwapSeconds);
                        break;
                    case MoveReplay.FrameKind.Cleared:
                        SetMatchOutlines(frame.Step, true);
                        string special = SpecialLabel(frame.Step);
                        if (special.Length > 0)
                        {
                            _bonus.text = special;
                        }
                        else if (frame.StepIndex > 0)
                        {
                            _bonus.text = frame.StepIndex > 1 ? $"Bonus! x{frame.StepIndex}" : "Bonus!";
                        }
                        yield return new WaitForSeconds(FlashSeconds);
                        SetMatchOutlines(frame.Step, false);
                        Render(frame.Grid, frame.Specials, frame.Stones);
                        yield return new WaitForSeconds(ClearSeconds);
                        break;
                    case MoveReplay.FrameKind.Filled:
                        Render(frame.Grid, frame.Specials, frame.Stones);
                        yield return new WaitForSeconds(FillSeconds);
                        break;
                    case MoveReplay.FrameKind.Reshuffled:
                        _status.text = "No moves left: the board was reshuffled.";
                        yield return new WaitForSeconds(0.3f);
                        Render(frame.Grid, frame.Specials, frame.Stones);
                        break;
                }
            }
            _bonus.text = "";
            RefreshHud();
            IsBusy = false;
            if (_run.Puzzle.IsFinished)
            {
                ShowFinished();
            }
        }

        private IEnumerator Reject(GridPos a, GridPos b)
        {
            IsBusy = true;
            foreach (GridPos cell in new[] { a, b })
            {
                _outlines[cell.X, cell.Y].effectColor = Palette.Bad;
                _outlines[cell.X, cell.Y].enabled = true;
            }
            yield return new WaitForSeconds(RejectSeconds);
            foreach (GridPos cell in new[] { a, b })
            {
                _outlines[cell.X, cell.Y].enabled = false;
                _outlines[cell.X, cell.Y].effectColor = Palette.Highlight;
            }
            IsBusy = false;
        }

        /// <summary>Outlines the matched cells in white and whatever special gems destroyed in gold.</summary>
        private void SetMatchOutlines(MoveResult.CascadeStep step, bool on)
        {
            foreach (GridPos cell in step.Cleared)
            {
                _outlines[cell.X, cell.Y].effectColor = Palette.Gold;
                _outlines[cell.X, cell.Y].enabled = on;
            }
            foreach (MatchGroup group in step.Groups)
            {
                foreach (GridPos cell in group.Cells)
                {
                    _outlines[cell.X, cell.Y].effectColor = Palette.Highlight;
                    _outlines[cell.X, cell.Y].enabled = on;
                }
            }
        }

        private static string SpecialLabel(MoveResult.CascadeStep step)
        {
            foreach (MoveResult.SpecialTrigger trigger in step.Triggers)
            {
                if (trigger.Kind == SpecialKind.Prism)
                {
                    return "Prism!";
                }
            }
            if (step.Triggers.Count > 0)
            {
                return "Blast!";
            }
            return step.Cracked.Count > 0 ? "Stone cracked!" : "";
        }

        private void Render(int[,] grid, SpecialKind[,] specials, bool[,] stones)
        {
            IReadOnlyList<string> keys = _run.Puzzle.GemRewardKeys;
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    int gem = grid[x, y];
                    bool empty = gem == BoardModel.Empty;
                    _cells[x, y].color = empty ? Color.clear : Palette.ForGem(keys[gem], StageRun.RallyKey);
                    if (!empty)
                    {
                        _cells[x, y].sprite = GemShapes.Get(Palette.ShapeForGem(keys[gem], StageRun.RallyKey));
                    }
                    _tags[x, y].text = "";
                    SpecialKind kind = specials[x, y];
                    bool stone = !empty && stones[x, y];
                    _stoneMarks[x, y].enabled = stone;
                    if (stone)
                    {
                        _stoneMarks[x, y].sprite = GemShapes.GetStoneHalf(Palette.ShapeForGem(keys[gem], StageRun.RallyKey));
                    }
                    _marks[x, y].enabled = !empty && kind != SpecialKind.None;
                    if (_marks[x, y].enabled)
                    {
                        _marks[x, y].sprite = GemShapes.Get(kind == SpecialKind.Blast ? GemShape.Ring : GemShape.Sparkle);
                    }
                }
            }
        }

        private void RefreshHud()
        {
            _moves.text = $"Moves: {_run.Puzzle.MovesLeft}";
            foreach ((string key, RectTransform fill, Text label) in _meters)
            {
                RecruitmentMeter meter = _run.Puzzle.Meters[key];
                fill.anchorMax = new Vector2(meter.IsFull ? 1f : meter.Progress, 1f);
                string tag = Palette.GemTag(key, StageRun.RallyKey);
                label.text = key == StageRun.RallyKey
                    ? $"Rally {meter.Rewards}/{meter.MaxRewards}"
                    : $"{tag} x{meter.Rewards}";
            }
        }

        private void ShowFinished()
        {
            var summary = new StringBuilder("Army recruited: ");
            summary.Append(_run.Roster.Bench.Count).Append(" units, ")
                .Append(_run.RallyCharges).Append(" Rally");
            _status.text = summary.ToString();
            _status.rectTransform.anchorMin = new Vector2(0.04f, 0.095f);
            _status.rectTransform.anchorMax = new Vector2(0.96f, 0.155f);
            _continue.gameObject.SetActive(true);
        }
    }
}
