using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace BasicTestApp.ServerReliability
{
    public class ThrowingConstructorComponent : IComponent
    {
        public ThrowingConstructorComponent()
        {
            throw new InvalidTimeZoneException();
        }

        public void Attach(RenderHandle renderHandle)
        {
            throw new NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
        }
    }
}
