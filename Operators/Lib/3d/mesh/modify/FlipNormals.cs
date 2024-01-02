using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.lib._3d.mesh.modify
{
	[Guid("0f61b638-7bda-4331-944a-50fdca401223")]
    public class FlipNormals : Instance<FlipNormals>
    {
        [Output(Guid = "83268faa-5360-43af-9f85-eaec02574272")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new();
        

        [Input(Guid = "89400186-9c36-4ba6-ac8a-55b7801fb99a")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();
    }
}

