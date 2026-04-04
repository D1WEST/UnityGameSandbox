using UnityEngine;

namespace Assets.Modules.GenerationModule.Models.WestMM
{
    [CreateAssetMenu(fileName = "WorldProfile", menuName = "Voxels/World Profile")]
    public class WorldProfile : ScriptableObject
    {
        public float biomeMapScale = 0.01f;
        public BiomeData[] biomes;
    }
}
