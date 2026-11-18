using System.Collections.Generic;
using PuzzleKingdoms.Logic.Analytics;

namespace PuzzleKingdoms.Logic.Stages
{
    /// <summary>
    /// Applies remote-config overrides to one stage (PB-29, D-047), so the moves and the enemy strength
    /// of a stage can be tuned after release without a build. Keys, with the stage id:
    /// <c>stage.&lt;id&gt;.moves</c>, <c>.stones</c>, <c>.enemy_hp</c>, <c>.enemy_damage</c>. A missing key
    /// keeps the asset's value; a value out of its range is ignored, so a typo cannot break a stage.
    /// </summary>
    public static class StageTuning
    {
        public const int MinMoves = 1;
        public const int MaxMoves = 50;
        public const int MaxStones = 30;
        public const float MinMultiplier = 0.25f;
        public const float MaxMultiplier = 5f;

        public static string Key(string stageId, string field) => $"stage.{stageId}.{field}";

        /// <summary>Changes <paramref name="stage"/> in place; returns the keys that were applied.</summary>
        public static List<string> Apply(StageDef stage, RemoteConfig config)
        {
            var applied = new List<string>();
            if (stage == null || config == null || config.Count == 0)
            {
                return applied;
            }

            string key = Key(stage.Id, "moves");
            if (config.TryGetInt(key, out int moves) && moves >= MinMoves && moves <= MaxMoves)
            {
                stage.Moves = moves;
                applied.Add(key);
            }
            key = Key(stage.Id, "stones");
            if (config.TryGetInt(key, out int stones) && stones >= 0 && stones <= MaxStones)
            {
                stage.StoneGems = stones;
                applied.Add(key);
            }
            key = Key(stage.Id, "enemy_hp");
            if (config.TryGetFloat(key, out float hp) && hp >= MinMultiplier && hp <= MaxMultiplier)
            {
                stage.EnemyHpMultiplier = hp;
                applied.Add(key);
            }
            key = Key(stage.Id, "enemy_damage");
            if (config.TryGetFloat(key, out float damage) && damage >= MinMultiplier && damage <= MaxMultiplier)
            {
                stage.EnemyDamageMultiplier = damage;
                applied.Add(key);
            }
            return applied;
        }
    }
}
