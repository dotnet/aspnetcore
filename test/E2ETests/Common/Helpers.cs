using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Framework.Logging;

namespace E2ETests
{
    public class Helpers
    {
        public static bool RunningOnMono
        {
            get
            {
                return Type.GetType("Mono.Runtime") != null;
            }
        }

        public static void Retry(Action retryBlock, ILogger logger, int retryCount = 7)
        {
            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    logger.LogWarning("Retry count {retryCount}..", retry + 1);
                    retryBlock();
                    break; //Went through successfully
                }
                catch (AggregateException exception)
                {
                    if (exception.InnerException is HttpRequestException
#if DNX451
                        || exception.InnerException is WebException
#endif
                        )
                    {
                        logger.LogWarning("Failed to complete the request.", exception);
                        Thread.Sleep(7 * 1000); //Wait for a while before retry.
                    }
                }
            }
        }
    }
}
