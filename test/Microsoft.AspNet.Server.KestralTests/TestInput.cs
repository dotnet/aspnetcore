using System;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.KestralTests
{
    class TestInput : IConnectionControl, IFrameControl
    {
        public TestInput()
        {
            var memory = new MemoryPool();
            FrameContext = new FrameContext
            {
                SocketInput = new SocketInput(memory),
                Memory = memory,
                ConnectionControl = this,
                FrameControl = this
            };
        }

        public FrameContext FrameContext { get; set; }

        public void Add(string text, bool fin = false)
        {
            var encoding = System.Text.Encoding.ASCII;
            var count = encoding.GetByteCount(text);
            var buffer = FrameContext.SocketInput.Available(text.Length);
            count = encoding.GetBytes(text, 0, text.Length, buffer.Array, buffer.Offset);
            FrameContext.SocketInput.Extend(count);
            if (fin)
            {
                FrameContext.SocketInput.RemoteIntakeFin = true;
            }
        }

        public void ProduceContinue()
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Write(ArraySegment<byte> data, Action<object> callback, object state)
        {
        }
        public void End(ProduceEndType endType)
        {
        }
    }
}

