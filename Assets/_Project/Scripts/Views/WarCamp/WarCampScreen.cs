using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Views.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.WarCamp
{
    /// <summary>
    /// Grey-box War Camp: tap a bench unit, then a cell, to deploy it; tap a
    /// deployed unit, then another cell, to move or swap it; tap it twice to send
    /// it back. Merge, Auto and Fight buttons call <see cref="Roster"/> and
    /// <see cref="StageRun.StartBattle"/>.
    /// </summary>
    public sealed class WarCampScreen : MonoBehaviour
    {
        private const int BenchColumns = 9;
        private const int BenchRows = 3;

        private StageRun _run;
        private Action _onFight;
        private FormationGridView _grid;
        private RectTransform _bench;
        private Text _info;
        private Button _merge;
        private Button _mergeAll;
        private Button _fight;
        private ArmyUnit _selected;

        private Roster Roster => _run.Roster;

        public static WarCampScreen Create(Transform parent, StageRun run, Action onFight)
        {
            RectTransform root = Ui.Stretch(parent, "WarCampScreen");
            var screen = root.gameObject.AddComponent<WarCampScreen>();
            screen.Build(run, onFight);
            return screen;
        }

        private void Build(StageRun run, Action onFight)
        {
            _run = run;
            _onFight = onFight;
            Transform root = transform;
            Ui.Box(root, "Background", Palette.Background);

            Ui.Label(root, "War Camp", 52, TextAnchor.MiddleLeft, Palette.Text, 0.04f, 0.93f, 0.6f, 0.99f);
            Ui.Label(root, $"Rally charges: {_run.RallyCharges}", 40, TextAnchor.MiddleRight, Palette.Gold,
                0.5f, 0.93f, 0.96f, 0.99f);

            Ui.Box(root, "Enemy Panel", Palette.Panel, 0.03f, 0.75f, 0.97f, 0.925f);
            Ui.Label(root, "Enemy army", 36, TextAnchor.UpperLeft, Palette.Muted, 0.06f, 0.75f, 0.4f, 0.915f);
            StageBadges.AddPanelLine(root, _run.Stage);
            new FormationGridView(root, "Enemy Formation", true, false, 0.35f, 0.755f, 0.65f, 0.92f)
                .Show(Formation.ForStage(_run.Stage));

            Ui.Label(root, "Front row faces the enemy", 34, TextAnchor.MiddleCenter, Palette.Muted,
                0.04f, 0.705f, 0.96f, 0.74f);
            _grid = new FormationGridView(root, "Player Formation", false, true, 0.15f, 0.41f, 0.85f, 0.70f);
            _grid.CellClicked += OnCellClicked;

            _info = Ui.Label(root, "", 30, TextAnchor.MiddleCenter, Palette.Text, 0.04f, 0.33f, 0.96f, 0.41f);
            Ui.Label(root, "Bench", 32, TextAnchor.MiddleLeft, Palette.Muted, 0.04f, 0.303f, 0.5f, 0.33f);
            Image benchPanel = Ui.Box(root, "Bench", Palette.Panel, 0.03f, 0.165f, 0.97f, 0.303f);
            _bench = benchPanel.rectTransform;

            Ui.Button(root, "Auto", Palette.Button, OnAuto, 0.03f, 0.09f, 0.25f, 0.15f, 32);
            _merge = Ui.Button(root, "Merge", Palette.Button, OnMerge, 0.27f, 0.09f, 0.49f, 0.15f, 32);
            _mergeAll = Ui.Button(root, "Merge all", Palette.Primary, OnMergeAll, 0.51f, 0.09f, 0.73f, 0.15f, 32);
            Ui.Button(root, "Clear", Palette.Button, OnClear, 0.75f, 0.09f, 0.97f, 0.15f, 32);
            _fight = Ui.Button(root, "Fight!", Palette.Bad, OnFight, 0.2f, 0.015f, 0.8f, 0.08f, 52);

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

        private void OnCellClicked(GridPos cell)
        {
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

        private void OnBenchClicked(ArmyUnit unit) => Select(_selected == unit ? null : unit);

        private void OnAuto()
        {
            Select(null);
            AutoFormation.Fill(Roster);
        }

        private void OnMerge()
        {
            if (_selected == null)
            {
                return;
            }
            ArmyUnit target = _selected;
            _selected = null;
            Select(Roster.Merge(target));
        }

        /// <summary>Merges everything that can be merged in one go (see <see cref="Roster.MergeAll"/>).</summary>
        private void OnMergeAll()
        {
            Select(null);
            Roster.MergeAll();
        }

        private void OnClear()
        {
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
            if (_run.StartBattle())
            {
                _onFight?.Invoke();
            }
        }

        private void Select(ArmyUnit unit)
        {
            _selected = unit != null && Roster.Owns(unit) ? unit : null;
            Refresh();
        }

        private void Refresh()
        {
            if (_selected != null && !Roster.Owns(_selected))
            {
                _selected = null;
            }
            _grid.Show(Roster.Formation, _selected);
            RebuildBench();
            _merge.interactable = _selected != null && Roster.CanMerge(_selected);
            _mergeAll.interactable = Roster.CanMergeAny;
            _fight.interactable = _run.CanStartBattle;
            _info.text = _selected != null ? Describe(_selected) : DefaultHint();
        }

        private void RebuildBench()
        {
            Ui.Clear(_bench);
            IReadOnlyList<ArmyUnit> bench = Roster.Bench;
            if (bench.Count == 0)
            {
                Ui.Label(_bench, "Empty", 34, TextAnchor.MiddleCenter, Palette.Muted);
                return;
            }
            float w = 1f / BenchColumns;
            float h = 1f / BenchRows;
            int shown = Mathf.Min(bench.Count, BenchColumns * BenchRows);
            for (int i = 0; i < shown; i++)
            {
                ArmyUnit unit = bench[i];
                int col = i % BenchColumns;
                int row = i / BenchColumns;
                float top = 1f - row * h;
                Button button = Ui.Button(_bench, "", Palette.For(unit.Def), () => OnBenchClicked(unit),
                    col * w + 0.005f, top - h + 0.03f, (col + 1) * w - 0.005f, top - 0.03f, 36);
                UnitSilhouettes.AddIcon(button.transform, Palette.LookFor(unit.Def), unit.Tier);
                UnitSilhouettes.AddTierLabel(button.transform, unit.Tier, 24);
                if (unit == _selected)
                {
                    Outline outline = button.gameObject.AddComponent<Outline>();
                    outline.effectColor = Palette.Highlight;
                    outline.effectDistance = new Vector2(5, 5);
                }
                else if (Roster.CanMerge(unit))
                {
                    // Gold edge: this unit has enough copies to merge.
                    Outline outline = button.gameObject.AddComponent<Outline>();
                    outline.effectColor = Palette.Gold;
                    outline.effectDistance = new Vector2(4, 4);
                }
            }
            if (bench.Count > shown)
            {
                Ui.Label(_bench, $"+{bench.Count - shown} more", 30, TextAnchor.LowerRight, Palette.Muted);
            }
        }

        private string Describe(ArmyUnit unit)
        {
            UnitDef def = unit.Def;
            ArmyRules rules = Roster.Rules;
            int owned = Roster.AllUnits().Count(u => u.IsSameKind(unit));
            string stats = $"{def.DisplayName} T{unit.Tier}  HP {unit.MaxHp(rules)}  DMG {unit.Damage(rules)}  " +
                           $"Range {def.AttackRange:0.#}  (you have {owned})";
            return stats + "\n" + MergeLine(unit);
        }

        /// <summary>What merging this unit gives, or why it cannot be merged.</summary>
        private string MergeLine(ArmyUnit unit)
        {
            ArmyRules rules = Roster.Rules;
            if (unit.Tier >= rules.MaxTier)
            {
                return "Highest tier: cannot merge.";
            }
            if (!Roster.CanMerge(unit))
            {
                int owned = Roster.AllUnits().Count(u => u.IsSameKind(unit));
                return $"Merge needs {rules.MergeCount} of the same class and tier (you have {owned}).";
            }
            var next = new ArmyUnit(unit.Def, unit.Tier + 1);
            return $"Merge: {rules.MergeCount} x {unit.Def.DisplayName} T{unit.Tier} -> T{next.Tier}  " +
                   $"(HP {unit.MaxHp(rules)} -> {next.MaxHp(rules)}, DMG {unit.Damage(rules)} -> {next.Damage(rules)})";
        }

        private string DefaultHint()
        {
            string hint = Roster.Bench.Count > 0
                ? $"{Roster.Bench.Count} on the bench: they leave for gold when the battle starts."
                : "Tap a unit, then a cell. Tap a unit twice to bench it.";
            List<MergePlanEntry> plan = Roster.PlanMergeAll();
            if (plan.Count == 0)
            {
                return hint;
            }
            IEnumerable<string> parts = plan.Select(p =>
                $"{p.Merges} x {p.Def.DisplayName} T{p.FromTier}->T{p.ToTier}");
            return hint + "\nMerge all: " + string.Join(", ", parts);
        }
    }
}
