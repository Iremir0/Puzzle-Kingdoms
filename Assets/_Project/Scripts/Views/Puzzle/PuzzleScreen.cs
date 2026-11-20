using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PuzzleKingdoms.Data;
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
    /// <remarks>
    /// Every board cell is a fixed slot (input, never moves) with a child gem
    /// image. Animations render the next frame first and then slide, pop or drop
    /// the gem images from where they were back to their slots (PB-7).
    /// </remarks>
    public sealed class PuzzleScreen : MonoBehaviour
    {
        /// <summary>Multiplies every animation time below: 1 = as designed, 0.5 = twice as fast.</summary>
        private const float DurationScale = 1f;
        private const float SwapSeconds = 0.15f * DurationScale;
        /// <summary>Cleared gems grow to <see cref="PopScale"/> in the first part, then shrink and fade.</summary>
        private const float ClearSeconds = 0.2f * DurationScale;
        private const float PopShare = 0.3f;
        private const float PopScale = 1.1f;
        private const float FallSecondsPerCell = 0.06f * DurationScale;
        private const float LandingSeconds = 0.08f * DurationScale;
        /// <summary>Landing bounce height, in cells.</summary>
        private const float LandingBounce = 0.08f;
        private const float ReshuffleSeconds = 0.4f * DurationScale;
        private const float ReshuffleNoticeSeconds = 0.25f * DurationScale;
        private const float RejectSeconds = 0.25f * DurationScale;
        /// <summary>How far the two gems of an invalid swap lean towards each other, in cells.</summary>
        private const float RejectReach = 0.4f;
        /// <summary>Sideways shake of an invalid swap, in cells.</summary>
        private const float RejectShake = 0.06f;
        private const float SnapBackSeconds = 0.1f * DurationScale;
        /// <summary>Idle time before a valid move is pointed out (GDD §5 hint).</summary>
        private const float HintDelaySeconds = 5f;
        /// <summary>The hinted gems pulse between 100% and 100% + this.</summary>
        private const float HintPulse = 0.08f;
        private static readonly Color StoneTint = new Color(0.68f, 0.68f, 0.72f, 1f);
        /// <summary>PB-20: ice on a cell (one layer, two layers) and the bars of a chain.</summary>
        private static readonly Color IceOneLayer = new Color(0.62f, 0.85f, 0.96f, 0.35f);
        private static readonly Color IceTwoLayers = new Color(0.72f, 0.9f, 1f, 0.62f);
        private static readonly Color ChainTint = new Color(0.84f, 0.86f, 0.9f, 0.95f);
        private const float BossShake = 12f;
        /// <summary>Board shake in reference pixels (PB-8): Blast small, Prism bigger.</summary>
        private const float BlastShake = 10f;
        private const float PrismShake = 16f;
        private const float BlastShakeSeconds = 0.15f;
        private const float PrismShakeSeconds = 0.2f;

        private StageRun _run;
        private Action _onDone;
        private RectTransform _board;
        private BoardEffects _effects;
        private Button _sound;
        private readonly Dictionary<string, int> _rewardsHeard = new Dictionary<string, int>();
        private Image[,] _gems;
        private CanvasGroup[,] _gemAlpha;
        private Image[,] _marks;
        private Image[,] _stoneMarks;
        private Image[,] _iceTiles;      // PB-20: on the cell, behind the gem; ice never moves
        private GameObject[,] _chainMarks; // PB-20: on the gem, so a chain falls with it
        private Text _boss;              // PB-20: the boss countdown, null without a boss
        private Outline[,] _outlines;
        private int[,] _shownGrid;
        private SpecialKind[,] _shownSpecials;
        private bool[,] _shownStones;
        private Text _moves;
        private Text _goal; // PB-19: the stage goal and its progress, null without one
        private Text _status;
        private Text _bonus;
        private Button _continue;
        private Button _adMoves;
        private AdOffer _extraMoves; // PB-30: a rewarded ad for more moves when the puzzle has ended
        private readonly List<(string key, RectTransform fill, Text label)> _meters =
            new List<(string, RectTransform, Text)>();
        private GridPos? _selected;
        private float _idleSeconds;
        private (GridPos a, GridPos b)? _hint;

        /// <summary>True while a move is being animated; input is ignored.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>True when a touch may start a swap or drag a gem.</summary>
        public bool AcceptsInput => !IsBusy && !_run.Puzzle.IsFinished;

        /// <summary>Copies of what the board shows now (for tests: must equal the logic board once idle).</summary>
        public int[,] ShownGrid => (int[,])_shownGrid.Clone();
        public SpecialKind[,] ShownSpecials => (SpecialKind[,])_shownSpecials.Clone();
        public bool[,] ShownStones => (bool[,])_shownStones.Clone();

        /// <summary>The effect layer over the board (tests check that effects clean up).</summary>
        public BoardEffects Effects => _effects;

        /// <summary>True when every gem image sits in its own cell at full size and alpha.</summary>
        public bool GemsAtRest
        {
            get
            {
                for (int x = 0; x < _gems.GetLength(0); x++)
                {
                    for (int y = 0; y < _gems.GetLength(1); y++)
                    {
                        RectTransform gem = _gems[x, y].rectTransform;
                        if (gem.anchoredPosition.sqrMagnitude > 0.01f || Mathf.Abs(gem.localScale.x - 1f) > 0.001f
                            || Mathf.Abs(_gemAlpha[x, y].alpha - 1f) > 0.001f)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        public static PuzzleScreen Create(Transform parent, StageRun run, Action onDone, AdOffer extraMoves = null)
        {
            RectTransform root = Ui.Stretch(parent, "PuzzleScreen");
            var screen = root.gameObject.AddComponent<PuzzleScreen>();
            screen._extraMoves = extraMoves;
            screen.Build(run, onDone);
            return screen;
        }

        private void Build(StageRun run, Action onDone)
        {
            _run = run;
            _onDone = onDone;
            Transform root = transform;
            Ui.Backdrop(root);

            Ui.Label(root, Loc.T(_run.Stage.DisplayName), 52, TextAnchor.MiddleLeft, Palette.Text, 0.04f, 0.93f, 0.7f, 0.99f);
            _moves = Ui.Label(root, "", 52, TextAnchor.MiddleRight, Palette.Gold, 0.6f, 0.93f, 0.96f, 0.99f);

            Ui.Card(root, "Enemy Panel", Palette.Panel, 0.03f, 0.75f, 0.97f, 0.925f);
            Ui.Label(root, Loc.T("Enemy army"), 36, TextAnchor.UpperLeft, Palette.Muted, 0.06f, 0.75f, 0.4f, 0.915f);
            StageBadges.AddPanelLine(root, _run.Stage);
            _goal = StageBadges.AddGoalLine(root, _run.Stage, 0);
            if (_run.Puzzle.MovesUntilBossStones >= 0)
            {
                // PB-20: the boss countdown, under the goal in the enemy panel.
                _boss = Ui.Label(root, "", 30, TextAnchor.LowerRight, Palette.Bad, 0.66f, 0.76f, 0.95f, 0.84f);
                _boss.gameObject.name = "Boss";
            }
            new FormationGridView(root, "Enemy Formation", true, false, 0.35f, 0.755f, 0.65f, 0.92f)
                .Show(Formation.ForStage(_run.Stage));

            BuildMeters(root);
            BuildBoard(root);

            _bonus = Ui.Label(root, "", 64, TextAnchor.MiddleCenter, Palette.Gold, 0f, 0.1f, 1f, 0.15f);
            _status = Ui.Label(root, Loc.T("Swap two neighbouring gems. Tap or swipe."), 36, TextAnchor.MiddleCenter,
                Palette.Muted, 0.04f, 0.02f, 0.82f, 0.1f);
            _sound = Ui.Button(root, "", Ui.ButtonStyle.Secondary, ToggleSound, 0.84f, 0.03f, 0.97f, 0.09f, 28);
            RefreshSoundButton();
            _continue = Ui.Button(root, "To the War Camp", Ui.ButtonStyle.Primary, () => _onDone?.Invoke(),
                0.2f, 0.02f, 0.8f, 0.09f);
            _continue.gameObject.SetActive(false);

            Render(_run.Puzzle.Board.ToGrid(), _run.Puzzle.Board.ToSpecialGrid(), _run.Puzzle.Board.ToStoneGrid(),
                _run.Puzzle.Board.ToChainGrid(), _run.Puzzle.Board.ToIceGrid());
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
                Image bar = Ui.Card(root, "Meter " + key, Palette.Panel, left + 0.01f, 0.68f, left + width - 0.01f, 0.735f);
                Image fill = Ui.Box(bar.transform, "Fill", Palette.ForGem(key, StageRun.RallyKey), 0, 0, 0, 1);
                Text label = Ui.Label(bar.transform, "", 36, TextAnchor.MiddleCenter, Palette.Text);
                _meters.Add((key, fill.rectTransform, label));
                _rewardsHeard[key] = _run.Puzzle.Meters[key].Rewards;
            }
        }

        private void BuildBoard(Transform root)
        {
            BoardModel board = _run.Puzzle.Board;
            RectTransform area = Ui.Rect(root, "Board Area", 0.03f, 0.16f, 0.97f, 0.665f);
            // The board shakes inside this rect; the area itself never moves.
            RectTransform shaker = Ui.Stretch(area, "Board Shake");
            _board = Ui.Stretch(shaker, "Board");
            var fitter = _board.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)board.Width / board.Height;
            // New gems start above the board and fall in: hide them until they enter.
            _board.gameObject.AddComponent<RectMask2D>();
            Ui.Card(_board, "Board Background", Palette.Panel);

            _gems = new Image[board.Width, board.Height];
            _gemAlpha = new CanvasGroup[board.Width, board.Height];
            _marks = new Image[board.Width, board.Height];
            _stoneMarks = new Image[board.Width, board.Height];
            _iceTiles = new Image[board.Width, board.Height];
            _chainMarks = new GameObject[board.Width, board.Height];
            _outlines = new Outline[board.Width, board.Height];
            float w = 1f / board.Width;
            float h = 1f / board.Height;
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    float top = 1f - y * h;
                    // The slot takes the touches and never moves; its gem child is what animates.
                    Image slot = Ui.Box(_board, $"Cell {x},{y}", Color.clear,
                        x * w + 0.008f, top - h + 0.008f, (x + 1) * w - 0.008f, top - 0.008f);
                    // Ice (PB-20) is the cell's floor: created first so it sits behind the gem.
                    Image ice = Ui.Box(slot.transform, "Ice", IceOneLayer);
                    ice.raycastTarget = false;
                    ice.enabled = false;
                    _iceTiles[x, y] = ice;
                    Image gem = Ui.Box(slot.transform, "Gem", Color.clear);
                    gem.preserveAspect = true;
                    gem.raycastTarget = false;
                    _gems[x, y] = gem;
                    _gemAlpha[x, y] = gem.gameObject.AddComponent<CanvasGroup>();
                    slot.gameObject.AddComponent<GemCell>().Init(this, new GridPos(x, y), gem.rectTransform);
                    // Half of a stone gem is rock (D-032): drawn over the gem, so the other half keeps its color.
                    Image stone = Ui.Box(gem.transform, "Stone", StoneTint);
                    stone.preserveAspect = true;
                    stone.raycastTarget = false;
                    stone.enabled = false;
                    _stoneMarks[x, y] = stone;
                    // A chain (PB-20): two crossed bars over the gem, travelling with it.
                    RectTransform chain = Ui.Stretch(gem.transform, "Chain");
                    for (int i = 0; i < 2; i++)
                    {
                        Image bar = Ui.Box(chain, "Bar", ChainTint, 0.06f, 0.44f, 0.94f, 0.56f);
                        bar.raycastTarget = false;
                        bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
                    }
                    chain.gameObject.SetActive(false);
                    _chainMarks[x, y] = chain.gameObject;
                    Image mark = Ui.Box(gem.transform, "Special", Color.white);
                    mark.preserveAspect = true;
                    mark.raycastTarget = false;
                    mark.enabled = false;
                    _marks[x, y] = mark;
                    Outline outline = gem.gameObject.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(7, 7);
                    outline.enabled = false;
                    _outlines[x, y] = outline;
                }
            }
            _effects = BoardEffects.Create(_board, shaker, board.Width, board.Height);
        }

        private void Update()
        {
            if (!AcceptsInput)
            {
                return;
            }
            _idleSeconds += Time.deltaTime;
            if (_hint == null && _idleSeconds >= HintDelaySeconds)
            {
                if (_run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b))
                {
                    _hint = (a, b);
                }
                else
                {
                    // No move to show: look again in a second instead of scanning the whole board every frame (PB-15).
                    _idleSeconds = HintDelaySeconds - 1f;
                }
            }
            if (_hint != null)
            {
                // The two hinted gems breathe gently.
                float scale = 1f + HintPulse * Mathf.SmoothStep(0f, 1f, Mathf.PingPong(Time.time * 1.6f, 1f));
                Gem(_hint.Value.a).localScale = Vector3.one * scale;
                Gem(_hint.Value.b).localScale = Vector3.one * scale;
            }
        }

        private void ClearHint()
        {
            _idleSeconds = 0f;
            if (_hint == null)
            {
                return;
            }
            Gem(_hint.Value.a).localScale = Vector3.one;
            Gem(_hint.Value.b).localScale = Vector3.one;
            _hint = null;
        }

        // --- Input (called by GemCell) -------------------------------------

        public void OnCellPressed(GridPos cell)
        {
            if (!AcceptsInput)
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
            if (!AcceptsInput)
            {
                return;
            }
            Select(null);
            GridPos target = cell + direction;
            if (_run.Puzzle.Board.IsInside(target))
            {
                PlaySwap(cell, target);
            }
            else
            {
                StartCoroutine(Tween.Move(Gem(cell), Vector2.zero, SnapBackSeconds));
            }
        }

        /// <summary>Holds a gem away from its cell while the finger drags it (before the swipe counts).</summary>
        public void DragGem(GridPos cell, Vector2 offset)
        {
            if (AcceptsInput)
            {
                ClearHint();
                Gem(cell).anchoredPosition = offset;
            }
        }

        /// <summary>The finger let go before the swipe counted: the gem slides back.</summary>
        public void ReleaseGem(GridPos cell)
        {
            if (AcceptsInput && Gem(cell).anchoredPosition != Vector2.zero)
            {
                StartCoroutine(Tween.Move(Gem(cell), Vector2.zero, SnapBackSeconds));
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
            // Only snap-backs can be running here; the swap takes over every gem.
            StopAllCoroutines();
            ParkAllExcept(a, b);
            int[,] before = _run.Puzzle.Board.ToGrid();
            SpecialKind[,] beforeSpecials = _run.Puzzle.Board.ToSpecialGrid();
            bool[,] beforeStones = _run.Puzzle.Board.ToStoneGrid();
            bool[,] beforeChains = _run.Puzzle.Board.ToChainGrid();
            int[,] beforeIce = _run.Puzzle.Board.ToIceGrid();
            MoveResult result = _run.Puzzle.PlaySwap(a, b);
            IsBusy = true;
            StartCoroutine(result.IsValid ? Animate(before, beforeSpecials, beforeStones, beforeChains, beforeIce, result) : Reject(a, b));
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

        private IEnumerator Animate(int[,] before, SpecialKind[,] beforeSpecials, bool[,] beforeStones, bool[,] beforeChains,
            int[,] beforeIce, MoveResult result)
        {
            IsBusy = true;
            foreach (MoveReplay.Frame frame in MoveReplay.Frames(before, result, beforeSpecials, beforeStones, beforeChains, beforeIce))
            {
                switch (frame.Kind)
                {
                    case MoveReplay.FrameKind.Swapped:
                    {
                        GridPos a = result.SwapA;
                        GridPos b = result.SwapB;
                        // Start from wherever the finger left the gems.
                        Vector2 heldA = Gem(a).anchoredPosition;
                        Vector2 heldB = Gem(b).anchoredPosition;
                        Render(frame.Grid, frame.Specials, frame.Stones, frame.Chains, frame.Ice);
                        var moves = new List<(GridPos, Vector2)> { (a, Offset(b, a) + heldB), (b, Offset(a, b) + heldA) };
                        yield return SlideHome(moves, SwapSeconds);
                        break;
                    }
                    case MoveReplay.FrameKind.Cleared:
                        SetMatchOutlines(frame.Step, true);
                        string special = SpecialLabel(frame.Step);
                        if (special.Length > 0)
                        {
                            _bonus.text = special;
                        }
                        else if (frame.StepIndex > 0)
                        {
                            _bonus.text = frame.StepIndex > 1 ? Loc.F("Bonus! x{0}", frame.StepIndex) : Loc.T("Bonus!");
                        }
                        PlayStepFeedback(frame.Step, frame.StepIndex);
                        yield return Pop(frame.Step.Cleared);
                        SetMatchOutlines(frame.Step, false);
                        Render(frame.Grid, frame.Specials, frame.Stones, frame.Chains, frame.Ice);
                        foreach (GridPos cell in frame.Step.Cleared)
                        {
                            Park(cell);
                        }
                        PlayCreated(frame.Step);
                        break;
                    case MoveReplay.FrameKind.Filled:
                        Render(frame.Grid, frame.Specials, frame.Stones, frame.Chains, frame.Ice);
                        yield return Drop(frame.Step);
                        break;
                    case MoveReplay.FrameKind.Reshuffled:
                        _status.text = Loc.T("No moves left: the board was reshuffled.");
                        yield return new WaitForSeconds(ReshuffleNoticeSeconds);
                        List<(GridPos, Vector2)> flights = ReshuffleFlights(frame.Grid);
                        Render(frame.Grid, frame.Specials, frame.Stones, frame.Chains, frame.Ice);
                        yield return SlideHome(flights, ReshuffleSeconds);
                        break;
                    case MoveReplay.FrameKind.StonesDropped:
                        // PB-20: the boss throws stones once the move has settled.
                        _bonus.text = Loc.T("The boss throws stones!");
                        Render(frame.Grid, frame.Specials, frame.Stones, frame.Chains, frame.Ice);
                        _effects.Shake(BossShake, BlastShakeSeconds);
                        PlaySound(b => b.StoneCrack, 0.8f);
                        yield return new WaitForSeconds(ReshuffleNoticeSeconds);
                        break;
                }
            }
            _bonus.text = "";
            PlayRecruitSounds();
            RefreshHud();
            IsBusy = false;
            if (_run.Puzzle.IsFinished)
            {
                ShowFinished();
            }
        }

        /// <summary>Invalid swap: both gems lean towards each other and come back with a short shake.</summary>
        private IEnumerator Reject(GridPos a, GridPos b)
        {
            IsBusy = true;
            PlaySound(bank => bank.InvalidSwap);
            foreach (GridPos cell in new[] { a, b })
            {
                _outlines[cell.X, cell.Y].effectColor = Palette.Bad;
                _outlines[cell.X, cell.Y].enabled = true;
            }
            Vector2 heldA = Gem(a).anchoredPosition;
            Vector2 heldB = Gem(b).anchoredPosition;
            Vector2 towardsB = Offset(b, a) * RejectReach;
            float shake = Mathf.Abs(CellStep.x) * RejectShake;
            yield return Tween.Run(RejectSeconds, elapsed =>
            {
                float t = Tween.Progress(elapsed, RejectSeconds);
                float settle = 1f - Tween.EaseOut(t);
                float lean = Tween.Hump(t);
                var wobble = new Vector2(Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * shake, 0f);
                Gem(a).anchoredPosition = heldA * settle + towardsB * lean + wobble;
                Gem(b).anchoredPosition = heldB * settle - towardsB * lean + wobble;
            });
            foreach (GridPos cell in new[] { a, b })
            {
                Park(cell);
                _outlines[cell.X, cell.Y].enabled = false;
                _outlines[cell.X, cell.Y].effectColor = Palette.Highlight;
            }
            IsBusy = false;
        }

        // --- Sound and effects (PB-8) -------------------------------------

        private static void PlaySound(Func<SoundBank, AudioClip> pick, float pitch = 1f)
        {
            SoundBank bank = Sfx.Bank;
            if (bank != null)
            {
                Sfx.Play(pick(bank), pitch);
            }
        }

        /// <summary>
        /// What a cascade wave sounds and looks like as it clears: the match pop
        /// (higher with every wave), Blast rings, Prism beams, stone chips and a shake.
        /// </summary>
        private void PlayStepFeedback(MoveResult.CascadeStep step, int stepIndex)
        {
            if (step.Cleared.Count > 0)
            {
                SoundBank bank = Sfx.Bank;
                PlaySound(b => b.Match, bank != null ? bank.CascadePitch(stepIndex) : 1f);
            }
            bool blast = false;
            bool prism = false;
            foreach (MoveResult.SpecialTrigger trigger in step.Triggers)
            {
                if (trigger.Kind == SpecialKind.Prism)
                {
                    prism = true;
                    _effects.Beams(trigger.Origin, PrismTargets(step, trigger.Origin));
                }
                else
                {
                    blast = true;
                    _effects.Ring(trigger.Origin, Palette.Gold);
                }
            }
            if (blast)
            {
                PlaySound(b => b.BlastFired);
            }
            if (prism)
            {
                PlaySound(b => b.PrismFired);
                _effects.Shake(PrismShake, PrismShakeSeconds);
            }
            else if (blast)
            {
                _effects.Shake(BlastShake, BlastShakeSeconds);
            }
            foreach (GridPos cell in step.Cracked)
            {
                _effects.Chips(cell);
            }
            if (step.Cracked.Count > 0)
            {
                PlaySound(b => b.StoneCrack);
            }
        }

        /// <summary>
        /// The gems a Prism took: cleared this wave, not part of a normal match,
        /// and not inside a Blast's 3×3 area.
        /// </summary>
        private static List<GridPos> PrismTargets(MoveResult.CascadeStep step, GridPos origin)
        {
            var matched = new HashSet<GridPos>();
            foreach (MatchGroup group in step.Groups)
            {
                matched.UnionWith(group.Cells);
            }
            var targets = new List<GridPos>();
            foreach (GridPos cell in step.Cleared)
            {
                if (cell == origin || matched.Contains(cell))
                {
                    continue;
                }
                bool inBlast = false;
                foreach (MoveResult.SpecialTrigger trigger in step.Triggers)
                {
                    if (trigger.Kind == SpecialKind.Blast && Mathf.Abs(trigger.Origin.X - cell.X) <= 1
                        && Mathf.Abs(trigger.Origin.Y - cell.Y) <= 1)
                    {
                        inBlast = true;
                    }
                }
                if (!inBlast)
                {
                    targets.Add(cell);
                }
            }
            return targets;
        }

        /// <summary>A special gem made this wave flashes and makes its own sound.</summary>
        private void PlayCreated(MoveResult.CascadeStep step)
        {
            foreach (MoveResult.SpecialCreated created in step.Created)
            {
                _effects.Flash(created.Cell, _gems[created.Cell.X, created.Cell.Y].sprite);
                if (created.Kind == SpecialKind.Prism)
                {
                    PlaySound(b => b.PrismCreated);
                }
                else
                {
                    PlaySound(b => b.BlastCreated);
                }
            }
        }

        /// <summary>A ding when a meter gave a unit this move, a horn when it gave a Rally charge.</summary>
        private void PlayRecruitSounds()
        {
            bool unit = false;
            bool rally = false;
            foreach ((string key, RectTransform _, Text _) in _meters)
            {
                int rewards = _run.Puzzle.Meters[key].Rewards;
                if (rewards > _rewardsHeard[key])
                {
                    if (key == StageRun.RallyKey)
                    {
                        rally = true;
                    }
                    else
                    {
                        unit = true;
                    }
                }
                _rewardsHeard[key] = rewards;
            }
            if (unit)
            {
                PlaySound(b => b.UnitRecruited);
            }
            if (rally)
            {
                PlaySound(b => b.RallyCharge);
            }
        }

        private void ToggleSound()
        {
            Sfx.Muted = !Sfx.Muted;
            RefreshSoundButton();
        }

        private void RefreshSoundButton() => Ui.SetCaption(_sound, Sfx.Muted ? "Muted" : "Sound");

        /// <summary>Gems start at the given offsets and glide back into their cells together.</summary>
        private IEnumerator SlideHome(List<(GridPos cell, Vector2 start)> moves, float seconds)
        {
            yield return Tween.Run(seconds, elapsed =>
            {
                float left = 1f - Tween.EaseInOut(Tween.Progress(elapsed, seconds));
                foreach ((GridPos cell, Vector2 start) in moves)
                {
                    Gem(cell).anchoredPosition = start * left;
                }
            });
        }

        /// <summary>Cleared gems swell a little, then shrink away and fade.</summary>
        private IEnumerator Pop(List<GridPos> cleared)
        {
            yield return Tween.Run(ClearSeconds, elapsed =>
            {
                float t = Tween.Progress(elapsed, ClearSeconds);
                float scale;
                float alpha = 1f;
                if (t < PopShare)
                {
                    scale = Mathf.Lerp(1f, PopScale, Tween.EaseOut(t / PopShare));
                }
                else
                {
                    float shrink = Tween.EaseIn((t - PopShare) / (1f - PopShare));
                    scale = Mathf.Lerp(PopScale, 0f, shrink);
                    alpha = 1f - shrink;
                }
                foreach (GridPos cell in cleared)
                {
                    Gem(cell).localScale = Vector3.one * scale;
                    _gemAlpha[cell.X, cell.Y].alpha = alpha;
                }
            });
        }

        /// <summary>
        /// Gems fall from their old cells (or from above the board for new ones),
        /// faster the further they fall, and bounce a little on landing.
        /// </summary>
        private IEnumerator Drop(MoveResult.CascadeStep step)
        {
            var falls = new List<(GridPos cell, Vector2 start, float seconds)>();
            foreach (MoveResult.GemFall fall in step.Falls)
            {
                falls.Add((fall.To, Offset(fall.From, fall.To), (fall.To.Y - fall.From.Y) * FallSecondsPerCell));
            }
            foreach (MoveResult.GemSpawn spawn in step.Spawns)
            {
                var above = new GridPos(spawn.Cell.X, spawn.Cell.Y - spawn.DropDistance);
                falls.Add((spawn.Cell, Offset(above, spawn.Cell), spawn.DropDistance * FallSecondsPerCell));
            }
            float total = 0f;
            foreach ((GridPos _, Vector2 _, float seconds) in falls)
            {
                total = Mathf.Max(total, seconds + LandingSeconds);
            }
            float bounce = Mathf.Abs(CellStep.y) * LandingBounce;
            yield return Tween.Run(total, elapsed =>
            {
                foreach ((GridPos cell, Vector2 start, float seconds) in falls)
                {
                    RectTransform gem = Gem(cell);
                    if (elapsed < seconds)
                    {
                        gem.anchoredPosition = start * (1f - Tween.EaseIn(elapsed / seconds));
                    }
                    else
                    {
                        float landed = Tween.Progress(elapsed - seconds, LandingSeconds);
                        gem.anchoredPosition = landed < 1f ? new Vector2(0f, bounce * Tween.Hump(landed)) : Vector2.zero;
                    }
                }
            });
        }

        /// <summary>
        /// The logic only reports the new board, so each gem flies from some old
        /// cell of the same colour (first come, first served).
        /// </summary>
        private List<(GridPos, Vector2)> ReshuffleFlights(int[,] after)
        {
            var oldCells = new Dictionary<int, Queue<GridPos>>();
            for (int y = 0; y < _shownGrid.GetLength(1); y++)
            {
                for (int x = 0; x < _shownGrid.GetLength(0); x++)
                {
                    int gem = _shownGrid[x, y];
                    if (!oldCells.TryGetValue(gem, out Queue<GridPos> queue))
                    {
                        oldCells[gem] = queue = new Queue<GridPos>();
                    }
                    queue.Enqueue(new GridPos(x, y));
                }
            }
            var flights = new List<(GridPos, Vector2)>();
            for (int y = 0; y < after.GetLength(1); y++)
            {
                for (int x = 0; x < after.GetLength(0); x++)
                {
                    var cell = new GridPos(x, y);
                    if (oldCells.TryGetValue(after[x, y], out Queue<GridPos> queue) && queue.Count > 0)
                    {
                        flights.Add((cell, Offset(queue.Dequeue(), cell)));
                    }
                }
            }
            return flights;
        }

        /// <summary>Distance between two cells in the board's local units, per cell (board y grows downwards).</summary>
        private Vector2 CellStep
        {
            get
            {
                Rect rect = _board.rect;
                return new Vector2(rect.width / _gems.GetLength(0), -rect.height / _gems.GetLength(1));
            }
        }

        /// <summary>Offset that draws the gem of cell <paramref name="to"/> on top of cell <paramref name="from"/>.</summary>
        private Vector2 Offset(GridPos from, GridPos to)
        {
            Vector2 step = CellStep;
            return new Vector2((from.X - to.X) * step.x, (from.Y - to.Y) * step.y);
        }

        private RectTransform Gem(GridPos cell) => _gems[cell.X, cell.Y].rectTransform;

        /// <summary>Puts a gem image back in its cell at full size.</summary>
        private void Park(GridPos cell)
        {
            RectTransform gem = Gem(cell);
            gem.anchoredPosition = Vector2.zero;
            gem.localScale = Vector3.one;
            _gemAlpha[cell.X, cell.Y].alpha = 1f;
        }

        private void ParkAllExcept(GridPos a, GridPos b)
        {
            for (int x = 0; x < _gems.GetLength(0); x++)
            {
                for (int y = 0; y < _gems.GetLength(1); y++)
                {
                    var cell = new GridPos(x, y);
                    if (cell != a && cell != b)
                    {
                        Park(cell);
                    }
                }
            }
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
            switch (step.Combo) // PB-20: two special gems swapped together
            {
                case ComboKind.BlastBlast: return Loc.T("Blast + Blast!");
                case ComboKind.PrismBlast: return Loc.T("Prism + Blast!");
                case ComboKind.PrismPrism: return Loc.T("Prism + Prism!");
            }
            foreach (MoveResult.SpecialTrigger trigger in step.Triggers)
            {
                if (trigger.Kind == SpecialKind.Prism)
                {
                    return Loc.T("Prism!");
                }
            }
            if (step.Triggers.Count > 0)
            {
                return Loc.T("Blast!");
            }
            return step.Cracked.Count > 0 ? Loc.T("Stone cracked!") : "";
        }

        private void Render(int[,] grid, SpecialKind[,] specials, bool[,] stones, bool[,] chains = null, int[,] ice = null)
        {
            _shownGrid = (int[,])grid.Clone();
            _shownSpecials = (SpecialKind[,])specials.Clone();
            _shownStones = (bool[,])stones.Clone();
            IReadOnlyList<string> keys = _run.Puzzle.GemRewardKeys;
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    int gem = grid[x, y];
                    bool empty = gem == BoardModel.Empty;
                    _gems[x, y].color = empty ? Color.clear : Palette.ForGem(keys[gem], StageRun.RallyKey);
                    if (!empty)
                    {
                        _gems[x, y].sprite = GemShapes.Get(Palette.ShapeForGem(keys[gem], StageRun.RallyKey));
                    }
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
                    // PB-20: a chain on the gem, ice on the cell.
                    _chainMarks[x, y].SetActive(!empty && chains != null && chains[x, y]);
                    int layers = ice == null ? 0 : ice[x, y];
                    _iceTiles[x, y].enabled = layers > 0;
                    _iceTiles[x, y].color = layers >= 2 ? IceTwoLayers : IceOneLayer;
                }
            }
        }

        private void RefreshHud()
        {
            _moves.text = Loc.F("Moves: {0}", _run.Puzzle.MovesLeft);
            if (_goal != null)
            {
                _goal.text = StageBadges.GoalText(_run.Stage, _run.Puzzle.StonesBroken,
                    _run.Puzzle.IceAtStart - _run.Puzzle.Board.IceLeft, _run.Puzzle.IceAtStart);
            }
            if (_boss != null)
            {
                _boss.text = Loc.F("Stones fall in {0}", _run.Puzzle.MovesUntilBossStones);
            }
            foreach ((string key, RectTransform fill, Text label) in _meters)
            {
                RecruitmentMeter meter = _run.Puzzle.Meters[key];
                fill.anchorMax = new Vector2(meter.IsFull ? 1f : meter.Progress, 1f);
                // The one-letter class tag ("W") is translated as a text of its own ("S" for Savaşçı).
                string tag = Loc.T(Palette.GemTag(key, StageRun.RallyKey));
                label.text = key == StageRun.RallyKey
                    ? Loc.F("Rally {0}/{1}", meter.Rewards, meter.MaxRewards)
                    : $"{tag} x{meter.Rewards}";
            }
        }

        private void ShowFinished()
        {
            _status.text = Loc.F("Army recruited: {0} units, {1} Rally", _run.Roster.Bench.Count, _run.RallyCharges);
            _status.rectTransform.anchorMin = new Vector2(0.04f, 0.095f);
            _status.rectTransform.anchorMax = new Vector2(0.96f, 0.155f);
            _continue.gameObject.SetActive(true);
            bool offer = _extraMoves != null && _extraMoves.Available();
            Ui.SetAnchors(_continue.GetComponent<RectTransform>(), offer ? 0.03f : 0.2f, 0.02f, offer ? 0.5f : 0.8f, 0.09f);
            if (offer)
            {
                if (_adMoves == null)
                {
                    _adMoves = Ui.Button(transform, _extraMoves.Caption, Palette.Gold, TakeExtraMoves, 0.52f, 0.02f, 0.97f, 0.09f, 30);
                    _adMoves.gameObject.name = "Button Extra moves";
                    _adMoves.GetComponentInChildren<Text>().color = Palette.Background;
                }
                _adMoves.gameObject.SetActive(true);
            }
        }

        private void TakeExtraMoves()
        {
            _extraMoves.Request(() =>
            {
                // The run is back in the puzzle phase with more moves: hide the buttons and play on.
                _adMoves.gameObject.SetActive(false);
                _continue.gameObject.SetActive(false);
                _status.text = Loc.T("Swap two neighbouring gems. Tap or swipe.");
                _status.rectTransform.anchorMin = new Vector2(0.04f, 0.02f);
                _status.rectTransform.anchorMax = new Vector2(0.82f, 0.1f);
                RefreshHud();
            });
        }
    }
}
