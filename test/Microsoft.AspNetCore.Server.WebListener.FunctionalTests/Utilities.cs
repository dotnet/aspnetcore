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
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Features;
using Microsoft.Net.Http.Server;

namespace Microsoft.AspNetCore.Server.WebListener
{
    internal static class Utilities
    {
        // When tests projects are run in parallel, overlapping port ranges can cause a race condition when looking for free
        // ports during dynamic port allocation. To avoid this, make sure the port range here is different from the range in
        // Microsoft.Net.Http.Server.
        private const int BasePort = 5001;
        private const int MaxPort = 8000;
        private static int NextPort = BasePort;
        private static object PortLock = new object();

        internal static IServer CreateHttpServer(out string baseAddress, RequestDelegate app)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, AuthenticationSchemes.AllowAnonymous, out root, out baseAddress, app);
        }

        internal static IServer CreateHttpServerReturnRoot(string path, out string root, RequestDelegate app)
        {
            string baseAddress;
            return CreateDynamicHttpServer(path, AuthenticationSchemes.AllowAnonymous, out root, out baseAddress, app);
        }

        internal static IServer CreateHttpAuthServer(AuthenticationSchemes authType, out string baseAddress, RequestDelegate app)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, authType, out root, out baseAddress, app);
        }

        internal static IServer CreateDynamicHttpServer(string basePath, AuthenticationSchemes authType, out string root, out string baseAddress, RequestDelegate app)
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

                    var server = factory.CreateServer(configuration: null);
                    var listener = server.Features.Get<Microsoft.Net.Http.Server.WebListener>();
                    listener.UrlPrefixes.Add(prefix);
                    listener.AuthenticationManager.AuthenticationSchemes = authType;
                    try
                    {
                        server.Start(new DummyApplication(app));
                        return server;
                    }
                    catch (WebListenerException)
                    {
                    }
                }
                NextPort = BasePort;
            }
            throw new Exception("Failed to locate a free port.");
        }

        internal static IServer CreateHttpsServer(RequestDelegate app)
        {
            return CreateServer("https", "localhost", 9090, string.Empty, app);
        }

        internal static IServer CreateServer(string scheme, string host, int port, string path, RequestDelegate app)
        {
            var factory = new ServerFactory(loggerFactory: null);
            var server = factory.CreateServer(configuration: null);
            server.Features.Get<IServerAddressesFeature>().Addresses.Add(UrlPrefix.Create(scheme, host, port, path).ToString());
            server.Start(new DummyApplication(app));
            return server;
        }
    }
}
