using System;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public interface IFrameControl
    {
        void ProduceContinue();
        void Write(ArraySegment<byte> data, Action<Exception, object> callback, object state);
    }
}