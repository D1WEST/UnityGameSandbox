using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Modules.GenerationModule.Models
{
    [System.Serializable]
    public struct TerrainSettings
    {
        public float seed;

        [Header("World Limits")]
        public int minChunkY;
        public int maxChunkY;

        [Header("Caves - Hubs")]
        public float hubScale;      // (например, 0.03)
        public float hubThreshold;  // (например, 0.4)

        [Header("Caves - Branches")]
        public float branchScale;     // (например, 0.01)
        public float branchThreshold; // (например, 0.025)
    }
}
