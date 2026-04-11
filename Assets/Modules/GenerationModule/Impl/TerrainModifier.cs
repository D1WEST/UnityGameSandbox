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

        public void ModifyTerrain(Vector3 worldPoint, Vector3 normal, float radius, float amount)
        {
            float offsetDir = amount < 0 ? -0.5f : 0.5f;
            Vector3 targetCenter = worldPoint + (normal * offsetDir);

            int3 centerInt = new int3(
                Mathf.RoundToInt(targetCenter.x),
                Mathf.RoundToInt(targetCenter.y),
                Mathf.RoundToInt(targetCenter.z)
            );

            int r = Mathf.CeilToInt(radius);
            // ОПТИМИЗАЦИЯ 1: Используем квадрат радиуса, чтобы не вычислять квадратный корень (math.distance) для каждого вокселя
            float radiusSq = radius * radius;

            // ОПТИМИЗАЦИЯ 2: Находим глобальные границы нашей "кисти" (AABB)
            int3 minPos = centerInt - new int3(r, r, r);
            int3 maxPos = centerInt + new int3(r, r, r);

            // Переводим эти границы в координаты чанков
            int3 chunkSize = worldManager.chunkSize;
            int3 minChunk = worldManager.WorldToChunkPos(minPos);
            int3 maxChunk = worldManager.WorldToChunkPos(maxPos);

            List<Chunk> chunksToUpdate = new List<Chunk>();

            // ОПТИМИЗАЦИЯ 3: Проходимся ТОЛЬКО по тем чанкам, которые зацепила кисть (их максимум 1-8 штук)
            for (int cx = minChunk.x; cx <= maxChunk.x; cx += chunkSize.x)
            {
                for (int cy = minChunk.y; cy <= maxChunk.y; cy += chunkSize.y)
                {
                    for (int cz = minChunk.z; cz <= maxChunk.z; cz += chunkSize.z)
                    {
                        int3 chunkWorldPos = new int3(cx, cy, cz);
                        Chunk chunk = worldManager.GetChunkAt(chunkWorldPos);

                        if (chunk != null)
                        {
                            bool isModified = false;
                            ChunkData data = chunk.GetVoxelData();

                            // ОПТИМИЗАЦИЯ 4: Локальные границы. 
                            // Находим, какую часть массива этого чанка нам нужно проверить.
                            // Размер массива 17x17x17, поэтому границы от 0 до 16.
                            int3 startLocal = math.max(new int3(0, 0, 0), minPos - chunkWorldPos);
                            int3 endLocal = math.min(data.Size - new int3(1, 1, 1), maxPos - chunkWorldPos);

                            // Цикл работает только в зоне кисти внутри конкретного чанка
                            for (int lx = startLocal.x; lx <= endLocal.x; lx++)
                            {
                                for (int ly = startLocal.y; ly <= endLocal.y; ly++)
                                {
                                    for (int lz = startLocal.z; lz <= endLocal.z; lz++)
                                    {
                                        int3 localPos = new int3(lx, ly, lz);
                                        int3 globalPos = chunkWorldPos + localPos;

                                        // Проверяем, попадает ли воксель в сферу
                                        if (math.distancesq(centerInt, globalPos) <= radiusSq)
                                        {
                                            float currentDensity = data.GetDensity(localPos);
                                            float newDensity = math.clamp(currentDensity + amount, -1f, 1f);

                                            // Если плотность реально изменилась (не пытаемся копать воздух)
                                            if (math.abs(currentDensity - newDensity) > 0.001f)
                                            {
                                                data.SetDensity(localPos, newDensity);
                                                isModified = true;
                                            }
                                        }
                                    }
                                }
                            }

                            // ОПТИМИЗАЦИЯ 5: Сохраняем состояние чанка ОДИН раз, если хоть что-то изменилось
                            if (isModified)
                            {
                                worldManager.SaveChunkState(chunkWorldPos, data.GetNativeArray());
                                chunksToUpdate.Add(chunk);
                            }
                        }
                    }
                }
            }

            // Запускаем перестроение мешей
            foreach (Chunk chunk in chunksToUpdate)
            {
                chunk.UpdateMesh();
            }
        }
    }
}