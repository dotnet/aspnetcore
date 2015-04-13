using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Framework.Logging;

namespace DeploymentHelpers
{
    public class RetryHelper
    {
        public static void RetryRequest(Func<HttpResponseMessage> retryBlock, ILogger logger, int retryCount = 12)
        {
            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
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
                            logger.LogWarning("Failed to complete the request : {0}.", exception.Message);
                            Thread.Sleep(7 * 1000); //Wait for a while before retry.
                        }
                    }
                }
            }
        }
    }
}