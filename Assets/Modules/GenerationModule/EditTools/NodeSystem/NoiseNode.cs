namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Collections.Generic;
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

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";

            varName = $"noise_{varCount++}";
            cache.Add(this, varName);

            string func = SelectedType == NoiseType.Simplex ? "SimplexNoise" : "PerlinNoise";
            string s = Scale.ToString("F4", CultureInfo.InvariantCulture);

            return $"float {varName} = {func}(worldPos * {s});\n";
        }

        public void RefreshUI()
        {
            var enumField = mainContainer.Query<EnumField>().First();
            if (enumField != null) enumField.value = SelectedType;

            var scaleField = mainContainer.Query<FloatField>().First();
            if (scaleField != null) scaleField.value = Scale;
        }
    }
}
