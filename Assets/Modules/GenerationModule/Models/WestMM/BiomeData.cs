using Unity.Mathematics;
using UnityEngine;

namespace Assets.Modules.GenerationModule.Models.WestMM
{
    [System.Serializable]
    public struct BiomeData
    {
        public float targetTemp;
        public float minHeight;
        public float maxHeight;
        public float detailScale;
        public float detailAmplitude;
        public float biomeWeightMultiplier;
        public float2 padding; // Это 8 байт
        public Color biomeColor; // Это 16 байт. Итого: 24 (6 флоатов) + 8 + 16 = 48 байт.
    }
}
