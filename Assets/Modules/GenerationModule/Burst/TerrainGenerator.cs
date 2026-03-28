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

                // 1. БАЗОВЫЙ РЕЛЬЕФ
                float biomeNoise = noise.snoise(pos2D * Settings.biomeScale);
                float targetHeight = math.lerp(Settings.oceanHeight, Settings.plainsHeight,
                    math.smoothstep(-0.2f, 0.1f, biomeNoise));
                targetHeight = math.lerp(targetHeight, Settings.mountainHeight,
                    math.smoothstep(0.3f, 0.8f, biomeNoise));

                // Основная плотность поверхности
                float density = targetHeight - worldPos.y;

                // ВАЖНО: Ограничиваем плотность земли. 
                // Теперь на любой глубине плотность камня не превысит 15.
                // Это позволит пещерам легко "прогрызать" землю даже на самом дне.
                density = math.clamp(density, -15f, 15f);

                // 2. ГИГАНТСКИЕ ПЕЩЕРЫ (3D Тоннели)
                // Чтобы пещеры были километровыми, масштаб (scale) должен быть ОЧЕНЬ маленьким
                float3 cavePos1 = worldPos * Settings.caveScale;
                float3 cavePos2 = (worldPos + new float3(1234.5f, 678.9f, 543.2f)) * Settings.caveScale;

                // Оставляем высоту (y) как есть или чуть сжимаем (0.9), чтобы были почти круглыми
                cavePos1.y *= 0.9f;
                cavePos2.y *= 0.9f;

                float n1 = noise.snoise(cavePos1);
                float n2 = noise.snoise(cavePos2);

                // Формула "Трубы"
                float caveShape = (n1 * n1) + (n2 * n2);

                // Порог CaveThreshold теперь напрямую управляет радиусом.
                if (caveShape < Settings.caveThreshold)
                {
                    // Плавный переход для стенок пещеры
                    // Вычисляем, насколько глубоко мы внутри "трубы" пещеры
                    float caveInfluence = math.remap(0, Settings.caveThreshold, 1f, 0f, caveShape);
                    caveInfluence = math.smoothstep(0, 1, caveInfluence);

                    // Резко уводим плотность в минус (в воздух), если мы в центре трубы
                    // 20f - это сила вырезания, она должна быть больше нашего зажатого density (15f)
                    float caveDensity = -20f;

                    // Смешиваем плотность земли и пещеры
                    density = math.lerp(density, caveDensity, caveInfluence);
                }

                // 3. БЕДРОК (Твердое дно)
                // Если хотим, чтобы пещеры не прорывали самый низ мира:
                if (worldPos.y < (Settings.minChunkY * 16) + 5)
                {
                    float bedrockForce = math.smoothstep((Settings.minChunkY * 16), (Settings.minChunkY * 16) + 5,
                        worldPos.y);
                    density = math.lerp(20f, density, bedrockForce);
                }

                Densities[index] = density;
            }
        }
    }
}


