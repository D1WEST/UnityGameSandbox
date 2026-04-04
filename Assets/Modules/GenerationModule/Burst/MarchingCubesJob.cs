using Assets.Modules.GenerationModule.Models;
using Assets.Modules.GenerationModule.Models.WestMM;
using Assets.Modules.GenerationModule.Static;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.Modules.GenerationModule.Burst
{
    [BurstCompile]
    public struct MarchingCubesJob : IJob
    {
        [ReadOnly] public NativeArray<float> Densities;
        [ReadOnly] public NativeArray<BiomeData> Biomes;

        public int3 ChunkSize;
        public int3 WorldOffset;
        public float IsoLevel;
        public float BiomeMapScale;
        public float Seed;

        // Выходные данные
        public NativeList<float3> Vertices;
        public NativeList<int> Triangles;
        public NativeList<float4> Colors; // Цвета вершин

        public void Execute()
        {
            for (int x = 0; x < ChunkSize.x - 1; x++)
            {
                for (int y = 0; y < ChunkSize.y - 1; y++)
                {
                    for (int z = 0; z < ChunkSize.z - 1; z++)
                    {
                        ProcessCube(new int3(x, y, z));
                    }
                }
            }
        }

        private void ProcessCube(int3 pos)
        {
            float[] cubeDensities = new float[8];
            int cubeIndex = 0;

            for (int i = 0; i < 8; i++)
            {
                float d = Densities[GetIndex(pos + MarchingCubesTables.Corners[i])];
                cubeDensities[i] = d;
                if (d > IsoLevel) cubeIndex |= (1 << i);
            }

            if (cubeIndex == 0 || cubeIndex == 255) return;

            float3[] edgeVertices = new float3[12];
            for (int i = 0; i < 12; i++)
            {
                int2 edge = MarchingCubesTables.EdgeVertices[i];
                float3 p1 = pos + MarchingCubesTables.Corners[edge.x];
                float3 p2 = pos + MarchingCubesTables.Corners[edge.y];
                edgeVertices[i] = Interpolate(p1, cubeDensities[edge.x], p2, cubeDensities[edge.y]);
            }

            int tableOffset = cubeIndex * 16;
            for (int i = 0; i < 16; i += 3)
            {
                int a = MarchingCubesTables.TriTable[tableOffset + i];
                if (a == -1) break;

                int b = MarchingCubesTables.TriTable[tableOffset + i + 1];
                int c = MarchingCubesTables.TriTable[tableOffset + i + 2];

                // Добавляем вершины
                float3 vA = edgeVertices[a];
                float3 vB = edgeVertices[b];
                float3 vC = edgeVertices[c];

                Vertices.Add(vA);
                Vertices.Add(vB);
                Vertices.Add(vC);

                // РАССЧИТЫВАЕМ ЦВЕТА (как в шейдере)
                Colors.Add(GetBlendedColor(vA + (float3)WorldOffset));
                Colors.Add(GetBlendedColor(vB + (float3)WorldOffset));
                Colors.Add(GetBlendedColor(vC + (float3)WorldOffset));

                int vCount = Vertices.Length;
                Triangles.Add(vCount - 3);
                Triangles.Add(vCount - 2);
                Triangles.Add(vCount - 1);
            }
        }

        private float4 GetBlendedColor(float3 worldPos)
        {
            if (Biomes.Length == 0) return new float4(1, 1, 1, 1);

            // 1. РАСЧЕТ СЕЛЕКТОРА (Температуры)
            // Масштаб ДОЛЖЕН СОВПАДАТЬ с твоей нодой шума в порту Selector (например, 0.001)
            float3 tempPos = worldPos * 0.001f;

            // Используем cnoise (Perlin), если в графе выбран Perlin
            float currentTemp = math.saturate((noise.cnoise(tempPos) + 1.0f) / 2.0f);

            float4 finalCol = float4.zero;
            float totalW = 0.0f;

            for (int i = 0; i < Biomes.Length; i++)
            {
                float dist = math.abs(currentTemp - Biomes[i].targetTemp);

                // Если множитель 5.0, то биом "гаснет" при дистанции 0.2
                float w = math.saturate(1.0f - dist * 5.0f);
                w = math.pow(w, 2.0f);

                float4 bCol = new float4(Biomes[i].biomeColor.r, Biomes[i].biomeColor.g, Biomes[i].biomeColor.b, 1f);
                finalCol += bCol * w;
                totalW += w;
            }

            // 2. ЗАЩИТА ОТ БЕЛОГО ЦВЕТА
            // Если мы между биомами и суммарный вес слишком мал, берем цвет первого биома
            if (totalW < 0.01f)
            {
                return new float4(Biomes[0].biomeColor.r, Biomes[0].biomeColor.g, Biomes[0].biomeColor.b, 1f);
            }

            return finalCol / totalW;
        }

        private float3 Interpolate(float3 p1, float v1, float3 p2, float v2)
        {
            float mu = (IsoLevel - v1) / (v2 - v1);
            return p1 + mu * (p2 - p1);
        }

        private int GetIndex(int3 pos) => pos.x + pos.y * ChunkSize.x + pos.z * ChunkSize.x * ChunkSize.y;
    }
}