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

        public override string GetHLSL(ref int varCount, out string varName)
        {
            string cIn = GetInputHLSL(InputIn, ref varCount, out string nIn);
            string cEdge = GetInputHLSL(InputEdge, ref varCount, out string nEdge);
            varName = $"step_{varCount++}";
            return cIn + cEdge + $"float {varName} = step({nEdge}, {nIn});\n";
        }
    }
}
