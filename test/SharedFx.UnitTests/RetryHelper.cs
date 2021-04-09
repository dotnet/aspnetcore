// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, IReporter reporter)
        {
            Exception firstException = null;

            var retriesRemaining = 5;
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

                    retriesRemaining--;
                    reporter.Output($"Waiting {retryDelayInMinutes} minute(s) to retry ({retriesRemaining} left)...");

                    await Task.Delay(retryDelayInMinutes * 60 * 1000);

                    // Do exponential back-off, but limit it (1, 2, 4, 8, 15)
                    // With MaxRetryMinutes=15 and MaxRetries=5, this will delay a maximum of 30 minutes (1/3 job timeout).
                    retryDelayInMinutes = Math.Min(2 * retryDelayInMinutes, MaxRetryMinutes);
                }
            }
            throw new InvalidOperationException("Max exception retries reached, giving up.", firstException);
        }
    }
}
