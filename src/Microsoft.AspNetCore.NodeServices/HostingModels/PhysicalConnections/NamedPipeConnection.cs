using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.HostingModels.PhysicalConnections
{
    internal class NamedPipeConnection : StreamConnection
    {
        private bool _disposedValue = false;
        private NamedPipeClientStream _namedPipeClientStream;

#pragma warning disable 1998 // Because in the NET451 code path, there's nothing to await
        public override async Task<Stream> Open(string address)
        {
            _namedPipeClientStream = new NamedPipeClientStream(".", address, PipeDirection.InOut);
#if NET451
            _namedPipeClientStream.Connect();
#else
            await _namedPipeClientStream.ConnectAsync().ConfigureAwait(false);
#endif
            return _namedPipeClientStream;
        }
#pragma warning restore 1998

        public override void Dispose()
        {
            if (!_disposedValue)
            {
                if (_namedPipeClientStream != null)
                {
                    _namedPipeClientStream.Dispose();
                }

                _disposedValue = true;
            }
        }
    }
}