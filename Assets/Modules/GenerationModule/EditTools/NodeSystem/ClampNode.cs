namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Collections.Generic;
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
        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            // 1. Проверка кэша
            if (cache.TryGetValue(this, out varName)) return "";

            // 2. Получаем ввод от портов, передавая cache дальше
            string cIn = GetInputHLSL(InputIn, ref varCount, out string nIn, cache);
            string cMin = GetInputHLSL(InputMin, ref varCount, out string nMin, cache);
            string cMax = GetInputHLSL(InputMax, ref varCount, out string nMax, cache);

            // 3. Создаем уникальное имя переменной
            varName = $"clamp_{varCount++}";

            // 4. Регистрируем в кэше
            cache.Add(this, varName);

            // 5. Формируем HLSL код
            return cIn + cMin + cMax + $"float {varName} = clamp({nIn}, {nMin}, {nMax});\n";
        }
    }
}
