using UnityEngine;
using System.Collections.Generic;

namespace Assets.Modules.GenerationModule.Models
{
    [CreateAssetMenu(fileName = "VoxelMaterialConfig", menuName = "Voxels/Material Config")]
    public class VoxelMaterialConfig : ScriptableObject
    {
        [System.Serializable]
        public class TextureEntry
        {
            public string Name;
            public Texture2D MainTex;
            public float Tiling = 0.5f;
        }

        public List<TextureEntry> Textures = new List<TextureEntry>();
        public Material TargetMaterial; // Ссылка на материал, который использует шейдер
    }
}