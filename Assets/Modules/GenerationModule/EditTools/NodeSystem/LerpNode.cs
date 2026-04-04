namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Collections.Generic;
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
        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";

            // Передаем cache в каждый вход
            string cA = GetInputHLSL(InputA, ref varCount, out string nA, cache);
            string cB = GetInputHLSL(InputB, ref varCount, out string nB, cache);
            string cT = GetInputHLSL(InputT, ref varCount, out string nT, cache);

            varName = $"mix_{varCount++}";
            cache.Add(this, varName);

            string type = this is LerpColorNode ? "float4" : "float";
            return cA + cB + cT + $"{type} {varName} = lerp({nA}, {nB}, {nT});\n";
        }
    }
}
