using UnityEditor.UIElements;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine.UIElements;

    public class BiomeNode : VoxelNode
    {
        public float TargetTemp = 0.5f;
        public Color ColorValue = Color.white;
        public Port DensityInput;
        public Port ColorInput;

        public BiomeNode()
        {
            title = "Biome Definition";
            GUID = System.Guid.NewGuid().ToString();

            // Поле температуры
            var tempField = new FloatField("Target Temp (0-1)");
            tempField.value = TargetTemp;
            tempField.RegisterValueChangedCallback(evt => TargetTemp = evt.newValue);
            mainContainer.Add(tempField);

            var colorField = new ColorField("Biome Color");
            colorField.value = ColorValue;
            colorField.RegisterValueChangedCallback(evt => ColorValue = evt.newValue);
            mainContainer.Add(colorField);

            DensityInput = GeneratePort(Direction.Input);
            DensityInput.portName = "Density (float)";
            inputContainer.Add(DensityInput);

            ColorInput = GeneratePort(UnityEditor.Experimental.GraphView.Direction.Input, type: typeof(UnityEngine.Vector4));
            ColorInput.portName = "Color (float4)";
            inputContainer.Add(ColorInput);

            var outPort = GeneratePort(Direction.Output);
            outPort.portName = "Biome Link";
            outputContainer.Add(outPort);

            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            varName = "";
            return "";
        }

        public override void RefreshUI()
        {
            var tempField = mainContainer.Q<FloatField>();
            if (tempField != null) tempField.value = TargetTemp;

            var colorField = mainContainer.Q<ColorField>();
            if (colorField != null)
            {
                colorField.value = ColorValue;
                // Принудительно уведомляем систему об изменении, чтобы UI перерисовался
                colorField.MarkDirtyRepaint();
            }
        }
    }
}
