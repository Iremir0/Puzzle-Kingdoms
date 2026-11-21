using System.Collections;
using System.IO;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Views.Battle;
using PuzzleKingdoms.Views.Puzzle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>
    /// Saves a 1080×1920 PNG of every grey-box screen to <c>Logs/Screenshots/</c>
    /// for reviewing layouts without a device. Explicit: run it with
    /// <c>-testFilter GreyboxScreenshots</c> or from the Test Runner.
    /// </summary>
    [Explicit("writes screenshots; run on demand")]
    public class GreyboxScreenshots
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private string _saveFolder;
        private GameApp _app;

        [TearDown]
        public void TearDown()
        {
            if (_app != null)
            {
                Object.Destroy(_app.gameObject);
            }
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                Object.Destroy(canvas.gameObject);
            }
            foreach (EventSystem system in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                Object.Destroy(system.gameObject);
            }
            if (_saveFolder != null)
            {
                Directory.Delete(_saveFolder, true);
            }
        }

        [UnityTest]
        public IEnumerator CaptureEveryScreen()
        {
#if UNITY_EDITOR
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            GameContent content = null;
            Assert.Ignore("editor only");
#endif
            // PB-32: English by default; PK_SCREENSHOT_LANG=tr writes the Turkish set to Logs/Screenshots/tr/.
            Language language = Languages.TryParse(System.Environment.GetEnvironmentVariable("PK_SCREENSHOT_LANG"),
                out Language chosen) ? chosen : Language.English;
            string output = Path.GetFullPath(language == Language.English ? "Logs/Screenshots" : "Logs/Screenshots/" + Languages.Code(language));
            Directory.CreateDirectory(output);
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-shots-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);

            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(content);
            _app.TutorialEnabled = false; // PB-13 prompts are tested in TutorialFlowTests
            _app.ConsentEnabled = false; // PB-29: the question is captured on its own below
            _app.UseStore(new ProfileStore(_saveFolder));
            _app.UsePlaytestLog(new PlaytestLog(_saveFolder));
            _app.UseLanguage(language);
            yield return null;
            yield return Capture(Path.Combine(output, "1-stage-map.png"));
            // PB-18 / PB-22 / PB-23: the second chapter, the Kingdom and the Commanders, with some gold and two stages won.
            _app.Profile.Apply("stage_1_3", new StageReward { Stars = 3, VictoryGold = 390 });
            _app.Profile.Apply("stage_2_1", new StageReward { Stars = 2 });
            _app.Profile.Apply("stage_2_2", new StageReward { Stars = 3 });
            _app.Profile.TryUpgradeBuilding(content.KingdomRules.Rules, "barracks");
            _app.ShowStageSelect();
            yield return null;
            ((Views.Menus.StageMapScreen)_app.CurrentScreen).OpenChapter(2);
            yield return null;
            yield return Capture(Path.Combine(output, "1-stage-map-chapter2.png"));
            _app.ShowKingdom();
            yield return null;
            yield return Capture(Path.Combine(output, "1-kingdom.png"));
            _app.ShowCommanders();
            yield return null;
            yield return Capture(Path.Combine(output, "1-commanders.png"));
            _app.Quests.Record(new Logic.Quests.QuestEvent { Won = true, RallyUsed = 2, Recruited = 16, Merges = 3, Gold = 100, MaxTier = 2 });
            _app.ShowQuests();
            yield return null;
            yield return Capture(Path.Combine(output, "1-quests.png"));
            _app.Profile.Apply("stage_1_1", new StageReward { Stars = 0 });
            _app.ShowStageSelect();
            yield return null;
            var settings = _app.OpenSettings(); // PB-14
            yield return null;
            yield return Capture(Path.Combine(output, "1-settings.png"));
            Object.Destroy(settings.gameObject);
            var consent = _app.AskConsent(); // PB-29
            yield return null;
            yield return Capture(Path.Combine(output, "1-consent.png"));
            Object.Destroy(consent.gameObject);

            _app.TutorialEnabled = true; // one prompt for the record (PB-13), then off again
            _app.StartStage(2);
            yield return null;
            yield return null;
            yield return Capture(Path.Combine(output, "2-tutorial.png"));
            _app.Tutorial?.Next();
            _app.TutorialEnabled = false;
            yield return null;
            yield return Capture(Path.Combine(output, "2-puzzle.png"));

            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            Time.timeScale = 8f;
            while (!_app.Run.Puzzle.IsFinished)
            {
                _app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b);
                puzzle.PlaySwap(a, b);
                yield return new WaitUntil(() => !puzzle.IsBusy);
            }
            Time.timeScale = 1f;
            yield return Capture(Path.Combine(output, "3-puzzle-finished.png"));

            _app.ShowWarCamp();
            yield return null;
            yield return Capture(Path.Combine(output, "4-war-camp-bench.png")); // PB-9: grouped bench
            AutoFormation.Fill(_app.Run.Roster);
            yield return null;
            yield return Capture(Path.Combine(output, "4-war-camp.png"));

            _app.Run.StartBattle();
            _app.ShowBattle();
            var battle = (BattleScreen)_app.CurrentScreen;
            battle.Simulator.ActivateRally();
            yield return new WaitForSeconds(2f);
            yield return Capture(Path.Combine(output, "5-battle.png"));

            if (_app.CurrentScreen == battle)
            {
                battle.OnSkip();
            }
            float deadline = Time.realtimeSinceStartup + 10f;
            yield return new WaitUntil(() => _app.CurrentScreen is Views.Menus.ResultsScreen
                                             || Time.realtimeSinceStartup > deadline);
            yield return Capture(Path.Combine(output, "6-results.png"));
            Debug.Log($"[GreyboxScreenshots] saved to {output}");
        }

        /// <summary>PB-21: the puzzle and the war camp of a stage with four classes plus the Banner gem, into <c>Logs/Screenshots/classes-*.png</c>.</summary>
        [UnityTest]
        public IEnumerator CaptureFourClassStage()
        {
#if UNITY_EDITOR
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            GameContent content = null;
            Assert.Ignore("editor only");
#endif
            string output = Path.GetFullPath("Logs/Screenshots");
            Directory.CreateDirectory(output);
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-shots-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);
            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(content);
            _app.TutorialEnabled = false;
            _app.ConsentEnabled = false;
            _app.UseStore(new ProfileStore(_saveFolder));
            _app.UseLanguage(Language.English);
            yield return null;
            _app.StartStage(11); // 2-9: Warrior, Archer, Mage and Defender
            yield return null;
            yield return Capture(Path.Combine(output, "classes-puzzle.png"));
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            Time.timeScale = 8f;
            while (!_app.Run.Puzzle.IsFinished)
            {
                _app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b);
                puzzle.PlaySwap(a, b);
                yield return new WaitUntil(() => !puzzle.IsBusy);
            }
            Time.timeScale = 1f;
            _app.ShowWarCamp();
            yield return null;
            yield return Capture(Path.Combine(output, "classes-war-camp.png"));
        }

        /// <summary>PB-18: the 3D battle of the boss stage, with the four new Chapter 2 models, into <c>Logs/Screenshots/chapter2-battle*.png</c>.</summary>
        [UnityTest]
        public IEnumerator CaptureChapterTwoBattle()
        {
#if UNITY_EDITOR
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            GameContent content = null;
            Assert.Ignore("editor only");
#endif
            string output = Path.GetFullPath("Logs/Screenshots");
            Directory.CreateDirectory(output);
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-shots-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);
            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(content);
            _app.TutorialEnabled = false;
            _app.ConsentEnabled = false;
            _app.UseStore(new ProfileStore(_saveFolder));
            _app.UseLanguage(Language.English);
            yield return null;
            _app.StartStage(content.CampaignOrder().IndexOf("stage_2_10")); // Shield, Chief, Wolf, Shaman and Bomber
            yield return null;
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            Time.timeScale = 8f;
            while (!_app.Run.Puzzle.IsFinished)
            {
                _app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b);
                puzzle.PlaySwap(a, b);
                yield return new WaitUntil(() => !puzzle.IsBusy);
            }
            Time.timeScale = 1f;
            _app.ShowWarCamp();
            yield return null;
            AutoFormation.Fill(_app.Run.Roster);
            _app.Run.StartBattle();
            _app.ShowBattle();
            var battle = (BattleScreen)_app.CurrentScreen;
            yield return new WaitForSeconds(0.3f);
            yield return Capture(Path.Combine(output, "chapter2-battle-start.png"));
            battle.Simulator.ActivateRally();
            yield return new WaitForSeconds(5f);
            yield return Capture(Path.Combine(output, "chapter2-battle.png"));
        }

        /// <summary>
        /// PB-42: plays every Chapter 3 stage through the real screens (puzzle by the first valid move, "Merge all",
        /// auto formation, Rally when ready) with the prompts on, into <c>Logs/Screenshots/chapter3/</c>: the map, the
        /// puzzle, the War Camp, the battle at the start and after 4 s, and the results. The log lists every outcome.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureChapterThree()
        {
#if UNITY_EDITOR
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            GameContent content = null;
            Assert.Ignore("editor only");
#endif
            string output = Path.GetFullPath("Logs/Screenshots/chapter3");
            Directory.CreateDirectory(output);
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-shots-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);
            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(content);
            _app.TutorialEnabled = false;
            _app.ConsentEnabled = false;
            _app.UseStore(new ProfileStore(_saveFolder));
            _app.UseLanguage(Language.English);
            yield return null;
            var order = content.CampaignOrder();
            foreach (string id in order)
            {
                if (!id.StartsWith("stage_3_"))
                {
                    _app.Profile.Apply(id, new StageReward { Stars = 3 });
                }
            }
            _app.ShowStageSelect();
            yield return null;
            ((Views.Menus.StageMapScreen)_app.CurrentScreen).OpenChapter(3);
            yield return null;
            yield return Capture(Path.Combine(output, "0-map.png"));

            var report = new System.Text.StringBuilder();
            for (int n = 1; n <= 10; n++)
            {
                string id = "stage_3_" + n;
                string name = "3-" + n.ToString("00");
                _app.TutorialEnabled = true;
                _app.StartStage(order.IndexOf(id));
                yield return null;
                yield return null;
                yield return Capture(Path.Combine(output, name + "-a-puzzle.png"));
                _app.Tutorial?.Next();
                var puzzle = (PuzzleScreen)_app.CurrentScreen;
                Time.timeScale = 8f;
                while (!_app.Run.Puzzle.IsFinished)
                {
                    _app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b);
                    puzzle.PlaySwap(a, b);
                    yield return new WaitUntil(() => !puzzle.IsBusy);
                }
                Time.timeScale = 1f;
                _app.ShowWarCamp();
                yield return null;
                _app.Run.Roster.MergeAll();
                AutoFormation.Fill(_app.Run.Roster);
                yield return null;
                yield return Capture(Path.Combine(output, name + "-b-war-camp.png"));
                _app.Tutorial?.Next();
                _app.Run.StartBattle();
                _app.ShowBattle();
                var battle = (BattleScreen)_app.CurrentScreen;
                yield return new WaitForSeconds(0.3f);
                yield return Capture(Path.Combine(output, name + "-c-battle-start.png"));
                _app.Tutorial?.Next();
                _app.TutorialEnabled = false;
                float until = Time.realtimeSinceStartup + 4f;
                while (Time.realtimeSinceStartup < until && !battle.Simulator.IsFinished)
                {
                    if (battle.Simulator.Rally.CanActivate)
                    {
                        battle.Simulator.ActivateRally();
                    }
                    yield return null;
                }
                yield return Capture(Path.Combine(output, name + "-d-battle.png"));
                if (_app.CurrentScreen == battle)
                {
                    battle.OnSkip();
                }
                float deadline = Time.realtimeSinceStartup + 10f;
                yield return new WaitUntil(() => _app.CurrentScreen is Views.Menus.ResultsScreen
                                                 || Time.realtimeSinceStartup > deadline);
                yield return Capture(Path.Combine(output, name + "-e-results.png"));
                report.AppendLine($"{id}: won {_app.Run.Won}, battle {battle.Simulator.Result.Ticks / 20f:0.0}s, " +
                    $"recruited {_app.Run.Roster.Formation.Count} deployed, survivors {battle.Simulator.Result.Survivors}, " +
                    $"reason {battle.Simulator.Result.Reason}");
            }
            File.WriteAllText(Path.Combine(output, "report.txt"), report.ToString());
            Debug.Log("[GreyboxScreenshots] chapter 3: " + report);
        }

        /// <summary>The two screens with the most rows, on a shorter phone (1080×1800).</summary>
        [UnityTest]
        public IEnumerator CaptureShortScreen()
        {
#if UNITY_EDITOR
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            GameContent content = null;
            Assert.Ignore("editor only");
#endif
            const int shortHeight = 1800;
            string output = Path.GetFullPath("Logs/Screenshots");
            Directory.CreateDirectory(output);
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-shots-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);

            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(content);
            _app.TutorialEnabled = false; // PB-13 prompts are tested in TutorialFlowTests
            _app.ConsentEnabled = false; // PB-29: the question is captured on its own below
            _app.UseStore(new ProfileStore(_saveFolder));
            yield return null;
            yield return Capture(Path.Combine(output, "short-1-stage-select.png"), Width, shortHeight);

            _app.StartStage(1);
            yield return null;
            yield return Capture(Path.Combine(output, "short-2-puzzle.png"), Width, shortHeight);
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            Time.timeScale = 8f;
            while (!_app.Run.Puzzle.IsFinished)
            {
                _app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b);
                puzzle.PlaySwap(a, b);
                yield return new WaitUntil(() => !puzzle.IsBusy);
            }
            Time.timeScale = 1f;
            // No Auto here: every unit stays on the bench, so the bench is in the picture.
            _app.ShowWarCamp();
            yield return null;
            yield return Capture(Path.Combine(output, "short-3-war-camp.png"), Width, shortHeight);
        }

        /// <summary>
        /// PB-20: the puzzle of the two draft Chapter 2 stages (ice and the ice goal; chains and the boss
        /// countdown), and the board right after the boss threw stones, into <c>Logs/Screenshots/obstacles-*.png</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureObstacleStages()
        {
#if UNITY_EDITOR
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
            var gate = UnityEditor.AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/_Project/Data/Stages/Stage_2_2.asset");
            var forge = UnityEditor.AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/_Project/Data/Stages/Stage_2_9.asset");
#else
            GameContent content = null;
            StageDefinition gate = null, forge = null;
            Assert.Ignore("editor only");
#endif
            string output = Path.GetFullPath("Logs/Screenshots");
            Directory.CreateDirectory(output);
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-shots-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);
            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(content);
            _app.TutorialEnabled = false;
            _app.ConsentEnabled = false;
            _app.UseStore(new ProfileStore(_saveFolder));
            _app.UseLanguage(Language.English);
            yield return null;

            _app.StartStage(gate);
            yield return null;
            yield return Capture(Path.Combine(output, "obstacles-ice.png"));

            _app.StartStage(forge);
            yield return null;
            yield return Capture(Path.Combine(output, "obstacles-chains.png"));
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            for (int move = 0; move < _app.Run.Puzzle.BossStonesEvery; move++)
            {
                Assert.IsTrue(_app.Run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                Assert.IsTrue(puzzle.PlaySwap(a, b), "a valid move");
                while (puzzle.IsBusy)
                {
                    yield return null;
                }
            }
            yield return Capture(Path.Combine(output, "obstacles-boss-stones.png"));
        }

        /// <summary>
        /// PB-7: records the puzzle board at 30 fps (fixed time steps) through an
        /// invalid swap, eight moves, the idle hint and the PB-8 effects into
        /// <c>Logs/Screenshots/anim/frame-NNNN.png</c>, to turn into a GIF or video
        /// with any tool (the PB-7 card has the one used for its notes).
        /// </summary>
        [UnityTest]
        public IEnumerator RecordMoveAnimation()
        {
#if UNITY_EDITOR
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Project/Data/GameContent.asset");
#else
            GameContent content = null;
            Assert.Ignore("editor only");
#endif
            const int width = 540;
            const int height = 960;
            string output = Path.GetFullPath("Logs/Screenshots/anim");
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }
            Directory.CreateDirectory(output);
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-shots-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);

            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(content);
            _app.TutorialEnabled = false; // PB-13 prompts are tested in TutorialFlowTests
            _app.ConsentEnabled = false; // PB-29: the question is captured on its own below
            _app.UseStore(new ProfileStore(_saveFolder));
            yield return null;
            _app.StartStage(2);
            yield return null;
            var puzzle = (PuzzleScreen)_app.CurrentScreen;
            var board = _app.Run.Puzzle.Board;

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            var target = new RenderTexture(width, height, 24);
            var camera = new GameObject("Record Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            int frame = 0;
            Time.captureDeltaTime = 1f / 30f;
            try
            {
                void Shoot()
                {
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(output, $"frame-{frame++:0000}.png"), texture.EncodeToPNG());
                }

                IEnumerator Hold(int frames)
                {
                    for (int i = 0; i < frames; i++)
                    {
                        yield return null;
                        Shoot();
                    }
                }

                yield return Hold(10);
                for (int y = 0; y < board.Height; y++)
                {
                    var a = new GridPos(0, y);
                    var b = new GridPos(1, y);
                    if (!board.WouldSwapMatch(a, b))
                    {
                        puzzle.PlaySwap(a, b);
                        break;
                    }
                }
                while (puzzle.IsBusy)
                {
                    yield return Hold(1);
                }
                yield return Hold(8);
                for (int move = 0; move < 8 && !_app.Run.Puzzle.IsFinished; move++)
                {
                    board.TryFindValidMove(out GridPos a, out GridPos b);
                    puzzle.PlaySwap(a, b);
                    while (puzzle.IsBusy)
                    {
                        yield return Hold(1);
                    }
                    yield return Hold(12);
                }
                // Skip most of the idle wait without recording, then show the hint pulse.
                Time.captureDeltaTime = 0.25f;
                for (int i = 0; i < 20; i++)
                {
                    yield return null;
                }
                Time.captureDeltaTime = 1f / 30f;
                yield return Hold(45);

                // PB-8 showcase: the special-gem effects, which a few random moves rarely make.
                BoardEffects effects = puzzle.Effects;
                effects.Ring(new GridPos(2, 2), Views.Common.Palette.Gold);
                effects.Shake(10f, 0.15f);
                yield return Hold(12);
                effects.Beams(new GridPos(3, 3), new[] { new GridPos(0, 0), new GridPos(5, 1), new GridPos(1, 5), new GridPos(5, 5) });
                effects.Shake(16f, 0.2f);
                yield return Hold(12);
                effects.Chips(new GridPos(1, 1));
                effects.Chips(new GridPos(4, 4));
                effects.Flash(new GridPos(2, 4), Views.Common.GemShapes.Get(Views.Common.GemShape.Diamond));
                yield return Hold(15);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                camera.targetTexture = null;
                Object.Destroy(camera.gameObject);
                Object.Destroy(texture);
                target.Release();
                Object.Destroy(target);
            }
            Debug.Log($"[GreyboxScreenshots] {frame} animation frames saved to {output}");
        }

        /// <summary>Renders the overlay canvas through a temporary camera into a PNG.</summary>
        private static IEnumerator Capture(string path, int width = Width, int height = Height)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            var target = new RenderTexture(width, height, 24);
            var cameraObject = new GameObject("Capture Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.targetTexture = target;
            RenderMode mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            // WaitForEndOfFrame never fires in batch mode, so render by hand.
            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, texture.EncodeToPNG());

            canvas.renderMode = mode;
            canvas.worldCamera = null;
            camera.targetTexture = null;
            Object.Destroy(cameraObject);
            Object.Destroy(texture);
            target.Release();
            Object.Destroy(target);
        }
    }
}
