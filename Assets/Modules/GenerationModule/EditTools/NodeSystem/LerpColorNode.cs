namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;

    using System.Collections.Generic;
    using UnityEngine.UIElements;

    public class LerpColorNode : VoxelNode
    {
        public Port InputA, InputB, InputT;

        public LerpColorNode()
        {
            title = "Mix Colors (Lerp)";
            GUID = System.Guid.NewGuid().ToString();

            // Указываем тип порта Vector4 для цвета
            InputA = GeneratePort(Direction.Input, type: typeof(UnityEngine.Vector4));
            InputA.portName = "Color A";
            inputContainer.Add(InputA);

            InputB = GeneratePort(Direction.Input, type: typeof(UnityEngine.Vector4));
            InputB.portName = "Color B";
            inputContainer.Add(InputB);

            InputT = GeneratePort(Direction.Input); // float
            InputT.portName = "Alpha";
            inputContainer.Add(InputT);

            var outPort = GeneratePort(Direction.Output, type: typeof(UnityEngine.Vector4));
            outPort.portName = "Out";
            outputContainer.Add(outPort);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            // 1. Проверка кэша (чтобы не дублировать код)
            if (cache.TryGetValue(this, out varName)) return "";

            // 2. Получаем код от входов, передавая cache дальше
            string cA = GetInputHLSL(InputA, ref varCount, out string nA, cache);
            string cB = GetInputHLSL(InputB, ref varCount, out string nB, cache);
            string cT = GetInputHLSL(InputT, ref varCount, out string nT, cache);

            // Исправляем дефолтные значения для цвета, если ничего не подключено
            if (nA == "0.0f") nA = "float4(1,1,1,1)";
            if (nB == "0.0f") nB = "float4(0,0,0,1)";

            varName = $"color_mix_{varCount++}";
            cache.Add(this, varName);

            // Возвращаем собранную строку кода
            return cA + cB + cT + $"float4 {varName} = lerp({nA}, {nB}, {nT});\n";
        }
    }
}
