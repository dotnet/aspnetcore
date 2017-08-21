using System;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public abstract partial class TransportConnection
    {
        private readonly TaskCompletionSource<object> _abortTcs = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _closedTcs = new TaskCompletionSource<object>();

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

        public virtual PipeFactory PipeFactory { get; }
        public virtual IScheduler InputWriterScheduler { get; }
        public virtual IScheduler OutputReaderScheduler { get; }

        public IPipeConnection Transport { get; set; }
        public IPipeConnection Application { get; set; }

        protected void Abort(Exception exception)
        {
            if (exception == null)
            {
                _abortTcs.TrySetResult(null);
            }
            else
            {
                _abortTcs.TrySetException(exception);
            }
        }

        protected void Close(Exception exception)
        {
            if (exception == null)
            {
                _closedTcs.TrySetResult(null);
            }
            else
            {
                _closedTcs.TrySetException(exception);
            }
        }
    }
}
