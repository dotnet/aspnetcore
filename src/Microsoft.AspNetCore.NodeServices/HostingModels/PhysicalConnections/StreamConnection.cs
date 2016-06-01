using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.HostingModels.PhysicalConnections
{
    internal abstract class StreamConnection : IDisposable
    {
        public abstract Task<Stream> Open(string address);
        public abstract void Dispose();

        public static StreamConnection Create()
        {
            var useNamedPipes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
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