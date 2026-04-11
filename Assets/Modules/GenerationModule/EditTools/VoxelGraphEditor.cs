namespace Assets.Modules.GenerationModule.EditTools
{
    using Assets.Modules.GenerationModule.EditTools.NodeSystem;
    using Assets.Modules.GenerationModule.Impl;
    using Assets.Modules.GenerationModule.Models;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.Experimental.GraphView;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class VoxelGraphEditor : EditorWindow
    {
        private WorldGraphView _graphView;
        private VoxelGraphData _currentAsset;
        private ObjectField _assetSelector;

        // --- PREVIEW VARIABLES ---
        private PreviewRenderUtility _previewRenderUtility;
        private Mesh _previewMesh;
        private Material _previewMaterial;
        private Texture2D _previewTexture2D;
        private Vector2 _previewDragRotation = new Vector2(30, -45);
        private int _previewRenderDistance = 6;
        private Image _2dPreviewImage;
        private IMGUIContainer _3dPreviewContainer;
        private Vector3Int _previewChunkPos = new Vector3Int(0, 1, 0);

        [MenuItem("Window/Voxel Magic Editor")]
        public static void Open() => GetWindow<VoxelGraphEditor>("Voxel Graph");

        private void OnEnable()
        {
            _graphView = new WorldGraphView(this);
            rootVisualElement.Add(_graphView);
            GenerateToolbar();
            SetupPreviewPanel(); // Создаем панель превью
        }

        private void OnDisable()
        {
            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }
            if (_previewMesh != null) DestroyImmediate(_previewMesh);
            if (_previewTexture2D != null) DestroyImmediate(_previewTexture2D);
            if (_previewMaterial != null) DestroyImmediate(_previewMaterial);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();
            _assetSelector = new ObjectField("Graph Asset") { objectType = typeof(VoxelGraphData) };
            _assetSelector.RegisterValueChangedCallback(evt => {
                _currentAsset = (VoxelGraphData)evt.newValue;
                if (_currentAsset != null) Load();
            });
            toolbar.Add(_assetSelector);
            toolbar.Add(new Button(Save) { text = "Save Graph" });
            toolbar.Add(new Button(Compile) { text = "COMPILE SHADER", style = { backgroundColor = new Color(0.2f, 0.5f, 0.2f), color = Color.white } });

            // Кнопка обновления превью
            toolbar.Add(new Button(UpdatePreviews) { text = "UPDATE PREVIEWS", style = { backgroundColor = new Color(0.2f, 0.4f, 0.6f), color = Color.white } });

            rootVisualElement.Add(toolbar);
        }

        private void SetupPreviewPanel()
        {
            var previewPanel = new VisualElement();
            previewPanel.style.position = Position.Absolute;
            previewPanel.style.right = 10;
            previewPanel.style.bottom = 10;
            previewPanel.style.width = 260;
            previewPanel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            previewPanel.style.borderTopWidth = previewPanel.style.borderBottomWidth = previewPanel.style.borderLeftWidth = previewPanel.style.borderRightWidth = 2;
            var bColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
            previewPanel.style.borderTopColor = bColor;
            previewPanel.style.borderBottomColor = bColor;
            previewPanel.style.borderLeftColor = bColor;
            previewPanel.style.borderRightColor = bColor;
            previewPanel.style.paddingTop = previewPanel.style.paddingBottom = previewPanel.style.paddingLeft = previewPanel.style.paddingRight = 5;

            var slider = new SliderInt("2D Render Dist", 2, 16);
            slider.value = _previewRenderDistance;
            slider.RegisterValueChangedCallback(evt => { _previewRenderDistance = evt.newValue; Update2DMap(); });
            previewPanel.Add(slider);

            // НОВОЕ: Поле для выбора чанка
            var chunkPosField = new Vector3IntField("3D Chunk Pos");
            chunkPosField.value = _previewChunkPos;
            chunkPosField.RegisterValueChangedCallback(evt => { _previewChunkPos = evt.newValue; Update3DChunk(); });
            previewPanel.Add(chunkPosField);

            previewPanel.Add(new Label("2D Biome Map:") { style = { marginTop = 5, unityFontStyleAndWeight = FontStyle.Bold } });
            _2dPreviewImage = new Image();
            _2dPreviewImage.style.width = 250;
            _2dPreviewImage.style.height = 250;
            _2dPreviewImage.style.backgroundColor = Color.black;
            previewPanel.Add(_2dPreviewImage);

            previewPanel.Add(new Label("3D Chunk Preview:") { style = { marginTop = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            _3dPreviewContainer = new IMGUIContainer(Draw3DPreview);
            _3dPreviewContainer.style.width = 250;
            _3dPreviewContainer.style.height = 250;
            _3dPreviewContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            previewPanel.Add(_3dPreviewContainer);

            rootVisualElement.Add(previewPanel);

            _previewRenderUtility = new PreviewRenderUtility();
            _previewRenderUtility.camera.fieldOfView = 45f;
            _previewRenderUtility.camera.nearClipPlane = 0.1f;
            _previewRenderUtility.camera.farClipPlane = 1000f;
            _previewRenderUtility.ambientColor = new Color(0.5f, 0.5f, 0.5f);
            _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50, 50, 0);

            // Используем стандартный шейдер (он отлично рисует Vertex Colors)
            _previewMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        private void UpdatePreviews()
        {
            if (_currentAsset == null) return;

            // Обязательно сохраняем, чтобы в ассете были свежие BakedBiomes и скейл
            Compile();

            Update2DMap();
            Update3DChunk();
        }

        private void Update2DMap()
        {
            int texSize = 128;
            if (_previewTexture2D == null)
            {
                _previewTexture2D = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                _previewTexture2D.filterMode = FilterMode.Point;
                _2dPreviewImage.image = _previewTexture2D;
            }

            float scale = _currentAsset.selectorScale;
            float worldSize = _previewRenderDistance * 32f;
            float step = worldSize / texSize;

            // ИСПРАВЛЕНИЕ: Берем ноды биомов прямо из открытого графа!
            var biomeNodes = _graphView.nodes.ToList().OfType<BiomeNode>().ToList();

            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    float wx = (x - texSize / 2f) * step;
                    float wy = (y - texSize / 2f) * step;

                    float temp = math.saturate((noise.cnoise(new float3(wx, 0, wy) * scale) + 1.0f) / 2.0f);

                    Color finalColor = Color.black;
                    float totalW = 0.0001f;
                    float r = 0, g = 0, b = 0;

                    if (biomeNodes.Count > 0)
                    {
                        foreach (var biome in biomeNodes)
                        {
                            float dist = Mathf.Abs(temp - biome.TargetTemp);
                            float w = Mathf.Pow(Mathf.Clamp01(1.0f - dist * 5.0f), 2.0f);
                            r += biome.TexIndexR * w;
                            g += biome.TexIndexG * w;
                            b += biome.TexIndexB * w;
                            totalW += w;
                        }
                        finalColor = new Color(r / totalW, g / totalW, b / totalW, 1f);
                    }

                    _previewTexture2D.SetPixel(x, y, finalColor);
                }
            }
            _previewTexture2D.Apply();
        }

        private void Update3DChunk()
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Modules/GenerationModule/Shaders/MarchingCubes.compute");
            if (shader == null) return;

            // ИСПРАВЛЕНИЕ: Находим конфиг материалов для превью
            var config = AssetDatabase.FindAssets("t:VoxelMaterialConfig")
                .Select(guid => AssetDatabase.LoadAssetAtPath<VoxelMaterialConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();

            if (config == null) return;

            // Теперь передаем и шейдер, и конфиг
            var gpuGen = new GPUChunkGenerator(shader, config);

            var settings = new TerrainSettings { seed = 1337f, hubScale = 0.03f, hubThreshold = 0.4f, branchScale = 0.01f, branchThreshold = 0.025f };

            int3 worldPos = new int3(_previewChunkPos.x * 32, _previewChunkPos.y * 32, _previewChunkPos.z * 32);
            var mesh = gpuGen.GenerateChunkSync(new int3(32, 32, 32), worldPos, _currentAsset, settings);

            if (_previewMesh != null) DestroyImmediate(_previewMesh);
            _previewMesh = mesh;

            if (_previewMesh != null) _previewMesh.RecalculateBounds();

            gpuGen.Dispose();

            if (_3dPreviewContainer != null) _3dPreviewContainer.MarkDirtyRepaint();
        }

        private void Draw3DPreview()
        {
            Rect rect = _3dPreviewContainer.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            Event e = Event.current;
            if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition) && e.button == 0)
            {
                _previewDragRotation.x += e.delta.y * 1.5f;
                _previewDragRotation.y += e.delta.x * 1.5f;
                e.Use();
            }

            _previewRenderUtility.BeginPreview(rect, GUIStyle.none);

            if (_previewMesh != null && _previewMesh.vertexCount > 0)
            {
                Vector3 chunkCenter = new Vector3(16, 16, 16);
                Quaternion camRot = Quaternion.Euler(_previewDragRotation.x, _previewDragRotation.y, 0);
                Vector3 camDir = camRot * new Vector3(0, 0, -60);
                _previewRenderUtility.camera.transform.position = chunkCenter + camDir;
                _previewRenderUtility.camera.transform.LookAt(chunkCenter);

                _previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, _previewMaterial, 0);
            }

            _previewRenderUtility.camera.Render();
            Texture renderResult = _previewRenderUtility.EndPreview();
            GUI.DrawTexture(rect, renderResult);

            // НОВОЕ: Если меш пустой (как у тебя сейчас), пишем подсказку!
            if (_previewMesh == null || _previewMesh.vertexCount == 0)
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.normal.textColor = Color.yellow;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(rect, "Chunk is empty\n(Underground or in the sky?)", labelStyle);
            }
        }

        // --- ДАЛЬШЕ ИДЕТ СТАРЫЙ КОД СОХРАНЕНИЯ, КОМПИЛЯЦИИ И ЗАГРУЗКИ ---

        private void Compile()
        {
            var outputNode = _graphView.nodes.ToList().OfType<OutputNode>().FirstOrDefault();
            if (outputNode == null || _currentAsset == null) return;

            var config = AssetDatabase.FindAssets("t:VoxelMaterialConfig")
                .Select(guid => AssetDatabase.LoadAssetAtPath<VoxelMaterialConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();
            if (config == null) return;

            ShaderWriter.UpdateShaders(config);

            int varCount = 0;
            var cache = new Dictionary<VoxelNode, string>();
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            // 1. КОД ДЛЯ COMPUTE SHADER (Только форма земли)
            string shapeCode = "";
            string densityVar = "0.0";
            var firstBiome = outputNode.BiomePorts.FirstOrDefault(p => p.connected)?.connections.First().output.node as BiomeNode;
            if (firstBiome != null) shapeCode = firstBiome.GetInputHLSL(firstBiome.DensityInput, ref varCount, out densityVar, cache);

            string computeDensityHLSL = $@"{shapeCode}
    density = {densityVar}; 
    density = clamp(density, -50.0, 50.0);";

            // 2. КОД ДЛЯ VISUAL SHADER (Пиксельная четкость текстур)
            varCount = 0; // Сбрасываем счетчик для чистого кода в шейдере
            cache.Clear();

            string selectorHLSL = outputNode.GetInputHLSL(outputNode.SelectorInput, ref varCount, out string rawSel, cache);
            string selVar = $"nSel_{varCount++}";
            string fragLogic = $"{selectorHLSL}\n    float {selVar} = saturate({rawSel} * 0.5 + 0.5);\n";

            foreach (var port in outputNode.BiomePorts)
            {
                var conn = port.connections.FirstOrDefault();
                if (conn == null) continue;
                var bNode = conn.output.node as BiomeNode;

                string wCode = bNode.GetInputHLSL(bNode.ColorInput, ref varCount, out string wMask, cache);
                string tStr = bNode.TargetTemp.ToString("F3", culture);
                string bWeight = $"bw_{varCount++}";

                fragLogic += $@"
    {{
        {wCode}
        float {bWeight} = pow(saturate(1.0 - abs({selVar} - {tStr}) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex{bNode.TexIndexR}, worldPos, normal, _Scale{bNode.TexIndexR}) * {wMask}.r +
                    GetTriplanar(_Tex{bNode.TexIndexG}, worldPos, normal, _Scale{bNode.TexIndexG}) * {wMask}.g +
                    GetTriplanar(_Tex{bNode.TexIndexB}, worldPos, normal, _Scale{bNode.TexIndexB}) * {wMask}.b +
                    GetTriplanar(_Tex{bNode.TexIndexA}, worldPos, normal, _Scale{bNode.TexIndexA}) * {wMask}.a);
        finalColor += c * {bWeight};
        totalW += {bWeight};
    }}";
            }
            fragLogic += "\n    finalColor /= totalW;";

            // ЗАПИСЬ В ФАЙЛЫ
            string computePath = "Assets/Modules/GenerationModule/Shaders/MarchingCubes.compute";
            string visualPath = "Assets/Modules/GenerationModule/Shaders/VoxelTriplanar.shader";

            File.WriteAllText(computePath, ReplaceTag(File.ReadAllText(computePath), "// [NODE_GRAPH_START]", "// [NODE_GRAPH_END]", computeDensityHLSL), new UTF8Encoding(false));
            File.WriteAllText(visualPath, ReplaceTag(File.ReadAllText(visualPath), "// [GENERATED_BIOME_LOGIC_START]", "// [GENERATED_BIOME_LOGIC_END]", fragLogic), new UTF8Encoding(false));

            Save();
            AssetDatabase.ImportAsset(computePath);
            AssetDatabase.ImportAsset(visualPath);
            Debug.Log("<color=cyan>Voxel Graph: Пиксельная отрисовка включена!</color>");
        }

        private string ReplaceTag(string text, string start, string end, string newText)
        {
            int s = text.IndexOf(start);
            int e = text.IndexOf(end);
            if (s == -1 || e == -1) return text;
            return text.Substring(0, s + start.Length) + "\n" + newText + "\n    " + text.Substring(e);
        }

        private void Save()
        {
            if (_currentAsset == null) return;
            _currentAsset.Nodes.Clear();
            _currentAsset.Edges.Clear();
            foreach (var node in _graphView.nodes.ToList().Cast<VoxelNode>())
            {
                var nData = new NodeSerializedData
                {
                    GUID = node.GUID,
                    Type = node.GetType().AssemblyQualifiedName,
                    Position = node.GetPosition().position,
                    Data = SerializeNodeData(node),
                    PortCount = node is OutputNode o ? o.BiomePorts.Count : 0
                };
                _currentAsset.Nodes.Add(nData);
            }
            foreach (var edge in _graphView.edges.ToList())
            {
                _currentAsset.Edges.Add(new EdgeSerializedData
                {
                    OutputNodeGUID = (edge.output.node as VoxelNode).GUID,
                    InputNodeGUID = (edge.input.node as VoxelNode).GUID,
                    OutputPortName = edge.output.portName,
                    InputPortName = edge.input.portName
                });
            }
            EditorUtility.SetDirty(_currentAsset);
            AssetDatabase.SaveAssets();
        }

        private void Load()
        {
            _graphView.graphElements.ForEach(e => _graphView.RemoveElement(e));
            if (_currentAsset == null) return;
            Dictionary<string, VoxelNode> nodeCache = new Dictionary<string, VoxelNode>();
            foreach (var nData in _currentAsset.Nodes)
            {
                var type = System.Type.GetType(nData.Type);
                if (type == null) continue;
                var node = (VoxelNode)System.Activator.CreateInstance(type);
                node.GUID = nData.GUID;
                node.SetPosition(new Rect(nData.Position, Vector2.zero));
                if (node is OutputNode outNode) { for (int i = 0; i < nData.PortCount; i++) outNode.AddBiomeSlot(); }
                _graphView.AddElement(node);
                nodeCache.Add(node.GUID, node);
                DeserializeNodeData(node, nData.Data);
                node.RefreshUI();
                _graphView.AddElement(node);
            }
            foreach (var eData in _currentAsset.Edges)
            {
                if (!nodeCache.ContainsKey(eData.OutputNodeGUID) || !nodeCache.ContainsKey(eData.InputNodeGUID)) continue;
                var outPort = nodeCache[eData.OutputNodeGUID].outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == eData.OutputPortName);
                var inPort = nodeCache[eData.InputNodeGUID].inputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == eData.InputPortName);
                if (outPort != null && inPort != null) _graphView.AddElement(outPort.ConnectTo(inPort));
            }
        }

        private string SerializeNodeData(VoxelNode node)
        {
            var c = CultureInfo.InvariantCulture;
            if (node is ConstantNode cn) return $"Const|{cn.Value.ToString(c)}";
            if (node is Vector3Node v3n) return $"Vec3|{v3n.Value.x.ToString(c)}|{v3n.Value.y.ToString(c)}|{v3n.Value.z.ToString(c)}";
            if (node is AdvancedNoiseNode adv) return $"AdvNoise|{adv.SelectedType}|{adv.Scale.ToString(c)}|{adv.Octaves}|{adv.Persistence.ToString(c)}|{adv.Lacunarity.ToString(c)}|{adv.Offset.x.ToString(c)}|{adv.Offset.y.ToString(c)}|{adv.Offset.z.ToString(c)}";

            if (node is NoiseNode n) return $"Noise|{n.SelectedType}|{n.Scale.ToString(c)}";
            if (node is OctaveNoiseNode oct) return $"Octave|{oct.SelectedType}|{oct.Octaves}|{oct.Persistence.ToString(c)}|{oct.Scale.ToString(c)}";

            if (node is MathNode m) return $"Math|{m.Operation}";
            if (node is BiomeNode b) return $"Biome|{b.TargetTemp.ToString(c)}|{b.TexIndexR}|{b.TexIndexG}|{b.TexIndexB}|{b.TexIndexA}";
            if (node is ColorNode col) return $"Color|{col.Value.r.ToString(c)}|{col.Value.g.ToString(c)}|{col.Value.b.ToString(c)}";
            if (node is MakeVector3Node) return "MakeVec3";
            if (node is CoordinateNode coord) return $"Coord|{coord.X.ToString(c)}|{coord.Y.ToString(c)}|{coord.Z.ToString(c)}";
            if (node is TextureSlotNode ts) return $"TexSlot|{ts.SelectedSlot}";
            if (node is TextureLayerNode tln) return "LayerMixer|" + JsonUtility.ToJson(tln);
            return "None";
        }

        private void DeserializeNodeData(VoxelNode node, string data)
        {
            if (string.IsNullOrEmpty(data) || data == "None") return;
            var p = data.Split('|');
            var c = CultureInfo.InvariantCulture;
            try
            {
                if (p[0] == "Const" && node is ConstantNode constant)
                {
                    constant.Value = float.Parse(p[1], c);
                    constant.RefreshUI();
                }
                else if (p[0] == "Vec3" && node is Vector3Node v3)
                {
                    v3.Value = new Vector3(float.Parse(p[1], c), float.Parse(p[2], c), float.Parse(p[3], c));
                    v3.RefreshUI();
                }
                else if (p[0] == "AdvNoise" && node is AdvancedNoiseNode adv)
                {
                    adv.SelectedType = (NoiseType)System.Enum.Parse(typeof(NoiseType), p[1]);
                    adv.Scale = float.Parse(p[2], c);
                    adv.Octaves = int.Parse(p[3]);
                    adv.Persistence = float.Parse(p[4], c);
                    adv.Lacunarity = float.Parse(p[5], c);
                    adv.Offset = new Vector3(float.Parse(p[6], c), float.Parse(p[7], c), float.Parse(p[8], c));
                    adv.RefreshUI();
                }
                else if (p[0] == "LayerMixer" && node is TextureLayerNode tln)
                {
                    // Загружаем данные из JSON в существующий объект
                    JsonUtility.FromJsonOverwrite(p[1], tln);
                    tln.RefreshUI();
                }
                else if (p[0] == "Coord" && node is CoordinateNode coord)
                {
                    coord.X = float.Parse(p[1], c);
                    coord.Y = float.Parse(p[2], c);
                    coord.Z = float.Parse(p[3], c);
                    coord.RefreshUI();
                }
                else if (p[0] == "Noise" && node is NoiseNode noise)
                {
                    noise.SelectedType = (NoiseType)System.Enum.Parse(typeof(NoiseType), p[1]);
                    noise.Scale = float.Parse(p[2], c);
                    noise.RefreshUI();
                }
                else if (p[0] == "Math" && node is MathNode math)
                {
                    math.SetOperation((MathType)System.Enum.Parse(typeof(MathType), p[1]));
                }
                else if (p[0] == "Biome" && node is BiomeNode biome)
                {
                    biome.TargetTemp = float.Parse(p[1], c);
                    if (p.Length >= 6)
                    {
                        biome.TexIndexR = int.Parse(p[2]);
                        biome.TexIndexG = int.Parse(p[3]);
                        biome.TexIndexB = int.Parse(p[4]);
                        biome.TexIndexA = int.Parse(p[5]);
                    }
                    biome.RefreshUI();
                }
                else if (p[0] == "Color" && node is ColorNode col)
                {
                    col.Value = new Color(float.Parse(p[1], c), float.Parse(p[2], c), float.Parse(p[3], c), 1f);
                    col.RefreshUI();
                }
                else if (p[0] == "TexSlot" && node is TextureSlotNode ts)
                {
                    // Превращаем строку обратно в Enum
                    ts.SelectedSlot = (TextureSlotNode.SlotIndex)System.Enum.Parse(typeof(TextureSlotNode.SlotIndex), p[1]);
                    ts.RefreshUI();
                }
                else if (p[0] == "Octave" && node is OctaveNoiseNode oct)
                {
                    oct.SelectedType = (NoiseType)System.Enum.Parse(typeof(NoiseType), p[1]);
                    oct.Octaves = int.Parse(p[2]);
                    oct.Persistence = float.Parse(p[3], c);
                    oct.Scale = float.Parse(p[4], c);
                    oct.RefreshUI();
                }
            }
            catch { }
        }
    }
}