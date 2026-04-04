using System.Collections.Generic;
using Assets.Modules.GenerationModule.EditTools.NodeSystem;
using System.IO;
using UnityEngine;

namespace Assets.Modules.GenerationModule.Shaders
{
    public class ShaderGenerator
    {
        public static void GenerateComputeShader(VoxelNode finalOutputNode)
        {
            int varCounter = 0;
            string resultVariable;

            // Рекурсивно генерируем HLSL код начиная с последней ноды
            string generatedHLSL = finalOutputNode.GetHLSL(ref varCounter, out resultVariable, new Dictionary<VoxelNode, string>());

            // Добавляем финальное присвоение
            generatedHLSL += $"\n    float finalDensity = {resultVariable};";

            // Читаем шаблон
            string templatePath = "Assets/Modules/GenerationModule/Shaders/MarchingCubesTemplate.txt";
            string shaderCode = File.ReadAllText(templatePath);

            // Вставляем сгенерированный код
            shaderCode = shaderCode.Replace("// #GENERATED_NOISE_CODE#", generatedHLSL);

            // Сохраняем как рабочий ComputeShader
            string outputPath = "Assets/Modules/GenerationModule/Shaders/MarchingCubes.compute";
            File.WriteAllText(outputPath, shaderCode);

            // Обновляем ассеты в Unity
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("Шейдер успешно сгенерирован!");
        }
    }
}
