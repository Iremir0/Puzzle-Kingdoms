using System.IO;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Views.Battle3D;
using UnityEditor;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// PB-11 check: builds a real battle (stage 1-3 against a full player army), plays it through
    /// <see cref="BattleWorld3D"/> exactly like the battle screen does and saves a few frames to
    /// <c>Logs/Battle3D/</c>, so the 3D view can be judged without opening the game.
    /// Menu: Puzzle Kingdoms → Battle 3D Screenshots. Batch mode:
    /// -executeMethod PuzzleKingdoms.EditorTools.BattleView3DSpike.Capture
    /// </summary>
    public static class BattleView3DSpike
    {
        private const string ContentPath = "Assets/_Project/Data/GameContent.asset";
        private const string OutputFolder = "Logs/Battle3D";
        private static readonly int[] CaptureTicks = { 0, 80, 140, 200, 280, 400 };

        [MenuItem("Puzzle Kingdoms/Battle 3D Screenshots")]
        public static void Capture()
        {
            // The game registers every unit's colour and look at start-up; do the same here.
            foreach (string guid in AssetDatabase.FindAssets("t:UnitDefinition"))
            {
                PuzzleKingdoms.Views.Common.Palette.Register(AssetDatabase.LoadAssetAtPath<UnitDefinition>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            GameContent content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
            UnitModels.Register(content.BattleModels);
            StageDefinition stage = content.Campaign[content.Campaign.Count - 1];
            Formation player = BuildPlayerArmy(content);
            Formation enemy = Formation.ForStage(stage.ToStageDef());
            var sim = new BattleSimulator(content.BattleRules.Rules, content.ArmyRules.Rules, player, enemy, 1);

            Directory.CreateDirectory(OutputFolder);
            BattleWorld3D world = BattleWorld3D.Create(sim);
            world.Camera.Render(); // warm-up frame: the very first render of a camera is badly lit
            const float tickSeconds = 1f / 20f;
            int shot = 0;
            for (int tick = 0; tick <= CaptureTicks[CaptureTicks.Length - 1] && !sim.IsFinished; tick++)
            {
                if (tick == 90)
                {
                    sim.ActivateRally();
                }
                if (shot < CaptureTicks.Length && tick == CaptureTicks[shot])
                {
                    world.Render(1f, tickSeconds);
                    Save(world, $"{OutputFolder}/battle-{tick:D3}.png");
                    shot++;
                }
                world.OnStep(sim.Step());
                world.Render(1f, tickSeconds);
            }
            Debug.Log($"[BattleView3DSpike] stage {stage.Id}: {shot} frames, tick {sim.Tick}, finished={sim.IsFinished}");
            world.Dispose();
        }

        private static Formation BuildPlayerArmy(GameContent content)
        {
            var army = new Formation();
            UnitDef warrior = content.RecruitableUnits[0].Def;
            UnitDef archer = content.RecruitableUnits[1].Def;
            UnitDef mage = content.RecruitableUnits[2].Def;
            for (int lane = 0; lane < Formation.Size; lane++)
            {
                army.Place(new ArmyUnit(warrior, lane == 1 ? 2 : 1), new GridPos(lane, 0));
                army.Place(new ArmyUnit(archer, lane == 2 ? 3 : 1), new GridPos(lane, 1));
                army.Place(new ArmyUnit(mage), new GridPos(lane, 2));
            }
            return army;
        }

        private static void Save(BattleWorld3D world, string path)
        {
            world.Camera.Render();
            RenderTexture texture = world.Texture;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }
    }
}
