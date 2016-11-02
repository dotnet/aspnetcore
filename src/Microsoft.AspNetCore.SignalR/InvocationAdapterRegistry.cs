using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class InvocationAdapterRegistry
    {
        private Dictionary<string, IInvocationAdapter> _invocationAdapters = new Dictionary<string, IInvocationAdapter>();

        public void RegisterInvocationAdapter(string format, IInvocationAdapter adapter)
        {
            _invocationAdapters[format] = adapter;
        }

        public IInvocationAdapter GetInvocationAdapter(string format)
        {
            IInvocationAdapter value;

            return _invocationAdapters.TryGetValue(format, out value) ? value : null;
        }
    }
}