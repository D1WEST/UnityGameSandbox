using System.Text;
using System.IO;
using Assets.Modules.GenerationModule.Models;
using UnityEngine;
using UnityEditor;

public static class ShaderWriter
{
    public static void UpdateShaders(VoxelMaterialConfig config)
    {
        string computePath = "Assets/Modules/GenerationModule/Shaders/MarchingCubes.compute";
        string shaderPath = "Assets/Modules/GenerationModule/Shaders/VoxelTriplanar.shader";

        StringBuilder props = new StringBuilder();
        StringBuilder samplers = new StringBuilder();
        StringBuilder computeDecls = new StringBuilder();
        StringBuilder switchLogic = new StringBuilder();

        var inv = System.Globalization.CultureInfo.InvariantCulture;

        for (int i = 0; i < config.Textures.Count; i++)
        {
            string texName = $"_Tex{i}";
            string scaleName = $"_Scale{i}";

            props.AppendLine($"        {texName}(\"{config.Textures[i].Name}\", 2D) = \"white\" {{}}");
            props.AppendLine($"        {scaleName}(\"Scale {i}\", Float) = {config.Textures[i].Tiling.ToString("F3", inv)}");
            samplers.AppendLine($"            sampler2D {texName}; float {scaleName};");

            computeDecls.AppendLine($"Texture2D {texName}; SamplerState sampler{texName}; float {scaleName};");
            switchLogic.AppendLine($"    if (index == {i}) return GetTriplanarSample({texName}, sampler{texName}, worldPos, normal, {scaleName});");
        }

        // Обновляем Compute Shader
        string computeContent = File.ReadAllText(computePath);
        computeContent = ReplaceTag(computeContent, "// [TEX_DECL_START]", "// [TEX_DECL_END]", computeDecls.ToString());
        computeContent = ReplaceTag(computeContent, "// [TEX_LOGIC_START]", "// [TEX_LOGIC_END]", switchLogic.ToString());
        File.WriteAllText(computePath, computeContent, new UTF8Encoding(false));

        // Обновляем Visual Shader (ТЕПЕРЬ С КОНЕЧНЫМИ ТЕГАМИ)
        string shaderContent = File.ReadAllText(shaderPath);
        shaderContent = ReplaceTag(shaderContent, "// [GENERATED_PROPERTIES_START]", "// [GENERATED_PROPERTIES_END]", props.ToString());
        shaderContent = ReplaceTag(shaderContent, "// [GENERATED_SAMPLERS_START]", "// [GENERATED_SAMPLERS_END]", samplers.ToString());
        File.WriteAllText(shaderPath, shaderContent, new UTF8Encoding(false));

        if (config.TargetMaterial != null)
        {
            for (int i = 0; i < config.Textures.Count; i++)
            {
                config.TargetMaterial.SetTexture($"_Tex{i}", config.Textures[i].MainTex);
                config.TargetMaterial.SetFloat($"_Scale{i}", config.Textures[i].Tiling);
            }
        }
    }

    // ТЕПЕРЬ СТАТИЧЕСКИЙ
    private static string ReplaceTag(string text, string start, string end, string newText)
    {
        int s = text.IndexOf(start);
        if (s == -1) return text;

        if (string.IsNullOrEmpty(end))
        {
            // Для обычного шейдера просто вставляем после тега
            return text.Insert(s + start.Length, "\n" + newText);
        }

        int e = text.IndexOf(end);
        if (e == -1) return text;

        return text.Substring(0, s + start.Length) + "\n" + newText + "\n    " + text.Substring(e);
    }
}