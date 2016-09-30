// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            return CreateDynamicHttpServer(string.Empty, AuthenticationSchemes.None, true, out root, out baseAddress, app);
        }

        internal static IServer CreateHttpServerReturnRoot(string path, out string root, RequestDelegate app)
        {
            string baseAddress;
            return CreateDynamicHttpServer(path, AuthenticationSchemes.None, true, out root, out baseAddress, app);
        }

        internal static IServer CreateHttpAuthServer(AuthenticationSchemes authType, bool allowAnonymous, out string baseAddress, RequestDelegate app)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, authType, allowAnonymous, out root, out baseAddress, app);
        }

        internal static IServer CreateDynamicHttpServer(string basePath, AuthenticationSchemes authType, bool allowAnonymous, out string root, out string baseAddress, RequestDelegate app)
        {
            lock (PortLock)
            {
                while (NextPort < MaxPort)
                {

                    var port = NextPort++;
                    var prefix = UrlPrefix.Create("http", "localhost", port, basePath);
                    root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
                    baseAddress = prefix.ToString();

                    var server = new MessagePump(Options.Create(new WebListenerOptions()), new LoggerFactory());
                    server.Features.Get<IServerAddressesFeature>().Addresses.Add(baseAddress);
                    server.Listener.Settings.Authentication.Schemes = authType;
                    server.Listener.Settings.Authentication.AllowAnonymous = allowAnonymous;
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
            var server = new MessagePump(Options.Create(new WebListenerOptions()), new LoggerFactory());
            server.Features.Get<IServerAddressesFeature>().Addresses.Add(UrlPrefix.Create(scheme, host, port, path).ToString());
            server.Start(new DummyApplication(app));
            return server;
        }
    }
}
