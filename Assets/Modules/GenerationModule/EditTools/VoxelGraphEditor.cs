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
        private int _previewRenderDistance = 10; // 2D Zoom
        private int _previewRenderDistance3D = 1; // Area (1x1, 3x3...)
        private int _minPreviewY = -2, _maxPreviewY = 1; // Vertical range
        private Image _2dPreviewImage;
        private IMGUIContainer _3dPreviewContainer;
        private Vector3Int _previewChunkPos = new Vector3Int(0, 0, 0);

        [MenuItem("Window/Voxel Magic Editor")]
        public static void Open() => GetWindow<VoxelGraphEditor>("Voxel Graph");

        private void OnEnable()
        {
            _graphView = new WorldGraphView(this);
            rootVisualElement.Add(_graphView);
            GenerateToolbar();
            SetupPreviewPanel();
        }

        private void OnDisable()
        {
            if (_previewRenderUtility != null) { _previewRenderUtility.Cleanup(); _previewRenderUtility = null; }
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
            toolbar.Add(new Button(Compile) { text = "COMPILE & BAKE", style = { backgroundColor = new Color(0.2f, 0.5f, 0.2f), color = Color.white } });
            toolbar.Add(new Button(UpdatePreviews) { text = "UPDATE PREVIEWS", style = { backgroundColor = new Color(0.2f, 0.4f, 0.6f), color = Color.white } });
            rootVisualElement.Add(toolbar);
        }

        private void SetupPreviewPanel()
        {
            var previewPanel = new VisualElement();
            previewPanel.style.position = Position.Absolute;
            previewPanel.style.right = 10;
            previewPanel.style.bottom = 10;
            previewPanel.style.width = 280;
            previewPanel.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            previewPanel.style.paddingTop = previewPanel.style.paddingBottom = 10;
            previewPanel.style.paddingLeft = previewPanel.style.paddingRight = 10;
            previewPanel.style.borderTopWidth = previewPanel.style.borderBottomWidth = previewPanel.style.borderLeftWidth = previewPanel.style.borderRightWidth = 2;
            previewPanel.style.borderTopColor = previewPanel.style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));

            // UI CONTROLS
            var zoom2D = new SliderInt("2D Zoom", 1, 30) { value = _previewRenderDistance };
            zoom2D.RegisterValueChangedCallback(evt => { _previewRenderDistance = evt.newValue; Update2DMap(); });
            previewPanel.Add(zoom2D);

            var area3D = new SliderInt("3D Range", 1, 5) { value = _previewRenderDistance3D };
            area3D.RegisterValueChangedCallback(evt => { _previewRenderDistance3D = evt.newValue; Update3DChunk(); });
            previewPanel.Add(area3D);

            var yRange = new Vector2IntField("Y Range (Min/Max)") { value = new Vector2Int(_minPreviewY, _maxPreviewY) };
            yRange.RegisterValueChangedCallback(evt => { _minPreviewY = evt.newValue.x; _maxPreviewY = evt.newValue.y; Update3DChunk(); });
            previewPanel.Add(yRange);

            previewPanel.Add(new Label("2D Biome Map:") { style = { marginTop = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            _2dPreviewImage = new Image() { style = { width = 260, height = 180, backgroundColor = Color.black } };
            previewPanel.Add(_2dPreviewImage);

            previewPanel.Add(new Label("3D Pillar Preview:") { style = { marginTop = 10, unityFontStyleAndWeight = FontStyle.Bold } });
            _3dPreviewContainer = new IMGUIContainer(Draw3DPreview) { style = { width = 260, height = 260, backgroundColor = new Color(0.05f, 0.05f, 0.05f) } };
            previewPanel.Add(_3dPreviewContainer);

            rootVisualElement.Add(previewPanel);

            _previewRenderUtility = new PreviewRenderUtility();
            _previewRenderUtility.camera.fieldOfView = 45f;
            _previewRenderUtility.camera.farClipPlane = 5000f;
            _previewMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        private void UpdatePreviews()
        {
            if (_currentAsset == null) return;
            Compile();
            Update2DMap();
            Update3DChunk();
        }

        private void Update2DMap()
        {
            if (_currentAsset == null) return;
            int texSize = 128;
            if (_previewTexture2D == null)
            {
                _previewTexture2D = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                _previewTexture2D.filterMode = FilterMode.Bilinear;
                _2dPreviewImage.image = _previewTexture2D;
            }

            float scale = _currentAsset.selectorScale;
            var biomeNodes = _graphView.nodes.ToList().OfType<BiomeNode>().ToList();
            float viewScale = _previewRenderDistance * 15.0f;

            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    float wx = (x - texSize / 2f) * (viewScale / texSize);
                    float wz = (y - texSize / 2f) * (viewScale / texSize);

                    float rawNoise = noise.cnoise(new float3(wx, 0, wz) * scale);
                    float sel = math.saturate(rawNoise * 0.5f + 0.5f);

                    Color finalColor = Color.black;
                    float totalW = 0.0001f;

                    foreach (var b in biomeNodes)
                    {
                        float w = Mathf.Pow(Mathf.Clamp01(1.0f - Mathf.Abs(sel - b.TargetTemp) * 4.0f), 2.0f);
                        finalColor += Color.HSVToRGB(Mathf.Repeat(b.TargetTemp, 1f), 0.7f, 0.8f) * w;
                        totalW += w;
                    }
                    _previewTexture2D.SetPixel(x, y, finalColor / totalW);
                }
            }
            _previewTexture2D.Apply();
        }

        private void Update3DChunk()
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Modules/GenerationModule/Shaders/MarchingCubes.compute");
            var config = AssetDatabase.FindAssets("t:VoxelMaterialConfig")
                .Select(guid => AssetDatabase.LoadAssetAtPath<VoxelMaterialConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();

            if (shader == null || config == null || _currentAsset == null) return;

            var gpuGen = new GPUChunkGenerator(shader, config);
            var settings = new TerrainSettings { seed = 1337f, hubScale = 0.03f, hubThreshold = 0.4f, branchScale = 0.01f, branchThreshold = 0.025f };

            List<CombineInstance> combines = new List<CombineInstance>();
            int halfArea = _previewRenderDistance3D / 2;

            for (int x = -halfArea; x <= halfArea; x++)
            {
                for (int z = -halfArea; z <= halfArea; z++)
                {
                    for (int y = _minPreviewY; y <= _maxPreviewY; y++)
                    {
                        int3 worldPos = new int3((_previewChunkPos.x + x) * 32, y * 32, (_previewChunkPos.z + z) * 32);
                        Mesh m = gpuGen.GenerateChunkSync(new int3(32, 32, 32), worldPos, _currentAsset, settings);
                        if (m != null && m.vertexCount > 0)
                        {
                            combines.Add(new CombineInstance { mesh = m, transform = Matrix4x4.Translate(new Vector3(x * 32, y * 32, z * 32)) });
                        }
                    }
                }
            }

            if (_previewMesh != null) DestroyImmediate(_previewMesh);
            _previewMesh = new Mesh() { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            if (combines.Count > 0) _previewMesh.CombineMeshes(combines.ToArray());

            gpuGen.Dispose();
            _3dPreviewContainer.MarkDirtyRepaint();
        }

        private void Draw3DPreview()
        {
            Rect rect = _3dPreviewContainer.contentRect;
            if (rect.width <= 0 || _previewMesh == null) return;

            Event e = Event.current;
            if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
            {
                _previewDragRotation.x += e.delta.y * 0.8f; _previewDragRotation.y += e.delta.x * 0.8f;
                e.Use(); _3dPreviewContainer.MarkDirtyRepaint();
            }

            _previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            float centerY = (_minPreviewY + _maxPreviewY) * 16f;
            Vector3 focus = new Vector3(0, centerY, 0);
            Quaternion camRot = Quaternion.Euler(_previewDragRotation.x, _previewDragRotation.y, 0);
            _previewRenderUtility.camera.transform.position = focus + camRot * new Vector3(0, 0, -200 - (_previewRenderDistance3D * 40));
            _previewRenderUtility.camera.transform.LookAt(focus);

            _previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, _previewMaterial, 0);
            _previewRenderUtility.camera.Render();
            GUI.DrawTexture(rect, _previewRenderUtility.EndPreview());
        }

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

            // 1. COMPUTE LOGIC (Shape)
            string shapeCode = "";
            string densityVar = "0.0";
            var firstBiome = outputNode.BiomePorts.FirstOrDefault(p => p.connected)?.connections.First().output.node as BiomeNode;
            if (firstBiome != null) shapeCode = firstBiome.GetInputHLSL(firstBiome.DensityInput, ref varCount, out densityVar, cache);

            // Save scale for 2D Preview
            float scaleFromNode = 0.001f;
            var selectorConnection = outputNode.SelectorInput.connections.FirstOrDefault();
            if (selectorConnection != null)
            {
                var src = selectorConnection.output.node;
                if (src is NoiseNode n) scaleFromNode = n.Scale;
                else if (src is AdvancedNoiseNode adv) scaleFromNode = adv.Scale;
                else if (src is OctaveNoiseNode oct) scaleFromNode = oct.Scale;
            }
            _currentAsset.selectorScale = scaleFromNode;

            // 2. VISUAL LOGIC (Pixel Sharp Textures)
            varCount = 0; cache.Clear();
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
            fragLogic += "\n    finalColor /= max(0.0001, totalW);";

            string computePath = "Assets/Modules/GenerationModule/Shaders/MarchingCubes.compute";
            string visualPath = "Assets/Modules/GenerationModule/Shaders/VoxelTriplanar.shader";

            File.WriteAllText(computePath, ReplaceTag(File.ReadAllText(computePath), "// [NODE_GRAPH_START]", "// [NODE_GRAPH_END]", $"{shapeCode}\n    density = {densityVar}; density = clamp(density, -50.0, 50.0);"), new UTF8Encoding(false));
            File.WriteAllText(visualPath, ReplaceTag(File.ReadAllText(visualPath), "// [GENERATED_BIOME_LOGIC_START]", "// [GENERATED_BIOME_LOGIC_END]", fragLogic), new UTF8Encoding(false));

            Save();
            AssetDatabase.ImportAsset(computePath); AssetDatabase.ImportAsset(visualPath);
            Debug.Log("<color=cyan>Voxel Graph: Compiled Successfully!</color>");
        }

        private string ReplaceTag(string text, string start, string end, string newText)
        {
            int s = text.IndexOf(start), e = text.IndexOf(end);
            if (s == -1 || e == -1) return text;
            return text.Substring(0, s + start.Length) + "\n" + newText + "\n    " + text.Substring(e);
        }

        private void Save()
        {
            if (_currentAsset == null) return;
            _currentAsset.Nodes.Clear(); _currentAsset.Edges.Clear();
            foreach (var node in _graphView.nodes.ToList().Cast<VoxelNode>())
            {
                _currentAsset.Nodes.Add(new NodeSerializedData
                {
                    GUID = node.GUID,
                    Type = node.GetType().AssemblyQualifiedName,
                    Position = node.GetPosition().position,
                    Data = SerializeNodeData(node),
                    PortCount = node is OutputNode o ? o.BiomePorts.Count : 0
                });
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
            EditorUtility.SetDirty(_currentAsset); AssetDatabase.SaveAssets();
        }

        private void Load()
        {
            _graphView.graphElements.ForEach(e => _graphView.RemoveElement(e));
            Dictionary<string, VoxelNode> cache = new Dictionary<string, VoxelNode>();
            foreach (var n in _currentAsset.Nodes)
            {
                var type = System.Type.GetType(n.Type); if (type == null) continue;
                var node = (VoxelNode)System.Activator.CreateInstance(type);
                node.GUID = n.GUID; node.SetPosition(new Rect(n.Position, Vector2.zero));
                if (node is OutputNode outNode) for (int i = 0; i < n.PortCount; i++) outNode.AddBiomeSlot();
                _graphView.AddElement(node); cache.Add(node.GUID, node);
                DeserializeNodeData(node, n.Data); node.RefreshUI();
            }
            foreach (var e in _currentAsset.Edges)
            {
                if (!cache.ContainsKey(e.OutputNodeGUID) || !cache.ContainsKey(e.InputNodeGUID)) continue;
                var outP = cache[e.OutputNodeGUID].outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == e.OutputPortName);
                var inP = cache[e.InputNodeGUID].inputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == e.InputPortName);
                if (outP != null && inP != null) _graphView.AddElement(outP.ConnectTo(inP));
            }
        }

        private string SerializeNodeData(VoxelNode node)
        {
            var c = CultureInfo.InvariantCulture;
            if (node is ConstantNode cn) return $"Const|{cn.Value.ToString(c)}";
            if (node is Vector3Node v3n) return $"Vec3|{v3n.Value.x.ToString(c)}|{v3n.Value.y.ToString(c)}|{v3n.Value.z.ToString(c)}";
            if (node is AdvancedNoiseNode adv) return $"AdvNoise|{adv.SelectedType}|{adv.Scale.ToString(c)}|{adv.Octaves}|{adv.Persistence.ToString(c)}|{adv.Lacunarity.ToString(c)}|{adv.Offset.x.ToString(c)}|{adv.Offset.y.ToString(c)}|{adv.Offset.z.ToString(c)}";
            if (node is NoiseNode n) return $"Noise|{n.SelectedType}|{n.Scale.ToString(c)}";
            if (node is MathNode m) return $"Math|{m.Operation}";
            if (node is BiomeNode b) return $"Biome|{b.TargetTemp.ToString(c)}|{b.TexIndexR}|{b.TexIndexG}|{b.TexIndexB}|{b.TexIndexA}";
            if (node is ColorNode col) return $"Color|{col.Value.r.ToString(c)}|{col.Value.g.ToString(c)}|{col.Value.b.ToString(c)}";
            if (node is CoordinateNode coord) return $"Coord|{coord.X.ToString(c)}|{coord.Y.ToString(c)}|{coord.Z.ToString(c)}";
            if (node is TextureSlotNode ts) return $"TexSlot|{ts.SelectedSlot}";
            if (node is TextureLayerNode tln) return "LayerMixer|" + JsonUtility.ToJson(tln);
            if (node is OctaveNoiseNode oct) return $"Octave|{oct.SelectedType}|{oct.Octaves}|{oct.Persistence.ToString(c)}|{oct.Scale.ToString(c)}";
            return "None";
        }

        private void DeserializeNodeData(VoxelNode node, string data)
        {
            if (string.IsNullOrEmpty(data) || data == "None") return;
            var p = data.Split('|'); var c = CultureInfo.InvariantCulture;
            try
            {
                if (p[0] == "Const" && node is ConstantNode cn) cn.Value = float.Parse(p[1], c);
                else if (p[0] == "Vec3" && node is Vector3Node v3) v3.Value = new Vector3(float.Parse(p[1], c), float.Parse(p[2], c), float.Parse(p[3], c));
                else if (p[0] == "AdvNoise" && node is AdvancedNoiseNode adv)
                {
                    adv.SelectedType = (NoiseType)System.Enum.Parse(typeof(NoiseType), p[1]); adv.Scale = float.Parse(p[2], c);
                    adv.Octaves = int.Parse(p[3]); adv.Persistence = float.Parse(p[4], c); adv.Lacunarity = float.Parse(p[5], c);
                    adv.Offset = new Vector3(float.Parse(p[6], c), float.Parse(p[7], c), float.Parse(p[8], c));
                }
                else if (p[0] == "LayerMixer" && node is TextureLayerNode tln) JsonUtility.FromJsonOverwrite(p[1], tln);
                else if (p[0] == "Coord" && node is CoordinateNode coord) { coord.X = float.Parse(p[1], c); coord.Y = float.Parse(p[2], c); coord.Z = float.Parse(p[3], c); }
                else if (p[0] == "Noise" && node is NoiseNode noise) { noise.SelectedType = (NoiseType)System.Enum.Parse(typeof(NoiseType), p[1]); noise.Scale = float.Parse(p[2], c); }
                else if (p[0] == "Math" && node is MathNode math) math.SetOperation((MathType)System.Enum.Parse(typeof(MathType), p[1]));
                else if (p[0] == "Biome" && node is BiomeNode b)
                {
                    b.TargetTemp = float.Parse(p[1], c);
                    if (p.Length >= 6) { b.TexIndexR = int.Parse(p[2]); b.TexIndexG = int.Parse(p[3]); b.TexIndexB = int.Parse(p[4]); b.TexIndexA = int.Parse(p[5]); }
                }
                else if (p[0] == "Color" && node is ColorNode col) col.Value = new Color(float.Parse(p[1], c), float.Parse(p[2], c), float.Parse(p[3], c), 1f);
                else if (p[0] == "TexSlot" && node is TextureSlotNode ts) ts.SelectedSlot = (TextureSlotNode.SlotIndex)System.Enum.Parse(typeof(TextureSlotNode.SlotIndex), p[1]);
                else if (p[0] == "Octave" && node is OctaveNoiseNode oct)
                {
                    oct.SelectedType = (NoiseType)System.Enum.Parse(typeof(NoiseType), p[1]); oct.Octaves = int.Parse(p[2]);
                    oct.Persistence = float.Parse(p[3], c); oct.Scale = float.Parse(p[4], c);
                }
            }
            catch { }
        }
    }
}