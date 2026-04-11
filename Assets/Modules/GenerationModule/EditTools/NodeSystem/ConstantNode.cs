namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using System.Collections.Generic;
    using System.Globalization;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine.UIElements;

    public class ConstantNode : VoxelNode
    {
        public float Value = 1.0f;
        public ConstantNode()
        {
            title = "Constant";
            GUID = System.Guid.NewGuid().ToString();
            var field = new FloatField();
            field.value = Value;
            field.RegisterValueChangedCallback(evt => Value = evt.newValue);
            mainContainer.Add(field);

            var outPort = GeneratePort(Direction.Output);
            outPort.portName = "Value";
            outputContainer.Add(outPort);
            RefreshPorts();
        }
        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";
            varName = Value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "f";
            cache.Add(this, varName);
            return "";
        }

        public override void RefreshUI()
        {
            var field = mainContainer.Query<FloatField>().First();
            if (field != null) field.value = Value;
        }
    }
}
