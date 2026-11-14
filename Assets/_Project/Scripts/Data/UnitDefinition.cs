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
        [Tooltip("Placeholder color for grey-box views.")]
        [SerializeField] private Color color = Color.white;
        [Tooltip("Silhouette drawn in the grey-box battle view.")]
        [SerializeField] private UnitLook look = UnitLook.Generic;

        [NonSerialized] private UnitDef _def;

        public Color Color => color;

        public UnitLook Look => look;

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
        };

        private void OnValidate() => _def = null;
    }
}
