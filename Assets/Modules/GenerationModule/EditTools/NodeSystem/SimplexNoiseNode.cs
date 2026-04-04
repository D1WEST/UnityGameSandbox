namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    public class SimplexNoiseNode : VoxelNode
    {
        public float scale = 0.01f;
        public VoxelNode inputPositionNode; // Ссылка на ноду координат (если есть)

        public override string GetHLSL(ref int variableCounter, out string outputVariableName)
        {
            outputVariableName = "noise_" + variableCounter;
            variableCounter++;

            // Если ко входу ничего не подключено, используем стандартный worldPos
            string posVar = "worldPos";

            return $"float {outputVariableName} = snoise({posVar} * {scale}f);\n";
        }
    }
}
