using System.Collections.Generic;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;

    public class PositionNode : VoxelNode
    {
        public PositionNode()
        {
            title = "World Position";
            GUID = System.Guid.NewGuid().ToString();

            // Создаем три выхода для X, Y, Z
            AddOutputPort("X");
            AddOutputPort("Y");
            AddOutputPort("Z");

            RefreshPorts();
        }
        private void AddOutputPort(string name)
        {
            var port = GeneratePort(Direction.Output);
            port.portName = name;
            outputContainer.Add(port);
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            varName = "worldPos"; // Это вектор float3
            return "";
        }
    }
}
