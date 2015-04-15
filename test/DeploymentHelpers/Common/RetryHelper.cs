using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Framework.Logging;

namespace DeploymentHelpers
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
        public static void RetryRequest(
            Func<HttpResponseMessage> retryBlock,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken),
            int retryCount = 60)
        {
            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    logger.LogWarning("Retry count {retryCount}..", retry + 1);
                    var response = retryBlock();

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        // Automatically retry on 503. May be application is still booting.
                        logger.LogWarning("Retrying a service unavailable error.");
                        continue;
                    }

                    break; //Went through successfully
                }
                catch (AggregateException exception)
                {
                    if (retry == retryCount - 1)
                    {
                        throw;
                    }
                    else
                    {
                        if (exception.InnerException is HttpRequestException
#if DNX451
                        || exception.InnerException is System.Net.WebException
#endif
                        )
                        {
                            logger.LogWarning("Failed to complete the request : {0}.", exception.InnerException.Message);
                            Thread.Sleep(1 * 1000); //Wait for a while before retry.
                        }
                    }
                }
            }
        }
    }
}