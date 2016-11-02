using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public class InvocationResultDescriptor
    {
        public string Id { get; set; }

        public object Result { get; set; }

        public string Error { get; set; }
    }
}
