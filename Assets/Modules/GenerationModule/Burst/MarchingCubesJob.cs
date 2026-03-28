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

        /// <summary>
        /// Executes chunk processing of each cube.
        /// </summary>
        public void Execute()
        {
            // Проходим по кубам. Если данных 17, то кубов 16.
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

        /// <summary>
        /// Processes one cube's density.
        /// </summary>
        /// <param name="pos"></param>
        private void ProcessCube(int3 pos)
        {
            // Вместо unsafe используем обычный массив. 
            // В Burst короткие массивы фиксированной длины не создают аллокаций в куче.
            float[] cubeDensities = new float[8];
            int cubeIndex = 0;

            // 1. Собираем данные из 8 углов
            for (int i = 0; i < 8; i++)
            {
                float d = Densities[GetIndex(pos + MarchingCubesTables.Corners[i])];
                cubeDensities[i] = d;

                if (d > IsoLevel)
                {
                    cubeIndex |= (1 << i);
                }
            }

            // Если куб пуст или полностью заполнен — выходим
            if (cubeIndex == 0 || cubeIndex == 255) return;

            // 2. Генерируем вершины на ребрах
            float3[] edgeVertices = new float3[12];

            // Получаем маску ребер для этого индекса куба (какие ребра пересекаются поверхностью)
            // В некоторых таблицах есть EdgeTable[256], если нет — просто считаем все 12.
            for (int i = 0; i < 12; i++)
            {
                int2 edge = MarchingCubesTables.EdgeVertices[i];

                float3 p1 = pos + MarchingCubesTables.Corners[edge.x];
                float3 p2 = pos + MarchingCubesTables.Corners[edge.y];

                float v1 = cubeDensities[edge.x];
                float v2 = cubeDensities[edge.y];

                edgeVertices[i] = Interpolate(p1, v1, p2, v2);
            }

            // 3. Формируем треугольники
            int tableOffset = cubeIndex * 16;
            for (int i = 0; i < 16; i += 3)
            {
                int a = MarchingCubesTables.TriTable[tableOffset + i];
                if (a == -1) break; // Конец треугольников для этого куба

                int b = MarchingCubesTables.TriTable[tableOffset + i + 1];
                int c = MarchingCubesTables.TriTable[tableOffset + i + 2];

                // Добавляем вершины
                Vertices.Add(edgeVertices[a]);
                Vertices.Add(edgeVertices[b]);
                Vertices.Add(edgeVertices[c]);

                // Добавляем индексы (порядок a, b, c)
                int vCount = Vertices.Length;
                Triangles.Add(vCount - 3);
                Triangles.Add(vCount - 2);
                Triangles.Add(vCount - 1);
            }
        }

        // Линейная интерполяция для гладкости
        private float3 Interpolate(float3 p1, float val1, float3 p2, float val2)
        {
            // 1. Проверка на экстремальную близость к изо-уровню
            if (math.abs(IsoLevel - val1) < 0.00001f) return p1;
            if (math.abs(IsoLevel - val2) < 0.00001f) return p2;
            if (math.abs(val1 - val2) < 0.00001f) return p1;

            // 2. Вычисляем коэффициент смещения (mu)
            // Формула: (Нужный_Уровень - Значение_В_Точке1) / (Значение_В_Точке2 - Значение_В_Точке1)
            float mu = (IsoLevel - val1) / (val2 - val1);

            // 3. Линейно смещаем позицию от p1 к p2
            return p1 + mu * (p2 - p1);
        }

        private int GetIndex(int3 pos) => pos.x + pos.y * ChunkSize.x + pos.z * ChunkSize.x * ChunkSize.y;
    }
}