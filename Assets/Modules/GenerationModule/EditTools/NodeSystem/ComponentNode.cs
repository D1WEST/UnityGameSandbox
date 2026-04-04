using System.Collections.Generic;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;

    public class ComponentNode : VoxelNode
    {
        public Port Input;

        public ComponentNode()
        {
            title = "Split Vector3";
            GUID = System.Guid.NewGuid().ToString();

            Input = GeneratePort(Direction.Input);
            Input.portName = "float3 In";
            inputContainer.Add(Input);

            // Создаем выходы с именами X, Y, Z (важно для суффиксов в GetInputHLSL)
            AddOutput("X");
            AddOutput("Y");
            AddOutput("Z");

            RefreshExpandedState();
            RefreshPorts();
        }

        private void AddOutput(string name)
        {
            var p = GeneratePort(Direction.Output);
            p.portName = name;
            outputContainer.Add(p);
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            // В этой ноде мы не кэшируем саму ноду, так как она лишь пробрасывает компоненты
            return GetInputHLSL(Input, ref varCount, out varName, cache);
        }
    }
}
