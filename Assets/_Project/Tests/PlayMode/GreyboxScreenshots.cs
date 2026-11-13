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

        /// <summary>Renders the overlay canvas through a temporary camera into a PNG.</summary>
        private static IEnumerator Capture(string path)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            var target = new RenderTexture(Width, Height, 24);
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
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
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
