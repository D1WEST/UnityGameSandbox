namespace Assets.Modules.GenerationModule.EditTools
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "NewVoxelGraph", menuName = "Voxels/Voxel Graph")]
    public class VoxelGraphData : ScriptableObject
    {
        public List<NodeSerializedData> Nodes = new List<NodeSerializedData>();
        public List<EdgeSerializedData> Edges = new List<EdgeSerializedData>();
    }

    [System.Serializable]
    public class NodeSerializedData
    {
        public string GUID;
        public string Type;     // Полное имя класса
        public Vector2 Position;
        public string Data;     // Параметры ноды (Scale, Color и т.д.)
        public int PortCount;   // Для динамических портов (OutputNode)
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
