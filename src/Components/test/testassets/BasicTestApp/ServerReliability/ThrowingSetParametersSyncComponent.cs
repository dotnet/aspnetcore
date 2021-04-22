using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace BasicTestApp.ServerReliability
{
    public class ThrowingSetParametersSyncComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new InvalidTimeZoneException();
        }
    }
}
