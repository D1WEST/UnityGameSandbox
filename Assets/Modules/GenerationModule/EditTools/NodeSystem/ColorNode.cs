namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Collections.Generic;
    using System.Globalization;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class ColorNode : VoxelNode
    {
        public Color Value = Color.white;

        public ColorNode()
        {
            title = "Color (float4)";
            GUID = System.Guid.NewGuid().ToString();

            var field = new ColorField();
            field.value = Value;
            field.RegisterValueChangedCallback(evt => Value = evt.newValue);
            mainContainer.Add(field);

            var outPort = GeneratePort(UnityEditor.Experimental.GraphView.Direction.Output, type: typeof(Vector4));
            outPort.portName = "RGBA";
            outputContainer.Add(outPort);
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";
            var c = System.Globalization.CultureInfo.InvariantCulture;
            varName = $"float4({Value.r.ToString("F3", c)}, {Value.g.ToString("F3", c)}, {Value.b.ToString("F3", c)}, 1.0f)";
            cache.Add(this, varName);
            return "";
        }

        public override void RefreshUI()
        {
            var field = mainContainer.Query<ColorField>().First();
            if (field != null) field.value = Value;
        }
    }
}
