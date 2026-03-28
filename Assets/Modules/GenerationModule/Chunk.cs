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
        private WorldManager worldManager;

        private IGenerator generator;
        private IMeshBuilder meshBuilder;

        public void Initialize(int3 size, int3 worldPos, TerrainSettings settings, WorldManager manager)
        {
            this.worldManager = manager; // Сохраняем ссылку на менеджер
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            voxelData = new ChunkData(size);

            // 1. Пытаемся загрузить из памяти
            if (!worldManager.TryLoadChunkState(worldPos, voxelData.GetNativeArray()))
            {
                // 2. Если данных нет - генерируем шум
                generator = new TerrainGenerator(settings);
                generator.Generate(voxelData, worldPos);
            }

            meshBuilder = new VoxelMeshBuilder();
            UpdateMesh();
        }

        public void UpdateMesh()
        {
            Mesh newMesh = meshBuilder.BuildMesh(voxelData);

            // 1. Простая проверка на существование меша
            if (newMesh == null || newMesh.vertexCount < 3)
            {
                meshFilter.sharedMesh = null;
                meshCollider.sharedMesh = null;
                meshCollider.enabled = false;
                return;
            }

            // 2. Сбрасываем коллайдер (стандартное правило Unity)
            meshCollider.sharedMesh = null;

            // 3. Назначаем меш визуальному фильтру
            meshFilter.sharedMesh = newMesh;

            // 4. ЗАПЕКАЕМ ФИЗИКУ (Новый способ)
            // Мы просто вызываем его отдельной строкой. 
            // Передаем сам объект меша и false (так как нам нужен не Convex, а обычный TriMesh)
            Physics.BakeMesh(newMesh.GetInstanceID(), false);

            // 5. Назначаем меш коллайдеру и включаем его
            meshCollider.sharedMesh = newMesh;
            meshCollider.enabled = true;
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
