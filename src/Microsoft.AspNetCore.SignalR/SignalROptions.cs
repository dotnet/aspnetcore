using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public class SignalROptions
    {
        internal readonly Dictionary<string, Type> _invocationMappings = new Dictionary<string, Type>();

        public void RegisterInvocationAdapter<TInvocationAdapter>(string format) where TInvocationAdapter : IInvocationAdapter
        {
            _invocationMappings[format] = typeof(TInvocationAdapter);
        }
    }
}
