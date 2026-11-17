using System.Collections;
using System.IO;
using NUnit.Framework;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
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
            string output = Path.GetFullPath("Logs/Screenshots");
            Directory.CreateDirectory(output);
            _saveFolder = Path.Combine(Path.GetTempPath(), "pk-shots-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_saveFolder);

            _app = new GameObject("GameApp").AddComponent<GameApp>();
            _app.SetContent(content);
            _app.UseStore(new ProfileStore(_saveFolder));
            _app.UsePlaytestLog(new PlaytestLog(_saveFolder));
            yield return null;
            yield return Capture(Path.Combine(output, "1-stage-select.png"));

            _app.StartStage(2);
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
