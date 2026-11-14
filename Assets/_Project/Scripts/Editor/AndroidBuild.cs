using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// Builds a development APK for device testing (debug-signed, not for the store).
    /// Batch mode: <c>-executeMethod PuzzleKingdoms.EditorTools.AndroidBuild.BuildDevApk</c>.
    /// Output: <c>Builds/Android/PuzzleKingdoms.apk</c> (gitignored).
    /// </summary>
    public static class AndroidBuild
    {
        public const string OutputPath = "Builds/Android/PuzzleKingdoms.apk";

        [MenuItem("Puzzle Kingdoms/Build Android APK (dev)")]
        public static void BuildDevApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[AndroidBuild] {summary.result}: {summary.outputPath} " +
                      $"{summary.totalSize / (1024f * 1024f):0.0} MB in {summary.totalTime.TotalSeconds:0}s, " +
                      $"{summary.totalErrors} errors");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
