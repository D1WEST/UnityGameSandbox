using System.Collections.Generic;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;

    public class StepNode : VoxelNode
    {
        public Port InputIn, InputEdge;

        public StepNode()
        {
            title = "Step / Threshold";
            InputIn = GeneratePort(UnityEditor.Experimental.GraphView.Direction.Input);
            InputIn.portName = "In";
            inputContainer.Add(InputIn);
            InputEdge = GeneratePort(UnityEditor.Experimental.GraphView.Direction.Input);
            InputEdge.portName = "Edge";
            inputContainer.Add(InputEdge);
            outputContainer.Add(GeneratePort(UnityEditor.Experimental.GraphView.Direction.Output));
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";

            string cIn = GetInputHLSL(InputIn, ref varCount, out string nIn, cache);
            string cEdge = GetInputHLSL(InputEdge, ref varCount, out string nEdge, cache);

            varName = $"step_{varCount++}";
            cache.Add(this, varName);

            return cIn + cEdge + $"float {varName} = step({nEdge}, {nIn});\n";
        }
    }
}
