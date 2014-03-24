using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
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
            var factory = new ServerFactory(loggerFactory: null);
            var serverInfo = (ServerInformation)factory.Initialize(configuration: null);
            serverInfo.Listener.UrlPrefixes.Add(UrlPrefix.Create(scheme, host, port, path));
            serverInfo.Listener.AuthenticationManager.AuthenticationTypes = authType;

            return factory.Start(serverInfo, app);
        }
    }
}
