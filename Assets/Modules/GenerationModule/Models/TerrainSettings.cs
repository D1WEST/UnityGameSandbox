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

        [Header("Biomes (2D)")]
        public float biomeScale;      // Масштаб пятен биомов (0.002)
        public float oceanHeight;     // Высота дна (10)
        public float plainsHeight;    // Высота равнин (30)
        public float mountainHeight;  // Высота гор (80)

        [Header("3D Details")]
        public float detailScale;     // Частота 3D неровностей (0.03)
        public float detailAmplitude; // СИЛА 3D неровностей (5) - ИМЕННО ОНА ЛОМАЛА КАРТИНКУ!

        [Header("Caves - Hubs")]
        public float hubScale;      // Очень маленький (0.004)
        public float hubThreshold;  // Порог для залов (0.1 - 0.2)

        [Header("Caves - Branches")]
        public float branchScale;     // Средний (0.02)
        public float branchThreshold; // Порог для тоннелей (0.05)
    }
}
