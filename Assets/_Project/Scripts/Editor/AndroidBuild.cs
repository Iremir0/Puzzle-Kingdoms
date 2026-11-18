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

        public const string ReleaseOutputPath = "Builds/Android/PuzzleKingdoms.aab";

        /// <summary>
        /// Builds the signed Android App Bundle for the store (PB-17): a release build, no development flag. The
        /// keystore is never in the repo; its path and passwords come from environment variables:
        /// <c>PK_KEYSTORE_PATH</c>, <c>PK_KEYSTORE_PASS</c>, <c>PK_KEY_ALIAS</c>, <c>PK_KEY_PASS</c>.
        /// Batch mode: <c>-executeMethod PuzzleKingdoms.EditorTools.AndroidBuild.BuildReleaseAab</c>.
        /// Output: <c>Builds/Android/PuzzleKingdoms.aab</c> (gitignored).
        /// </summary>
        public static void BuildReleaseAab()
        {
            string keystore = Environment.GetEnvironmentVariable("PK_KEYSTORE_PATH");
            string keystorePass = Environment.GetEnvironmentVariable("PK_KEYSTORE_PASS");
            string alias = Environment.GetEnvironmentVariable("PK_KEY_ALIAS");
            string keyPass = Environment.GetEnvironmentVariable("PK_KEY_PASS");
            if (string.IsNullOrEmpty(keystore) || !File.Exists(keystore) || string.IsNullOrEmpty(keystorePass)
                || string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(keyPass))
            {
                Debug.LogError("[AndroidBuild] Release build needs PK_KEYSTORE_PATH (an existing file), PK_KEYSTORE_PASS, " +
                               "PK_KEY_ALIAS and PK_KEY_PASS in the environment. See docs/SETUP.md, release section.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            SceneBuilder.EnsureReady();
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = keyPass;
            Directory.CreateDirectory(Path.GetDirectoryName(ReleaseOutputPath));

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = ReleaseOutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            // The passwords stay in memory only: do not leave them in the project settings.
            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
            PlayerSettings.Android.useCustomKeystore = false;
            Debug.Log($"[AndroidBuild] {summary.result}: {summary.outputPath} " +
                      $"{summary.totalSize / (1024f * 1024f):0.0} MB, version {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}), " +
                      $"{summary.totalErrors} errors");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Puzzle Kingdoms/Build Android APK (dev)")]
        public static void BuildDevApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            SceneBuilder.EnsureReady();
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
