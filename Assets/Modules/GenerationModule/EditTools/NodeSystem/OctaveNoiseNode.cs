namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Collections.Generic;
    using System.Globalization;
    using UnityEngine.UIElements;

    public class OctaveNoiseNode : VoxelNode
    {
        public NoiseType SelectedType = NoiseType.Simplex;

        public int Octaves = 4;
        public float Persistence = 0.5f; // Насколько сильно влияют мелкие детали
        public float Scale = 0.01f;

        public OctaveNoiseNode()
        {
            title = "Octave Noise (FBM)";
            GUID = System.Guid.NewGuid().ToString();

            var typeField = new EnumField("Type", SelectedType);
            typeField.RegisterValueChangedCallback(evt => SelectedType = (NoiseType)evt.newValue);
            mainContainer.Add(typeField);

            var octField = new IntegerField("Octaves");
            octField.value = Octaves;
            octField.RegisterValueChangedCallback(evt => Octaves = evt.newValue);
            mainContainer.Add(octField);

            var persistField = new FloatField("Persistence");
            persistField.value = Persistence;
            persistField.RegisterValueChangedCallback(evt => Persistence = evt.newValue);
            mainContainer.Add(persistField);

            var scaleField = new FloatField("Scale");
            scaleField.value = Scale;
            scaleField.RegisterValueChangedCallback(evt => Scale = evt.newValue);
            mainContainer.Add(scaleField);

            outputContainer.Add(GeneratePort(UnityEditor.Experimental.GraphView.Direction.Output));
            RefreshPorts();
        }

        public override void RefreshUI()
        {
            mainContainer.Q<EnumField>().value = SelectedType;
            mainContainer.Q<IntegerField>().value = Octaves;

            // Находим все FloatField и обновляем их по порядку создания
            var floats = mainContainer.Query<FloatField>().ToList();
            if (floats.Count >= 2)
            {
                floats[0].value = Persistence;
                floats[1].value = Scale;
            }
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return ""; // КРИТИЧЕСКИ ВАЖНО

            varName = $"fbm_{varCount++}";
            cache.Add(this, varName);

            var culture = System.Globalization.CultureInfo.InvariantCulture;
            string s = Scale.ToString("F4", culture);

            // Для OctaveNoise
            if (this is OctaveNoiseNode oct)
            {
                string p = oct.Persistence.ToString("F4", culture);
                string func = oct.SelectedType == NoiseType.Simplex ? "SimplexNoise_Octaves" : "PerlinNoise_Octaves";
                return $"float {varName} = {func}(worldPos, {s}, float3(0,0,0), uint({oct.Octaves}), 2.0, {p}, 0.0);\n";
            }

            // Для обычного шума
            return $"float {varName} = SimplexNoise(worldPos * {s});\n";
        }
    }
}
