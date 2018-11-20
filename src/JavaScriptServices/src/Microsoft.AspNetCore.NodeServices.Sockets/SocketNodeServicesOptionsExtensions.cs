using System;

namespace Microsoft.AspNetCore.NodeServices.Sockets
{
    /// <summary>
    /// Extension methods that help with populating a <see cref="NodeServicesOptions"/> object.
    /// </summary>
    public static class NodeServicesOptionsExtensions
    {
        /// <summary>
        /// Configures the <see cref="INodeServices"/> service so that it will use out-of-process
        /// Node.js instances and perform RPC calls over binary sockets (on Windows, this is
        /// implemented as named pipes; on other platforms it uses domain sockets).
        /// </summary>
        public static void UseSocketHosting(this NodeServicesOptions options)
        {
            var pipeName = "pni-" + Guid.NewGuid().ToString("D"); // Arbitrary non-clashing string
            options.NodeInstanceFactory = () => new SocketNodeInstance(options, pipeName);
        }
    }
}