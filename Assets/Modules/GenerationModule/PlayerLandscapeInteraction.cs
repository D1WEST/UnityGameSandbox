namespace Assets.Modules.GenerationModule
{
    using Assets.Modules.GenerationModule.Impl;
    using UnityEngine;
    using UnityEngine.InputSystem; // ОБЯЗАТЕЛЬНО добавляем библиотеку новой системы ввода

    public class PlayerLandscapeInteraction : MonoBehaviour
    {
        private WorldManager worldManager;
        private TerrainModifier terrainModifier;

        [SerializeField] private PlayerInputActions _playerInputActions;

        public float brushRadius = 0.5f;
        public float digPower = -2f; // Отрицательное = копать
        public float buildPower = 2f; // Положительное = строить
        private Camera _camera;

        private void OnEnable()
        {
            if (_playerInputActions == null)
            {
                _playerInputActions = new();
            }
            _playerInputActions.PlayerInterractionActions.Enable();
            _playerInputActions.PlayerInterractionActions.MainAction.started += DigLandscape;
            _playerInputActions.PlayerInterractionActions.SecondAction.started += BuildLandscape;
        }

        /// <summary>
        /// On input interaction disabled.
        /// </summary>
        private void OnDisable()
        {
            _playerInputActions.PlayerInterractionActions.Disable();
            _playerInputActions.PlayerInterractionActions.MainAction.started -= DigLandscape;
            _playerInputActions.PlayerInterractionActions.SecondAction.started -= BuildLandscape;
        }

        void Start()
        {
            worldManager = FindObjectOfType<WorldManager>();

            // Защита от дурака: проверяем, нашли ли мы менеджера
            if (worldManager == null)
            {
                Debug.LogError("WorldManager не найден на сцене! Убедись, что он существует.");
                return;
            }

            _camera = GameObject.FindWithTag("PlayerCamera").GetComponent<Camera>();
            terrainModifier = new TerrainModifier(worldManager);
        }

        private void BuildLandscape(InputAction.CallbackContext obj)
        {
            PerformRaycast(buildPower);
        }

        private void DigLandscape(InputAction.CallbackContext obj)
        {
            PerformRaycast(digPower);
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
}