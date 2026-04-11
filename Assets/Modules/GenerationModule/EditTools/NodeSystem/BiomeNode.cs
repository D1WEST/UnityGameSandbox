using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    public class BiomeNode : VoxelNode
    {
        public float TargetTemp = 0.5f;

        // Индексы текстур из вашего глобального списка
        public int TexIndexR = 0;
        public int TexIndexG = 1;
        public int TexIndexB = 2;
        public int TexIndexA = 3;

        public Port DensityInput;
        public Port ColorInput;

        public BiomeNode()
        {
            title = "Biome Definition";
            GUID = System.Guid.NewGuid().ToString();
            style.width = 220;

            var tempField = new FloatField("Target Temp (0-1)") { value = TargetTemp };
            tempField.RegisterValueChangedCallback(evt => TargetTemp = evt.newValue);
            mainContainer.Add(tempField);

            // Поля индексов текстур
            var texLabel = new Label("Global Texture Indices (R, G, B, A):") { style = { marginTop = 5, fontSize = 10 } };
            mainContainer.Add(texLabel);

            var texRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            texRow.Add(CreateIntField(v => TexIndexR = v, TexIndexR));
            texRow.Add(CreateIntField(v => TexIndexG = v, TexIndexG));
            texRow.Add(CreateIntField(v => TexIndexB = v, TexIndexB));
            texRow.Add(CreateIntField(v => TexIndexA = v, TexIndexA));
            mainContainer.Add(texRow);

            DensityInput = GeneratePort(Direction.Input);
            DensityInput.portName = "Density (float)";
            inputContainer.Add(DensityInput);

            ColorInput = GeneratePort(Direction.Input, type: typeof(UnityEngine.Vector4));
            ColorInput.portName = "Weights (float4)";
            inputContainer.Add(ColorInput);

            var outPort = GeneratePort(Direction.Output);
            outPort.portName = "Biome Link";
            outputContainer.Add(outPort);

            RefreshPorts();
        }

        private IntegerField CreateIntField(System.Action<int> setter, int val)
        {
            var f = new IntegerField() { value = val, style = { width = 45 } };
            f.RegisterValueChangedCallback(e => setter(e.newValue));
            return f;
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            varName = ""; // ИСПРАВЛЕНИЕ ОШИБКИ CS0177
            return "";
        }

        public override void RefreshUI()
        {
            var tempField = mainContainer.Q<FloatField>();
            if (tempField != null) tempField.value = TargetTemp;

            var ints = mainContainer.Query<IntegerField>().ToList();
            if (ints.Count >= 4)
            {
                ints[0].value = TexIndexR;
                ints[1].value = TexIndexG;
                ints[2].value = TexIndexB;
                ints[3].value = TexIndexA;
            }
        }
    }
}