namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;
    using UnityEngine.UIElements;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class VoxelNode : Node
    {
        public string GUID;

        // Обновленная сигнатура для всех нод
        public abstract string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache);

        // Исправленный метод создания портов с параметром типа
        protected Port GeneratePort(Direction direction, Port.Capacity capacity = Port.Capacity.Single, System.Type type = null)
        {
            if (type == null) type = typeof(float);
            var cap = (direction == Direction.Output) ? Port.Capacity.Multi : Port.Capacity.Single;
            return InstantiatePort(Orientation.Horizontal, direction, cap, type);
        }
        public virtual void RefreshUI() { }
        // Делаем PUBLIC, чтобы VoxelGraphEditor мог вызывать этот метод
        public string GetInputHLSL(Port port, ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            var connection = port.connections.FirstOrDefault();
            if (connection != null)
            {
                var connectedNode = connection.output.node as VoxelNode;
                string code = connectedNode.GetHLSL(ref varCount, out varName, cache);

                string pName = connection.output.portName;
                if (pName == "X") varName += ".x";
                else if (pName == "Y") varName += ".y";
                else if (pName == "Z") varName += ".z";

                return code;
            }
            varName = "0.0f";
            return "";
        }
    }
}
