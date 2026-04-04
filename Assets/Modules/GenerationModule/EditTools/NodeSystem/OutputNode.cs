namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;
    using UnityEngine.UIElements;
    using System.Collections.Generic;

    public class OutputNode : VoxelNode
    {
        public Port SelectorInput; // Сюда подключаем шум температуры/влажности
        public List<Port> BiomePorts = new List<Port>();

        public OutputNode()
        {
            title = "WORLD OUTPUT";
            GUID = "FINAL_OUTPUT";

            // 1. Главный порт управления переключением биомов
            SelectorInput = GeneratePort(Direction.Input);
            SelectorInput.portName = "Biome Selector (0-1)";
            inputContainer.Add(SelectorInput);

            // 2. Кнопка добавления нового слота биома
            var addBtn = new Button(AddBiomeSlot) { text = "Add Biome Slot (+)" };
            titleContainer.Add(addBtn);

            RefreshExpandedState();
            RefreshPorts();
        }

        public void AddBiomeSlot()
        {
            var p = GeneratePort(Direction.Input);
            int index = BiomePorts.Count;
            p.portName = $"Biome {index}";

            // Кнопка удаления конкретного порта
            var removeBtn = new Button(() => RemoveBiomeSlot(p)) { text = "X", style = { fontSize = 10 } };
            p.contentContainer.Add(removeBtn);

            BiomePorts.Add(p);
            inputContainer.Add(p);

            RefreshExpandedState();
            RefreshPorts();
        }

        private void RemoveBiomeSlot(Port p)
        {
            inputContainer.Remove(p);
            BiomePorts.Remove(p);
            RefreshExpandedState();
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            varName = "";
            return "";
        }
    }
}
