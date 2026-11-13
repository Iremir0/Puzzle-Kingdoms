using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// Applies the project's fixed player settings (docs/SETUP.md §4, D-012/D-018)
    /// so they are reproducible instead of clicked by hand.
    /// Menu: Puzzle Kingdoms → Apply Project Settings. Batch mode:
    /// Unity -batchmode -quit -projectPath . -executeMethod PuzzleKingdoms.EditorTools.ProjectSetup.Apply
    /// </summary>
    public static class ProjectSetup
    {
        public const string CompanyName = "Puzzle Kingdoms Team";
        public const string ProductName = "Puzzle Kingdoms";
        public const string AndroidPackage = "com.puzzlekingdoms.game";
        private const int MinAndroidApi = 26;

        [MenuItem("Puzzle Kingdoms/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;

            // Portrait only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Android: IL2CPP + ARM64 is required by Google Play.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinAndroidApi;

            // Logic must stay deterministic and allocation-light; keep the default .NET Standard 2.1 profile.
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);

            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Player settings applied.");
        }
    }
}
