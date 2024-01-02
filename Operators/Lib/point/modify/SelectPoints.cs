using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace Operators.lib.point.modify
{
	[Guid("371d72b8-69d4-4ced-beda-271386ad2fd6")]
    public class SelectPoints : Instance<SelectPoints>, ITransformable
    {
        [Output(Guid = "d81a0df4-54b4-4587-8f0f-32a740261d73")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Result2 = new();

        public SelectPoints()
        {
            Result2.TransformableOp = this;
        }

        IInputSlot ITransformable.TranslationInput => VolumeCenter;
        IInputSlot ITransformable.RotationInput => VolumeRotate;
        IInputSlot ITransformable.ScaleInput => VolumeScale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }


        [Input(Guid = "f9a61731-c35e-48fd-b297-922fb4c3da4a")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "2c5e5bb5-6023-4ff4-906d-d0a905110e95", MappedType = typeof(Shapes))]
        public readonly InputSlot<int> VolumeShape = new();
        
        [Input(Guid = "0b11d459-01cc-4e91-99d2-37e77a0c8a35")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeCenter = new();

        [Input(Guid = "ebe18d4c-547b-4946-8367-098e61ec9942")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeStretch = new();

        [Input(Guid = "ef02525a-3a75-4ae7-bc01-7581efeda246")]
        public readonly InputSlot<float> VolumeScale = new();

        [Input(Guid = "1b762425-c35a-4ba0-bbed-71d6c42a082b")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeRotate = new();

        [Input(Guid = "56bf96b4-e7c4-4747-b57a-64a39e0d6314")]
        public readonly InputSlot<float> FallOff = new();

        [Input(Guid = "687fdc7e-8867-4883-9c41-06b9435c0562", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();

        [Input(Guid = "33e347d3-3edd-4428-bd82-dbe87daf063d")]
        public readonly InputSlot<bool> ClampResult = new();

        [Input(Guid = "b712d83b-6d89-43fb-8061-9ab9d38899a9")]
        public readonly InputSlot<float> Strength = new();

        [Input(Guid = "39c1db68-f3f7-4dac-b9e6-25877fe7e18a")]
        public readonly InputSlot<float> Phase = new();

        [Input(Guid = "7b9bafb9-3fee-4685-a76f-0a6c26a34399")]
        public readonly InputSlot<float> Threshold = new();

        [Input(Guid = "e577a3fb-0655-48b2-998a-1080e872c2cd")]
        public readonly InputSlot<bool> DiscardNonSelected = new();

        [Input(Guid = "bdfbbc26-6815-4c5e-b67f-fbe0bbf1f58b")]
        public readonly InputSlot<float> Bias = new();

        [Input(Guid = "62119baf-5e02-4d2e-82bb-f82a149ccfb7")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> Visibility = new();


        
        private enum Shapes
        {
            Sphere,
            Box,
            Plane,
            Zebra,
            Noise,
        }
        
        private enum Modes
        {
            Override,
            Add,
            Sub,
            Multiply,
            Invert,
        }
    }
}

