using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Assets.Modules.GenerationModule.Models;
using Assets.Modules.GenerationModule.Static;

public class GPUChunkGenerator
{
    private ComputeShader computeShader;
    private int densityKernel;
    private int meshKernel;

    private static ComputeBuffer triTableBuffer;
    private static ComputeBuffer edgeVerticesBuffer;
    private static ComputeBuffer cornersBuffer;

    struct Triangle { public Vector3 a, b, c; }

    public GPUChunkGenerator(ComputeShader shader)
    {
        computeShader = shader;
        densityKernel = computeShader.FindKernel("GenerateDensity");
        meshKernel = computeShader.FindKernel("GenerateMesh");
        InitializeTables();
    }

    private void InitializeTables()
    {
        if (triTableBuffer != null) return;

        triTableBuffer = new ComputeBuffer(MarchingCubesTables.TriTable.Length, sizeof(int));
        triTableBuffer.SetData(MarchingCubesTables.TriTable);

        edgeVerticesBuffer = new ComputeBuffer(MarchingCubesTables.EdgeVertices.Length, sizeof(int) * 2);
        edgeVerticesBuffer.SetData(MarchingCubesTables.EdgeVertices);

        cornersBuffer = new ComputeBuffer(MarchingCubesTables.Corners.Length, sizeof(int) * 3);
        cornersBuffer.SetData(MarchingCubesTables.Corners);
    }

    public void GenerateChunkAsync(int3 size, int3 worldPos, TerrainSettings settings, System.Action<Mesh, float[]> onComplete)
    {
        // СЕКРЕТ ИДЕАЛЬНЫХ СТЫКОВ: Данных должно быть на 1 больше, чем размер чанка!
        int3 actualSize = size + new int3(1, 1, 1);

        int numPoints = actualSize.x * actualSize.y * actualSize.z;
        int maxTriangles = (actualSize.x - 1) * (actualSize.y - 1) * (actualSize.z - 1) * 5;

        ComputeBuffer densitiesBuffer = new ComputeBuffer(numPoints, sizeof(float));
        ComputeBuffer trianglesBuffer = new ComputeBuffer(maxTriangles, sizeof(float) * 9, ComputeBufferType.Append);
        trianglesBuffer.SetCounterValue(0);

        // Передаем actualSize (33) вместо size (32)
        computeShader.SetInts("ChunkSize", actualSize.x, actualSize.y, actualSize.z);
        computeShader.SetInts("WorldOffset", worldPos.x, worldPos.y, worldPos.z);
        computeShader.SetFloat("IsoLevel", 0f);

        computeShader.SetFloat("_Seed", settings.seed);
        computeShader.SetFloat("_BiomeScale", settings.biomeScale);
        computeShader.SetFloat("_OceanHeight", settings.oceanHeight);
        computeShader.SetFloat("_PlainsHeight", settings.plainsHeight);
        computeShader.SetFloat("_MountainHeight", settings.mountainHeight);
        computeShader.SetFloat("_HubScale", settings.hubScale);
        computeShader.SetFloat("_HubThreshold", settings.hubThreshold);
        computeShader.SetFloat("_BranchScale", settings.branchScale);
        computeShader.SetFloat("_BranchThreshold", settings.branchThreshold);

        computeShader.SetBuffer(densityKernel, "Densities", densitiesBuffer);

        // Считаем потоки на основе actualSize
        int threadGroupsX = Mathf.CeilToInt(actualSize.x / 8f);
        int threadGroupsY = Mathf.CeilToInt(actualSize.y / 8f);
        int threadGroupsZ = Mathf.CeilToInt(actualSize.z / 8f);

        computeShader.Dispatch(densityKernel, threadGroupsX, threadGroupsY, threadGroupsZ);

        computeShader.SetBuffer(meshKernel, "Densities", densitiesBuffer);
        computeShader.SetBuffer(meshKernel, "Triangles", trianglesBuffer);
        computeShader.SetBuffer(meshKernel, "TriTable", triTableBuffer);
        computeShader.SetBuffer(meshKernel, "EdgeVertices", edgeVerticesBuffer);
        computeShader.SetBuffer(meshKernel, "Corners", cornersBuffer);

        computeShader.Dispatch(meshKernel, threadGroupsX, threadGroupsY, threadGroupsZ);

        ComputeBuffer argsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(trianglesBuffer, argsBuffer, 0);

        void ReleaseBuffers()
        {
            densitiesBuffer?.Release();
            trianglesBuffer?.Release();
            argsBuffer?.Release();
        }

        AsyncGPUReadback.Request(argsBuffer, argsReq =>
        {
            if (argsReq.hasError) { ReleaseBuffers(); return; }

            int triCount = argsReq.GetData<int>()[0];

            if (triCount == 0)
            {
                ReleaseBuffers();
                onComplete?.Invoke(null, null);
                return;
            }

            AsyncGPUReadback.Request(trianglesBuffer, triCount * sizeof(float) * 9, 0, triReq =>
            {
                if (triReq.hasError) { ReleaseBuffers(); return; }

                Triangle[] gpuTriangles = triReq.GetData<Triangle>().ToArray();

                AsyncGPUReadback.Request(densitiesBuffer, denReq =>
                {
                    if (denReq.hasError) { ReleaseBuffers(); return; }

                    float[] chunkDensities = denReq.GetData<float>().ToArray();

                    Mesh mesh = new Mesh();
                    Vector3[] vertices = new Vector3[triCount * 3];
                    int[] indices = new int[triCount * 3];

                    for (int i = 0; i < triCount; i++)
                    {
                        vertices[i * 3] = gpuTriangles[i].a;
                        vertices[i * 3 + 1] = gpuTriangles[i].b;
                        vertices[i * 3 + 2] = gpuTriangles[i].c;

                        indices[i * 3] = i * 3;
                        indices[i * 3 + 1] = i * 3 + 1;
                        indices[i * 3 + 2] = i * 3 + 2;
                    }

                    mesh.SetVertices(vertices);
                    MeshUpdateFlags flags = MeshUpdateFlags.DontValidateIndices |
                                            MeshUpdateFlags.DontResetBoneBounds |
                                            MeshUpdateFlags.DontNotifyMeshUsers |
                                            MeshUpdateFlags.DontRecalculateBounds;

                    mesh.SetTriangles(indices, 0, false, 0);
                    mesh.RecalculateNormals(flags);
                    mesh.RecalculateBounds(flags);

                    ReleaseBuffers();

                    onComplete?.Invoke(mesh, chunkDensities);
                });
            });
        });
    }

    public void Dispose()
    {
        triTableBuffer?.Release();
        edgeVerticesBuffer?.Release();
        cornersBuffer?.Release();
    }
}