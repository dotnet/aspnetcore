using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Sockets
{
    public class SocketsApplicationLifetimeEvents : IApplicationLifetimeEvents
    {
        private readonly ConnectionManager _connectionManager;

        public SocketsApplicationLifetimeEvents(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public void OnApplicationStarted()
        {

        }

        public void OnApplicationStopped()
        {

        }

        public void OnApplicationStopping()
        {
            _connectionManager.CloseConnections();
        }
    }
}
