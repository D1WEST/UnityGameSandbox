namespace Assets.Modules.GenerationModule.Abstractions
{
    using Assets.Modules.GenerationModule.Models;
    using UnityEngine;

    /// <summary>
    /// Builds the mesh from voxel data.
    /// </summary>
    public interface IMeshBuilder
    {
        Mesh BuildMesh(ChunkData chunkData);
    }
}
