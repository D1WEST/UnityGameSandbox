namespace Assets.Modules.GenerationModule
{
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;

    public class WorldManager : MonoBehaviour
    {
        public GameObject chunkPrefab;
        public int3 chunkSize = new int3(16, 16, 16);
        public int renderDistance = 2;

        private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();

        void Start()
        {
            GenerateWorld();
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
                        chunk.Initialize(chunkSize, chunkPos); 

                        chunks.Add(chunkPos, chunk);
                    }
                }
            }
        }

        // --- НОВЫЕ МЕТОДЫ ДЛЯ СИСТЕМЫ КОПАНИЯ ---

        // Возвращает позицию чанка по глобальной координате вокселя
        public int3 WorldToChunkPos(int3 globalVoxelPos)
        {
            int x = (int)math.floor((float)globalVoxelPos.x / chunkSize.x) * chunkSize.x;
            int y = (int)math.floor((float)globalVoxelPos.y / chunkSize.y) * chunkSize.y;
            int z = (int)math.floor((float)globalVoxelPos.z / chunkSize.z) * chunkSize.z;
            return new int3((int)x, (int)y, (int)z);
        }

        // Возвращает чанк, если он загружен
        public Chunk GetChunkAt(int3 globalVoxelPos)
        {
            int3 chunkPos = WorldToChunkPos(globalVoxelPos);
            if (chunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                return chunk;
            }

            return null;
        }
    }
}