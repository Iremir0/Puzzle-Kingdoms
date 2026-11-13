using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
            EnsureRenderPipeline();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Player settings applied.");
        }

        public const string SettingsFolder = "Assets/_Project/Settings";
        private const string PipelinePath = SettingsFolder + "/URP_Mobile.asset";
        private const string RendererPath = SettingsFolder + "/URP_Mobile_Renderer.asset";

        /// <summary>Creates the URP asset once and assigns it to Graphics and every quality level (D-024).</summary>
        private static void EnsureRenderPipeline()
        {
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Settings");
            }

            // URP generates these at the Assets root; keep everything we own under _Project (GUIDs survive the move).
            MoveIfAtRoot("Assets/UniversalRenderPipelineGlobalSettings.asset");
            MoveIfAtRoot("Assets/DefaultVolumeProfile.asset");

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = false; // mobile: cheaper framebuffer
                pipeline.msaaSampleCount = 2;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        private static void MoveIfAtRoot(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                return;
            }
            string target = SettingsFolder + "/" + System.IO.Path.GetFileName(path);
            string error = AssetDatabase.MoveAsset(path, target);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"[ProjectSetup] could not move {path}: {error}");
            }
        }
    }
}
