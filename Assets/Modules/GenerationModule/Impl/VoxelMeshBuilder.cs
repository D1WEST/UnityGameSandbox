namespace Assets.Modules.GenerationModule.Impl
{
    using Assets.Modules.GenerationModule.Abstractions;
    using Assets.Modules.GenerationModule.Burst;
    using Assets.Modules.GenerationModule.EditTools;
    using Assets.Modules.GenerationModule.Models;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;
    public class VoxelMeshBuilder : IMeshBuilder
    {
        public Mesh BuildMesh(ChunkData chunkData, VoxelGraphData graph, int3 worldOffset)
        {
            var vertices = new NativeList<float3>(Allocator.TempJob);
            var triangles = new NativeList<int>(Allocator.TempJob);
            var colors = new NativeList<float4>(Allocator.TempJob);

            // Копируем запеченные данные из ассета графа
            var biomesNative = new NativeArray<BakedBiome>(graph.bakedBiomes, Allocator.TempJob);

            var job = new MarchingCubesJob
            {
                Densities = chunkData.GetNativeArray(),
                Biomes = biomesNative,
                ChunkSize = chunkData.Size,
                WorldOffset = worldOffset,
                IsoLevel = 0f,
                SelectorScale = graph.selectorScale, // Передаем масштаб
                Seed = 1337f,
                Vertices = vertices,
                Triangles = triangles,
                Colors = colors
            };

            job.Schedule().Complete();

            if (triangles.Length == 0)
            {
                vertices.Dispose(); triangles.Dispose(); colors.Dispose(); biomesNative.Dispose();
                return null;
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices.AsArray().Reinterpret<Vector3>());
            mesh.SetColors(colors.AsArray().Reinterpret<Color>());
            mesh.SetTriangles(triangles.AsArray().ToArray(), 0);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            vertices.Dispose();
            triangles.Dispose();
            colors.Dispose();
            biomesNative.Dispose();

            return mesh;
        }
    }
}