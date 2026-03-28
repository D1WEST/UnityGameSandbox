namespace Assets.Modules.GenerationModule.Abstractions
{
    using Unity.Mathematics;

    /// <summary>
    /// All voxel information.
    /// </summary>
    public interface IVoxelData
    {
        float GetDensity(int3 localPos);
        void SetDensity(int3 localPos, float value);
        int3 Size { get; }
    }
}
