using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Kingdom
{
    /// <summary>
    /// The permanent bonuses the player's buildings give (PB-22). Applied in one place: when a battle's player
    /// units are created (<see cref="Combat.Combatant"/>) and when the victory gold is paid
    /// (<see cref="Progression.StageReward"/>). <see cref="None"/> changes nothing.
    /// </summary>
    public sealed class KingdomBonuses
    {
        public static readonly KingdomBonuses None = new KingdomBonuses();

        private readonly Dictionary<string, float> _hp = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _attackSpeed = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _damage = new Dictionary<string, float>();
        private float _gold;

        public void Add(BuildingStat stat, string unitId, float bonus)
        {
            switch (stat)
            {
                case BuildingStat.UnitHp: AddTo(_hp, unitId, bonus); break;
                case BuildingStat.UnitAttackSpeed: AddTo(_attackSpeed, unitId, bonus); break;
                case BuildingStat.UnitDamage: AddTo(_damage, unitId, bonus); break;
                case BuildingStat.StageGold: _gold += bonus; break;
            }
        }

        private static void AddTo(Dictionary<string, float> map, string unitId, float bonus)
        {
            map.TryGetValue(unitId ?? "", out float current);
            map[unitId ?? ""] = current + bonus;
        }

        private static float Multiplier(Dictionary<string, float> map, string unitId) =>
            1f + (map.TryGetValue(unitId ?? "", out float bonus) ? bonus : 0f);

        /// <summary>Every bonus added so far, for saving a replay (PB-28).</summary>
        public System.Collections.Generic.List<(BuildingStat stat, string unitId, float bonus)> Entries()
        {
            var list = new System.Collections.Generic.List<(BuildingStat, string, float)>();
            foreach (var pair in _hp) { list.Add((BuildingStat.UnitHp, pair.Key, pair.Value)); }
            foreach (var pair in _attackSpeed) { list.Add((BuildingStat.UnitAttackSpeed, pair.Key, pair.Value)); }
            foreach (var pair in _damage) { list.Add((BuildingStat.UnitDamage, pair.Key, pair.Value)); }
            if (_gold != 0f) { list.Add((BuildingStat.StageGold, "", _gold)); }
            return list;
        }

        public float HpMultiplier(string unitId) => Multiplier(_hp, unitId);

        public float DamageMultiplier(string unitId) => Multiplier(_damage, unitId);

        /// <summary>Attacks per second are multiplied by this; the interval is divided by it.</summary>
        public float AttackSpeedMultiplier(string unitId) => Multiplier(_attackSpeed, unitId);

        public float GoldMultiplier => 1f + _gold;
    }
}
