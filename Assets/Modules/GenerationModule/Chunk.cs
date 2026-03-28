namespace Assets.Modules.GenerationModule
{
    using Assets.Modules.GenerationModule.Abstractions;
    using Assets.Modules.GenerationModule.Burst;
    using Assets.Modules.GenerationModule.Impl;
    using Assets.Modules.GenerationModule.Models;
    using Unity.Mathematics;
    using UnityEngine;

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        private ChunkData voxelData;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        private IGenerator generator;
        private IMeshBuilder meshBuilder;

        public void Initialize(int3 size, int3 worldPos)
        {
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();

            // Инжектим зависимости (D из SOLID)
            voxelData = new ChunkData(size);
            generator = new TerrainGenerator();
            meshBuilder = new VoxelMeshBuilder();

            // 1. Генерируем плотность
            generator.Generate(voxelData, worldPos);

            // 2. Строим меш
            UpdateMesh();
        }

        public void UpdateMesh()
        {
            Mesh newMesh = meshBuilder.BuildMesh(voxelData);

            meshFilter.sharedMesh = newMesh;

            // Обновление коллайдера может быть тяжелым. 
            // В Unity 2023+ есть Physics.BakeMesh для асинхронности.
            meshCollider.sharedMesh = newMesh;
        }

        // Доступ к данным для системы копания (из первой части)
        public ChunkData GetVoxelData() => voxelData;

        private void OnDestroy()
        {
            // Очищаем NativeArrays при удалении чанка
            voxelData?.Dispose();
        }
    }
}
