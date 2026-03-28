namespace Assets.Modules.GenerationModule
{
    using Assets.Modules.GenerationModule.Models;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

    public class WorldManager : MonoBehaviour
    {
        public GameObject chunkPrefab;
        public Transform player;

        public int3 chunkSize = new int3(16, 16, 16);
        public int renderDistance = 6;
        public int chunksPerFrame = 2;

        private WorldDataStore dataStore = new WorldDataStore();

        // ВОТ НАШИ НАСТРОЙКИ С ИДЕАЛЬНЫМИ БАЗОВЫМИ ЗНАЧЕНИЯМИ
        public TerrainSettings terrainSettings = new TerrainSettings
        {
            minChunkY = -6,
            maxChunkY = 4,
            seed = 1337f,
            biomeScale = 0.001f,
            oceanHeight = 15f,
            plainsHeight = 35f,
            mountainHeight = 70f,
            detailScale = 0.04f,
            detailAmplitude = 2f, // Не делай больше 10, иначе опять будут рваные куски!
            caveScale = 0.015f,
            caveThreshold = 0.05f
        };

        private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();

        // Хранилище всех активных чанков
        private Dictionary<int3, Chunk> activeChunks = new Dictionary<int3, Chunk>();
        // Список позиций, которые нужно создать
        private List<int3> chunkCreationQueue = new List<int3>();

        void Update()
        {
            if (player == null) return;

            // 1. Определяем, в каком чанке сейчас стоит игрок
            int3 currentPlayerChunk = GetChunkCoords(player.position);

            // 2. Ищем чанки вокруг игрока, которые нужно создать
            UpdateChunkQueue(currentPlayerChunk);

            // 3. Создаем чанки из очереди (порциями для плавности)
            ProcessQueue();

            // 4. Удаляем слишком далекие чанки (опционально, для экономии памяти)
            UnloadFarChunks(currentPlayerChunk);
        }

        void GenerateWorld()
        {
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        int3 chunkPos = new int3(x * chunkSize.x, y * chunkSize.y, z * chunkSize.z);

                        GameObject chunkObj = Instantiate(chunkPrefab, new Vector3(chunkPos.x, chunkPos.y, chunkPos.z),
                            Quaternion.identity);
                        chunkObj.name = $"Chunk {chunkPos}";

                        Chunk chunk = chunkObj.GetComponent<Chunk>();
                        chunk.Initialize(chunkSize, chunkPos, terrainSettings, this);

                        chunks.Add(chunkPos, chunk);
                    }
                }
            }
        }

        int3 GetChunkCoords(Vector3 worldPos)
        {
            return new int3(
                Mathf.FloorToInt(worldPos.x / chunkSize.x) * chunkSize.x,
                Mathf.FloorToInt(worldPos.y / chunkSize.y) * chunkSize.y,
                Mathf.FloorToInt(worldPos.z / chunkSize.z) * chunkSize.z
            );
        }
        void UpdateChunkQueue(int3 playerChunk)
        {
            chunkCreationQueue.Clear();

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    // Используем настройки из конфига!
                    for (int y = terrainSettings.minChunkY; y <= terrainSettings.maxChunkY; y++)
                    {
                        int3 pos = new int3(
                            playerChunk.x + x * chunkSize.x,
                            y * chunkSize.y,
                            playerChunk.z + z * chunkSize.z
                        );

                        if (!activeChunks.ContainsKey(pos))
                        {
                            chunkCreationQueue.Add(pos);
                        }
                    }
                }
            }
            // Сортируем очередь: сначала те, что ближе к игроку (для красоты)
            chunkCreationQueue.Sort((a, b) =>
                (int)math.distancesq(playerChunk, a) - (int)math.distancesq(playerChunk, b));
        }

        void ProcessQueue()
        {
            int spawnedThisFrame = 0;

            while (spawnedThisFrame < chunksPerFrame && chunkCreationQueue.Count > 0)
            {
                int3 pos = chunkCreationQueue[0];
                chunkCreationQueue.RemoveAt(0);

                if (!activeChunks.ContainsKey(pos))
                {
                    SpawnChunk(pos);
                    spawnedThisFrame++;
                }
            }
        }

        void SpawnChunk(int3 pos)
        {
            GameObject obj = Instantiate(chunkPrefab, new Vector3(pos.x, pos.y, pos.z), Quaternion.identity, transform);
            obj.name = $"Chunk_{pos.x}_{pos.y}_{pos.z}";

            Chunk chunk = obj.GetComponent<Chunk>();
            chunk.Initialize(chunkSize, pos, terrainSettings, this);

            activeChunks.Add(pos, chunk);
        }

        void UnloadFarChunks(int3 playerChunk)
        {
            List<int3> toRemove = new List<int3>();
            float unloadDist = (renderDistance + 1) * chunkSize.x;

            foreach (var pair in activeChunks)
            {
                if (math.distance(playerChunk, pair.Key) > unloadDist)
                {
                    toRemove.Add(pair.Key);
                }
            }

            foreach (int3 pos in toRemove)
            {
                Destroy(activeChunks[pos].gameObject);
                activeChunks.Remove(pos);
            }
        }

        // Вспомогательные методы для копания (TerrainModifier)
        public Chunk GetChunkAt(int3 globalVoxelPos)
        {
            int3 chunkPos = WorldToChunkPos(globalVoxelPos);
            return activeChunks.TryGetValue(chunkPos, out Chunk chunk) ? chunk : null;
        }

        // --- НОВЫЕ МЕТОДЫ ДЛЯ СИСТЕМЫ КОПАНИЯ ---

        // Возвращает позицию чанка по глобальной координате вокселя
        public int3 WorldToChunkPos(int3 globalVoxelPos)
        {
            return new int3(
                Mathf.FloorToInt((float)globalVoxelPos.x / chunkSize.x) * chunkSize.x,
                Mathf.FloorToInt((float)globalVoxelPos.y / chunkSize.y) * chunkSize.y,
                Mathf.FloorToInt((float)globalVoxelPos.z / chunkSize.z) * chunkSize.z
            );
        }

        public void SaveChunkState(int3 chunkPos, NativeArray<float> densities)
        {
            dataStore.SaveChunk(chunkPos, densities);
        }

        public bool TryLoadChunkState(int3 chunkPos, NativeArray<float> densities)
        {
            if (dataStore.HasData(chunkPos))
            {
                dataStore.LoadChunk(chunkPos, densities);
                return true;
            }
            return false;
        }

    }
}