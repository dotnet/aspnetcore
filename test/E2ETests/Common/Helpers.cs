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
                    logger.WriteWarning("Retry count {0}..", retry + 1);
                    retryBlock();
                    break; //Went through successfully
                }
                catch (AggregateException exception)
                {
                    if (exception.InnerException is HttpRequestException || exception.InnerException is WebException)
                    {
                        logger.WriteWarning("Failed to complete the request.", exception);
                        var waitTimeInMilliSeconds = (RunningOnMono ? 6 : 1) * 1000;
                        Thread.Sleep(waitTimeInMilliSeconds); //Wait for a second before retry
                    }
                }
            }
        }
    }
}