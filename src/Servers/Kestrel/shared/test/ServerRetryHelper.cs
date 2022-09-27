// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing;

public static class ServerRetryHelper
{
    private const int RetryCount = 20;

    /// <summary>
    /// Retry a func. Useful when a test needs an explicit port and you want to avoid port conflicts.
    /// </summary>
    public static async Task BindPortsWithRetry(Func<int, Task> retryFunc, ILogger logger)
    {
        var retryCount = 0;

        // Add a random number to starting port to reduce chance of conflicts because of multiple tests using this retry.
        var nextPortAttempt = 5000 + Random.Shared.Next(500);

        while (true)
        {
            // Find a port that's available for TCP and UDP. Start with port 5000 and search upwards from there.
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

        var unavailableEndpoints = new List<IPEndPoint>();

        var properties = IPGlobalProperties.GetIPGlobalProperties();

        // Ignore active connections
        AddEndpoints(startingPort, unavailableEndpoints, properties.GetActiveTcpConnections().Select(c => c.LocalEndPoint));

        // Ignore active tcp listners
        AddEndpoints(startingPort, unavailableEndpoints, properties.GetActiveTcpListeners());

        // Ignore active UDP listeners
        AddEndpoints(startingPort, unavailableEndpoints, properties.GetActiveUdpListeners());

        logger.LogInformation($"Found {unavailableEndpoints.Count} unavailable endpoints.");

        for (var i = startingPort; i < ushort.MaxValue; i++)
        {
            var match = unavailableEndpoints.FirstOrDefault(ep => ep.Port == i);
            if (match == null)
            {
                logger.LogInformation($"Port {i} free.");
                return i;
            }
            else
            {
                logger.LogInformation($"Port {i} in use. End point: {match}");
            }
        }

        throw new Exception($"Couldn't find a free port after {startingPort}.");

        static void AddEndpoints(int startingPort, List<IPEndPoint> endpoints, IEnumerable<IPEndPoint> activeEndpoints)
        {
            foreach (IPEndPoint endpoint in activeEndpoints)
            {
                if (endpoint.Port >= startingPort)
                {
                    endpoints.Add(endpoint);
                }
            }
        }
    }
}
