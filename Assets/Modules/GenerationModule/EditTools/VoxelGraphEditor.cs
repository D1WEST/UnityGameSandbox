using System.Collections.Generic;
using System.Globalization;
using Assets.Modules.GenerationModule.EditTools.NodeSystem;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Modules.GenerationModule.EditTools
{
    public class VoxelGraphEditor : EditorWindow
    {
        private WorldGraphView _graphView;
        private VoxelGraphData _currentAsset;
        private ObjectField _assetSelector;

        private VisualElement _previewContainer;
        private Image _previewImage;
        private Texture2D _previewTexture;

        [MenuItem("Window/Voxel Magic Editor")]
        public static void Open() => GetWindow<VoxelGraphEditor>("Voxel Graph");

        private void OnEnable()
        {
            _graphView = new WorldGraphView(this);
            rootVisualElement.Add(_graphView);
            GenerateToolbar();
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
            toolbar.Add(new Button(Compile)
            {
                text = "COMPILE SHADER",
                style = {
                    backgroundColor = new Color(0.2f, 0.6f, 0.2f),
                    color = Color.white 
                }
            });
            toolbar.Add(new Button(UpdatePreview) { text = "Update Preview" });

            rootVisualElement.Add(toolbar);
        }

        private void Save()
        {
            if (_currentAsset == null)
            {
                EditorUtility.DisplayDialog("Voxel Editor", "Please select a Graph Asset first!", "OK");
                return;
            }

            _currentAsset.Nodes.Clear();
            _currentAsset.Edges.Clear();

            // Сохраняем ноды
            foreach (var node in _graphView.nodes.ToList().Cast<VoxelNode>())
            {
                _currentAsset.Nodes.Add(new NodeSerializedData
                {
                    GUID = node.GUID,
                    Type = node.GetType().AssemblyQualifiedName, // Используем полное имя типа
                    Position = node.GetPosition().position,
                    Data = SerializeNodeData(node)
                });
            }

            // Сохраняем связи
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
            Debug.Log("<color=green>Voxel Graph Saved Successfully!</color>");
        }

        private void Load()
        {
            _graphView.graphElements.ForEach(e => _graphView.RemoveElement(e));
            if (_currentAsset == null) return;

            Dictionary<string, VoxelNode> nodeCache = new Dictionary<string, VoxelNode>();

            // 1. Восстанавливаем ноды
            foreach (var nData in _currentAsset.Nodes)
            {
                var type = System.Type.GetType(nData.Type);
                if (type == null) continue;

                var node = (VoxelNode)System.Activator.CreateInstance(type);
                node.GUID = nData.GUID;
                node.SetPosition(new Rect(nData.Position, Vector2.zero));
                DeserializeNodeData(node, nData.Data);

                _graphView.AddElement(node);
                nodeCache.Add(node.GUID, node);
            }

            // 2. Восстанавливаем связи
            foreach (var eData in _currentAsset.Edges)
            {
                if (!nodeCache.ContainsKey(eData.OutputNodeGUID) || !nodeCache.ContainsKey(eData.InputNodeGUID)) continue;

                var outNode = nodeCache[eData.OutputNodeGUID];
                var inNode = nodeCache[eData.InputNodeGUID];

                var outPort = outNode.outputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == eData.OutputPortName);
                var inPort = inNode.inputContainer.Query<Port>().ToList().FirstOrDefault(p => p.portName == eData.InputPortName);

                if (outPort != null && inPort != null)
                {
                    var edge = outPort.ConnectTo(inPort);
                    _graphView.AddElement(edge);
                }
            }
        }

        private void UpdatePreview()
        {
            int res = 128;
            if (_previewTexture == null) _previewTexture = new Texture2D(res, res);
            if (_previewImage == null)
            {
                _previewImage = new Image { style = { width = 200, height = 200, position = Position.Absolute, bottom = 10, right = 10, borderBottomWidth = 2, borderBottomColor = Color.white } };
                rootVisualElement.Add(_previewImage);
            }

            // Здесь можно было бы сделать полноценный интерпретатор графа, 
            // но для начала просто покажем шум первой ноды шума на графе
            var noiseNode = _graphView.nodes.ToList().OfType<NoiseNode>().FirstOrDefault();
            if (noiseNode != null)
            {
                for (int x = 0; x < res; x++)
                {
                    for (int z = 0; z < res; z++)
                    {
                        float v = Mathf.PerlinNoise(x * noiseNode.Scale, z * noiseNode.Scale);
                        _previewTexture.SetPixel(x, z, new Color(v, v, v));
                    }
                }
                _previewTexture.Apply();
                _previewImage.image = _previewTexture;
            }
        }

        private void Compile()
        {
            var outputNode = _graphView.nodes.ToList().OfType<OutputNode>().FirstOrDefault();
            if (outputNode == null)
            {
                Debug.LogError("Output node not found!");
                return;
            }

            int varCount = 0;
            string finalVar;
            string generatedCode = outputNode.GetHLSL(ref varCount, out finalVar);

            // Важно: записываем в существующую переменную density из шейдера
            string finalLine = $"\n    density = {finalVar};";

            string path = "Assets/Modules/GenerationModule/Shaders/MarchingCubes.compute";
            if (!File.Exists(path)) { Debug.LogError("Shader not found at: " + path); return; }

            string shaderContent = File.ReadAllText(path);
            string startTag = "// [NODE_GRAPH_START]";
            string endTag = "// [NODE_GRAPH_END]";

            int startIdx = shaderContent.IndexOf(startTag);
            int endIdx = shaderContent.IndexOf(endTag);

            if (startIdx == -1 || endIdx == -1)
            {
                Debug.LogError("Tags // [NODE_GRAPH_START] or // [NODE_GRAPH_END] not found in shader!");
                return;
            }

            string newContent = shaderContent.Substring(0, startIdx + startTag.Length) +
                                "\n" + generatedCode + finalLine + "\n    " +
                                shaderContent.Substring(endIdx);

            // Запись без BOM (важно для шейдеров)
            File.WriteAllText(path, newContent, new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);

            Debug.Log("<color=cyan>Voxel Shader Compiled! Formula: " + finalVar + "</color>");
        }

        private string SerializeNodeData(VoxelNode node)
        {
            var culture = CultureInfo.InvariantCulture;

            if (node is NoiseNode noise)
                return $"Noise|{noise.SelectedType}|{noise.Scale.ToString(culture)}";

            if (node is ConstantNode constant)
                return $"Const|{constant.Value.ToString(culture)}";

            if (node is MathNode math)
                return $"Math|{math.Operation}";

            return "None";
        }

        private void DeserializeNodeData(VoxelNode node, string data)
        {
            if (string.IsNullOrEmpty(data) || data == "None") return;

            var parts = data.Split('|');
            var culture = CultureInfo.InvariantCulture;

            try
            {
                // Проверяем первый тег, чтобы точно знать, что мы читаем
                if (parts[0] == "Noise" && node is NoiseNode noise)
                {
                    noise.SelectedType = (NoiseType)System.Enum.Parse(typeof(NoiseType), parts[1]);
                    noise.Scale = float.Parse(parts[2], culture);

                    // Обновляем визуальное поле в UI, если оно есть
                    var field = noise.mainContainer.Query<FloatField>().First();
                    if (field != null) field.value = noise.Scale;
                }
                else if (parts[0] == "Const" && node is ConstantNode constant)
                {
                    constant.Value = float.Parse(parts[1], culture);

                    var field = constant.mainContainer.Query<FloatField>().First();
                    if (field != null) field.value = constant.Value;
                }
                else if (parts[0] == "Math" && node is MathNode math)
                {
                    var op = (MathType)System.Enum.Parse(typeof(MathType), parts[1]);
                    math.SetOperation(op);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not load data for node {node.title}: {e.Message}. Using defaults.");
            }
        }
    }
}
