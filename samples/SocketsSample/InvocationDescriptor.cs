using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocketsSample
{
    public class InvocationDescriptor
    {
        public string Id { get; set; }

        public string Method { get; set; }

        public object[] Arguments { get; set; }
    }
}
