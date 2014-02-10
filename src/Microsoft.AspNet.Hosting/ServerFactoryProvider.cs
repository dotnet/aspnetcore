using System;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Hosting
{
    public class ServerFactoryProvider : IServerFactoryProvider
    {
        public IServerFactory GetServerFactory(string serverFactoryIdentifier)
        {
            throw new NotImplementedException();
        }
    }
}