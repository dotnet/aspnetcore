// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing;

public static class ServerRetryHelper
{
    /// <summary>
    /// Retry a func. Useful when a test needs an explicit port and you want to avoid port conflicts.
    /// </summary>
    public static async Task BindPortsWithRetry(Func<int, Task> retryFunc, ILogger logger)
    {
        var retryCount = 0;
        while (true)
        {
            // Approx dynamic port range on Windows and Linux.
            var randomPort = Random.Shared.Next(35000, 60000);

            try
            {
                await retryFunc(randomPort);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;

                if (retryCount >= 5)
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
}
