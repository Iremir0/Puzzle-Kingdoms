using PuzzleKingdoms.Data;
using PuzzleKingdoms.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// Generates the grey-box scene instead of editing it by hand (D-025): a
    /// camera and one <see cref="GameApp"/> wired to GameContent. Safe to re-run.
    /// Menu: Puzzle Kingdoms → Build Main Scene. Batch mode:
    /// -executeMethod PuzzleKingdoms.EditorTools.SceneBuilder.Build
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        public const string ContentPath = "Assets/_Project/Data/GameContent.asset";

        [MenuItem("Puzzle Kingdoms/Build Main Scene")]
        public static void Build()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
            if (content == null)
            {
                throw new System.InvalidOperationException($"Missing {ContentPath}");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x1E, 0x22, 0x30, 0xFF);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var app = new GameObject("GameApp").AddComponent<GameApp>();
            var serialized = new SerializedObject(app);
            serialized.FindProperty("content").objectReferenceValue = content;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneBuilder] wrote {ScenePath}");
        }
    }
}
