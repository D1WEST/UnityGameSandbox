using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    public class CoordinateNode : VoxelNode
    {
        public float X, Y, Z;

        public CoordinateNode()
        {
            title = "Coordinate";
            GUID = System.Guid.NewGuid().ToString();

            // Поля ввода для инспектора ноды
            var xField = new FloatField("X");
            xField.value = X;
            xField.RegisterValueChangedCallback(evt => X = evt.newValue);
            mainContainer.Add(xField);

            var yField = new FloatField("Y");
            yField.value = Y;
            yField.RegisterValueChangedCallback(evt => Y = evt.newValue);
            mainContainer.Add(yField);

            var zField = new FloatField("Z");
            zField.value = Z;
            zField.RegisterValueChangedCallback(evt => Z = evt.newValue);
            mainContainer.Add(zField);

            // Создаем три отдельных выхода
            AddOutputPort("X");
            AddOutputPort("Y");
            AddOutputPort("Z");

            RefreshExpandedState();
            RefreshPorts();
        }

        private void AddOutputPort(string name)
        {
            var port = GeneratePort(Direction.Output);
            port.portName = name;
            outputContainer.Add(port);
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";

            // Нода генерирует один float3, а базовый класс VoxelNode 
            // сам добавит .x, .y или .z в зависимости от того, из какого порта тянут связь!
            varName = $"coord_{varCount++}";
            cache.Add(this, varName);

            var c = System.Globalization.CultureInfo.InvariantCulture;
            return $"float3 {varName} = float3({X.ToString("F4", c)}, {Y.ToString("F4", c)}, {Z.ToString("F4", c)});\n";
        }

        public override void RefreshUI()
        {
            var fields = mainContainer.Query<FloatField>().ToList();
            if (fields.Count >= 3)
            {
                fields[0].value = X;
                fields[1].value = Y;
                fields[2].value = Z;
            }
        }
    }
}