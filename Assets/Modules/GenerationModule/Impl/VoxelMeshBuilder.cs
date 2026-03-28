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
        public Mesh BuildMesh(IVoxelData data)
        {
            var chunkData = (ChunkData)data;

            // Выделяем память под результаты (TempJob - автоматически очистится, если мы вызовем Dispose)
            var vertices = new NativeList<float3>(Allocator.TempJob);
            var triangles = new NativeList<int>(Allocator.TempJob);

            // Настраиваем Job
            var job = new MarchingCubesJob
            {
                Densities = chunkData.GetNativeArray(),
                ChunkSize = chunkData.Size,
                IsoLevel = 0f,
                Vertices = vertices,
                Triangles = triangles
            };

            // Запускаем и ждем (в финальной игре здесь лучше сделать асинхронное ожидание)
            job.Schedule().Complete();

            Mesh mesh = new Mesh();

            // Проверяем, есть ли вообще геометрия (чтобы не создавать пустые меши)
            if (vertices.Length > 0)
            {
                // Быстрая конвертация NativeList в массивы Unity
                mesh.SetVertices(vertices.AsArray().Reinterpret<Vector3>());
                mesh.SetTriangles(triangles.AsArray().ToList(), 0);

                // Unity сама посчитает нормали для освещения
                mesh.RecalculateNormals();
            }

            // КРИТИЧЕСКИ ВАЖНО: Очищаем память, иначе игра крашнется от утечки!
            vertices.Dispose();
            triangles.Dispose();

            return mesh;
        }
    }
}
