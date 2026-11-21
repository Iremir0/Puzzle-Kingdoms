using System;
using PuzzleKingdoms.Logic.Army;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// Designer-editable tier 1 stats of a unit class (player or enemy, D-015).
    /// Logic never sees this asset, only the <see cref="UnitDef"/> built from it.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Unit Definition", fileName = "NewUnit")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField, Min(1)] private int maxHp = 100;
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField] private DamageType damageType = DamageType.Physical;
        [Tooltip("Seconds between attacks.")]
        [SerializeField, Min(0.05f)] private float attackInterval = 1f;
        [Tooltip("Attack range in battlefield cells.")]
        [SerializeField, Min(0.1f)] private float attackRange = 1f;
        [Tooltip("Cells per second.")]
        [SerializeField, Min(0f)] private float moveSpeed = 1f;
        [Tooltip("Fraction of Physical damage blocked.")]
        [SerializeField, Range(0f, 0.95f)] private float armor;
        [Tooltip("Fraction of Magic damage blocked.")]
        [SerializeField, Range(0f, 0.95f)] private float ward;
        [Header("Behaviours (PB-18); all off by default")]
        [Tooltip("Fraction of ranged damage blocked while this unit is the front-most of its side (shield goblin).")]
        [SerializeField, Range(0f, 0.95f)] private float rangedDamageReduction;
        [Tooltip("HP given to the most hurt ally in range per heal; 0 = cannot heal (shaman).")]
        [SerializeField, Min(0)] private int healAmount;
        [SerializeField, Min(0.1f)] private float healInterval = 2f;
        [SerializeField, Min(0.1f)] private float healRange = 3.5f;
        [Tooltip("Attacks the back-most row of the other side first (wolf rider).")]
        [SerializeField] private bool targetsBackRow;
        [Tooltip("Others this close to the target take splash damage; 0 = none (bomber).")]
        [SerializeField, Min(0f)] private float splashRadius;
        [SerializeField, Range(0f, 1f)] private float splashFraction = 0.5f;
        [Tooltip("Enemies this close to this unit attack it first (taunt); 0 = none (defender, PB-21).")]
        [SerializeField, Min(0f)] private float tauntRadius;
        [Header("Chapter 3 behaviours (PB-41); all off by default")]
        [Tooltip("Share of its max HP restored every second; 0 = none (bog troll).")]
        [SerializeField, Range(0f, 0.2f)] private float regenPercent;
        [Tooltip("At or below this share of its HP the unit attacks faster; 0 = never (berserker).")]
        [SerializeField, Range(0f, 0.95f)] private float enrageBelow;
        [SerializeField, Min(1f)] private float enrageSpeed = 2f;
        [Tooltip("Enemies this close take a blast when the unit dies; 0 = none (sapper).")]
        [SerializeField, Min(0f)] private float deathBurstRadius;
        [Tooltip("The blast as a multiple of the unit's own damage.")]
        [SerializeField, Min(0f)] private float deathBurstDamage = 3f;
        [Tooltip("Id of the stage whose first win unlocks this class (PB-21); empty = always available.")]
        [SerializeField] private string unlockStageId = "";

        [Header("Look")]
        [Tooltip("Placeholder color for grey-box views.")]
        [SerializeField] private Color color = Color.white;
        [Tooltip("Silhouette drawn in the grey-box battle view.")]
        [SerializeField] private UnitLook look = UnitLook.Generic;

        [NonSerialized] private UnitDef _def;

        public Color Color => color;

        public UnitLook Look => look;

        public string UnlockStageId => unlockStageId;

        /// <summary>Logic definition. Cached so every user shares one instance (merge compares by reference).</summary>
        public UnitDef Def => _def ??= new UnitDef
        {
            Id = id,
            DisplayName = displayName,
            MaxHp = maxHp,
            Damage = damage,
            DamageType = damageType,
            AttackInterval = attackInterval,
            AttackRange = attackRange,
            MoveSpeed = moveSpeed,
            Armor = armor,
            Ward = ward,
            RangedDamageReduction = rangedDamageReduction,
            HealAmount = healAmount,
            HealInterval = healInterval,
            HealRange = healRange,
            TargetsBackRow = targetsBackRow,
            SplashRadius = splashRadius,
            SplashFraction = splashFraction,
            TauntRadius = tauntRadius,
            RegenPercent = regenPercent,
            EnrageBelow = enrageBelow,
            EnrageSpeed = enrageSpeed,
            DeathBurstRadius = deathBurstRadius,
            DeathBurstDamage = deathBurstDamage,
        };

        private void OnValidate() => _def = null;
    }
}
