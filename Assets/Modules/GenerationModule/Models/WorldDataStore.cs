namespace Assets.Modules.GenerationModule.Models
{
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Mathematics;

    public class WorldDataStore
    {
        // Храним только те чанки, которые игрок ПОТРОГАЛ (копал или строил)
        // Чтобы не забивать память всеми чанками мира
        private Dictionary<int3, float[]> modifiedChunks = new Dictionary<int3, float[]>();

        public bool HasData(int3 chunkPos) => modifiedChunks.ContainsKey(chunkPos);

        public void SaveChunk(int3 chunkPos, NativeArray<float> data)
        {
            if (!modifiedChunks.ContainsKey(chunkPos))
            {
                modifiedChunks[chunkPos] = new float[data.Length];
            }

            data.CopyTo(modifiedChunks[chunkPos]);
        }

        public void LoadChunk(int3 chunkPos, NativeArray<float> destination)
        {
            if (modifiedChunks.TryGetValue(chunkPos, out float[] savedData))
            {
                destination.CopyFrom(savedData);
            }
        }
    }
}
