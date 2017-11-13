using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public abstract partial class TransportConnection
    {
        public TransportConnection()
        {
            _currentIConnectionIdFeature = this;
            _currentIConnectionTransportFeature = this;
            _currentIHttpConnectionFeature = this;
        }

        public IPAddress RemoteAddress { get; set; }
        public int RemotePort { get; set; }
        public IPAddress LocalAddress { get; set; }
        public int LocalPort { get; set; }

        public string ConnectionId { get; set; }

        public virtual BufferPool BufferPool { get; }
        public virtual IScheduler InputWriterScheduler { get; }
        public virtual IScheduler OutputReaderScheduler { get; }

        public IPipeConnection Transport { get; set; }
        public IPipeConnection Application { get; set; }

        public IPipeWriter Input => Application.Output;
        public IPipeReader Output => Application.Input;
    }
}
