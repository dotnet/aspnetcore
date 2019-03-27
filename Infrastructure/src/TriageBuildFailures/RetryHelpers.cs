// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace TriageBuildFailures
{
    internal static class RetryHelpers
    {
        /// <summary>
        /// Constrain the exponential back-off to this many minutes.
        /// </summary>
        private const int MaxRetryMinutes = 15;

        private static int TotalRetriesUsed;

        public static int GetTotalRetriesUsed()
        {
            return TotalRetriesUsed;
        }

        public static async Task RetryAsync(Func<Task> action, IReporter reporter)
        {
            await RetryAsync<object>(
                async () =>
                {
                    await action();
                    return null;
                },
                reporter);
        }

        public static async Task<HttpResponseMessage> RetryHttpRequestAsync(HttpClient client, HttpMethod verb, Uri uri, IReporter reporter)
        {
            HttpResponseMessage firstResponse = null;

            var retriesRemaining = 10;
            var retryDelayInMinutes = 1;

            while (retriesRemaining > 0)
            {
                // Worst-case this could actually take 210 minutes if we get the max failures on both Exceptions and StatusCode,
                // but that seems very unlikely.
                var result = await RetryAsync(async () =>
                {
                    var request = new HttpRequestMessage(verb, uri);
                    return await client.SendAsync(request);
                }, reporter);

                if (result.IsSuccessStatusCode)
                {
                    return result;
                }
                else
                {
                    firstResponse = firstResponse ?? result;
                    reporter.Output($"Bad StatusCode {result.StatusCode} against {uri}");
                    reporter.Output($"Waiting {retryDelayInMinutes} minute(s) to retry ({retriesRemaining} left)...");

                    // Do exponential back-off, but limit it (1, 2, 4, 8, 15, 15, 15, ...)
                    // With MaxRetryMinutes=15 and MaxRetries=10, this will delay a maximum of 105 minutes
                    retryDelayInMinutes = Math.Min(2 * retryDelayInMinutes, MaxRetryMinutes);
                    retriesRemaining--;
                    TotalRetriesUsed++;
                }
            }

            // Give them the first failure in case they need to do something smart with it.
            return firstResponse;
        }

        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, IReporter reporter)
        {
            Exception firstException = null;

            var retriesRemaining = 10;
            var retryDelayInMinutes = 1;

            while (retriesRemaining > 0)
            {
                try
                {
                    return await action();
                }
                catch (Exception e)
                {
                    firstException = firstException ?? e;
                    reporter.Output($"Exception thrown! {e.Message}");
                    reporter.Output($"Waiting {retryDelayInMinutes} minute(s) to retry ({retriesRemaining} left)...");
                    await Task.Delay(retryDelayInMinutes * 60 * 1000);

                    // Do exponential back-off, but limit it (1, 2, 4, 8, 15, 15, 15, ...)
                    // With MaxRetryMinutes=15 and MaxRetries=10, this will delay a maximum of 105 minutes
                    retryDelayInMinutes = Math.Min(2 * retryDelayInMinutes, MaxRetryMinutes);
                    retriesRemaining--;
                    TotalRetriesUsed++;
                }
            }
            throw new InvalidOperationException("Max exception retries reached, giving up.", firstException);
        }
    }
}
