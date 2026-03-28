using Assets.Modules.GenerationModule.Abstractions;
using Unity.Collections;
using Unity.Mathematics;

namespace Assets.Modules.GenerationModule.Models
{
    public class ChunkData : IVoxelData
    {
        public int3 Size { get; private set; }
        private NativeArray<float> densities;

        // Для 16х16х16 делаем размер 17х17х17 для сглаживания швов!
        public ChunkData(int3 size)
        {
            Size = size + new int3(1, 1, 1);
            densities = new NativeArray<float>(Size.x * Size.y * Size.z, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (densities.IsCreated) densities.Dispose();
        }

        public float GetDensity(int3 pos)
        {
            if (pos.x < 0 || pos.x >= Size.x || pos.y < 0 || pos.y >= Size.y || pos.z < 0 || pos.z >= Size.z)
                return -1f; // Воздух за пределами
            return densities[GetIndex(pos)];
        }

        public void SetDensity(int3 pos, float value)
        {
            if (pos.x >= 0 && pos.x < Size.x && pos.y >= 0 && pos.y < Size.y && pos.z >= 0 && pos.z < Size.z)
                densities[GetIndex(pos)] = value;
        }

        private int GetIndex(int3 pos) => pos.x + pos.y * Size.x + pos.z * Size.x * Size.y;

        public NativeArray<float> GetNativeArray() => densities;
    }
}
