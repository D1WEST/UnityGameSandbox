using Assets.Modules.GenerationModule.EditTools;
using Assets.Modules.GenerationModule.Impl;
using Assets.Modules.GenerationModule.Models;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Modules.GenerationModule
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        private ChunkData voxelData;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private MeshRenderer meshRenderer;

        private WorldManager worldManager;
        private GPUChunkGenerator gpuChunkGenerator;
        private int3 chunkWorldPos;
        private int3 chunkSize;

        public void InitializeGPU(int3 size, int3 worldPos, TerrainSettings globalSettings, VoxelGraphData graph, WorldManager manager, GPUChunkGenerator gpuGen)
        {
            worldManager = manager;
            gpuChunkGenerator = gpuGen;
            chunkWorldPos = worldPos;
            chunkSize = size;

            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            meshRenderer = GetComponent<MeshRenderer>();

            voxelData = new ChunkData(size);

            // Если чанк был сохранен (ранее копали) — загружаем его плотности и перестраиваем меш
            if (worldManager.TryLoadChunkState(worldPos, voxelData.GetNativeArray()))
            {
                UpdateMesh();
            }
            else
            {
                // Иначе генерируем с нуля (Шум + Меш) на GPU
                gpuChunkGenerator.GenerateChunkAsync(size, worldPos, graph, globalSettings, (mesh, densitiesArray) =>
                {
                    if (this == null || gameObject == null)
                    {
                        if (mesh != null) Destroy(mesh);
                        return;
                    }

                    ApplyMesh(mesh);

                    // Сохраняем сгенерированные плотности для будущих раскопок
                    if (densitiesArray != null && voxelData != null)
                    {
                        voxelData.GetNativeArray().CopyFrom((float[])densitiesArray);
                    }
                });
            }
        }

        // Вызывается при копании/строительстве игроком
        public void UpdateMesh()
        {
            if (gpuChunkGenerator == null || voxelData == null) return;

            // Запускаем ТОЛЬКО перестроение меша на основе измененного массива плотностей
            gpuChunkGenerator.RebuildMeshAsync(chunkSize, chunkWorldPos, worldManager.graphData, voxelData.GetNativeArray(), (newMesh) =>
            {
                if (this == null || gameObject == null)
                {
                    if (newMesh != null) Destroy(newMesh);
                    return;
                }

                ApplyMesh(newMesh);
            });
        }

        private void ApplyMesh(Mesh mesh)
        {
            if (mesh != null && mesh.vertexCount >= 3)
            {
                meshFilter.sharedMesh = mesh;
                Physics.BakeMesh(mesh.GetInstanceID(), false);
                meshCollider.sharedMesh = mesh;
                meshCollider.enabled = true;
                meshRenderer.enabled = true;

                if (chunkWorldPos.y < 0)
                {
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            else
            {
                meshFilter.sharedMesh = null;
                meshCollider.sharedMesh = null;
            }
        }

        public ChunkData GetVoxelData() => voxelData;

        private void OnDestroy()
        {
            voxelData?.Dispose();
        }
    }
}