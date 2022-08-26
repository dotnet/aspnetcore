// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing;

public static class ServerRetryHelper
{
    private const int RetryCount = 20;

    /// <summary>
    /// Retry a func. Useful when a test needs an explicit port and you want to avoid port conflicts.
    /// </summary>
    /// <summary>
    /// Retry a func. Useful when a test needs an explicit port and you want to avoid port conflicts.
    /// </summary>
    public static async Task BindPortsWithRetry(Func<int, Task> retryFunc, ILogger logger)
    {
        var retryCount = 0;
        var nextPortAttempt = 5000;

        while (true)
        {
            // Approx dynamic port range on Windows and Linux.
            var port = GetAvailablePort(nextPortAttempt, logger);

            try
            {
                await retryFunc(port);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                nextPortAttempt = port + 1;

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

    private static int GetAvailablePort(int startingPort, ILogger logger)
    {
        logger.LogInformation($"Searching for free port starting at {startingPort}.");

        var portArray = new List<int>();

        var properties = IPGlobalProperties.GetIPGlobalProperties();

        // Ignore active connections
        var connections = properties.GetActiveTcpConnections();
        portArray.AddRange(from n in connections
                           where n.LocalEndPoint.Port >= startingPort
                           select n.LocalEndPoint.Port);

        // Ignore active tcp listners
        var endPoints = properties.GetActiveTcpListeners();
        portArray.AddRange(from n in endPoints
                           where n.Port >= startingPort
                           select n.Port);

        // Ignore active UDP listeners
        endPoints = properties.GetActiveUdpListeners();
        portArray.AddRange(from n in endPoints
                           where n.Port >= startingPort
                           select n.Port);

        portArray.Sort();

        for (var i = startingPort; i < ushort.MaxValue; i++)
        {
            if (!portArray.Contains(i))
            {
                logger.LogInformation($"Port {i} free.");
                return i;
            }
            else
            {
                logger.LogInformation($"Port {i} in use.");
            }
        }

        throw new Exception($"Couldn't find a free port after {startingPort}.");
    }
}
