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
        public int3 ChunkSize;
        public float IsoLevel; // Граница плотности (обычно 0f)

        // Выходные данные для Unity Mesh
        public NativeList<float3> Vertices;
        public NativeList<int> Triangles;

        public void Execute()
        {
            // Проходим по каждому вокселю (не доходя до края +1)
            for (int x = 0; x < ChunkSize.x - 1; x++)
            {
                for (int y = 0; y < ChunkSize.y - 1; y++)
                {
                    for (int z = 0; z < ChunkSize.z - 1; z++)
                    {
                        int3 pos = new int3(x, y, z);
                        ProcessCube(pos);
                    }
                }
            }
        }

        private void ProcessCube(int3 pos)
        {
            float[] cubeDensities = new float[8];
            int cubeIndex = 0;

            // 1. Узнаем плотность в 8 углах куба и формируем 8-битный индекс
            for (int i = 0; i < 8; i++)
            {
                int3 cornerPos = pos + MarchingCubesTables.Corners[i];
                cubeDensities[i] = Densities[GetIndex(cornerPos)];

                // ТЕПЕРЬ: если плотность БОЛЬШЕ IsoLevel (0), значит это земля (бит = 1)
                if (cubeDensities[i] > IsoLevel)
                {
                    cubeIndex |= (1 << i);
                }
            }

            // Если куб полностью внутри (0) или снаружи (255) - пропускаем
            if (cubeIndex == 0 || cubeIndex == 255) return;

            // 2. Вычисляем интерполированные позиции на 12 рёбрах куба
            float3[] edgeVertices = new float3[12];
            for (int i = 0; i < 12; i++)
            {
                // ЧИТАЕМ ИЗ int2
                int v1 = MarchingCubesTables.EdgeVertices[i].x;
                int v2 = MarchingCubesTables.EdgeVertices[i].y;

                edgeVertices[i] = Interpolate(
                    pos + MarchingCubesTables.Corners[v1], cubeDensities[v1],
                    pos + MarchingCubesTables.Corners[v2], cubeDensities[v2]
                );
            }

            // 3. Строим треугольники по таблице
            for (int i = 0; MarchingCubesTables.TriTable[cubeIndex * 16 + i] != -1; i += 3)
            {

                int a = MarchingCubesTables.TriTable[cubeIndex * 16 + i];
                int b = MarchingCubesTables.TriTable[cubeIndex * 16 + i + 1];
                int c = MarchingCubesTables.TriTable[cubeIndex * 16 + i + 2];

                Vertices.Add(edgeVertices[a]);
                Vertices.Add(edgeVertices[b]);
                Vertices.Add(edgeVertices[c]);

                Triangles.Add(Vertices.Length - 3);
                Triangles.Add(Vertices.Length - 2);
                Triangles.Add(Vertices.Length - 1);
            }
        }

        // Линейная интерполяция для гладкости
        private float3 Interpolate(float3 p1, float val1, float3 p2, float val2)
        {
            // Если плотности почти одинаковые, не делим, а берем середину
            if (math.abs(val1 - val2) < 0.00001f)
            {
                return (p1 + p2) * 0.5f;
            }

            // Если одна из точек точно на уровне IsoLevel
            if (math.abs(val1) < 0.00001f) return p1;
            if (math.abs(val2) < 0.00001f) return p2;

            float mu = (0f - val1) / (val2 - val1);
            return p1 + mu * (p2 - p1);
        }

        private int GetIndex(int3 pos) => pos.x + pos.y * ChunkSize.x + pos.z * ChunkSize.x * ChunkSize.y;
    }
}