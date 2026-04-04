using Assets.Modules.GenerationModule.Abstractions;
using Assets.Modules.GenerationModule.Burst;
using Assets.Modules.GenerationModule.Models;
using Assets.Modules.GenerationModule.Models.WestMM;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Modules.GenerationModule.Impl
{
    public class VoxelMeshBuilder : IMeshBuilder
    {
        public Mesh BuildMesh(ChunkData chunkData, WorldProfile profile, int3 worldOffset)
        {
            var vertices = new NativeList<float3>(Allocator.TempJob);
            var triangles = new NativeList<int>(Allocator.TempJob);
            var colors = new NativeList<float4>(Allocator.TempJob);

            // Копируем данные биомов в NativeArray для Burst
            var biomesNative = new NativeArray<BiomeData>(profile.biomes, Allocator.TempJob);

            var job = new MarchingCubesJob
            {
                Densities = chunkData.GetNativeArray(),
                Biomes = biomesNative,
                ChunkSize = chunkData.Size,
                WorldOffset = worldOffset,
                IsoLevel = 0f,
                BiomeMapScale = profile.biomeMapScale,
                Seed = 1337f, // Убедись, что сид совпадает с WorldManager
                Vertices = vertices,
                Triangles = triangles,
                Colors = colors
            };

            job.Schedule().Complete();

            if (triangles.Length == 0)
            {
                vertices.Dispose();
                triangles.Dispose();
                colors.Dispose();
                biomesNative.Dispose();
                return null;
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices.AsArray().Reinterpret<Vector3>());
            mesh.SetColors(colors.AsArray().Reinterpret<Color>()); // УСТАНАВЛИВАЕМ ЦВЕТА
            mesh.SetTriangles(triangles.AsArray().ToArray(), 0);

            // Оптимизированное обновление меша
            MeshUpdateFlags flags = MeshUpdateFlags.DontValidateIndices |
                                    MeshUpdateFlags.DontResetBoneBounds |
                                    MeshUpdateFlags.DontNotifyMeshUsers |
                                    MeshUpdateFlags.DontRecalculateBounds;

            mesh.RecalculateNormals(flags);
            mesh.RecalculateBounds(flags);

            vertices.Dispose();
            triangles.Dispose();
            colors.Dispose();
            biomesNative.Dispose();

            return mesh;
        }
    }
}