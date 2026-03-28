using Assets.Modules.GenerationModule;
using Assets.Modules.GenerationModule.Impl;
using UnityEngine;
using UnityEngine.InputSystem; // ОБЯЗАТЕЛЬНО добавляем библиотеку новой системы ввода

public class PlayerInteraction : MonoBehaviour
{
    private WorldManager worldManager;
    private TerrainModifier terrainModifier;

    public float brushRadius = 2f;
    public float digPower = -1f; // Отрицательное = копать
    public float buildPower = 1f;  // Положительное = строить
    private Camera _camera;

    void Start()
    {
        worldManager = FindObjectOfType<WorldManager>();

        // Защита от дурака: проверяем, нашли ли мы менеджера
        if (worldManager == null)
        {
            Debug.LogError("WorldManager не найден на сцене! Убедись, что он существует.");
            return;
        }
        _camera = gameObject.GetComponentInChildren<Camera>();
        terrainModifier = new TerrainModifier(worldManager);
    }

    void Update()
    {
        // Проверяем, подключена ли мышь (требование New Input System)
        if (Mouse.current == null) return;

        // Левая кнопка мыши - копать
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PerformRaycast(digPower);
        }

        // Правая кнопка мыши - строить
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            PerformRaycast(buildPower);
        }
    }

    private void PerformRaycast(float amount)
    {
        // Пускаем луч из центра экрана
        Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        // 20f - это дальность копания. Можно увеличить.
        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
        {
            terrainModifier.ModifyTerrain(hit.point, hit.normal, brushRadius, amount);
        }
    }
}