// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Microsoft.Extensions.WebSockets.Internal
{
    public static class PipeReaderExtensions
    {
        // TODO: Pull this up to Channels. We should be able to do it there without allocating a Task<T> in any case (rather than here where we can avoid allocation
        // only if the buffer is already ready and has enough data)
        public static async ValueTask<ReadResult> ReadAtLeastAsync(this IPipeReader input, int minimumRequiredBytes)
        {
            var awaiter = input.ReadAsync(/* cancellationToken */);

            // Short-cut path!
            ReadResult result;
            if (awaiter.IsCompleted)
            {
                // We have a buffer, is it big enough?
                result = awaiter.GetResult();

                if (result.IsCompleted || result.Buffer.Length >= minimumRequiredBytes)
                {
                    return result;
                }

                // Buffer wasn't big enough, mark it as examined and continue to the "slow" path below
                input.Advance(
                    consumed: result.Buffer.Start,
                    examined: result.Buffer.End);
            }
            result = await awaiter;
            while (!result.IsCancelled && !result.IsCompleted && result.Buffer.Length < minimumRequiredBytes)
            {
                input.Advance(
                    consumed: result.Buffer.Start,
                    examined: result.Buffer.End);
                result = await input.ReadAsync(/* cancelToken */);
            }
            return result;
        }
    }
}
