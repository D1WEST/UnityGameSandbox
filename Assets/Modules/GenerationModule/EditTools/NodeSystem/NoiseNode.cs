namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Globalization;
    using UnityEditor.Experimental.GraphView;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public enum NoiseType
    {
        Simplex,
        Perlin,
        WhiteNoise
    }

    public class NoiseNode : VoxelNode
    {
        public NoiseType SelectedType = NoiseType.Simplex;
        public float Scale = 0.01f;

        public NoiseNode()
        {
            title = "Noise Generator";
            GUID = System.Guid.NewGuid().ToString();

            var enumField = new EnumField("Type", SelectedType);
            enumField.RegisterValueChangedCallback(evt => SelectedType = (NoiseType)evt.newValue);
            mainContainer.Add(enumField);

            var scaleField = new FloatField("Scale");
            scaleField.value = Scale;
            scaleField.RegisterValueChangedCallback(evt => Scale = evt.newValue);
            mainContainer.Add(scaleField);

            var outputPort = GeneratePort(Direction.Output);
            outputPort.portName = "Out";
            outputContainer.Add(outputPort);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName)
        {
            varName = $"noise_{varCount++}";
            string functionName = SelectedType switch
            {
                NoiseType.Simplex => "SimplexNoise", // НЕ snoise
                NoiseType.Perlin => "PerlinNoise",
                NoiseType.WhiteNoise => "inoise",
                _ => "SimplexNoise"
            };

            string s = Scale.ToString("F4", CultureInfo.InvariantCulture);

            if (SelectedType == NoiseType.WhiteNoise)
                return $"float {varName} = {functionName}(worldPos * {s}, 1.0).x;\n"; // jitter = 1.0

            return $"float {varName} = {functionName}(worldPos * {s});\n";
        }
    }
}
