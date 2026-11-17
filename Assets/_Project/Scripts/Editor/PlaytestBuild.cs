using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// Builds the Windows playtest build for testers without an Android phone
    /// (PB-5). Not a development build, so testers see the game as players will.
    /// Batch mode: <c>-executeMethod PuzzleKingdoms.EditorTools.PlaytestBuild.BuildWindows</c>.
    /// Output: <c>Builds/Windows/PuzzleKingdoms/</c> (gitignored); zip that folder
    /// and send it. Switches the editor back to Android afterwards.
    /// </summary>
    public static class PlaytestBuild
    {
        public const string OutputFolder = "Builds/Windows/PuzzleKingdoms";
        public const string ExeName = "PuzzleKingdoms.exe";

        [MenuItem("Puzzle Kingdoms/Build Windows Playtest")]
        public static void BuildWindows()
        {
            SceneBuilder.EnsureReady();
            if (Directory.Exists(OutputFolder))
            {
                Directory.Delete(OutputFolder, true);
            }
            Directory.CreateDirectory(OutputFolder);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(OutputFolder, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[PlaytestBuild] {summary.result}: {summary.outputPath} " +
                      $"{summary.totalSize / (1024f * 1024f):0.0} MB in {summary.totalTime.TotalSeconds:0}s, " +
                      $"{summary.totalErrors} errors");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
