namespace Assets.Modules.GenerationModule.Abstractions
{
    using UnityEngine;

    /// <summary>
    /// Builds the mesh from voxel data.
    /// </summary>
    public interface IMeshBuilder
    {
        Mesh BuildMesh(IVoxelData data);
    }
}
