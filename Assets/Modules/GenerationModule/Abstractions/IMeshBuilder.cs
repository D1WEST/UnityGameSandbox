namespace Assets.Modules.GenerationModule.Abstractions
{
    using Assets.Modules.GenerationModule.Models.WestMM;
    using JetBrains.Annotations;
    using Assets.Modules.GenerationModule.Models;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Builds the mesh from voxel data.
    /// </summary>
    public interface IMeshBuilder
    {
        Mesh BuildMesh([CanBeNull] ChunkData chunkData, WorldProfile profile, int3 worldOffset);
    }
}
