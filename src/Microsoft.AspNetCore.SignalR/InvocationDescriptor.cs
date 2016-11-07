using System;

namespace Microsoft.AspNetCore.SignalR
{
    public class InvocationDescriptor
    {
        public string Id { get; set; }

        public string Method { get; set; }

        public object[] Arguments { get; set; }

        public override string ToString()
        {
            return $"{Id}: {Method}({(Arguments ?? new object[0]).Length})";
        }
    }
}
