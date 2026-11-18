using System;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Progression;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// A commander (PB-23, docs/GDD.md §9): the Rally variant it brings, its numbers and the stage whose first
    /// win unlocks it. Numbers are edited in the Inspector (<c>Data/Commanders/*.asset</c>).
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Commander Definition", fileName = "NewCommander")]
    public sealed class CommanderDefinition : ScriptableObject
    {
        [Tooltip("Saved in the profile: never rename a commander that players may have chosen.")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField, TextArea(2, 3)] private string description = "";
        [Tooltip("Id of the stage whose first win unlocks this commander.")]
        [SerializeField] private string unlockStageId = "";
        [SerializeField] private Color color = Color.white;

        [Header("Rally")]
        [SerializeField] private RallyKind kind;
        [SerializeField, Min(0.5f)] private float durationSeconds = 5f;
        [Tooltip("Extra damage for the whole army while Rally lasts (0.15 = +15 %).")]
        [SerializeField, Min(0f)] private float damageBonus;
        [SerializeField, Min(0f)] private float attackSpeedBonus;

        [Header("Shield Wall")]
        [Tooltip("Share of damage the front row does not take while Rally lasts.")]
        [SerializeField, Range(0f, 0.9f)] private float frontRowDamageReduction;

        [Header("Arrow Rain")]
        [SerializeField, Min(0)] private int volleyCount;
        [Tooltip("Share of max HP each volley takes off every enemy of the back row.")]
        [SerializeField, Range(0f, 0.5f)] private float volleyPercentMaxHp;

        [Header("Fireball")]
        [SerializeField, Min(0f)] private float blastRadius;
        [Tooltip("Share of max HP the blast takes off every enemy near the densest group.")]
        [SerializeField, Range(0f, 1f)] private float blastPercentMaxHp;

        [NonSerialized] private Commander _commander;

        public string Id => id;
        public Color Color => color;
        public RallyKind Kind => kind;

        /// <summary>Logic object built from the asset (cached).</summary>
        public Commander Commander => _commander ??= new Commander
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            UnlockStageId = unlockStageId,
            Variant = new RallyVariant
            {
                Id = id,
                DisplayName = displayName,
                Kind = kind,
                DurationSeconds = durationSeconds,
                DamageBonus = damageBonus,
                AttackSpeedBonus = attackSpeedBonus,
                FrontRowDamageReduction = frontRowDamageReduction,
                VolleyCount = volleyCount,
                VolleyPercentMaxHp = volleyPercentMaxHp,
                BlastRadius = blastRadius,
                BlastPercentMaxHp = blastPercentMaxHp,
            },
        };

        private void OnValidate() => _commander = null;
    }
}
