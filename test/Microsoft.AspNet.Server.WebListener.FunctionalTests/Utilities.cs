// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading.Tasks;
using Microsoft.Net.Http.Server;

namespace Microsoft.AspNet.Server.WebListener
{
    using AppFunc = Func<object, Task>;

    internal static class Utilities
    {
        private const int BasePort = 5001;
        private const int MaxPort = 8000;
        private static int NextPort = BasePort;
        private static object PortLock = new object();

        internal static IDisposable CreateHttpServer(out string baseAddress, AppFunc app)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, AuthenticationSchemes.AllowAnonymous, out root, out baseAddress, app);
        }

        internal static IDisposable CreateHttpServerReturnRoot(string path, out string root, AppFunc app)
        {
            string baseAddress;
            return CreateDynamicHttpServer(path, AuthenticationSchemes.AllowAnonymous, out root, out baseAddress, app);
        }

        internal static IDisposable CreateHttpAuthServer(AuthenticationSchemes authType, out string baseAddress, AppFunc app)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, authType, out root, out baseAddress, app);
        }

        internal static IDisposable CreateDynamicHttpServer(string basePath, AuthenticationSchemes authType, out string root, out string baseAddress, AppFunc app)
        {
            var factory = new ServerFactory(loggerFactory: null);
            lock (PortLock)
            {
                while (NextPort < MaxPort)
                {

                    var port = NextPort++;
                    var prefix = UrlPrefix.Create("http", "localhost", port, basePath);
                    root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
                    baseAddress = prefix.ToString();

                    var serverInfo = (ServerInformation)factory.Initialize(configuration: null);
                    serverInfo.Listener.UrlPrefixes.Add(prefix);
                    serverInfo.Listener.AuthenticationManager.AuthenticationSchemes = authType;
                    try
                    {
                        return factory.Start(serverInfo, app);
                    }
                    catch (WebListenerException)
                    {
                    }
                }
                NextPort = BasePort;
            }
            throw new Exception("Failed to locate a free port.");
        }

        internal static IDisposable CreateHttpsServer(AppFunc app)
        {
            return CreateServer("https", "localhost", 9090, string.Empty, app);
        }

        internal static IDisposable CreateServer(string scheme, string host, int port, string path, AppFunc app)
        {
            var factory = new ServerFactory(loggerFactory: null);
            var serverInfo = (ServerInformation)factory.Initialize(configuration: null);
            serverInfo.Listener.UrlPrefixes.Add(UrlPrefix.Create(scheme, host, port, path));

            return factory.Start(serverInfo, app);
        }
    }
}
