using Assets.Modules.GenerationModule.EditTools;
using Assets.Modules.GenerationModule.Models;
using Assets.Modules.GenerationModule.Static;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

namespace Assets.Modules.GenerationModule.Impl
{
    public class GPUChunkGenerator
    {
        private ComputeShader computeShader;
        private int densityKernel, meshKernel;

        // Статические буферы для таблиц Marching Cubes
        private static ComputeBuffer triTableBuffer, edgeVerticesBuffer, cornersBuffer;

        private VoxelMaterialConfig _cachedConfig;

        struct Triangle
        {
            public Vector3 a, b, c;
            public Color colorA, colorB, colorC;
        }

        public GPUChunkGenerator(ComputeShader shader, VoxelMaterialConfig config)
        {
            computeShader = shader;
            _cachedConfig = config;
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

        private void BindTexturesAndParams(int kernel, int3 worldPos)
        {
            computeShader.SetInts("WorldOffset", worldPos.x, worldPos.y, worldPos.z);
            if (_cachedConfig == null) return;
            for (int i = 0; i < _cachedConfig.Textures.Count; i++)
            {
                if (_cachedConfig.Textures[i].MainTex != null)
                {
                    computeShader.SetTexture(kernel, $"_Tex{i}", _cachedConfig.Textures[i].MainTex);
                    computeShader.SetFloat($"_Scale{i}", _cachedConfig.Textures[i].Tiling);
                }
            }
        }

        public void GenerateChunkAsync(int3 size, int3 worldPos, VoxelGraphData graph, TerrainSettings globalSettings, System.Action<Mesh, float[]> onComplete)
        {
            int3 actualSize = size + new int3(1, 1, 1);
            int numPoints = actualSize.x * actualSize.y * actualSize.z;

            ComputeBuffer densitiesBuffer = new ComputeBuffer(numPoints, sizeof(float));
            ComputeBuffer trianglesBuffer = new ComputeBuffer(numPoints * 5, 84, ComputeBufferType.Append);
            trianglesBuffer.SetCounterValue(0);

            // Установка глобальных параметров
            computeShader.SetInts("ChunkSize", actualSize.x, actualSize.y, actualSize.z);
            computeShader.SetFloat("IsoLevel", 0f);
            computeShader.SetFloat("_Seed", globalSettings.seed);
            computeShader.SetFloat("_HubScale", globalSettings.hubScale);
            computeShader.SetFloat("_HubThreshold", globalSettings.hubThreshold);
            computeShader.SetFloat("_BranchScale", globalSettings.branchScale);
            computeShader.SetFloat("_BranchThreshold", globalSettings.branchThreshold);

            // Ядро плотности
            BindTexturesAndParams(densityKernel, worldPos);
            computeShader.SetBuffer(densityKernel, "Densities", densitiesBuffer);
            computeShader.Dispatch(densityKernel, Mathf.CeilToInt(actualSize.x / 4f), Mathf.CeilToInt(actualSize.y / 4f), Mathf.CeilToInt(actualSize.z / 4f));

            // Ядро меша
            BindTexturesAndParams(meshKernel, worldPos);
            computeShader.SetBuffer(meshKernel, "Densities", densitiesBuffer);
            computeShader.SetBuffer(meshKernel, "Triangles", trianglesBuffer);
            computeShader.SetBuffer(meshKernel, "TriTable", triTableBuffer);
            computeShader.SetBuffer(meshKernel, "EdgeVertices", edgeVerticesBuffer);
            computeShader.SetBuffer(meshKernel, "Corners", cornersBuffer);
            computeShader.Dispatch(meshKernel, Mathf.CeilToInt(size.x / 4f), Mathf.CeilToInt(size.y / 4f), Mathf.CeilToInt(size.z / 4f));

            ComputeBuffer argsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(trianglesBuffer, argsBuffer, 0);

            AsyncGPUReadback.Request(argsBuffer, argsReq => {
                int triCount = argsReq.GetData<int>()[0];
                if (triCount == 0)
                {
                    onComplete?.Invoke(null, null);
                    densitiesBuffer.Release(); trianglesBuffer.Release(); argsBuffer.Release();
                    return;
                }

                AsyncGPUReadback.Request(trianglesBuffer, triCount * 84, 0, triReq => {
                    Triangle[] gpuTriangles = triReq.GetData<Triangle>().ToArray();
                    AsyncGPUReadback.Request(densitiesBuffer, denReq => {
                        float[] dens = denReq.GetData<float>().ToArray();
                        Mesh mesh = BuildMesh(gpuTriangles, triCount);
                        onComplete?.Invoke(mesh, dens);
                        densitiesBuffer.Release(); trianglesBuffer.Release(); argsBuffer.Release();
                    });
                });
            });
        }

        public Mesh GenerateChunkSync(int3 size, int3 worldPos, VoxelGraphData graph, TerrainSettings globalSettings)
        {
            int3 actualSize = size + new int3(1, 1, 1);
            int numPoints = actualSize.x * actualSize.y * actualSize.z;
            ComputeBuffer densitiesBuffer = new ComputeBuffer(numPoints, sizeof(float));
            ComputeBuffer trianglesBuffer = new ComputeBuffer(numPoints * 5, 84, ComputeBufferType.Append);
            trianglesBuffer.SetCounterValue(0);

            computeShader.SetInts("ChunkSize", actualSize.x, actualSize.y, actualSize.z);
            computeShader.SetFloat("IsoLevel", 0f);
            computeShader.SetFloat("_Seed", globalSettings.seed);

            BindTexturesAndParams(densityKernel, worldPos);
            computeShader.SetBuffer(densityKernel, "Densities", densitiesBuffer);
            computeShader.Dispatch(densityKernel, Mathf.CeilToInt(actualSize.x / 4f), Mathf.CeilToInt(actualSize.y / 4f), Mathf.CeilToInt(actualSize.z / 4f));

            BindTexturesAndParams(meshKernel, worldPos);
            computeShader.SetBuffer(meshKernel, "Densities", densitiesBuffer);
            computeShader.SetBuffer(meshKernel, "Triangles", trianglesBuffer);
            computeShader.SetBuffer(meshKernel, "TriTable", triTableBuffer);
            computeShader.SetBuffer(meshKernel, "EdgeVertices", edgeVerticesBuffer);
            computeShader.SetBuffer(meshKernel, "Corners", cornersBuffer);
            computeShader.Dispatch(meshKernel, Mathf.CeilToInt(size.x / 4f), Mathf.CeilToInt(size.y / 4f), Mathf.CeilToInt(size.z / 4f));

            ComputeBuffer argsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(trianglesBuffer, argsBuffer, 0);
            int[] args = new int[4]; argsBuffer.GetData(args);
            int triCount = args[0];

            Mesh mesh = null;
            if (triCount > 0)
            {
                Triangle[] gpuTriangles = new Triangle[triCount];
                trianglesBuffer.GetData(gpuTriangles, 0, 0, triCount);
                mesh = BuildMesh(gpuTriangles, triCount);
            }

            densitiesBuffer.Release(); trianglesBuffer.Release(); argsBuffer.Release();
            return mesh;
        }

        public void RebuildMeshAsync(int3 size, int3 worldPos, VoxelGraphData graph, Unity.Collections.NativeArray<float> existingDensities, System.Action<Mesh> onComplete)
        {
            int3 actualSize = size + new int3(1, 1, 1);
            ComputeBuffer densitiesBuffer = new ComputeBuffer(actualSize.x * actualSize.y * actualSize.z, sizeof(float));
            densitiesBuffer.SetData(existingDensities);
            ComputeBuffer trianglesBuffer = new ComputeBuffer((size.x * size.y * size.z) * 5, 84, ComputeBufferType.Append);
            trianglesBuffer.SetCounterValue(0);

            computeShader.SetInts("ChunkSize", actualSize.x, actualSize.y, actualSize.z);
            computeShader.SetFloat("IsoLevel", 0f);

            BindTexturesAndParams(meshKernel, worldPos);
            computeShader.SetBuffer(meshKernel, "Densities", densitiesBuffer);
            computeShader.SetBuffer(meshKernel, "Triangles", trianglesBuffer);
            computeShader.SetBuffer(meshKernel, "TriTable", triTableBuffer);
            computeShader.SetBuffer(meshKernel, "EdgeVertices", edgeVerticesBuffer);
            computeShader.SetBuffer(meshKernel, "Corners", cornersBuffer);
            computeShader.Dispatch(meshKernel, Mathf.CeilToInt(size.x / 4f), Mathf.CeilToInt(size.y / 4f), Mathf.CeilToInt(size.z / 4f));

            ComputeBuffer argsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(trianglesBuffer, argsBuffer, 0);

            AsyncGPUReadback.Request(argsBuffer, argsReq => {
                int triCount = argsReq.GetData<int>()[0];
                if (triCount == 0)
                {
                    onComplete?.Invoke(null);
                    densitiesBuffer.Release(); trianglesBuffer.Release(); argsBuffer.Release();
                    return;
                }
                AsyncGPUReadback.Request(trianglesBuffer, triCount * 84, 0, triReq => {
                    Triangle[] gpuTriangles = triReq.GetData<Triangle>().ToArray();
                    Mesh mesh = BuildMesh(gpuTriangles, triCount);
                    onComplete?.Invoke(mesh);
                    densitiesBuffer.Release(); trianglesBuffer.Release(); argsBuffer.Release();
                });
            });
        }

        private Mesh BuildMesh(Triangle[] triangles, int triCount)
        {
            Mesh mesh = new Mesh();
            if (triCount > 21845) mesh.indexFormat = IndexFormat.UInt32; // Для больших мешей

            int vertCount = triCount * 3;
            Vector3[] v = new Vector3[vertCount];
            Color[] c = new Color[vertCount];
            int[] idx = new int[vertCount];

            for (int i = 0; i < triCount; i++)
            {
                int b = i * 3;
                v[b] = triangles[i].a; v[b + 1] = triangles[i].b; v[b + 2] = triangles[i].c;
                c[b] = triangles[i].colorA; c[b + 1] = triangles[i].colorB; c[b + 2] = triangles[i].colorC;
                idx[b] = b; idx[b + 1] = b + 1; idx[b + 2] = b + 2;
            }
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(idx, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Dispose()
        {
            triTableBuffer?.Release(); edgeVerticesBuffer?.Release(); cornersBuffer?.Release();
            triTableBuffer = edgeVerticesBuffer = cornersBuffer = null;
        }
    }
}