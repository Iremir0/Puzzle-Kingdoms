using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>What <see cref="MeshyRigImport.Apply"/> did to one model.</summary>
    public enum RigImportResult
    {
        /// <summary>The path is not a Meshy rig by our naming rule, or not a model at all.</summary>
        NotARig,
        /// <summary>Already a valid Humanoid without finger mapping, looping clips set: nothing to do.</summary>
        AlreadyOk,
        /// <summary>Settings were changed and the model now has a valid Humanoid avatar.</summary>
        Fixed,
        /// <summary>Unity still cannot build a Humanoid avatar for this model (see the log).</summary>
        Failed,
    }

    /// <summary>
    /// PB-12 / D-043: makes our Meshy-rigged models import as Humanoid by code instead of by clicking in the
    /// Inspector. Meshy's skeleton uses Mixamo bone names but adds extra "SmartRig" bones; Unity's automatic
    /// finger mapping then picks some of them and the avatar fails ("mixamorig:RightHand is not an ancestor of
    /// Bone_047"). Fingers are not needed in battle, so the finger mapping is dropped: 44 → 25 mapped bones and
    /// a valid avatar. Clips named Running, Walking or Idle are set to loop.
    /// Rule: a model file is ours when its name ends with <c>_Rig</c> or <c>_RigAnim</c>, or starts with
    /// <c>Clips_</c>, under <c>Assets/_Project/Art/</c> or <c>Assets/_Project/Sandbox/Meshy/</c>.
    /// Menu: Puzzle Kingdoms → Fix Meshy Rigs. Batch mode:
    /// Unity -batchmode -quit -projectPath . -executeMethod PuzzleKingdoms.EditorTools.MeshyRigImport.FixAll
    /// New or changed rigged models are also fixed automatically after an import (not in batch mode).
    /// </summary>
    public class MeshyRigImport : AssetPostprocessor
    {
        public static readonly string[] Roots = { "Assets/_Project/Art/", "Assets/_Project/Sandbox/Meshy/" };
        private static readonly string[] FingerNames = { "Thumb", "Index", "Middle", "Ring", "Little" };
        private static readonly string[] LoopingClips = { "Running", "Walking", "Idle" };

        /// <summary>Paths currently being fixed, so the reimport that follows a fix does not start another one.</summary>
        private static readonly HashSet<string> Busy = new HashSet<string>();

        /// <summary>True when <paramref name="assetPath"/> is a model that follows our Meshy rig naming rule.</summary>
        public static bool IsMeshyRigPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!Roots.Any(root => assetPath.StartsWith(root, System.StringComparison.Ordinal)))
            {
                return false;
            }
            string name = Path.GetFileNameWithoutExtension(assetPath);
            return name.EndsWith("_Rig", System.StringComparison.Ordinal)
                || name.EndsWith("_RigAnim", System.StringComparison.Ordinal)
                || name.StartsWith("Clips_", System.StringComparison.Ordinal);
        }

        /// <summary>True for the Humanoid finger bones (Left/Right Thumb, Index, Middle, Ring, Little ...).</summary>
        public static bool IsFingerBone(string humanName)
        {
            return !string.IsNullOrEmpty(humanName) && FingerNames.Any(finger => humanName.Contains(finger));
        }

        [MenuItem("Puzzle Kingdoms/Fix Meshy Rigs")]
        public static void FixAll()
        {
            var report = new StringBuilder();
            int fixedCount = 0;
            int failed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", Roots.Where(Directory.Exists).ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RigImportResult result = Apply(path);
                if (result == RigImportResult.NotARig)
                {
                    continue;
                }
                report.AppendLine($"{result,-10} {path}");
                fixedCount += result == RigImportResult.Fixed ? 1 : 0;
                failed += result == RigImportResult.Failed ? 1 : 0;
            }
            Debug.Log($"[MeshyRigImport] {fixedCount} fixed, {failed} failed.\n{report}");
            if (Application.isBatchMode)
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/meshy-rig-import.txt", report.ToString());
            }
        }

        /// <summary>Makes one model a valid Humanoid (see the class comment). Safe to call again and again.</summary>
        public static RigImportResult Apply(string assetPath)
        {
            if (!IsMeshyRigPath(assetPath) || !(AssetImporter.GetAtPath(assetPath) is ModelImporter importer))
            {
                return RigImportResult.NotARig;
            }
            if (!Busy.Add(assetPath))
            {
                return RigImportResult.NotARig;
            }
            try
            {
                if (IsInDesiredState(importer, assetPath))
                {
                    return RigImportResult.AlreadyOk;
                }

                // Pass 1: Humanoid with Unity's automatic mapping. This also fills humanDescription, even when the
                // avatar then fails.
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.SaveAndReimport();
                    importer = (ModelImporter)AssetImporter.GetAtPath(assetPath);
                }

                // Pass 2: no finger mapping, looping clips.
                HumanDescription description = importer.humanDescription;
                description.human = description.human.Where(bone => !IsFingerBone(bone.humanName)).ToArray();
                importer.humanDescription = description;
                importer.clipAnimations = LoopedClips(importer);
                importer.SaveAndReimport();

                if (HasValidHumanAvatar(assetPath))
                {
                    return RigImportResult.Fixed;
                }
                Debug.LogError($"[MeshyRigImport] {assetPath}: Unity still cannot build a Humanoid avatar. Open the model's Rig tab → Configure.");
                return RigImportResult.Failed;
            }
            finally
            {
                Busy.Remove(assetPath);
            }
        }

        private static bool IsInDesiredState(ModelImporter importer, string assetPath)
        {
            if (importer.animationType != ModelImporterAnimationType.Human
                || importer.humanDescription.human.Any(bone => IsFingerBone(bone.humanName))
                || !HasValidHumanAvatar(assetPath))
            {
                return false;
            }
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            // A re-downloaded file can hold other clips than the list frozen in the .meta (new clips would not show).
            ModelImporterClipAnimation[] inFile = importer.defaultClipAnimations;
            if (clips.Length != inFile.Length || !clips.Select(c => c.name).OrderBy(n => n).SequenceEqual(inFile.Select(c => c.name).OrderBy(n => n)))
            {
                return false;
            }
            return clips.Length == 0 || clips.All(clip => clip.loopTime == IsLoopingName(clip.name));
        }

        private static bool HasValidHumanAvatar(string assetPath)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Avatar>().FirstOrDefault();
            return avatar != null && avatar.isValid && avatar.isHuman;
        }

        private static bool IsLoopingName(string clipName)
        {
            return LoopingClips.Contains(clipName);
        }

        private static ModelImporterClipAnimation[] LoopedClips(ModelImporter importer)
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations; // always the clips the file holds now
            foreach (ModelImporterClipAnimation clip in clips)
            {
                bool loop = IsLoopingName(clip.name);
                clip.loopTime = loop;
                clip.loopPose = loop;
            }
            return clips;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Application.isBatchMode)
            {
                return;
            }
            foreach (string path in imported.Where(IsMeshyRigPath))
            {
                string captured = path;
                EditorApplication.delayCall += () => Apply(captured);
            }
        }
    }
}
