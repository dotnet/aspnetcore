// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace TriageBuildFailures
{
    internal static class RetryHelpers
    {
        private static int TotalRetriesUsed;

        public static int GetTotalRetriesUsed()
        {
            return TotalRetriesUsed;
        }

        public static async Task RetryAsync(Func<Task> action, IReporter reporter)
        {
            Exception firstException = null;

            var retriesRemaining = 10;
            while (retriesRemaining > 0)
            {
                try
                {
                    await action();
                    break;
                }
                catch (Exception e)
                {
                    firstException = firstException ?? e;
                    reporter.Output($"Exception thrown! {e.Message}");
                    reporter.Output($"Waiting 1 minute to retry ({retriesRemaining} left)...");
                    await Task.Delay(1 * 60 * 1000);
                    retriesRemaining--;
                    TotalRetriesUsed++;
                }
            }
            throw new InvalidOperationException("Max exception retries reached, giving up.");
        }

        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, IReporter reporter)
        {
            Exception firstException = null;

            var retriesRemaining = 10;
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
                    reporter.Output($"Waiting 1 minute to retry ({retriesRemaining} left)...");
                    await Task.Delay(1 * 60 * 1000);
                    retriesRemaining--;
                    TotalRetriesUsed++;
                }
            }
            throw new InvalidOperationException("Max exception retries reached, giving up.");
        }
    }
}
