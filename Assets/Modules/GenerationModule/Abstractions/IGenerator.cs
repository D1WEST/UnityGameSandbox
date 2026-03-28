namespace Assets.Modules.GenerationModule.Abstractions
{
    using Unity.Mathematics;

    /// <summary>
    /// Generates chunk.
    /// </summary>
    public interface IGenerator
    {
        void Generate(IVoxelData data, int3 chunkWorldPos);
    }
}
