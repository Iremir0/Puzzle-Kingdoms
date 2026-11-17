using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Views.Common;
using PuzzleKingdoms.Views.WarCamp;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-9: drag and drop, tap fallback, grouped bench, merge animation and Undo in the War Camp.</summary>
    public class WarCampInteractionTests
    {
        private float _timeScale;
        private Canvas _canvas;
        private StageRun _run;
        private WarCampScreen _screen;

        private Roster Roster => _run.Roster;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _timeScale = Time.timeScale;
            Time.timeScale = 8f;
            GameContent content = LoadContent();
            foreach (UnitDefinition unit in content.RecruitableUnits)
            {
                Palette.Register(unit);
            }
            _run = content.CreateRun(content.Campaign[0], new SeededRandom(11));
            while (!_run.Puzzle.IsFinished)
            {
                Assert.IsTrue(_run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                _run.Puzzle.PlaySwap(a, b);
            }
            Assert.AreEqual(StagePhase.WarCamp, _run.Phase);

            Ui.EnsureEventSystem();
            _canvas = Ui.CreateCanvas("Test Canvas");
            _screen = WarCampScreen.Create(_canvas.transform, _run, null);
            yield return null; // let the canvas scaler lay everything out
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _timeScale;
            Object.Destroy(_canvas.gameObject);
            foreach (EventSystem system in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                Object.Destroy(system.gameObject);
            }
        }

        private static GameContent LoadContent()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            Assert.Ignore("needs the editor to load GameContent");
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator BenchIsGroupedByClassAndTier()
        {
            List<BenchGroup> expected = BenchGroup.FromBench(Roster.Bench, null);
            Assert.Greater(Roster.Bench.Count, 0, "the puzzle recruited units");
            Assert.AreEqual(expected.Count, _screen.Bench.Groups.Count, "one tile per class and tier");
            Assert.AreEqual(Roster.Bench.Count, _screen.Bench.Groups.Sum(g => g.Count));
            foreach (BenchGroup group in _screen.Bench.Groups)
            {
                RectTransform tile = _screen.Bench.TileFor(group.Def, group.Tier);
                Assert.IsNotNull(tile);
                StringAssert.Contains("×" + group.Count, string.Join(" ", tile.GetComponentsInChildren<Text>().Select(t => t.text)));
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator DragMovesUnitsBetweenBenchAndCells()
        {
            BenchGroup group = _screen.Bench.Groups[0];
            ArmyUnit first = group.First;
            var front = new GridPos(1, 0);
            var side = new GridPos(2, 0);

            DragTo(_screen.Bench.TileFor(group.Def, group.Tier), CenterOnScreen(_screen.Grid.CellRect(front)));
            Assert.AreEqual(first, Roster.Formation.GetUnit(front), "bench → cell");
            Assert.IsFalse(_screen.IsDragging);
            yield return null;

            BenchGroup other = _screen.Bench.Groups[0];
            ArmyUnit second = other.First;
            DragTo(_screen.Bench.TileFor(other.Def, other.Tier), CenterOnScreen(_screen.Grid.CellRect(side)));
            Assert.AreEqual(second, Roster.Formation.GetUnit(side));
            yield return null;

            DragTo(_screen.Grid.CellRect(front), CenterOnScreen(_screen.Grid.CellRect(side)));
            Assert.AreEqual(first, Roster.Formation.GetUnit(side), "cell → occupied cell swaps");
            Assert.AreEqual(second, Roster.Formation.GetUnit(front));
            yield return null;

            DragTo(_screen.Grid.CellRect(side), CenterOnScreen(_screen.Bench.Panel));
            Assert.IsNull(Roster.Formation.GetUnit(side), "cell → bench");
            Assert.IsTrue(Roster.Bench.Contains(first));
            yield return null;

            int deployed = Roster.Formation.Count;
            DragTo(_screen.Grid.CellRect(front), new Vector2(Screen.width * 0.5f, Screen.height * 0.97f));
            Assert.AreEqual(second, Roster.Formation.GetUnit(front), "dropping elsewhere changes nothing");
            Assert.AreEqual(deployed, Roster.Formation.Count);
            Assert.IsTrue(_screen.IsBusy, "the unit snaps back");
            yield return new WaitUntil(() => !_screen.IsBusy);
            yield return null; // Destroy happens at the end of the frame
            Assert.IsNull(_screen.transform.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Ghost")),
                "no ghost left behind");
        }

        [UnityTest]
        public IEnumerator TapThenCellStillDeploys()
        {
            BenchGroup group = _screen.Bench.Groups[0];
            ArmyUnit unit = group.First;
            var cell = new GridPos(3, 1);

            _screen.Bench.TileFor(group.Def, group.Tier).GetComponent<Button>().onClick.Invoke();
            yield return null;
            _screen.Grid.CellRect(cell).GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(unit, Roster.Formation.GetUnit(cell));
        }

        [UnityTest]
        public IEnumerator MergeAllAnimatesAndUndoRestores()
        {
            // 13 of every class: Merge all leaves a T3, a T2 and a T1 of each (9 + 3 + 1).
            List<UnitDef> classes = Roster.AllUnits().Select(u => u.Def).Distinct().ToList();
            Assert.AreEqual(3, classes.Count, "1-1 recruits every class");
            foreach (UnitDef def in classes)
            {
                int have = Roster.AllUnits().Count(u => u.Def == def);
                Assert.LessOrEqual(have, 13);
                Roster.Recruit(def, 13 - have);
            }
            yield return null;
            Roster.Deploy(Roster.Bench[0], new GridPos(0, 0));
            List<ArmyUnit> bench = Roster.Bench.ToList();
            ArmyUnit[] cells = AllCells();
            Assert.IsTrue(Roster.CanMergeAny);
            yield return null;

            Press("Merge all");
            Assert.IsTrue(_screen.IsBusy, "Merge all animates");
            Assert.IsTrue(Roster.CanUndo);
            Press("Undo");
            Assert.IsTrue(Roster.CanUndo, "input waits for the animation");
            yield return new WaitUntil(() => !_screen.IsBusy);

            foreach (BenchGroup group in _screen.Bench.Groups)
            {
                Assert.AreEqual(Vector3.one, _screen.Bench.TileFor(group.Def, group.Tier).localScale, "tiles back to full size");
            }
            Assert.AreEqual(Vector3.one, _screen.Grid.CellRect(new GridPos(0, 0)).localScale);
            Assert.IsTrue(_screen.Bench.Groups.Any(g => g.Tier == 3));
            Assert.GreaterOrEqual(_screen.Bench.Groups.Count, 8);
            // The canvas width depends on the window; on a 9:16 phone 5 tiles fit.
            float row = BenchView.Gap + _screen.Bench.Groups.Count * (BenchView.TileWidth + BenchView.Gap);
            Assert.AreEqual(row > _screen.Bench.Panel.rect.width, _screen.Bench.Overflows);
            if (_screen.Bench.Overflows)
            {
                RectTransform content = _screen.Bench.Scroll.content;
                float x = content.anchoredPosition.x;
                _screen.Bench.Scroll.horizontalNormalizedPosition = 1f;
                Assert.Less(content.anchoredPosition.x, x - 1f, "the bench scrolls sideways");
                _screen.Bench.Scroll.horizontalNormalizedPosition = 0f;
            }

            Press("Undo");
            CollectionAssert.AreEqual(bench, Roster.Bench, "same units back on the bench, same order");
            CollectionAssert.AreEqual(cells, AllCells(), "same units back on the same cells");
            Assert.IsFalse(Roster.CanUndo);
            Assert.IsFalse(_screen.transform.Find("Button Undo").GetComponent<Button>().interactable);
        }

        // ---- helpers ----

        private ArmyUnit[] AllCells()
        {
            var cells = new ArmyUnit[Formation.Size * Formation.Size];
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    cells[y * Formation.Size + x] = Roster.Formation.GetUnit(new GridPos(x, y));
                }
            }
            return cells;
        }

        private void Press(string caption) =>
            _screen.transform.Find("Button " + caption).GetComponent<Button>().onClick.Invoke();

        private static Vector2 CenterOnScreen(RectTransform rect) =>
            RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));

        /// <summary>Plays a whole drag through the uGUI handlers, as the input module would.</summary>
        private static void DragTo(RectTransform from, Vector2 to)
        {
            Vector2 start = CenterOnScreen(from);
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = start,
                pressPosition = start,
                eligibleForClick = true,
            };
            GameObject target = from.gameObject;
            ExecuteEvents.Execute(target, data, ExecuteEvents.initializePotentialDrag);
            Vector2 halfway = Vector2.Lerp(start, to, 0.5f);
            data.delta = halfway - start;
            data.position = halfway;
            data.dragging = true;
            ExecuteEvents.Execute(target, data, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.dragHandler);
            data.delta = to - halfway;
            data.position = to;
            ExecuteEvents.Execute(target, data, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.endDragHandler);
        }
    }
}
