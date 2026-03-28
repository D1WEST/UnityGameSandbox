namespace Assets.Modules.GenerationModule.Abstractions
{
    using UnityEngine;
    /// <summary>
    /// Can interact via terrain if connected.
    /// </summary>
    public interface ITerrainInteractor
    {
        void ModifyTerrain(Vector3 worldPoint, Vector3 normal, float radius, float amount);
    }
}
