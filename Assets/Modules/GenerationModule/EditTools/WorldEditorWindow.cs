using UnityEditor;
using UnityEngine;
using Assets.Modules.GenerationModule.Models.WestMM;

public class WorldEditorWindow : EditorWindow
{
    public WorldProfile profile;
    private Texture2D mapPreview;
    private PreviewRenderUtility previewRender;
    private Mesh previewMesh;
    private Material previewMaterial;
    private Vector2 scrollPos;

    [MenuItem("Window/World Magic Editor")]
    public static void ShowWindow() => GetWindow<WorldEditorWindow>("Magic Map");

    void OnEnable()
    {
        previewRender = new PreviewRenderUtility();
        previewRender.camera.transform.position = new Vector3(16, 16, -32);
        previewRender.camera.transform.LookAt(new Vector3(16, 16, 16));
        previewRender.camera.farClipPlane = 1000;
        previewMaterial = new Material(Shader.Find("Standard"));
    }

    void OnDisable()
    {
        previewRender.Cleanup();
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.BeginVertical("box");
        profile = (WorldProfile)EditorGUILayout.ObjectField("World Profile", profile, typeof(WorldProfile), false);

        if (profile != null)
        {
            if (GUILayout.Button("GENERATE PREVIEW", GUILayout.Height(40)))
            {
                GenerateMap();
                // Тут можно вызвать генерацию 3D превью, если хочешь
            }

            if (mapPreview != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Temperature Map (2D):");
                Rect rect = GUILayoutUtility.GetAspectRect(1.0f);
                GUI.DrawTexture(rect, mapPreview);
            }

            GUILayout.Space(20);
            GUILayout.Label("Biome Settings:", EditorStyles.boldLabel);

            // Отрисовка стандартного инспектора профиля, чтобы были ползунки
            Editor editor = Editor.CreateEditor(profile);
            editor.OnInspectorGUI();
        }
        else
        {
            EditorGUILayout.HelpBox("Пожалуйста, выберите WorldProfile ассет!", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    void GenerateMap()
    {
        int res = 128;
        mapPreview = new Texture2D(res, res);
        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                // Имитация шума из шейдера
                float pX = x * profile.biomeMapScale;
                float pY = y * profile.biomeMapScale;
                float noise = Mathf.PerlinNoise(pX, pY); // Упрощенно

                mapPreview.SetPixel(x, y, GetBiomeColor(noise));
            }
        }
        mapPreview.Apply();
    }

    Color GetBiomeColor(float temp)
    {
        if (profile.biomes == null || profile.biomes.Length == 0) return Color.black;

        float bestDist = float.MaxValue;
        Color bestColor = Color.gray;

        foreach (var b in profile.biomes)
        {
            float dist = Mathf.Abs(temp - b.targetTemp);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestColor = b.biomeColor;
            }
        }
        return bestColor;
    }
}