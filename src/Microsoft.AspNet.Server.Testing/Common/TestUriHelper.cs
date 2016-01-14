// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNet.Server.Testing.Common
{
    public static class TestUriHelper
    {
        public static Uri BuildTestUri()
        {
            return new UriBuilder("http", "localhost", FindFreePort()).Uri;
        }

        public static Uri BuildTestUri(string hint)
        {
            if (string.IsNullOrEmpty(hint))
            {
                return BuildTestUri();
            }
            else
            {
                var uriHint = new Uri(hint);
                return new UriBuilder(uriHint) { Port = FindFreePort(uriHint.Port) }.Uri;
            }
        }

        public static int FindFreePort()
        {
            return FindFreePort(0);
        }

        public static int FindFreePort(int initialPort)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, initialPort));
                }
                catch (SocketException)
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                }

                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}
