using Assets.Modules.GenerationModule.Abstractions;
using Assets.Modules.GenerationModule.Impl;
using Assets.Modules.GenerationModule.Models;
using Assets.Modules.GenerationModule.Models.WestMM;
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

        public void InitializeGPU(int3 size, int3 worldPos, TerrainSettings globalSettings, WorldProfile worldProfile, WorldManager manager, GPUChunkGenerator gpuGen)
        {
            this.worldManager = manager;
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            meshRenderer = GetComponent<MeshRenderer>();

            voxelData = new ChunkData(size);
            meshBuilder = new VoxelMeshBuilder();

            if (worldManager.TryLoadChunkState(worldPos, voxelData.GetNativeArray()))
            {
                UpdateMesh();
            }
            else
            {
                // ИСПРАВЛЕНО: Передаем 5 аргументов (добавлен worldProfile)
                gpuGen.GenerateChunkAsync(size, worldPos, worldProfile, globalSettings, (mesh, densitiesArray) =>
                {
                    if (this == null || gameObject == null)
                    {
                        if (mesh != null) Destroy(mesh);
                        return;
                    }

                    if (mesh != null && mesh.vertexCount >= 3)
                    {
                        meshFilter.sharedMesh = mesh;

                        // ИСПРАВЛЕНО: Явное приведение к (int) для BakeMesh
                        Physics.BakeMesh((int)mesh.GetInstanceID(), false);

                        meshCollider.sharedMesh = mesh;
                        meshCollider.enabled = true;
                        meshRenderer.enabled = true;

                        if (worldPos.y < 0)
                        {
                            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        }
                    }

                    if (densitiesArray != null && voxelData != null)
                    {
                        // ИСПРАВЛЕНО: Явное приведение к (float[]) для CopyFrom
                        voxelData.GetNativeArray().CopyFrom((float[])densitiesArray);
                    }
                });
            }
        }

        public void UpdateMesh()
        {
            if (meshBuilder == null || voxelData == null) return;

            // Передаем профиль и глобальную позицию чанка
            int3 worldOffset = new int3((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
            Mesh newMesh = meshBuilder.BuildMesh(voxelData, worldManager.worldProfile, worldOffset);

            if (newMesh == null)
            {
                meshFilter.sharedMesh = null;
                meshCollider.sharedMesh = null;
                return;
            }

            meshFilter.sharedMesh = newMesh;
            Physics.BakeMesh((int)newMesh.GetInstanceID(), false);
            meshCollider.sharedMesh = newMesh;
        }

        public ChunkData GetVoxelData() => voxelData;

        private void OnDestroy()
        {
            voxelData?.Dispose();
        }
    }
}