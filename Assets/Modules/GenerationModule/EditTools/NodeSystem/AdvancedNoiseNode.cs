using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    public class AdvancedNoiseNode : VoxelNode
    {
        public NoiseType SelectedType = NoiseType.Simplex;
        public float Scale = 0.01f;
        public int Octaves = 4;
        public float Persistence = 0.5f; // Затухание амплитуды
        public float Lacunarity = 2.0f;  // Увеличение частоты с каждой октавой
        public Vector3 Offset = Vector3.zero;

        public AdvancedNoiseNode()
        {
            title = "Advanced Noise";
            GUID = System.Guid.NewGuid().ToString();

            var typeField = new EnumField("Type", SelectedType);
            typeField.RegisterValueChangedCallback(evt => SelectedType = (NoiseType)evt.newValue);
            mainContainer.Add(typeField);

            var scaleField = new FloatField("Scale (Freq)");
            scaleField.value = Scale;
            scaleField.RegisterValueChangedCallback(evt => Scale = evt.newValue);
            mainContainer.Add(scaleField);

            var octField = new IntegerField("Octaves");
            octField.value = Octaves;
            octField.RegisterValueChangedCallback(evt => Octaves = evt.newValue);
            mainContainer.Add(octField);

            var persistField = new FloatField("Persistence (Amp mult)");
            persistField.value = Persistence;
            persistField.RegisterValueChangedCallback(evt => Persistence = evt.newValue);
            mainContainer.Add(persistField);

            var lacunarityField = new FloatField("Lacunarity (Freq mult)");
            lacunarityField.value = Lacunarity;
            lacunarityField.RegisterValueChangedCallback(evt => Lacunarity = evt.newValue);
            mainContainer.Add(lacunarityField);

            var offsetField = new Vector3Field("Offset");
            offsetField.value = Offset;
            offsetField.RegisterValueChangedCallback(evt => Offset = evt.newValue);
            mainContainer.Add(offsetField);

            outputContainer.Add(GeneratePort(UnityEditor.Experimental.GraphView.Direction.Output));
            RefreshExpandedState();
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";

            varName = $"adv_noise_{varCount++}";
            cache.Add(this, varName);

            var c = System.Globalization.CultureInfo.InvariantCulture;
            string s = Scale.ToString("F4", c);
            string p = Persistence.ToString("F4", c);
            string l = Lacunarity.ToString("F4", c);
            string off = $"float3({Offset.x.ToString("F2", c)}, {Offset.y.ToString("F2", c)}, {Offset.z.ToString("F2", c)})";

            string func = SelectedType == NoiseType.Simplex ? "SimplexNoise_Octaves" : "PerlinNoise_Octaves";

            // В нашем HLSL: (coord, scale, speed, octaves, octaveScale, octaveAttenuation, time)
            // Мы используем speed как offset, а time ставим 1.0
            return $"float {varName} = {func}(worldPos, {s}, {off}, uint({Octaves}), {l}, {p}, 1.0);\n";
        }

        public override void RefreshUI()
        {
            mainContainer.Q<EnumField>().value = SelectedType;
            mainContainer.Q<IntegerField>().value = Octaves;

            var floats = mainContainer.Query<FloatField>().ToList();
            if (floats.Count >= 3)
            {
                floats[0].value = Scale;
                floats[1].value = Persistence;
                floats[2].value = Lacunarity;
            }
            var vec = mainContainer.Q<Vector3Field>();
            if (vec != null) vec.value = Offset;
        }
    }
}