using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace Assets.Modules.GenerationModule.EditTools.NodeSystem
{
    public class MakeVector3Node : VoxelNode
    {
        public Port InputX, InputY, InputZ;

        public MakeVector3Node()
        {
            title = "Make Vector3";
            GUID = System.Guid.NewGuid().ToString();

            InputX = GeneratePort(Direction.Input); InputX.portName = "X"; inputContainer.Add(InputX);
            InputY = GeneratePort(Direction.Input); InputY.portName = "Y"; inputContainer.Add(InputY);
            InputZ = GeneratePort(Direction.Input); InputZ.portName = "Z"; inputContainer.Add(InputZ);

            var outPort = GeneratePort(Direction.Output, Port.Capacity.Multi, typeof(UnityEngine.Vector3));
            outPort.portName = "XYZ";
            outputContainer.Add(outPort);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override string GetHLSL(ref int varCount, out string varName, Dictionary<VoxelNode, string> cache)
        {
            if (cache.TryGetValue(this, out varName)) return "";

            string cX = GetInputHLSL(InputX, ref varCount, out string nX, cache);
            string cY = GetInputHLSL(InputY, ref varCount, out string nY, cache);
            string cZ = GetInputHLSL(InputZ, ref varCount, out string nZ, cache);

            varName = $"vec3_{varCount++}";
            cache.Add(this, varName);

            return cX + cY + cZ + $"float3 {varName} = float3({nX}, {nY}, {nZ});\n";
        }
    }
}