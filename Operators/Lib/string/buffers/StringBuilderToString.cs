using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.lib.@string.buffers 
{
	[Guid("c5f1292a-e692-422b-9261-b5ae3451cd7c")]
    public class StringBuilderToString : Instance<StringBuilderToString>
    {
        [Output(Guid = "71C4AA96-9A20-494A-9A28-6DD33A867ED2")]
        public readonly Slot<string> String = new();
        
        public StringBuilderToString()
        {
            String.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var stringBuilder = InputBuffer.GetValue(context);
            if (stringBuilder == null)
            {
                String.Value = System.String.Empty;    
                return;
            }
            
            String.Value = stringBuilder.ToString();
        }
        
        [Input(Guid = "4690405C-343C-4F4B-BF32-51F5D2CC9C76")]
        public readonly InputSlot<System.Text.StringBuilder> InputBuffer = new();
        
    }
}
