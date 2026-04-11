namespace Assets.Modules.GenerationModule.EditTools
{
    using Assets.Modules.GenerationModule.EditTools.NodeSystem;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
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
            toolbar.Add(new Button(Compile) { text = "COMPILE SHADER", style = { backgroundColor = new Color(0.2f, 0.5f, 0.2f), color = Color.white } });
            rootVisualElement.Add(toolbar);
        }

        private void Compile()
        {
            var outputNode = _graphView.nodes.ToList().OfType<OutputNode>().FirstOrDefault();
            if (outputNode == null || _currentAsset == null) return;

            // Пытаемся найти масштаб шума в Selector
            float scaleFromNode = 0.001f;
            var selectorConnection = outputNode.SelectorInput.connections.FirstOrDefault();
            if (selectorConnection != null)
            {
                var sourceNode = selectorConnection.output.node;
                if (sourceNode is NoiseNode n) scaleFromNode = n.Scale;
                else if (sourceNode is OctaveNoiseNode oct) scaleFromNode = oct.Scale;
            }
            _currentAsset.selectorScale = scaleFromNode;

            EditorUtility.SetDirty(_currentAsset);
            AssetDatabase.SaveAssets();

            // --- ГЕНЕРАЦИЯ HLSL ---
            int varCount = 0;
            var cache = new Dictionary<VoxelNode, string>();
            string selCode = outputNode.GetInputHLSL(outputNode.SelectorInput, ref varCount, out string selVar, cache);
            if (selVar == "0.0f") selVar = "0.5f";

            List<string> biomeBlocks = new List<string>();
            foreach (var port in outputNode.BiomePorts)
            {
                var conn = port.connections.FirstOrDefault();
                if (conn == null) continue;
                var bNode = conn.output.node as BiomeNode;
                string dCode = bNode.GetInputHLSL(bNode.DensityInput, ref varCount, out string dVar, cache);
                string cCode = bNode.GetInputHLSL(bNode.ColorInput, ref varCount, out string cVar, cache);
                if (cVar == "0.0f") cVar = "float4(1,1,1,1)";
                string tStr = bNode.TargetTemp.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                string wVar = $"weight_{varCount++}";

                biomeBlocks.Add($@"
        {dCode} {cCode}
        float {wVar} = pow(saturate(1.0 - abs({selVar} - {tStr}) * 5.0), 2.0);
        finalDensity += {dVar} * {wVar};
        finalColor += {cVar} * {wVar};
        totalW += {wVar};");
            }

            string sharedLogic = $"{selCode}\n" + string.Join("\n", biomeBlocks);
            string densityFinal = $"float totalW = 0.0001f; float finalDensity = 0; float4 finalColor = 0; {sharedLogic} density = finalDensity / totalW;";
            string colorFinal = $"float totalW = 0.0001f; float finalDensity = 0; float4 finalColor = 0; {sharedLogic} return finalColor / totalW;";

            string shaderPath = "Assets/Modules/GenerationModule/Shaders/MarchingCubes.compute";
            string content = System.IO.File.ReadAllText(shaderPath);
            content = ReplaceTag(content, "// [NODE_GRAPH_START]", "// [NODE_GRAPH_END]", densityFinal);
            content = ReplaceTag(content, "// [NODE_COLOR_START]", "// [NODE_COLOR_END]", colorFinal);

            System.IO.File.WriteAllText(shaderPath, content, new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(shaderPath);
            Debug.Log("<color=cyan>Voxel Graph: Compiled!</color>");
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
            if (node is NoiseNode n) return $"Noise|{n.SelectedType}|{n.Scale.ToString(c)}";
            if (node is ConstantNode cn) return $"Const|{cn.Value.ToString(c)}";
            if (node is MathNode m) return $"Math|{m.Operation}";
            if (node is BiomeNode b) return $"Biome|{b.TargetTemp.ToString(c)}|{b.ColorValue.r.ToString(c)}|{b.ColorValue.g.ToString(c)}|{b.ColorValue.b.ToString(c)}";
            if (node is ColorNode col) return $"Color|{col.Value.r.ToString(c)}|{col.Value.g.ToString(c)}|{col.Value.b.ToString(c)}";
            if (node is OctaveNoiseNode oct) return $"Octave|{oct.SelectedType}|{oct.Octaves}|{oct.Persistence.ToString(c)}|{oct.Scale.ToString(c)}";
            return "None";
        }

        private void DeserializeNodeData(VoxelNode node, string data)
        {
            if (string.IsNullOrEmpty(data) || data == "None") return;
            var p = data.Split('|');
            var c = CultureInfo.InvariantCulture;
            try
            {
                if (p[0] == "Noise" && node is NoiseNode noise)
                {
                    noise.SelectedType = (NoiseType)System.Enum.Parse(typeof(NoiseType), p[1]);
                    noise.Scale = float.Parse(p[2], c);
                    noise.RefreshUI(); // Вызываем метод обновления UI
                }
                else if (p[0] == "Const" && node is ConstantNode constant)
                {
                    constant.Value = float.Parse(p[1], c);
                    constant.RefreshUI();
                }
                else if (p[0] == "Math" && node is MathNode math)
                {
                    math.SetOperation((MathType)System.Enum.Parse(typeof(MathType), p[1]));
                }
                else if (p[0] == "Biome" && node is BiomeNode biome)
                {
                    biome.TargetTemp = float.Parse(p[1], c);
                    if (p.Length >= 5)
                    {
                        biome.ColorValue = new Color(float.Parse(p[2], c), float.Parse(p[3], c), float.Parse(p[4], c), 1f);
                    }
                    biome.RefreshUI();
                }
                else if (p[0] == "Color" && node is ColorNode col)
                {
                    col.Value = new Color(float.Parse(p[1], c), float.Parse(p[2], c), float.Parse(p[3], c), 1f);
                    col.RefreshUI();
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