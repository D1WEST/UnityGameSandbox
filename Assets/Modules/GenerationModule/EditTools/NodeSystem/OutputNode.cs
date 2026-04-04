namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    using UnityEditor.Experimental.GraphView;
    public class OutputNode : VoxelNode
    {
        public Port Input;

        public OutputNode()
        {
            title = "FINAL DENSITY";
            GUID = "FINAL_OUTPUT"; // Фиксированный ID для удобства поиска

            Input = GeneratePort(Direction.Input);
            Input.portName = "Density In";
            inputContainer.Add(Input);

            // Эту ноду нельзя удалить (опционально)
            capabilities &= ~Capabilities.Deletable;

            RefreshExpandedState();
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName)
        {
            // Просто прокидываем код от входа дальше
            return GetInputHLSL(Input, ref varCount, out varName);
        }
    }
}
