namespace Assets.Modules.GenerationModule.Models.WestMM
{
    using System.Collections.Generic;
    using UnityEngine;
    [CreateAssetMenu(fileName = "NewVoxelGraph", menuName = "Voxels/Voxel Graph")]
    public class VoxelGraphData : ScriptableObject
    {
        [SerializeField] public List<NodeData> Nodes = new List<NodeData>();
        [SerializeField] public List<ConnectionData> Connections = new List<ConnectionData>();
    }

    [System.Serializable]
    public class NodeData
    {
        public string GUID;
        public string Type; // Имя класса ноды
        public Vector2 Position;
        public string ExtraData; // Для хранения типа шума, масштаба и т.д.
    }

    [System.Serializable]
    public class ConnectionData
    {
        public string OutputNodeGUID;
        public string InputNodeGUID;
        public string OutputPortName;
        public string InputPortName;
    }
}
