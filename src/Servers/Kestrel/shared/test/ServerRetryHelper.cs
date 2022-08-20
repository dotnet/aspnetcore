// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing;

public static class ServerRetryHelper
{
    private const int RetryCount = 10;

    /// <summary>
    /// Retry a func. Useful when a test needs an explicit port and you want to avoid port conflicts.
    /// </summary>
    public static async Task BindPortsWithRetry(Func<int, Task> retryFunc, ILogger logger)
    {
        var ports = GetFreePorts(RetryCount);

        var retryCount = 0;
        while (true)
        {

            try
            {
                await retryFunc(ports[retryCount]);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;

                if (retryCount >= RetryCount)
                {
                    throw;
                }
                else
                {
                    logger.LogError(ex, $"Error running test {retryCount}. Retrying.");
                }
            }
        }
    }

    private static int[] GetFreePorts(int count)
    {
        var sockets = new List<Socket>();

        for (var i = 0; i < count; i++)
        {
            // Find a port that's free by binding port 0.
            // Note that this port should be free when the test runs, but:
            // - Something else could steal it before the test uses it.
            // - UDP port with the same number could be in use.
            // For that reason, some retries should be available.
            var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            var listenSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listenSocket.Bind(ipEndPoint);

            sockets.Add(listenSocket);
        }

        // Ports are calculated upfront. Rebinding with port 0 could result the same port
        // being returned for each retry.
        var ports = sockets.Select(s => (IPEndPoint)s.LocalEndPoint).Select(ep => ep.Port).ToArray();

        foreach (var socket in sockets)
        {
            socket.Dispose();
        }

        return ports;
    }
}
