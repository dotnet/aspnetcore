// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNet.Server.Testing.Common
{
    public static class FreePortHelper
    {
        public static Uri FindFreeUrl(string urlHint)
        {
            var uriHint = new Uri(urlHint);
            var builder = new UriBuilder(uriHint)
            {
                Port = FindFreePort(uriHint.Port)
            };
            return builder.Uri;
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
