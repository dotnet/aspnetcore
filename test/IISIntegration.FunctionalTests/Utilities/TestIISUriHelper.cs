// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    // Copied from Hosting for now https://github.com/aspnet/Hosting/blob/970bc8a30d66dd6894f8f662e5fdab9e68d57777/src/Microsoft.AspNetCore.Server.IntegrationTesting/Common/TestUriHelper.cs
    internal static class TestIISUriHelper
    {
        internal static Uri BuildTestUri(ServerType serverType)
        {
            return BuildTestUri(serverType, hint: null);
        }

        internal static Uri BuildTestUri(ServerType serverType, string hint)
        {
            // Assume status messages are enabled for Kestrel and disabled for all other servers.
            return BuildTestUri(serverType, hint, statusMessagesEnabled: serverType == ServerType.Kestrel);
        }

        internal static Uri BuildTestUri(ServerType serverType, string hint, bool statusMessagesEnabled)
        {
            if (string.IsNullOrEmpty(hint))
            {
                if (serverType == ServerType.Kestrel && statusMessagesEnabled)
                {
                    // Most functional tests use this codepath and should directly bind to dynamic port "0" and scrape
                    // the assigned port from the status message, which should be 100% reliable since the port is bound
                    // once and never released.  Binding to dynamic port "0" on "localhost" (both IPv4 and IPv6) is not
                    // supported, so the port is only bound on "127.0.0.1" (IPv4).  If a test explicitly requires IPv6,
                    // it should provide a hint URL with "localhost" (IPv4 and IPv6) or "[::1]" (IPv6-only).
                    return new UriBuilder("http", "127.0.0.1", 0).Uri;
                }
                else
                {
                    // If the server type is not Kestrel, or status messages are disabled, there is no status message
                    // from which to scrape the assigned port, so the less reliable GetNextPort() must be used.  The
                    // port is bound on "localhost" (both IPv4 and IPv6), since this is supported when using a specific
                    // (non-zero) port.
                    return new UriBuilder("http", "localhost", GetNextPort()).Uri;
                }
            }
            else
            {
                var uriHint = new Uri(hint);
                if (uriHint.Port == 0)
                {
                    // Only a few tests use this codepath, so it's fine to use the less reliable GetNextPort() for simplicity.
                    // The tests using this codepath will be reviewed to see if they can be changed to directly bind to dynamic
                    // port "0" on "127.0.0.1" and scrape the assigned port from the status message (the default codepath).
                    return new UriBuilder(uriHint) { Port = GetNextPort() }.Uri;
                }
                else
                {
                    // If the hint contains a specific port, return it unchanged.
                    return uriHint;
                }
            }
        }

        // Copied from https://github.com/aspnet/KestrelHttpServer/blob/47f1db20e063c2da75d9d89653fad4eafe24446c/test/Microsoft.AspNetCore.Server.Kestrel.FunctionalTests/AddressRegistrationTests.cs#L508
        //
        // This method is an attempt to safely get a free port from the OS.  Most of the time,
        // when binding to dynamic port "0" the OS increments the assigned port, so it's safe
        // to re-use the assigned port in another process.  However, occasionally the OS will reuse
        // a recently assigned port instead of incrementing, which causes flaky tests with AddressInUse
        // exceptions.  This method should only be used when the application itself cannot use
        // dynamic port "0" (e.g. IISExpress).  Most functional tests using raw Kestrel
        // (with status messages enabled) should directly bind to dynamic port "0" and scrape 
        // the assigned port from the status message, which should be 100% reliable since the port
        // is bound once and never released.
        internal static int GetNextPort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}
