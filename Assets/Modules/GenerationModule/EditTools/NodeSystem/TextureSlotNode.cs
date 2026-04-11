using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    public class TextureSlotNode : VoxelNode
    {
        public enum SlotIndex { Slot_0, Slot_1, Slot_2, Slot_3 }
        public SlotIndex SelectedSlot = SlotIndex.Slot_0;

        public TextureSlotNode()
        {
            title = "Texture Slot";
            GUID = System.Guid.NewGuid().ToString();

            var enumField = new UnityEngine.UIElements.EnumField("Slot", SelectedSlot);
            enumField.RegisterValueChangedCallback(evt => SelectedSlot = (SlotIndex)evt.newValue);
            mainContainer.Add(enumField);

            var outPort = GeneratePort(Direction.Output, type: typeof(Vector4));
            outPort.portName = "Weights (float4)";
            outputContainer.Add(outPort);
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";

            // Генерируем вектор, где 1 стоит только в выбранном канале
            float r = SelectedSlot == SlotIndex.Slot_0 ? 1 : 0;
            float g = SelectedSlot == SlotIndex.Slot_1 ? 1 : 0;
            float b = SelectedSlot == SlotIndex.Slot_2 ? 1 : 0;
            float a = SelectedSlot == SlotIndex.Slot_3 ? 1 : 0;

            varName = $"float4({r},{g},{b},{a})";
            cache.Add(this, varName);
            return "";
        }

        public override void RefreshUI()
        {
            var field = mainContainer.Q<UnityEngine.UIElements.EnumField>();
            if (field != null) field.value = SelectedSlot;
        }
    }
}