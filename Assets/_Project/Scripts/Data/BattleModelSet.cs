using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleKingdoms.Data
{
    /// <summary>
    /// Which 3D model stands in for each <see cref="UnitLook"/> in the 3D battle (PB-11, D-042): the imported
    /// model, its material (URP Lit with the base-colour texture) and the rotation that stands a Meshy export up
    /// and makes it face +Z. Built by <c>Puzzle Kingdoms → Build Battle Models</c>; a look without an entry is
    /// drawn as a coloured capsule.
    /// </summary>
    [CreateAssetMenu(menuName = "Puzzle Kingdoms/Battle Model Set", fileName = "BattleModels")]
    public sealed class BattleModelSet : ScriptableObject
    {
        /// <summary>Height of a tier 1 unit in metres (one cell is 1.6 m wide).</summary>
        public const float UnitHeightMetres = 1.15f;

        [Serializable]
        public struct Entry
        {
            public UnitLook look;
            [Tooltip("The imported model (FBX). Its own size does not matter: it is scaled to the unit height.")]
            public GameObject model;
            [Tooltip("Material to put on every part of the model; empty keeps the imported materials.")]
            public Material material;
            [Tooltip("Euler angles that make the model stand upright and face +Z (Meshy exports lie on their back).")]
            public Vector3 uprightEuler;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(UnitLook look, out Entry entry)
        {
            foreach (Entry candidate in entries)
            {
                if (candidate.look == look && candidate.model != null)
                {
                    entry = candidate;
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }
}
