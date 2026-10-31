// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.Common;

public static class TestPortHelper
{
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
    public static int GetNextPort()
    {
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)socket.LocalEndPoint).Port;
        }
    }

    // IIS Express preregisteres 44300-44399 ports with SSL bindings.
    // So some tests always have to use ports in this range, and we can't rely on OS-allocated ports without a whole lot of ceremony around
    // creating self-signed certificates and registering SSL bindings with HTTP.sys
    public static int GetNextSSLPort()
    {
        var next = 44300;
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            while (true)
            {
                try
                {
                    var port = next++;
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                    return port;
                }
                catch (SocketException)
                {
                    // Retry unless exhausted
                    if (next > 44399)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
