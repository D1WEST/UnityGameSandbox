namespace Assets.Modules.GenerationModule.EditTools
{
    using System.Collections.Generic;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Assets.Modules.GenerationModule.EditTools.NodeSystem;
    using UnityEditor;

    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private WorldGraphView _graphView;
        private EditorWindow _window;

        public void Init(WorldGraphView graphView, EditorWindow window)
        {
            _graphView = graphView;
            _window = window;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),

                // РАЗДЕЛ: ШУМЫ
                new SearchTreeGroupEntry(new GUIContent("Noise"), 1),
                new SearchTreeEntry(new GUIContent("Noise Generator")) { level = 2, userData = typeof(NoiseNode) },
                new SearchTreeEntry(new GUIContent("Octave")) { level = 2, userData = typeof(OctaveNoiseNode) },
                new SearchTreeEntry(new GUIContent("Advanced Noise")) { level = 2, userData = typeof(AdvancedNoiseNode) },
                new SearchTreeEntry(new GUIContent("Step")) { level = 2, userData = typeof(StepNode) },

                // РАЗДЕЛ: ЦВЕТА
                new SearchTreeGroupEntry(new GUIContent("Colors"), 1),
                new SearchTreeEntry(new GUIContent("Color Picker")) { level = 2, userData = typeof(ColorNode) },
                new SearchTreeEntry(new GUIContent("Mix Colors")) { level = 2, userData = typeof(LerpColorNode) },

                new SearchTreeGroupEntry(new GUIContent("Textures"), 1),
                new SearchTreeEntry(new GUIContent("Texture Slot")) { level = 2, userData = typeof(TextureSlotNode) },

                // РАЗДЕЛ: МАТЕМАТИКА
                new SearchTreeGroupEntry(new GUIContent("Math"), 1),
                new SearchTreeEntry(new GUIContent("Add (+)")) { level = 2, userData = MathType.Add },
                new SearchTreeEntry(new GUIContent("Subtract (-)")) { level = 2, userData = MathType.Subtract },
                new SearchTreeEntry(new GUIContent("Multiply (*)")) { level = 2, userData = MathType.Multiply },
                new SearchTreeEntry(new GUIContent("Divide (/)")) { level = 2, userData = MathType.Divide },
                new SearchTreeEntry(new GUIContent("Lerp (Mix)")) { level = 2, userData = typeof(LerpNode) },
                new SearchTreeEntry(new GUIContent("Clamp")) { level = 2, userData = typeof(ClampNode) },

                // РАЗДЕЛ: ВВОД И КОНСТАНТЫ
                new SearchTreeGroupEntry(new GUIContent("Input"), 1),
                new SearchTreeEntry(new GUIContent("Constant (float)")) { level = 2, userData = typeof(ConstantNode) },
                new SearchTreeEntry(new GUIContent("Constant (Vector3)")) { level = 2, userData = typeof(Vector3Node) },
                new SearchTreeEntry(new GUIContent("Coordinate (X,Y,Z)")) { level = 2, userData = typeof(CoordinateNode) },
                new SearchTreeEntry(new GUIContent("Make Vector3")) { level = 2, userData = typeof(MakeVector3Node) },
                new SearchTreeEntry(new GUIContent("World Position")) { level = 2, userData = typeof(PositionNode) },
                new SearchTreeEntry(new GUIContent("Split Vector3")) { level = 2, userData = typeof(ComponentNode) },

                // РАЗДЕЛ: ВЫВОД
                new SearchTreeGroupEntry(new GUIContent("System"), 1),
                new SearchTreeEntry(new GUIContent("Biome Node")) { level = 2, userData = typeof(BiomeNode) },
                new SearchTreeEntry(new GUIContent("FINAL OUTPUT")) { level = 2, userData = typeof(OutputNode) }
            };
            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            VoxelNode node;

            // Логика создания: математика требует типа операции, остальные - просто типа класса
            if (searchTreeEntry.userData is MathType mathType)
            {
                node = new MathNode(mathType);
            }
            else
            {
                var type = (System.Type)searchTreeEntry.userData;
                node = (VoxelNode)System.Activator.CreateInstance(type);
            }

            // Позиционирование ноды под курсором
            var windowMousePos = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent,
                context.screenMousePosition - _window.position.position);
            var graphMousePos = _graphView.contentViewContainer.WorldToLocal(windowMousePos);
            node.SetPosition(new Rect(graphMousePos, Vector2.zero));

            _graphView.AddElement(node);
            return true;
        }
    }
}
