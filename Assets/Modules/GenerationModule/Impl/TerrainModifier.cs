using Assets.Modules.GenerationModule.Abstractions;
using Assets.Modules.GenerationModule.Models;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Modules.GenerationModule.Impl
{
    public class TerrainModifier : ITerrainInteractor
    {
        private WorldManager worldManager;

        public TerrainModifier(WorldManager manager)
        {
            worldManager = manager;
        }

        // worldPoint - точка попадания Raycast
        // normal - нормаль из RaycastHit
        // radius - радиус кисти (например, 2)
        // amount - сила (отрицательная для копания: -1f; положительная для стройки: 1f)
        public void ModifyTerrain(Vector3 worldPoint, Vector3 normal, float radius, float amount)
        {
            // СЕКРЕТ №1: Избавляемся от пустого клика. 
            // Сдвигаем точку немного ВНУТРЬ меша по нормали. 
            // Если строим (amount > 0), сдвигаем НАРУЖУ (прибавляем нормаль).
            float offsetDir = amount < 0 ? -0.5f : 0.5f;
            Vector3 targetCenter = worldPoint + (normal * offsetDir);

            // Переводим в целочисленные глобальные воксельные координаты
            int3 centerInt = new int3(
                Mathf.RoundToInt(targetCenter.x),
                Mathf.RoundToInt(targetCenter.y),
                Mathf.RoundToInt(targetCenter.z)
            );

            int r = Mathf.CeilToInt(radius);

            // Храним чанки, которые мы задели, чтобы обновить их меши в конце
            HashSet<Chunk> chunksToUpdate = new HashSet<Chunk>();

            // Проходимся кубиком (или сферой) вокруг точки клика
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    for (int z = -r; z <= r; z++)
                    {
                        int3 globalPos = centerInt + new int3(x, y, z);

                        // Делаем форму кисти сферической
                        if (math.distance(centerInt, globalPos) <= radius)
                        {

                            // СЕКРЕТ №2: Решение проблемы "дырок" между чанками.
                            // Так как у нас размер данных 17x17x17 (для чанка 16x16x16), 
                            // один физический глобальный воксель может лежать на стыке 
                            // и принадлежать СРАЗУ нескольким чанкам. 
                            // Поэтому мы берем радиус захвата +1 чанк и проверяем соседей.

                            ModifyVoxelInAllOverlappingChunks(globalPos, amount, chunksToUpdate);
                        }
                    }
                }
            }

            // Запускаем перестроение мешей только у задетых чанков
            foreach (Chunk chunk in chunksToUpdate)
            {
                chunk.UpdateMesh();
            }
        }

        private void ModifyVoxelInAllOverlappingChunks(int3 globalPos, float amount, HashSet<Chunk> chunksToUpdate)
        {
            // Проходим соседей (радиус 1 вокруг точки), чтобы зацепить края 17х17х17
            for (int ox = -1; ox <= 0; ox++)
            {
                for (int oy = -1; oy <= 0; oy++)
                {
                    for (int oz = -1; oz <= 0; oz++)
                    {
                        int3 checkPos = globalPos + new int3(ox * 16, oy * 16, oz * 16);
                        Chunk chunk = worldManager.GetChunkAt(checkPos);

                        if (chunk != null)
                        {
                            int3 chunkWorldPos = worldManager.WorldToChunkPos(checkPos);
                            int3 localPos = globalPos - chunkWorldPos;

                            ChunkData data = chunk.GetVoxelData();
                            if (localPos.x >= 0 && localPos.x < data.Size.x &&
                                localPos.y >= 0 && localPos.y < data.Size.y &&
                                localPos.z >= 0 && localPos.z < data.Size.z)
                            {
                                float currentDensity = data.GetDensity(localPos);
                                float newDensity = math.clamp(currentDensity + amount, -1f, 1f);
                                data.SetDensity(localPos, newDensity);
                                // !!! ВАЖНО: Сообщаем менеджеру, что данные чанка изменились
                                worldManager.SaveChunkState(chunkWorldPos, data.GetNativeArray());

                                chunksToUpdate.Add(chunk);
                            }
                        }
                    }
                }
            }
        }
    }
}
