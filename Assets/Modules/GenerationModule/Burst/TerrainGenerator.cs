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
                // 1. Координаты и базовая плотность (стандартно)
                int x = index % ChunkSize.x;
                int y = (index / ChunkSize.x) % ChunkSize.y;
                int z = index / (ChunkSize.x * ChunkSize.y);

                float3 worldPos = new float3(x, y, z) + WorldOffset;
                float2 pos2D = new float2(worldPos.x, worldPos.z) + Settings.seed;

                float biomeNoise = noise.snoise(pos2D * Settings.biomeScale);
                float targetHeight = math.lerp(Settings.oceanHeight, Settings.plainsHeight, math.smoothstep(-0.2f, 0.1f, biomeNoise));
                targetHeight = math.lerp(targetHeight, Settings.mountainHeight, math.smoothstep(0.3f, 0.8f, biomeNoise));

                float density = targetHeight - worldPos.y;
                density = math.clamp(density, -15f, 15f); // Ограничиваем, чтобы пещеры пробивали камень

                // --- ГЕНЕРАЦИЯ МАСКИ ПЕЩЕР ---

                // 2. ХАБЫ (Редкие залы)
                float3 hubPos = worldPos * Settings.hubScale;
                hubPos.y *= 2.5f; // Сжимаем по вертикали, чтобы залы были приплюснутыми (высота ~4м)
                float hNoise = noise.snoise(hubPos + Settings.seed);
                // Хаб появляется только в очень редких пиках шума
                float hubMask = math.smoothstep(Settings.hubThreshold, Settings.hubThreshold + 0.05f, hNoise);

                // 3. ТОННЕЛИ (Длинные и узкие "кишки")
                float3 bPos1 = worldPos * Settings.branchScale;
                float3 bPos2 = (worldPos + new float3(123f, 456f, 789f)) * Settings.branchScale;

                // Сильное сжатие по вертикали (4.0), чтобы высота прохода была около 2 метров
                bPos1.y *= 4.0f;
                bPos2.y *= 4.0f;

                float n1 = noise.snoise(bPos1);
                float n2 = noise.snoise(bPos2);
                float branchShape = (n1 * n1) + (n2 * n2);

                // Тоннель — это очень узкая область, где branchShape близок к нулю
                float branchMask = math.smoothstep(Settings.branchThreshold, Settings.branchThreshold - 0.01f, branchShape);

                // 4. ОБЪЕДИНЕНИЕ И ФИЛЬТРАЦИЯ
                // Смешиваем хабы и ветки в одну маску
                float caveMask = math.max(hubMask, branchMask);

                // Фильтр: убираем пещеры на поверхности (минимум 5 метров под землей)
                float surfaceMask = math.smoothstep(3f, 8f, targetHeight - worldPos.y);
                caveMask *= surfaceMask;

                // 5. ПРИМЕНЕНИЕ МАСКИ К ПЛОТНОСТИ
                // Если caveMask = 1, плотность станет -10 (воздух). Если 0 — останется камнем.
                density = math.lerp(density, -10f, caveMask);

                // 6. БЕДРОК (Твердое дно на уровне 2 вокселей)
                if (worldPos.y < (Settings.minChunkY * 16) + 2) density = 20f;

                Densities[index] = density;
            }
        }
    }
}


