namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Experimental.GraphView;
    public abstract class VoxelNode : Node
    {
        public string GUID;

        public abstract string GetHLSL(ref int varCount, out string varName);

        protected Port GeneratePort(Direction direction, Port.Capacity capacity = Port.Capacity.Single)
        {
            return InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(float));
        }

        // Улучшенный метод получения ввода: учитывает суффиксы .x, .y, .z
        protected string GetInputHLSL(Port port, ref int varCount, out string varName)
        {
            var connection = port.connections.FirstOrDefault();
            if (connection != null)
            {
                var connectedNode = connection.output.node as VoxelNode;
                string code = connectedNode.GetHLSL(ref varCount, out varName);

                // Если порт называется X, Y или Z - добавляем суффикс к имени переменной
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
