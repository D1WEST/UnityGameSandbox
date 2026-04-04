namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Collections.Generic;
    using UnityEditor.Experimental.GraphView;
    public enum MathType { Add, Subtract, Multiply, Divide }

    public class MathNode : VoxelNode
    {
        public MathType Operation;
        public Port InputA;
        public Port InputB;

        public MathNode()
        {
            title = "Math";
            GUID = System.Guid.NewGuid().ToString();

            InputA = GeneratePort(Direction.Input);
            InputA.portName = "A";
            inputContainer.Add(InputA);

            InputB = GeneratePort(Direction.Input);
            InputB.portName = "B";
            inputContainer.Add(InputB);

            var output = GeneratePort(Direction.Output);
            output.portName = "Out";
            outputContainer.Add(output);

            RefreshExpandedState();
            RefreshPorts();
        }

        public MathNode(MathType type) : this()
        {
            SetOperation(type);
        }

        public void SetOperation(MathType type)
        {
            Operation = type;
            title = type.ToString();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";

            string codeA = GetInputHLSL(InputA, ref varCount, out string nameA, cache);
            string codeB = GetInputHLSL(InputB, ref varCount, out string nameB, cache);

            varName = $"math_{varCount++}";
            cache.Add(this, varName);

            string op = Operation switch
            {
                MathType.Add => "+",
                MathType.Subtract => "-",
                MathType.Multiply => "*",
                MathType.Divide => "/",
                _ => "+"
            };

            return codeA + codeB + $"float {varName} = {nameA} {op} {nameB};\n";
        }
    }
}
