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

                // 1. БИОМЫ (Глобальная высота)
                float biomeNoise = noise.snoise(pos2D * Settings.biomeScale);
                float targetHeight = Settings.plainsHeight;

                if (biomeNoise < -0.2f)
                {
                    // Переход в океан
                    float t = math.unlerp(-0.4f, -0.2f, biomeNoise);
                    targetHeight = math.lerp(Settings.oceanHeight, Settings.plainsHeight, math.smoothstep(0f, 1f, t));
                }
                else if (biomeNoise > 0.3f)
                {
                    // Переход в горы
                    float t = math.unlerp(0.3f, 0.6f, biomeNoise);
                    targetHeight = math.lerp(Settings.plainsHeight, Settings.mountainHeight,
                        math.smoothstep(0f, 1f, t));

                    // Добавляем скалистость только горам
                    targetHeight += math.abs(noise.snoise(pos2D * 0.01f)) * 15f;
                }

                // ИДЕАЛЬНАЯ ФОРМУЛА ПЛОТНОСТИ: Высота минус Y. 
                // Это создает 100% твердую землю внизу и 100% пустой воздух вверху.
                float density = targetHeight - worldPos.y;

                // 2. 3D ДЕТАЛИ (Органичность)
                // Применяем 3D шум ТОЛЬКО в зоне поверхности (чтобы не тратить математику глубоко под землей или высоко в небе)
                if (math.abs(density) < 15f)
                {
                    float detail3D = noise.snoise((worldPos + Settings.seed) * Settings.detailScale);
                    density += detail3D * Settings.detailAmplitude;
                }

                // Делаем неразрушимое дно
                if (worldPos.y < 2f) density += 50f;

                // 3. ЧЕРВИВЫЕ ПЕЩЕРЫ
                // Генерируем пещеры только если мы под землей
                if (density > 0f && worldPos.y < targetHeight - 5f)
                {
                    float caveNoise1 = math.abs(noise.snoise((worldPos + Settings.seed) * Settings.caveScale));
                    float caveNoise2 = math.abs(noise.snoise((worldPos + Settings.seed + 100f) * Settings.caveScale));

                    if (caveNoise1 < Settings.caveThickness || caveNoise2 < Settings.caveThickness)
                    {
                        float caveWeight = math.max(0f, Settings.caveThickness - caveNoise1) +
                                           math.max(0f, Settings.caveThickness - caveNoise2);

                        // Вычитаем из положительной плотности большое число, чтобы стал воздух (-)
                        density -= caveWeight * 100f;
                    }
                }

                Densities[index] = math.clamp(density, -1f, 1f);
            }
        }
    }
}
