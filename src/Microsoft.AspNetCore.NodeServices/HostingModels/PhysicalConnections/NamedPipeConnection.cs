using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.HostingModels.PhysicalConnections
{
    internal class NamedPipeConnection : StreamConnection
    {
        private bool _disposedValue = false;
        private NamedPipeClientStream _namedPipeClientStream;

        public override async Task<Stream> Open(string address)
        {
            _namedPipeClientStream = new NamedPipeClientStream(".", address, PipeDirection.InOut);
            await _namedPipeClientStream.ConnectAsync().ConfigureAwait(false);
            return _namedPipeClientStream;
        }

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