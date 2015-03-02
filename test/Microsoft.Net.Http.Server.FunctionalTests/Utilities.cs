// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.Net.Http.Server
{
    internal static class Utilities
    {
        private const int BasePort = 5001;
        private const int MaxPort = 8000;
        private static int NextPort = BasePort;
        private static object PortLock = new object();

        internal static WebListener CreateHttpAuthServer(AuthenticationSchemes authScheme, out string baseAddress)
        {
            var listener = CreateHttpServer(out baseAddress);
            listener.AuthenticationManager.AuthenticationSchemes = authScheme;
            return listener;
        }

        internal static WebListener CreateHttpServer(out string baseAddress)
        {
            string root;
            return CreateDynamicHttpServer(string.Empty, out root, out baseAddress);
        }

        internal static WebListener CreateHttpServerReturnRoot(string path, out string root)
        {
            string baseAddress;
            return CreateDynamicHttpServer(path, out root, out baseAddress);
        }

        internal static WebListener CreateDynamicHttpServer(string basePath, out string root, out string baseAddress)
        {
            lock (PortLock)
            {
                while (NextPort < MaxPort)
                {
                    var port = NextPort++;
                    var prefix = UrlPrefix.Create("http", "localhost", port, basePath);
                    root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
                    baseAddress = prefix.ToString();
                    var listener = new WebListener();
                    listener.UrlPrefixes.Add(prefix);
                    try
                    {
                        listener.Start();
                        return listener;
                    }
                    catch (WebListenerException)
                    {
                        listener.Dispose();
                    }
                }
                NextPort = BasePort;
            }
            throw new Exception("Failed to locate a free port.");
        }


        internal static WebListener CreateHttpsServer()
        {
            return CreateServer("https", "localhost", 9090, string.Empty);
        }

        internal static WebListener CreateServer(string scheme, string host, int port, string path)
        {
            WebListener listener = new WebListener();
            listener.UrlPrefixes.Add(UrlPrefix.Create(scheme, host, port, path));
            listener.Start();
            return listener;
        }
    }
}
