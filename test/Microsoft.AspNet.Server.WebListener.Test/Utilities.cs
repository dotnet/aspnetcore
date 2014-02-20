using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Server.WebListener.Test
{
    using AppFunc = Func<object, Task>;

    internal static class Utilities
    {
        internal static IDisposable CreateHttpServer(AppFunc app)
        {
            return CreateServer("http", "localhost", "8080", string.Empty, app);
        }

        internal static IDisposable CreateHttpsServer(AppFunc app)
        {
            return CreateServer("https", "localhost", "9090", string.Empty, app);
        }

        internal static IDisposable CreateAuthServer(AuthenticationType authType, AppFunc app)
        {
            return CreateServer("http", "localhost", "8080", string.Empty, authType, app);
        }

        internal static IDisposable CreateServer(string scheme, string host, string port, string path, AppFunc app)
        {
            return CreateServer(scheme, host, port, path, AuthenticationType.None, app);
        }

        internal static IDisposable CreateServer(string scheme, string host, string port, string path, AuthenticationType authType, AppFunc app)
        {
            IDictionary<string, object> address = new Dictionary<string, object>();
            address["scheme"] = scheme;
            address["host"] = host;
            address["port"] = port;
            address["path"] = path;

            ServerFactory factory = new ServerFactory();
            IServerConfiguration config = factory.CreateConfiguration();
            config.Addresses.Add(address);

            OwinWebListener listener = (OwinWebListener)config.AdvancedConfiguration;
            listener.AuthenticationManager.AuthenticationTypes = authType;

            return factory.Start(config, app);
        }
    }
}
