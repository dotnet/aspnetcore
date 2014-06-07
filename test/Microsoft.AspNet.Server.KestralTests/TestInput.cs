using System;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.KestralTests
{
    class TestInput
    {
        public TestInput()
        {
            var memory = new MemoryPool();
            ConnectionContext = new ConnectionContext
            {
                SocketInput = new SocketInput(memory),
                Memory = memory,
            };

        }
        public ConnectionContext ConnectionContext { get; set; }

        public void Add(string text, bool fin = false)
        {
            var encoding = System.Text.Encoding.ASCII;
            var count = encoding.GetByteCount(text);
            var buffer = ConnectionContext.SocketInput.Available(text.Length);
            count = encoding.GetBytes(text, 0, text.Length, buffer.Array, buffer.Offset);
            ConnectionContext.SocketInput.Extend(count);
            if (fin)
            {
                ConnectionContext.SocketInput.RemoteIntakeFin = true;
            }
        }
    }
}

