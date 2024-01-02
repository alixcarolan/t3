using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.lib.img.adjust
{
	[Guid("d9a71078-8296-4a07-b7de-250d4e2b95ac")]
    public class Tint : Instance<Tint>
    {
        [Output(Guid = "ce2fb7bd-6204-4b07-ab35-42bcb7aeaffe")]
        public readonly Slot<Texture2D> Output = new();

        
        [Input(Guid = "3f1d8fa3-73bd-475c-a65b-c5352bf6ea85")]
        public readonly InputSlot<Texture2D> Texture2d = new();

        [Input(Guid = "387ce8fc-42c2-438d-bf52-144b5dfd8811")]
        public readonly InputSlot<System.Numerics.Vector4> MapBlackTo = new();

        [Input(Guid = "26a45300-c4d6-4e43-8550-37d2fa87799d")]
        public readonly InputSlot<System.Numerics.Vector4> MapWhiteTo = new();

        [Input(Guid = "7307d198-d2d5-41d4-b8b8-d2ece26dade8")]
        public readonly InputSlot<float> Amount = new();

        [Input(Guid = "d09c7a75-d02d-421b-ba0f-17b345f523ec")]
        public readonly InputSlot<System.Numerics.Vector4> ChannelWeights = new();

        [Input(Guid = "9756e240-6527-4cda-accd-e8096682b6bd")]
        public readonly InputSlot<float> Bias = new();

    }
}