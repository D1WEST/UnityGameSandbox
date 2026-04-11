using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    public class Vector3Node : VoxelNode
    {
        public Vector3 Value = Vector3.zero;

        public Vector3Node()
        {
            title = "Vector3 Constant";
            GUID = System.Guid.NewGuid().ToString();

            var field = new Vector3Field("");
            field.value = Value;
            field.RegisterValueChangedCallback(evt => Value = evt.newValue);
            mainContainer.Add(field);

            var outPort = GeneratePort(Direction.Output, type: typeof(Vector3));
            outPort.portName = "XYZ";
            outputContainer.Add(outPort);
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";
            var c = System.Globalization.CultureInfo.InvariantCulture;
            varName = $"float3({Value.x.ToString("F3", c)}, {Value.y.ToString("F3", c)}, {Value.z.ToString("F3", c)})";
            cache.Add(this, varName);
            return "";
        }

        public override void RefreshUI()
        {
            var field = mainContainer.Q<Vector3Field>();
            if (field != null) field.value = Value;
        }
    }
}