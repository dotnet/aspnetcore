// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class RetryHelper
    {
        /// <summary>
        /// Retries every 1 sec for 60 times by default.
        /// </summary>
        /// <param name="retryBlock"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="retryCount"></param>
        public static async Task<HttpResponseMessage> RetryRequest(
            Func<Task<HttpResponseMessage>> retryBlock,
            ILogger logger,
            CancellationToken cancellationToken = default,
            int retryCount = 60)
        {
            for (var retry = 0; retry < retryCount; retry++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Failed to connect, retry canceled.");
                    throw new OperationCanceledException("Failed to connect, retry canceled.", cancellationToken);
                }

                try
                {
                    logger.LogWarning("Retry count {retryCount}..", retry + 1);
                    var response = await retryBlock().ConfigureAwait(false);

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        // Automatically retry on 503. May be application is still booting.
                        logger.LogWarning("Retrying a service unavailable error.");
                        continue;
                    }

                    return response; // Went through successfully
                }
                catch (Exception exception)
                {
                    if (retry == retryCount - 1)
                    {
                        logger.LogError(0, exception, "Failed to connect, retry limit exceeded.");
                        throw;
                    }
                    else
                    {
                        if (exception is HttpRequestException || exception is WebException)
                        {
                            logger.LogWarning("Failed to complete the request : {0}.", exception.Message);
                            await Task.Delay(1 * 1000); //Wait for a while before retry.
                        }
                    }
                }
            }

            logger.LogInformation("Failed to connect, retry limit exceeded.");
            throw new OperationCanceledException("Failed to connect, retry limit exceeded.");
        }

        public static void RetryOperation(
            Action retryBlock,
            Action<Exception> exceptionBlock,
            int retryCount = 3,
            int retryDelayMilliseconds = 0)
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    retryBlock();
                    break;
                }
                catch (Exception exception)
                {
                    exceptionBlock(exception);
                }

                Thread.Sleep(retryDelayMilliseconds);
            }
        }
    }
}
