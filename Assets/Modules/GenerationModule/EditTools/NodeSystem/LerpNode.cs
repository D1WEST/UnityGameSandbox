namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;
    public class LerpNode : VoxelNode
    {
        public Port InputA, InputB, InputT;
        public LerpNode()
        {
            title = "Lerp (Mix)";
            InputA = GeneratePort(Direction.Input); InputA.portName = "A"; inputContainer.Add(InputA);
            InputB = GeneratePort(Direction.Input); InputB.portName = "B"; inputContainer.Add(InputB);
            InputT = GeneratePort(Direction.Input); InputT.portName = "Alpha (0-1)"; inputContainer.Add(InputT);
            var outp = GeneratePort(Direction.Output); outp.portName = "Out"; outputContainer.Add(outp);
            RefreshPorts();
        }
        public override string GetHLSL(ref int varCount, out string varName)
        {
            string cA = GetInputHLSL(InputA, ref varCount, out string nA);
            string cB = GetInputHLSL(InputB, ref varCount, out string nB);
            string cT = GetInputHLSL(InputT, ref varCount, out string nT);
            varName = $"lerp_{varCount++}";
            return cA + cB + cT + $"float {varName} = lerp({nA}, {nB}, {nT});\n";
        }
    }
}
