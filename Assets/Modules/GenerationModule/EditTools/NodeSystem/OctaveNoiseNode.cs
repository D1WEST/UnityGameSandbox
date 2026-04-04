namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEngine.UIElements;
    using System.Globalization;

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

        public override string GetHLSL(ref int varCount, out string varName)
        {
            varName = $"fbm_{varCount++}";
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            string s = Scale.ToString("F4", culture);
            string p = Persistence.ToString("F4", culture);

            string func = SelectedType == NoiseType.Simplex ? "SimplexNoise_Octaves" : "PerlinNoise_Octaves";

            // ВАЖНО: используем uint для октав, чтобы соответствовать сигнатуре функции в HLSL
            return $"float {varName} = {func}(worldPos, {s}, float3(0,0,0), uint({Octaves}), 2.0, {p}, 0.0);\n";
        }
    }
}
