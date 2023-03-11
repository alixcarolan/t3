using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Animation;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Utilities = T3.Core.Utils.Utilities;

namespace T3.Operators.Types.Id_2c53eee7_eb38_449b_ad2a_d7a674952e5b
{
    public class GradientsToTexture : Instance<GradientsToTexture>
    {
        [Output(Guid = "7ad741ec-274d-493c-994f-1a125b96a6e9")]
        public readonly Slot<Texture2D> GradientsTexture = new Slot<Texture2D>();
        
        public GradientsToTexture()
        {
            GradientsTexture.UpdateAction = Update;
        }
        
        private void Update(EvaluationContext context)
        {
            if (!Gradients.IsConnected)
                return;
            
            var useHorizontal = Direction.GetValue(context) == 0;
            var gradientsCount = Gradients.CollectedInputs.Count;
            if (gradientsCount == 0)
                return;

            var sampleCount = Resolution.GetValue(context).Clamp(1,16384);
            const int entrySizeInBytes = sizeof(float) * 4;
            var gradientSizeInBytes = sampleCount * entrySizeInBytes;
            var bufferSizeInBytes = gradientsCount * gradientSizeInBytes;
            var gradientsCollectedInputs = Gradients.CollectedInputs;
            try
            {
                using var dataStream = new DataStream(bufferSizeInBytes, true, true);

                var texDesc = new Texture2DDescription()
                                  {
                                      Width = useHorizontal ? sampleCount : gradientsCount,
                                      Height = useHorizontal ? gradientsCount : sampleCount,
                                      ArraySize = 1,
                                      BindFlags = BindFlags.ShaderResource,
                                      Usage = ResourceUsage.Default,
                                      MipLevels = 1,
                                      CpuAccessFlags = CpuAccessFlags.None,
                                      Format = Format.R32G32B32A32_Float,
                                      SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                                  };

                if (useHorizontal)
                {
                    foreach (var gradientsInput in gradientsCollectedInputs)
                    {
                        var gradient = gradientsInput.GetValue(context);
                        if (gradient == null)
                        {
                            dataStream.Seek(gradientSizeInBytes, SeekOrigin.Current);
                            continue;
                        }

                        for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                        {
                            var sampledColor = gradient.Sample((float)sampleIndex / (sampleCount - 1f));
                            dataStream.Write(sampledColor.X);
                            dataStream.Write(sampledColor.Y);
                            dataStream.Write(sampledColor.Z);
                            dataStream.Write(sampledColor.W);
                        }
                    }
                }
                else
                {
                    var gradients = new List<Gradient>(gradientsCollectedInputs.Count);
                    foreach (var gradientsInput in gradientsCollectedInputs)
                    {
                        gradients.Add(gradientsInput.GetValue(context));
                    }
                    
                    
                    for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                    {
                        var f = sampleIndex / (sampleCount - 1f);
                        foreach (var gradient in gradients)
                        {
                            var sampledColor = gradient?.Sample(f) ?? System.Numerics.Vector4.Zero;
                            dataStream.Write(sampledColor.X);
                            dataStream.Write(sampledColor.Y);
                            dataStream.Write(sampledColor.Z);
                            dataStream.Write(sampledColor.W);
                        }
                    }
                }


                dataStream.Position = 0;
                var dataRectangles = new DataRectangle[]
                                         {
                                             new DataRectangle(dataPointer:dataStream.DataPointer, 
                                                               pitch:useHorizontal ? gradientSizeInBytes : gradientsCount * entrySizeInBytes)
                                         };
                Utilities.Dispose(ref GradientsTexture.Value);
                GradientsTexture.Value = new Texture2D(ResourceManager.Device, texDesc, dataRectangles);
            }
            catch (Exception e)
            {
                Log.Warning("Unable to export gradient to texture " + e.Message, this);
            }
            Gradients.DirtyFlag.Clear();
        }

        
        
        [Input(Guid = "588BE11F-D0DB-4E51-8DBB-92A25408511C")]
        public readonly MultiInputSlot<Gradient> Gradients = new();
        
        [Input(Guid = "1F1838E4-8502-4AC4-A8DF-DCB4CAE57DA4")]
        public readonly MultiInputSlot<int> Resolution = new();
        
        [Input(Guid = "65B83219-4E3F-4A3E-A35B-705E8658CC7B", MappedType = typeof(Directions))]
        public readonly InputSlot<int> Direction = new();
        
        private enum Directions
        {
            Horizontal,
            Vertical,
        }        
    }
}