using System;

namespace Microsoft.AspNet.Hosting.Server
{
    public class ServerManager : IServerManager
    {
        public IServerFactory GetServer(string serverName)
        {
            throw new NotImplementedException();
        }
    }
}