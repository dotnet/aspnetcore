using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace BasicTestApp.ServerReliability
{
    public class ThrowingSetParametersAsyncComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public async Task SetParametersAsync(ParameterView parameters)
        {
            await Task.Yield();
            throw new InvalidTimeZoneException();
        }
    }
}
