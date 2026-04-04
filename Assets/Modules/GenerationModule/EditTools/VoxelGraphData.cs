namespace Assets.Modules.GenerationModule.EditTools
{
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Mathematics;

    [CreateAssetMenu(fileName = "NewVoxelGraph", menuName = "Voxels/Voxel Graph")]
    public class VoxelGraphData : ScriptableObject
    {
        public List<NodeSerializedData> Nodes = new List<NodeSerializedData>();
        public List<EdgeSerializedData> Edges = new List<EdgeSerializedData>();

        // --- ДАННЫЕ ДЛЯ RUNTIME (Замена WorldProfile) ---
        [Header("Baked Data for CPU")]
        public float selectorScale = 0.001f;
        public BakedBiome[] bakedBiomes;
    }

    [System.Serializable]
    public struct BakedBiome // Компактная структура для Burst
    {
        public float targetTemp;
        public float4 color;
    }

    [System.Serializable]
    public class NodeSerializedData
    {
        public string GUID;
        public string Type;
        public Vector2 Position;
        public string Data;
        public int PortCount;
    }

    [System.Serializable]
    public class EdgeSerializedData
    {
        public string OutputNodeGUID;
        public string InputNodeGUID;
        public string OutputPortName;
        public string InputPortName;
    }
}
