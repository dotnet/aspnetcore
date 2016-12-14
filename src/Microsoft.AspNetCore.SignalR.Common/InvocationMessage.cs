using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class InvocationMessage
    {
        public string Id { get; set; }
    }
}
