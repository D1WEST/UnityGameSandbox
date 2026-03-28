using Assets.Modules.GenerationModule.Abstractions;
using Assets.Modules.GenerationModule.Models;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Modules.GenerationModule.Burst
{
    public class TerrainGenerator : IGenerator
    {
        public void Generate(IVoxelData data, int3 chunkWorldPos)
        {
            var chunkData = (ChunkData)data;
            var job = new DensityJob
            {
                Densities = chunkData.GetNativeArray(),
                ChunkSize = chunkData.Size,
                WorldOffset = chunkWorldPos
            };
            job.Schedule(chunkData.GetNativeArray().Length, 64).Complete();
        }

        [BurstCompile]
        private struct DensityJob : IJobParallelFor
        {
            public NativeArray<float> Densities;
            public int3 ChunkSize;
            public int3 WorldOffset;

            public void Execute(int index)
            {
                // Декодируем 1D индекс в 3D координаты
                int x = index % ChunkSize.x;
                int y = (index / ChunkSize.x) % ChunkSize.y;
                int z = index / (ChunkSize.x * ChunkSize.y);

                float3 worldPos = new float3(x, y, z) + WorldOffset;

                // 1. Биомы (по 2D карте температуры/влажности)
                float temperature = noise2D(worldPos.x * 0.001f, worldPos.z * 0.001f);

                // 2. Базовый рельеф (Перлин) - высота зависит от температуры
                float baseHeight = 50f + (temperature * 20f);
                float density = baseHeight - worldPos.y; // Чем выше, тем меньше плотность

                // Добавляем 3D шум для неровностей
                density += noise3D(worldPos * 0.05f) * 10f;

                // 3. Пещеры ("Червивый" шум - Риджед 3D)
                float caveNoise = noise3D_ridged(worldPos * 0.02f);
                if (caveNoise > 0.6f && worldPos.y < 40f)
                { // Пещеры только под землей
                    density = -1f; // Вырезаем пещеру
                }

                Densities[index] = density;
            }

            // Заглушки для шумов (используй FastNoiseLite в реальности)
            private float noise2D(float x, float y) => 0f;
            private float noise3D(float3 pos) => 0f;
            private float noise3D_ridged(float3 pos) => 0f;
        }
    }
}
