using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.WarCamp
{
    /// <summary>
    /// Grey-box War Camp (PB-9). Drag a unit from the bench onto a cell, from a
    /// cell to another (swap) or back onto the bench; dropping anywhere else snaps
    /// it back. Tapping still works: tap a unit, then a cell; tap a deployed unit
    /// twice to bench it. Merge / Merge all animate; Undo takes the last merge
    /// back until Fight! (D-038).
    /// </summary>
    public sealed class WarCampScreen : MonoBehaviour, IUnitDragHandler
    {
        // Timings (seconds), see the PB-9 card.
        public const float SnapBackSeconds = 0.15f;
        public const float MergeFlySeconds = 0.25f;
        public const float MergePopSeconds = 0.25f;
        public const float MergeAllStagger = 0.15f;
        private const float GhostTilt = 8f;

        private static readonly Color HoverEmpty = Palette.Primary;
        private static readonly Color HoverTaken = Palette.Bad;

        private StageRun _run;
        private Action _onFight;
        private AdOffer _extraRally; // PB-30: a rewarded ad for one more Rally charge
        private Text _rallyLabel;
        private FormationGridView _grid;
        private BenchView _bench;
        private Text _info;
        private Button _merge;
        private Button _mergeAll;
        private Button _undo;
        private Button _fight;
        private ArmyUnit _selected;
        private List<string> _classOrder;
        private int _animations;
        private Drag _drag;

        private Roster Roster => _run.Roster;

        /// <summary>True while a merge animation plays; input waits.</summary>
        public bool IsBusy => _animations > 0;

        public bool IsDragging => _drag != null;
        public FormationGridView Grid => _grid;
        public BenchView Bench => _bench;

        /// <summary>The unit being dragged and where it came from.</summary>
        private sealed class Drag
        {
            public ArmyUnit Unit;
            public GridPos FromCell;
            public RectTransform Source;
            public RectTransform Ghost;
        }

        public static WarCampScreen Create(Transform parent, StageRun run, Action onFight, AdOffer extraRally = null)
        {
            RectTransform root = Ui.Stretch(parent, "WarCampScreen");
            var screen = root.gameObject.AddComponent<WarCampScreen>();
            screen._extraRally = extraRally;
            screen.Build(run, onFight);
            return screen;
        }

        private void Build(StageRun run, Action onFight)
        {
            _run = run;
            _onFight = onFight;
            _classOrder = run.Puzzle.GemRewardKeys.Where(k => k != StageRun.RallyKey).ToList();
            Transform root = transform;
            Ui.Backdrop(root);

            Ui.Label(root, Loc.T("War Camp"), 52, TextAnchor.MiddleLeft, Palette.Text, 0.04f, 0.93f, 0.6f, 0.99f);
            _rallyLabel = Ui.Label(root, Loc.F("Rally charges: {0}", _run.RallyCharges), 40, TextAnchor.MiddleRight, Palette.Gold,
                0.5f, 0.93f, 0.96f, 0.99f);
            if (_run.RallyVariant != null && _run.RallyVariant.Kind != RallyKind.Classic)
            {
                // PB-23: the commander leading this army and what its Rally does.
                Ui.Label(root, Loc.F("Commander: {0}", Loc.T(_run.RallyVariant.DisplayName)), 30, TextAnchor.MiddleRight, Palette.Muted,
                    0.4f, 0.905f, 0.96f, 0.935f);
            }
            if (_extraRally != null && _extraRally.Available())
            {
                Button ad = null;
                ad = Ui.Button(root, _extraRally.Caption, Palette.Gold, () => _extraRally.Request(() =>
                {
                    ad.gameObject.SetActive(false);
                    _rallyLabel.text = Loc.F("Rally charges: {0}", _run.RallyCharges + _run.BonusRally);
                }), 0.25f, 0.935f, 0.5f, 0.99f, 26);
                ad.gameObject.name = "Button Extra Rally";
                ad.GetComponentInChildren<Text>().color = Palette.Background;
            }

            Ui.Card(root, "Enemy Panel", Palette.Panel, 0.03f, 0.75f, 0.97f, 0.925f);
            Ui.Label(root, Loc.T("Enemy army"), 36, TextAnchor.UpperLeft, Palette.Muted, 0.06f, 0.75f, 0.4f, 0.915f);
            StageBadges.AddPanelLine(root, _run.Stage);
            StageBadges.AddGoalLine(root, _run.Stage, _run.Puzzle.StonesBroken);
            new FormationGridView(root, "Enemy Formation", true, false, 0.35f, 0.755f, 0.65f, 0.92f)
                .Show(Formation.ForStage(_run.Stage));

            Ui.Label(root, Loc.T("Front row faces the enemy"), 34, TextAnchor.MiddleCenter, Palette.Muted,
                0.04f, 0.705f, 0.96f, 0.74f);
            _grid = new FormationGridView(root, "Player Formation", false, true, 0.15f, 0.41f, 0.85f, 0.70f);
            _grid.CellClicked += OnCellClicked;
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    var cell = new GridPos(x, y);
                    DraggableUnit.Add(_grid.CellRect(cell).gameObject, () => Roster.Formation.GetUnit(cell), this);
                }
            }

            _info = Ui.Label(root, "", 30, TextAnchor.MiddleCenter, Palette.Text, 0.04f, 0.33f, 0.96f, 0.41f);
            Ui.Label(root, Loc.T("Bench"), 32, TextAnchor.MiddleLeft, Palette.Muted, 0.04f, 0.303f, 0.5f, 0.33f);
            _bench = new BenchView(Ui.Card(root, "Bench", Palette.Panel, 0.03f, 0.165f, 0.97f, 0.303f));

            const float w = 0.176f;
            const float gap = 0.012f;
            float left = 0.03f;
            Ui.Button(root, "Auto", Ui.ButtonStyle.Secondary, OnAuto, left, 0.09f, left + w, 0.15f, 30);
            left += w + gap;
            _merge = Ui.Button(root, "Merge", Ui.ButtonStyle.Secondary, OnMerge, left, 0.09f, left + w, 0.15f, 30);
            left += w + gap;
            _mergeAll = Ui.Button(root, "Merge all", Ui.ButtonStyle.Primary, OnMergeAll, left, 0.09f, left + w, 0.15f, 30);
            left += w + gap;
            _undo = Ui.Button(root, "Undo", Ui.ButtonStyle.Secondary, OnUndo, left, 0.09f, left + w, 0.15f, 30);
            left += w + gap;
            Ui.Button(root, "Clear", Ui.ButtonStyle.Secondary, OnClear, left, 0.09f, left + w, 0.15f, 30);
            _fight = Ui.Button(root, "Fight!", Ui.ButtonStyle.Danger, OnFight, 0.2f, 0.015f, 0.8f, 0.08f, 52);

            Roster.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_run != null)
            {
                Roster.Changed -= Refresh;
            }
        }

        private bool Blocked => IsBusy || IsDragging;

        // ---- Tap fallback -------------------------------------------------

        private void OnCellClicked(GridPos cell)
        {
            if (Blocked)
            {
                return;
            }
            ArmyUnit occupant = Roster.Formation.GetUnit(cell);
            if (_selected == null)
            {
                Select(occupant);
                return;
            }
            if (_selected == occupant)
            {
                Select(null);
                Roster.Undeploy(cell);
                return;
            }
            ArmyUnit moving = _selected;
            Select(null);
            Roster.Deploy(moving, cell);
        }

        private void OnBenchTapped(BenchGroup group)
        {
            if (Blocked)
            {
                return;
            }
            Select(_selected != null && group.Units.Contains(_selected) ? null : group.First);
        }

        // ---- Drag and drop ------------------------------------------------

        public bool BeginUnitDrag(ArmyUnit unit, RectTransform source, PointerEventData eventData)
        {
            if (Blocked || !Roster.Owns(unit))
            {
                return false;
            }
            // Clear the selection without rebuilding the bench: the tile being dragged
            // receives the drag events and must stay alive until the drop.
            _selected = null;
            Refresh(rebuildBench: false);
            _drag = new Drag
            {
                Unit = unit,
                FromCell = Roster.Formation.FindCell(unit),
                Source = source,
                Ghost = MakeGhost(unit, source.rect.size, GhostTilt),
            };
            _drag.Ghost.GetComponent<CanvasGroup>().alpha = 0.8f;
            if (_drag.FromCell != Formation.NoCell)
            {
                _grid.Dim(_drag.FromCell);
            }
            UnitDrag(eventData);
            return true;
        }

        public void UnitDrag(PointerEventData eventData)
        {
            if (_drag == null)
            {
                return;
            }
            _drag.Ghost.anchoredPosition = ToLocal(eventData.position, eventData.pressEventCamera);
            bool overCell = _grid.TryGetCellAt(eventData.position, eventData.pressEventCamera, out GridPos cell);
            if (overCell)
            {
                ArmyUnit there = Roster.Formation.GetUnit(cell);
                _grid.SetHover(cell, there == null || there == _drag.Unit ? HoverEmpty : HoverTaken);
            }
            else
            {
                _grid.SetHover(Formation.NoCell, HoverEmpty);
            }
            _bench.SetDropHover(!overCell && IsDeployed(_drag) && _bench.Contains(eventData.position, eventData.pressEventCamera));
        }

        public void EndUnitDrag(PointerEventData eventData)
        {
            Drag drag = _drag;
            if (drag == null)
            {
                return;
            }
            _drag = null;
            _grid.SetHover(Formation.NoCell, HoverEmpty);
            _bench.SetDropHover(false);

            Camera eventCamera = eventData.pressEventCamera;
            bool done = false;
            if (_grid.TryGetCellAt(eventData.position, eventCamera, out GridPos cell))
            {
                done = cell != drag.FromCell && Roster.Deploy(drag.Unit, cell);
            }
            else if (IsDeployed(drag) && _bench.Contains(eventData.position, eventCamera))
            {
                done = Roster.Undeploy(drag.FromCell);
            }

            if (done)
            {
                Destroy(drag.Ghost.gameObject);
            }
            else
            {
                Vector2 home = drag.Source != null ? CenterOf(drag.Source) : drag.Ghost.anchoredPosition;
                Refresh(); // undo the dimmed source cell
                StartCoroutine(SnapBack(drag.Ghost, home));
            }
        }

        private static bool IsDeployed(Drag drag) => drag.FromCell != Formation.NoCell;

        private IEnumerator SnapBack(RectTransform ghost, Vector2 home)
        {
            _animations++;
            try
            {
                yield return Tween.Move(ghost, home, SnapBackSeconds);
            }
            finally
            {
                _animations--;
                if (ghost != null)
                {
                    Destroy(ghost.gameObject);
                }
            }
        }

        // ---- Buttons ------------------------------------------------------

        private void OnAuto()
        {
            if (Blocked)
            {
                return;
            }
            Select(null);
            AutoFormation.Fill(Roster);
        }

        private void OnMerge()
        {
            if (Blocked || _selected == null)
            {
                return;
            }
            ArmyUnit target = _selected;
            _selected = null;
            Dictionary<ArmyUnit, Vector2> before = UnitPositions();
            ArmyUnit merged = Roster.Merge(target);
            if (merged == null)
            {
                Refresh();
                return;
            }
            Select(merged);
            PlayMerges(before, new List<ArmyUnit> { merged });
        }

        /// <summary>Merges everything that can be merged in one go (see <see cref="Roster.MergeAll"/>).</summary>
        private void OnMergeAll()
        {
            if (Blocked)
            {
                return;
            }
            Select(null);
            Dictionary<ArmyUnit, Vector2> before = UnitPositions();
            List<ArmyUnit> created = Roster.MergeAll();
            PlayMerges(before, created);
        }

        private void OnUndo()
        {
            if (Blocked)
            {
                return;
            }
            Select(null);
            Roster.Undo();
        }

        private void OnClear()
        {
            if (Blocked)
            {
                return;
            }
            Select(null);
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    Roster.Undeploy(new GridPos(x, y));
                }
            }
        }

        private void OnFight()
        {
            if (!Blocked && _run.StartBattle())
            {
                _onFight?.Invoke();
            }
        }

        // ---- Merge animation ----------------------------------------------

        /// <summary>
        /// The units that vanished fly into the new units of their class, then each new
        /// unit flashes and pops 120% → 100%. Merge all plays them one after another.
        /// Chained merges (T1 → T2 → T3) show as one: the T1s fly straight into the T3.
        /// </summary>
        private void PlayMerges(Dictionary<ArmyUnit, Vector2> before, List<ArmyUnit> created)
        {
            List<ArmyUnit> results = created.Where(Roster.Owns).ToList();
            List<ArmyUnit> gone = before.Keys.Where(u => !Roster.Owns(u)).OrderBy(u => u.Uid).ToList();
            // New bench units of the same class and tier share one tile: animate that tile once.
            var targets = new List<ArmyUnit>();
            foreach (ArmyUnit result in results)
            {
                if (!targets.Any(t => RectOf(t) == RectOf(result)))
                {
                    targets.Add(result);
                }
            }
            for (int i = 0; i < targets.Count; i++)
            {
                ArmyUnit target = targets[i];
                List<ArmyUnit> sameClass = targets.Where(r => r.Def == target.Def).ToList();
                int share = sameClass.IndexOf(target);
                List<(ArmyUnit, Vector2)> from = gone.Where(u => u.Def == target.Def)
                    .Where((u, index) => index % sameClass.Count == share)
                    .Select(u => (u, before[u]))
                    .ToList();
                StartCoroutine(PlayMerge(target, from, i * MergeAllStagger));
            }
            if (results.Count > 0)
            {
                Sfx.Play(Sfx.Bank != null ? Sfx.Bank.UnitRecruited : null, 1.2f);
            }
        }

        private IEnumerator PlayMerge(ArmyUnit result, List<(ArmyUnit unit, Vector2 at)> from, float delay)
        {
            _animations++;
            try
            {
                RectTransform target = RectOf(result);
                if (target == null)
                {
                    yield break;
                }
                target.localScale = Vector3.zero;
                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }

                Vector2 goal = CenterOf(target);
                var ghosts = new List<RectTransform>();
                foreach ((ArmyUnit unit, Vector2 at) in from)
                {
                    RectTransform ghost = MakeGhost(unit, target.rect.size * 0.7f, 0f);
                    ghost.anchoredPosition = at;
                    ghosts.Add(ghost);
                }
                var starts = ghosts.Select(g => g.anchoredPosition).ToList();
                yield return Tween.Run(MergeFlySeconds, elapsed =>
                {
                    float t = Tween.EaseIn(Tween.Progress(elapsed, MergeFlySeconds));
                    for (int g = 0; g < ghosts.Count; g++)
                    {
                        ghosts[g].anchoredPosition = Vector2.LerpUnclamped(starts[g], goal, t);
                        ghosts[g].localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t);
                    }
                });
                foreach (RectTransform ghost in ghosts)
                {
                    Destroy(ghost.gameObject);
                }

                target = RectOf(result) ?? target; // the bench may have been rebuilt meanwhile
                Image flash = Ui.Box(target, "Merge Flash", Color.white);
                flash.raycastTarget = false;
                yield return Tween.Run(MergePopSeconds, elapsed =>
                {
                    float t = Tween.Progress(elapsed, MergePopSeconds);
                    if (target != null)
                    {
                        // 0 → 120% in the first 60%, then settle to 100%.
                        float scale = t < 0.6f ? Mathf.Lerp(0f, 1.2f, Tween.EaseOut(t / 0.6f))
                            : Mathf.Lerp(1.2f, 1f, (t - 0.6f) / 0.4f);
                        target.localScale = Vector3.one * scale;
                    }
                    if (flash != null)
                    {
                        flash.color = new Color(1f, 1f, 1f, 0.8f * (1f - t));
                    }
                });
                if (flash != null)
                {
                    Destroy(flash.gameObject);
                }
            }
            finally
            {
                _animations--;
                RectTransform target = RectOf(result);
                if (target != null)
                {
                    target.localScale = Vector3.one;
                }
            }
        }

        /// <summary>Where every unit is drawn now: its cell, or its bench tile.</summary>
        private Dictionary<ArmyUnit, Vector2> UnitPositions()
        {
            var positions = new Dictionary<ArmyUnit, Vector2>();
            foreach (ArmyUnit unit in Roster.AllUnits())
            {
                RectTransform rect = RectOf(unit);
                if (rect != null)
                {
                    positions[unit] = CenterOf(rect);
                }
            }
            return positions;
        }

        private RectTransform RectOf(ArmyUnit unit)
        {
            GridPos cell = Roster.Formation.FindCell(unit);
            if (cell != Formation.NoCell)
            {
                return _grid.CellRect(cell);
            }
            return Roster.Bench.Contains(unit) ? _bench.TileFor(unit.Def, unit.Tier) : null;
        }

        // ---- Helpers ------------------------------------------------------

        /// <summary>A see-through copy of a unit tile that never blocks touches.</summary>
        private RectTransform MakeGhost(ArmyUnit unit, Vector2 size, float tilt)
        {
            Image box = Ui.Box(transform, "Ghost " + unit.Def.Id, Palette.For(unit.Def), 0.5f, 0.5f, 0.5f, 0.5f);
            RectTransform ghost = box.rectTransform;
            ghost.sizeDelta = size;
            ghost.localRotation = Quaternion.Euler(0f, 0f, tilt);
            UnitSilhouettes.AddIcon(ghost, Palette.LookFor(unit.Def), unit.Tier);
            UnitSilhouettes.AddTierLabel(ghost, unit.Tier, 28);
            var group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            ghost.SetAsLastSibling();
            return ghost;
        }

        private Vector2 ToLocal(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPoint, eventCamera,
                out Vector2 local);
            return local;
        }

        /// <summary>Centre of <paramref name="rect"/> in this screen's coordinates (where ghosts live).</summary>
        private Vector2 CenterOf(RectTransform rect)
        {
            Vector3 world = rect.TransformPoint(rect.rect.center);
            return ((RectTransform)transform).InverseTransformPoint(world);
        }

        private void Select(ArmyUnit unit)
        {
            _selected = unit != null && Roster.Owns(unit) ? unit : null;
            Refresh();
        }

        private void Refresh() => Refresh(rebuildBench: true);

        private void Refresh(bool rebuildBench)
        {
            if (_selected != null && !Roster.Owns(_selected))
            {
                _selected = null;
            }
            _grid.Show(Roster.Formation, _selected);
            if (rebuildBench)
            {
                _bench.Show(BenchGroup.FromBench(Roster.Bench, _classOrder), _selected, Roster.CanMerge, OnBenchTapped, this);
            }
            _merge.interactable = _selected != null && Roster.CanMerge(_selected);
            _mergeAll.interactable = Roster.CanMergeAny;
            _undo.interactable = Roster.CanUndo;
            _fight.interactable = _run.CanStartBattle;
            _info.text = _selected != null ? Describe(_selected) : DefaultHint();
        }

        private string Describe(ArmyUnit unit)
        {
            UnitDef def = unit.Def;
            ArmyRules rules = Roster.Rules;
            int owned = Roster.AllUnits().Count(u => u.IsSameKind(unit));
            string stats = Loc.F("{0} T{1}  HP {2}  DMG {3}  Range {4:0.#}  (you have {5})",
                Loc.T(def.DisplayName), unit.Tier, unit.MaxHp(rules), unit.Damage(rules), def.AttackRange, owned);
            return stats + "\n" + MergeLine(unit);
        }

        /// <summary>What merging this unit gives, or why it cannot be merged.</summary>
        private string MergeLine(ArmyUnit unit)
        {
            ArmyRules rules = Roster.Rules;
            if (unit.Tier >= rules.MaxTier)
            {
                return Loc.T("Highest tier: cannot merge.");
            }
            if (!Roster.CanMerge(unit))
            {
                int owned = Roster.AllUnits().Count(u => u.IsSameKind(unit));
                return Loc.F("Merge needs {0} of the same class and tier (you have {1}).", rules.MergeCount, owned);
            }
            var next = new ArmyUnit(unit.Def, unit.Tier + 1);
            return Loc.F("Merge: {0} x {1} T{2} -> T{3}  (HP {4} -> {5}, DMG {6} -> {7})",
                rules.MergeCount, Loc.T(unit.Def.DisplayName), unit.Tier, next.Tier,
                unit.MaxHp(rules), next.MaxHp(rules), unit.Damage(rules), next.Damage(rules));
        }

        private string DefaultHint()
        {
            string hint = Roster.Bench.Count > 0
                ? Loc.F("Drag units onto the grid. {0} on the bench leave for gold when the battle starts.", Roster.Bench.Count)
                : Loc.T("Drag a unit to move it, or onto the bench to take it off.");
            List<MergePlanEntry> plan = Roster.PlanMergeAll();
            if (plan.Count == 0)
            {
                return hint;
            }
            IEnumerable<string> parts = plan.Select(p =>
                $"{p.Merges} x {Loc.T(p.Def.DisplayName)} T{p.FromTier}->T{p.ToTier}");
            return hint + "\n" + Loc.F("Merge all: {0}", string.Join(", ", parts));
        }
    }
}
