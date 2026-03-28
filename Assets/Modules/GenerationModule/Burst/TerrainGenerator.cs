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

        private TerrainSettings settings;

        // Конструктор получает настройки из Инспектора
        public TerrainGenerator(TerrainSettings settings)
        {
            this.settings = settings;
        }

        public void Generate(IVoxelData data, int3 chunkWorldPos)
        {
            var chunkData = (ChunkData)data;

            var job = new DensityJob
            {
                Densities = chunkData.GetNativeArray(),
                ChunkSize = chunkData.Size,
                WorldOffset = chunkWorldPos,
                Settings = settings // Передаем структуру в Job
            };

            job.Schedule(chunkData.GetNativeArray().Length, 64).Complete();
        }


        [BurstCompile]
        private struct DensityJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<float> Densities;
            public int3 ChunkSize;
            public int3 WorldOffset;
            public TerrainSettings Settings;

            public void Execute(int index)
            {
                int x = index % ChunkSize.x;
                int y = (index / ChunkSize.x) % ChunkSize.y;
                int z = index / (ChunkSize.x * ChunkSize.y);

                float3 worldPos = new float3(x, y, z) + WorldOffset;
                float2 pos2D = new float2(worldPos.x, worldPos.z) + Settings.seed;

                // Генерация базовой высоты
                float biomeNoise = noise.snoise(pos2D * Settings.biomeScale);
                float targetHeight = Settings.plainsHeight;

                if (biomeNoise > 0.3f)
                {
                    float t = math.unlerp(0.3f, 0.7f, biomeNoise);
                    targetHeight = math.lerp(Settings.plainsHeight, Settings.mountainHeight,
                        math.smoothstep(0, 1, t));
                }

                float density = targetHeight - worldPos.y;

                // Добавляем 3D шум (пещеры и детали)
                float detail = noise.snoise(worldPos * Settings.detailScale) * Settings.detailAmplitude;
                density += detail;

                // Ограничение по высоте (небо и ад)
                if (worldPos.y < 5) density += (5 - worldPos.y) * 2f; // Жесткий пол
                if (worldPos.y > Settings.mountainHeight + 20)
                    density -= (worldPos.y - (Settings.mountainHeight + 20));

                Densities[index] = math.clamp(density, -1f, 1f);
            }
        }

    }
}

