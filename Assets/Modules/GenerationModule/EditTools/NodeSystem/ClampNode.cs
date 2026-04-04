namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;
    public class ClampNode : VoxelNode
    {
        public Port InputIn, InputMin, InputMax;
        public ClampNode()
        {
            title = "Clamp";
            InputIn = GeneratePort(Direction.Input); InputIn.portName = "In"; inputContainer.Add(InputIn);
            InputMin = GeneratePort(Direction.Input); InputMin.portName = "Min"; inputContainer.Add(InputMin);
            InputMax = GeneratePort(Direction.Input); InputMax.portName = "Max"; inputContainer.Add(InputMax);
            outputContainer.Add(GeneratePort(Direction.Output));
            RefreshPorts();
        }
        public override string GetHLSL(ref int varCount, out string varName)
        {
            string cIn = GetInputHLSL(InputIn, ref varCount, out string nIn);
            string cMin = GetInputHLSL(InputMin, ref varCount, out string nMin);
            string cMax = GetInputHLSL(InputMax, ref varCount, out string nMax);
            varName = $"clamp_{varCount++}";
            return cIn + cMin + cMax + $"float {varName} = clamp({nIn}, {nMin}, {nMax});\n";
        }
    }
}
