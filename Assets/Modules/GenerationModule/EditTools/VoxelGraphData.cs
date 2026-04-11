using System.Collections.Generic;
using UnityEngine;

namespace Assets.Modules.GenerationModule.EditTools
{
    [CreateAssetMenu(fileName = "NewVoxelGraph", menuName = "Voxels/Voxel Graph")]
    public class VoxelGraphData : ScriptableObject
    {
        public List<NodeSerializedData> Nodes = new List<NodeSerializedData>();
        public List<EdgeSerializedData> Edges = new List<EdgeSerializedData>();

        [Header("Baked Data for GPU")]
        public float selectorScale = 0.001f;
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