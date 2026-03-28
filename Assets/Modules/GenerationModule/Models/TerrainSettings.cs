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

        [Header("Caves (3D)")]
        public float caveScale;
        public float caveThreshold;
    }
}
