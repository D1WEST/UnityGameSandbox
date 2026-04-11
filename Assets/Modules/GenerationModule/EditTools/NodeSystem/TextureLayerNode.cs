using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Globalization;
using UnityEditor.UIElements;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    public class TextureLayerNode : VoxelNode
    {
        [System.Serializable]
        public class LayerRule
        {
            public float MinHeight = 0;
            public float MaxHeight = 50;
            public float Transition = 5.0f;
            public int TextureSlot = 0;
            public float NoiseInfluence = 0.5f;
        }

        public List<LayerRule> Rules = new List<LayerRule>();
        public float NoiseScale = 0.05f;

        private VisualElement _rulesContainer;

        public TextureLayerNode()
        {
            title = "Height Texture Mixer";
            GUID = System.Guid.NewGuid().ToString();
            style.width = 280; // Немного расширим ноду для удобства

            // Глобальные настройки
            var settings = new VisualElement() { style = { marginBottom = 5, paddingLeft = 5, paddingRight = 5 } };
            var scaleField = new FloatField("Mix Noise Scale") { value = NoiseScale };
            scaleField.RegisterValueChangedCallback(evt => NoiseScale = evt.newValue);
            settings.Add(scaleField);
            mainContainer.Add(settings);

            var addBtn = new Button(AddRule) { text = "Add Height Layer (+)", style = { height = 25 } };
            mainContainer.Add(addBtn);

            // Создаем шапку таблицы
            CreateHeader();

            _rulesContainer = new VisualElement();
            mainContainer.Add(_rulesContainer);

            var outPort = GeneratePort(Direction.Output, type: typeof(Vector4));
            outPort.portName = "Weights (RGBA)";
            outputContainer.Add(outPort);

            RefreshPorts();
        }

        private void CreateHeader()
        {
            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 5, paddingLeft = 5 } };
            header.Add(new Label("Min") { style = { width = 50, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(new Label("Max") { style = { width = 50, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(new Label("Slot") { style = { width = 40, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(new Label("Trans") { style = { width = 50, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            mainContainer.Add(header);
        }

        private void AddRule()
        {
            var rule = new LayerRule();
            Rules.Add(rule);
            CreateRuleUI(rule);
        }

        private void CreateRuleUI(LayerRule rule)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;
            row.style.paddingLeft = 2;
            row.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

            // Поля без меток (label = ""), задаем ширину напрямую
            var minH = new FloatField("") { value = rule.MinHeight, style = { width = 50 } };
            minH.RegisterValueChangedCallback(e => rule.MinHeight = e.newValue);

            var maxH = new FloatField("") { value = rule.MaxHeight, style = { width = 50 } };
            maxH.RegisterValueChangedCallback(e => rule.MaxHeight = e.newValue);

            var slot = new IntegerField("") { value = rule.TextureSlot, style = { width = 40 } };
            slot.RegisterValueChangedCallback(e => rule.TextureSlot = Mathf.Clamp(e.newValue, 0, 3));

            var trans = new FloatField("") { value = rule.Transition, style = { width = 50 } };
            trans.RegisterValueChangedCallback(e => rule.Transition = e.newValue);

            var remove = new Button(() => { _rulesContainer.Remove(row); Rules.Remove(rule); })
            { text = "x", style = { flexGrow = 1, marginLeft = 5 } };

            row.Add(minH);
            row.Add(maxH);
            row.Add(slot);
            row.Add(trans);
            row.Add(remove);
            _rulesContainer.Add(row);
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";
            varName = $"texWeights_{varCount++}";
            cache.Add(this, varName);

            var c = CultureInfo.InvariantCulture;
            string noiseVar = $"mixNoise_{varCount++}";

            string code = $"float {noiseVar} = SimplexNoise(worldPos * {NoiseScale.ToString("F4", c)});\n";
            code += $"    float4 {varName} = float4(0,0,0,0);\n";

            foreach (var r in Rules)
            {
                string min = r.MinHeight.ToString("F2", c);
                string max = r.MaxHeight.ToString("F2", c);
                string tr = r.Transition.ToString("F2", c);
                string nInf = r.NoiseInfluence.ToString("F2", c);

                string w = $"w_{varCount++}";
                code += $"    float {w} = saturate(smoothstep({min}-{tr}, {min}+{tr}, worldPos.y + {noiseVar}*{nInf})) * ";
                code += $"saturate(1.0 - smoothstep({max}-{tr}, {max}+{tr}, worldPos.y + {noiseVar}*{nInf}));\n";

                string chan = "rgba"[r.TextureSlot].ToString();
                code += $"    {varName}.{chan} += {w};\n";
            }

            code += $"    {varName} /= max(0.0001, {varName}.r + {varName}.g + {varName}.b + {varName}.a);\n";
            return code;
        }

        public override void RefreshUI()
        {
            _rulesContainer.Clear();
            foreach (var rule in Rules) CreateRuleUI(rule);
        }
    }
}