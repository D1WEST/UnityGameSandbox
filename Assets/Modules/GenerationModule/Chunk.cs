using Assets.Modules.GenerationModule.Abstractions;
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

        // Наш CPU-билдер для копания
        private IMeshBuilder meshBuilder;

        public void InitializeGPU(int3 size, int3 worldPos, TerrainSettings settings, WorldManager manager, GPUChunkGenerator gpuGen)
        {
            this.worldManager = manager;
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Твой ChunkData сам прибавляет +1 к размеру (становится 33x33x33), 
            // что идеально совпадает с массивом от GPU!
            voxelData = new ChunkData(size);

            // ОБЯЗАТЕЛЬНО: Инициализируем билдер, чтобы игрок мог копать
            meshBuilder = new VoxelMeshBuilder();

            // 1. Проверяем, копал ли игрок уже в этом чанке?
            if (worldManager.TryLoadChunkState(worldPos, voxelData.GetNativeArray()))
            {
                // Если копал — данные скачались из памяти. 
                // Сразу строим меш на процессоре (Burst).
                UpdateMesh();
            }
            else
            {
                // 2. Если чанк девственно чист — генерируем его асинхронно на GPU
                gpuGen.GenerateChunkAsync(size, worldPos, settings, (mesh, densitiesArray) =>
                {
                    // Проверка, не убежал ли игрок слишком далеко, пока GPU считала
                    if (this == null || gameObject == null)
                    {
                        if (mesh != null) Destroy(mesh);
                        return;
                    }

                    if (mesh != null && mesh.vertexCount >= 3)
                    {
                        meshFilter.sharedMesh = mesh;
                        Physics.BakeMesh(mesh.GetInstanceID(), false);
                        meshCollider.sharedMesh = mesh;
                        meshCollider.enabled = true;
                        meshRenderer.enabled = true;

                        // Оптимизация теней для подземных чанков
                        if (worldPos.y < 0)
                        {
                            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        }
                    }
                    else
                    {
                        meshFilter.sharedMesh = null;
                        meshCollider.sharedMesh = null;
                        meshCollider.enabled = false;
                        meshRenderer.enabled = false;
                    }

                    // Сохраняем плотности с GPU в нашу оперативную память (для копания)
                    if (densitiesArray != null && voxelData != null)
                    {
                        voxelData.GetNativeArray().CopyFrom(densitiesArray);
                    }
                });
            }
        }

        // Этот метод вызывается из TerrainModifier, когда игрок кликает мышкой
        public void UpdateMesh()
        {
            if (meshBuilder == null || voxelData == null) return;

            // Строим измененный меш на CPU (Burst)
            Mesh newMesh = meshBuilder.BuildMesh(voxelData);

            if (newMesh == null || newMesh.vertexCount < 3)
            {
                meshFilter.sharedMesh = null;
                meshCollider.sharedMesh = null;
                meshCollider.enabled = false;
                meshRenderer.enabled = false;
                return;
            }

            meshCollider.sharedMesh = null;
            meshFilter.sharedMesh = newMesh;
            Physics.BakeMesh(newMesh.GetInstanceID(), false);
            meshCollider.sharedMesh = newMesh;
            meshCollider.enabled = true;
            meshRenderer.enabled = true;
        }

        public ChunkData GetVoxelData() => voxelData;

        private void OnDestroy()
        {
            voxelData?.Dispose();
        }
    }
}