using System.Collections.Generic;
using System.IO;
using PuzzleKingdoms.Data;
using UnityEditor;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// PB-11: builds the data of the 3D battle models from <c>Assets/_Project/Art/Characters/&lt;Name&gt;/</c> so it is
    /// reproducible instead of clicked by hand: one URP Lit material per model (with its base-colour texture), the
    /// <c>Data/BattleModels.asset</c> (<see cref="BattleModelSet"/>: look → model, material, upright rotation) and the
    /// reference to it in <c>GameContent</c>. The upright rotation comes from <c>ArtSpike.FacingCamera</c> (found by
    /// rendering), turned half a circle so the model faces +Z, the way a player unit looks into the battle.
    /// Menu: Puzzle Kingdoms → Build Battle Models. Batch mode:
    /// -executeMethod PuzzleKingdoms.EditorTools.BattleModelBuilder.Build
    /// </summary>
    public static class BattleModelBuilder
    {
        private const string SetPath = "Assets/_Project/Data/BattleModels.asset";
        private const string ContentPath = "Assets/_Project/Data/GameContent.asset";

        /// <summary>Model folder name → the look that uses it.</summary>
        private static readonly (string Name, UnitLook Look)[] Models =
        {
            ("Hero_Knight", UnitLook.Warrior),
            ("Hero_Archer", UnitLook.Archer),
            ("Hero_Mage", UnitLook.Mage),
            ("Goblin_Grunt", UnitLook.GoblinGrunt),
            ("Goblin_Archer", UnitLook.GoblinArcher),
            ("Goblin_Brute", UnitLook.GoblinBrute),
            ("Goblin_Chief", UnitLook.GoblinChief),
        };

        [MenuItem("Puzzle Kingdoms/Build Battle Models")]
        public static void Build()
        {
            var set = AssetDatabase.LoadAssetAtPath<BattleModelSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<BattleModelSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }

            var entries = new List<BattleModelSet.Entry>();
            foreach ((string name, UnitLook look) in Models)
            {
                string folder = $"{ArtSpike.CharactersRoot}{name}";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{name}.fbx");
                if (model == null)
                {
                    Debug.LogWarning($"[BattleModelBuilder] {folder}/{name}.fbx not found, {look} stays a capsule.");
                    continue;
                }
                Quaternion facing = ArtSpike.FacingCamera.TryGetValue(name, out Quaternion known) ? known : Quaternion.Euler(90f, 0f, 0f);
                entries.Add(new BattleModelSet.Entry
                {
                    look = look,
                    model = model,
                    material = BuildMaterial(folder, name),
                    uprightEuler = (Quaternion.Euler(0f, 180f, 0f) * facing).eulerAngles,
                });
            }

            var serialized = new SerializedObject(set);
            SerializedProperty list = serialized.FindProperty("entries");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("look").enumValueIndex = (int)entries[i].look;
                item.FindPropertyRelative("model").objectReferenceValue = entries[i].model;
                item.FindPropertyRelative("material").objectReferenceValue = entries[i].material;
                item.FindPropertyRelative("uprightEuler").vector3Value = entries[i].uprightEuler;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);

            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
            var contentObject = new SerializedObject(content);
            contentObject.FindProperty("battleModels").objectReferenceValue = set;
            contentObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);

            AssetDatabase.SaveAssets();
            Debug.Log($"[BattleModelBuilder] {entries.Count} models in {SetPath}, linked in GameContent.");
        }

        private static Material BuildMaterial(string folder, string name)
        {
            string path = $"{folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{name}.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
