// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.VersionTools.Util
{
    public class ExponentialRetry
    {
        private Random _random = new Random();

        public int MaxAttempts { get; set; } = 10;

        /// <summary>
        /// Base, in seconds, raised to the power of the number of retries so far.
        /// </summary>
        public double DelayBase { get; set; } = 6;

        /// <summary>
        /// A constant, in seconds, added to (base^retries) to find the delay before retrying.
        /// 
        /// The default is -1 to make the first retry instant, because ((base^0)-1) == 0.
        /// </summary>
        public double DelayConstant { get; set; } = -1;

        public double MinRandomFactor { get; set; } = 0.5;
        public double MaxRandomFactor { get; set; } = 1.0;

        public CancellationToken DefaultCancellationToken { get; set; } = CancellationToken.None;

        public Task<bool> RunAsync(Func<int, Task<bool>> actionSuccessfulAsync)
        {
            return RunAsync(actionSuccessfulAsync, DefaultCancellationToken);
        }

        public async Task<bool> RunAsync(
            Func<int, Task<bool>> actionSuccessfulAsync,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < MaxAttempts; i++)
            {
                string attempt = $"Attempt {i + 1}/{MaxAttempts}";
                Trace.TraceInformation(attempt);

                if (await actionSuccessfulAsync(i))
                {
                    return true;
                }

                double randomFactor =
                    _random.NextDouble() * (MaxRandomFactor - MinRandomFactor) + MinRandomFactor;

                TimeSpan delay = TimeSpan.FromSeconds(
                    (Math.Pow(DelayBase, i) + DelayConstant) * randomFactor);

                Trace.TraceInformation($"{attempt} failed. Waiting {delay} before next try.");

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            return false;
        }
    }
}
