using Assets.Modules.GenerationModule.EditTools;

namespace Assets.Modules.GenerationModule.Abstractions
{
    using Assets.Modules.GenerationModule.Models;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Builds the mesh from voxel data.
    /// </summary>
    public interface IMeshBuilder
    {
        Mesh BuildMesh(ChunkData chunkData, VoxelGraphData graph, int3 worldOffset);
    }
}
