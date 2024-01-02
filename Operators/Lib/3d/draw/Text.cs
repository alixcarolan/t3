using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace Operators.lib._3d.draw
{
	[Guid("fd31d208-12fe-46bf-bfa3-101211f8f497")]
    public class Text : Instance<Text>, ITransformable
    {
        public enum HorizontalAligns
        {
            Left,
            Center,
            Right,
        }
        
        public enum VerticalAligns
        {
            Top,
            Middle,
            Bottom,
        }
        
        [Output(Guid = "3f8b20a7-c8b8-45ab-86a1-0efcd927358e", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly TransformCallbackSlot<Command> Output = new();

        
        public Text()
        {
            Output.TransformableOp = this;
        }
        
        IInputSlot ITransformable.TranslationInput => Position;
        IInputSlot ITransformable.RotationInput => null;
        IInputSlot ITransformable.ScaleInput => null;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        
        [Input(Guid = "f1f1be0e-d5bc-4940-bbc1-88bfa958f0e1")]
        public readonly InputSlot<string> InputText = new();

        [Input(Guid = "0e5f05b4-5e8a-4f6d-8cac-03b04649eb67")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "989e6950-fd32-4d0b-97c2-d03264cb2db5")]
        public readonly InputSlot<System.Numerics.Vector4> Shadow = new();

        [Input(Guid = "de0fed7d-d2af-4424-baf3-81606e26559f")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new();

        [Input(Guid = "d89c518c-a862-4f46-865b-0380350b7417")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "eaf9dc37-e70f-4197-895c-b5607456b4a2")]
        public readonly InputSlot<float> LineHeight = new();

        [Input(Guid = "ae7f7e83-fa18-44fd-b639-3bd0dbd3ac06", MappedType =  typeof(VerticalAligns))]
        public readonly InputSlot<int> VerticalAlign = new();

        [Input(Guid = "82cc31ff-3307-4b6d-be70-16e471c2ffc9", MappedType = typeof(HorizontalAligns))]
        public readonly InputSlot<int> HorizontalAlign = new();

        [Input(Guid = "835d7f17-9de4-4612-a2f0-01c1346cdf1a")]
        public readonly InputSlot<float> Spacing = new();

        [Input(Guid = "50c9ab21-39f4-4e92-b5a7-ad9e60ae6ec3")]
        public readonly InputSlot<string> FontPath = new();

        [Input(Guid = "28be4e86-6761-4d07-80bf-abf6f82897e4")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> CullMode = new();

        [Input(Guid = "7a76d5aa-1f44-4238-9333-7c2951becc31")]
        public readonly InputSlot<bool> EnableZTest = new();
    }
}

