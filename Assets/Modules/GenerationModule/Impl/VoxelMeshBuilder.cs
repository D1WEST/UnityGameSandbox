using System.Linq;
using Assets.Modules.GenerationModule.Abstractions;
using Assets.Modules.GenerationModule.Burst;
using Assets.Modules.GenerationModule.Models;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Modules.GenerationModule.Impl
{
    public class VoxelMeshBuilder : IMeshBuilder
    {
        public Mesh BuildMesh(ChunkData chunkData)
        {
            var vertices = new NativeList<float3>(Allocator.TempJob);
            var triangles = new NativeList<int>(Allocator.TempJob);

            var job = new MarchingCubesJob
            {
                Densities = chunkData.GetNativeArray(),
                ChunkSize = chunkData.Size,
                IsoLevel = 0f,
                Vertices = vertices,
                Triangles = triangles
            };

            job.Schedule().Complete();

            // ЕСЛИ ТРЕУГОЛЬНИКОВ НЕТ — возвращаем null, чтобы не ломать физику
            if (triangles.Length == 0)
            {
                vertices.Dispose();
                triangles.Dispose();
                return null;
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices.AsArray().Reinterpret<Vector3>());
            mesh.SetTriangles(triangles.AsArray().ToArray(), 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds(); // Обязательно считаем границы!

            vertices.Dispose();
            triangles.Dispose();

            return mesh;
        }
    }
}
