using Assets.Modules.GenerationModule.EditTools;
using Assets.Modules.GenerationModule.Models;
using Assets.Modules.GenerationModule.Models.WestMM;
using Assets.Modules.GenerationModule.Static;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Modules.GenerationModule.Impl
{
    public class GPUChunkGenerator
    {
        private ComputeShader computeShader;
        private int densityKernel;
        private int meshKernel;

        // Статические буферы таблиц (общие для всех чанков)
        private static ComputeBuffer triTableBuffer;
        private static ComputeBuffer edgeVerticesBuffer;
        private static ComputeBuffer cornersBuffer;

        // СТРУКТУРА ДОЛЖНА СОВПАДАТЬ С ШЕЙДЕРОМ (84 байта)
        // 3 x Vector3 (36 байт) + 3 x Color (48 байт) = 84
        struct Triangle
        {
            public Vector3 a, b, c;
            public Color colorA, colorB, colorC;
        }

        public GPUChunkGenerator(ComputeShader shader)
        {
            computeShader = shader;
            densityKernel = computeShader.FindKernel("GenerateDensity");
            meshKernel = computeShader.FindKernel("GenerateMesh");
            InitializeStaticTables();
        }

        private void InitializeStaticTables()
        {
            if (triTableBuffer != null) return;

            triTableBuffer = new ComputeBuffer(MarchingCubesTables.TriTable.Length, sizeof(int));
            triTableBuffer.SetData(MarchingCubesTables.TriTable);

            edgeVerticesBuffer = new ComputeBuffer(MarchingCubesTables.EdgeVertices.Length, sizeof(int) * 2);
            edgeVerticesBuffer.SetData(MarchingCubesTables.EdgeVertices);

            cornersBuffer = new ComputeBuffer(MarchingCubesTables.Corners.Length, sizeof(int) * 3);
            cornersBuffer.SetData(MarchingCubesTables.Corners);
        }

        public void GenerateChunkAsync(int3 size, int3 worldPos, VoxelGraphData graph, TerrainSettings globalSettings, System.Action<Mesh, float[]> onComplete)
        {
            // Размер +1 для сшивания швов между чанками
            int3 actualSize = size + new int3(1, 1, 1);
            int numPoints = actualSize.x * actualSize.y * actualSize.z;
            int maxTriangles = (actualSize.x - 1) * (actualSize.y - 1) * (actualSize.z - 1) * 5;

            // 1. Создаем буферы
            ComputeBuffer densitiesBuffer = new ComputeBuffer(numPoints, sizeof(float));
            // Stride = 84 байта (3x float3 позиции + 3x float4 цвета)
            ComputeBuffer trianglesBuffer = new ComputeBuffer(maxTriangles, 84, ComputeBufferType.Append);
            trianglesBuffer.SetCounterValue(0);

            // ВНИМАНИЕ: biomeBuffer УДАЛЕН. 
            // Вся логика смешивания биомов теперь находится внутри сгенерированного HLSL кода шейдера.

            // 2. Установка параметров в шейдер
            computeShader.SetInts("ChunkSize", actualSize.x, actualSize.y, actualSize.z);
            computeShader.SetInts("WorldOffset", worldPos.x, worldPos.y, worldPos.z);
            computeShader.SetFloat("IsoLevel", 0f);
            computeShader.SetFloat("_Seed", globalSettings.seed);

            // Масштаб селектора биомов берем напрямую из ассета графа
            computeShader.SetFloat("BiomeMapScale", graph.selectorScale);

            // Параметры пещер (оставляем как внешние настройки)
            computeShader.SetFloat("_HubScale", globalSettings.hubScale);
            computeShader.SetFloat("_HubThreshold", globalSettings.hubThreshold);
            computeShader.SetFloat("_BranchScale", globalSettings.branchScale);
            computeShader.SetFloat("_BranchThreshold", globalSettings.branchThreshold);

            // 3. Запуск генерации плотности (Ядро GenerateDensity)
            computeShader.SetBuffer(densityKernel, "Densities", densitiesBuffer);

            int groupsX = Mathf.CeilToInt(actualSize.x / 4f);
            int groupsY = Mathf.CeilToInt(actualSize.y / 4f);
            int groupsZ = Mathf.CeilToInt(actualSize.z / 4f);
            computeShader.Dispatch(densityKernel, groupsX, groupsY, groupsZ);

            // 4. Запуск Marching Cubes (Ядро GenerateMesh)
            computeShader.SetBuffer(meshKernel, "Densities", densitiesBuffer);
            computeShader.SetBuffer(meshKernel, "Triangles", trianglesBuffer);
            computeShader.SetBuffer(meshKernel, "TriTable", triTableBuffer);
            computeShader.SetBuffer(meshKernel, "EdgeVertices", edgeVerticesBuffer);
            computeShader.SetBuffer(meshKernel, "Corners", cornersBuffer);

            // ВАЖНО: Больше не передаем буфер биомов в шейдер, так как он там не используется (удален из .compute)

            computeShader.Dispatch(meshKernel, groupsX, groupsY, groupsZ);

            // 5. Асинхронное чтение результата
            ComputeBuffer argsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(trianglesBuffer, argsBuffer, 0);

            // Локальная функция очистки
            void Cleanup()
            {
                densitiesBuffer?.Release();
                trianglesBuffer?.Release();
                argsBuffer?.Release();
            }

            AsyncGPUReadback.Request(argsBuffer, argsReq => {
                if (argsReq.hasError) { Cleanup(); return; }
                int triCount = argsReq.GetData<int>()[0];

                if (triCount == 0)
                {
                    Cleanup();
                    onComplete?.Invoke(null, null);
                    return;
                }

                // Читаем треугольники (содержат позиции и цвета из графа)
                AsyncGPUReadback.Request(trianglesBuffer, triCount * 84, 0, triReq => {
                    if (triReq.hasError) { Cleanup(); return; }
                    Triangle[] gpuTriangles = triReq.GetData<Triangle>().ToArray();

                    // Читаем плотности для системы копания на CPU (чтобы Burst знал, где земля)
                    AsyncGPUReadback.Request(densitiesBuffer, denReq => {
                        if (denReq.hasError) { Cleanup(); return; }
                        float[] chunkDensities = denReq.GetData<float>().ToArray();

                        // Создаем меш
                        Mesh mesh = BuildMesh(gpuTriangles, triCount);

                        Cleanup();
                        onComplete?.Invoke(mesh, chunkDensities);
                    });
                });
            });
        }

        private Mesh BuildMesh(Triangle[] triangles, int triCount)
        {
            Mesh mesh = new Mesh();
            int vertCount = triCount * 3;

            Vector3[] vertices = new Vector3[vertCount];
            Color[] colors = new Color[vertCount];
            int[] indices = new int[vertCount];

            for (int i = 0; i < triCount; i++)
            {
                int baseIdx = i * 3;

                // Вершины
                vertices[baseIdx + 0] = triangles[i].a;
                vertices[baseIdx + 1] = triangles[i].b;
                vertices[baseIdx + 2] = triangles[i].c;

                // Цвета биомов
                colors[baseIdx + 0] = triangles[i].colorA;
                colors[baseIdx + 1] = triangles[i].colorB;
                colors[baseIdx + 2] = triangles[i].colorC;

                // Индексы
                indices[baseIdx + 0] = baseIdx + 0;
                indices[baseIdx + 1] = baseIdx + 1;
                indices[baseIdx + 2] = baseIdx + 2;
            }

            mesh.SetVertices(vertices);
            mesh.SetColors(colors); // КРИТИЧЕСКИ ВАЖНО ДЛЯ ЦВЕТА

            // Используем быстрые флаги обновления
            MeshUpdateFlags flags = MeshUpdateFlags.DontValidateIndices |
                                    MeshUpdateFlags.DontResetBoneBounds |
                                    MeshUpdateFlags.DontNotifyMeshUsers |
                                    MeshUpdateFlags.DontRecalculateBounds;

            mesh.SetTriangles(indices, 0, false, 0);
            mesh.RecalculateNormals(flags);
            mesh.RecalculateBounds(flags);

            return mesh;
        }

        public void Dispose()
        {
            triTableBuffer?.Release();
            edgeVerticesBuffer?.Release();
            cornersBuffer?.Release();
            triTableBuffer = null;
            edgeVerticesBuffer = null;
            cornersBuffer = null;
        }
    }
}