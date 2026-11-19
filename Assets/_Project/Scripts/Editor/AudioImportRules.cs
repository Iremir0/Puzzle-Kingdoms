using UnityEditor;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// Sets sensible import settings for new audio files (PB-33, D-066), so a file dropped into the right folder is
    /// already cheap on a phone: music in <c>Assets/_Project/Audio/Music</c> streams from disk as Vorbis, effects in
    /// <c>Assets/_Project/Audio/Sfx</c> are mono Vorbis decoded when loaded (no delay, no streaming cost). Only files
    /// imported for the first time are touched, so a setting changed by hand stays. <i>Puzzle Kingdoms → Apply Audio
    /// Import Rules</i> applies the rules to the files already there.
    /// </summary>
    public sealed class AudioImportRules : AssetPostprocessor
    {
        public const string MusicFolder = "Assets/_Project/Audio/Music/";
        public const string SfxFolder = "Assets/_Project/Audio/Sfx/";

        private void OnPreprocessAudio()
        {
            if (assetImporter.importSettingsMissing)
            {
                Apply((AudioImporter)assetImporter, assetPath);
            }
        }

        /// <summary>The kind of the file by its folder: "music", "sfx" or "" for anything else (the placeholders stay as they are).</summary>
        public static string KindOf(string path) =>
            path.StartsWith(MusicFolder, System.StringComparison.Ordinal) ? "music"
            : path.StartsWith(SfxFolder, System.StringComparison.Ordinal) ? "sfx"
            : "";

        /// <summary>Applies the rule of the file's folder. Returns false when the folder has none.</summary>
        public static bool Apply(AudioImporter importer, string path)
        {
            string kind = KindOf(path);
            if (kind == "")
            {
                return false;
            }
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            if (kind == "music")
            {
                // One music stream plays at a time: stream it, keep it stereo, do not keep it in memory.
                settings.loadType = AudioClipLoadType.Streaming;
                settings.quality = 0.5f;
                importer.forceToMono = false;
            }
            else
            {
                // Short effects start without delay and cost no decoding while they play.
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = 0.6f;
                importer.forceToMono = true;
            }
            importer.defaultSampleSettings = settings;
            return true;
        }

        [MenuItem("Puzzle Kingdoms/Apply Audio Import Rules")]
        public static void ApplyToExisting()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is AudioImporter importer && Apply(importer, path))
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            Debug.Log($"[Audio] import rules applied to {changed} files");
        }
    }
}
