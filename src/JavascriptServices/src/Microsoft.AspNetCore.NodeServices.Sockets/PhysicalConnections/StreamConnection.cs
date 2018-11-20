using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.Sockets.PhysicalConnections
{
    internal abstract class StreamConnection : IDisposable
    {
        public abstract Task<Stream> Open(string address);
        public abstract void Dispose();

        public static StreamConnection Create()
        {
            var useNamedPipes = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);
            if (useNamedPipes)
            {
                return new NamedPipeConnection();
            }
            else
            {
                return new UnixDomainSocketConnection();
            }
        }
    }
}