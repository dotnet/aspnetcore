// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipelines;

namespace Microsoft.Extensions.WebSockets.Internal
{
    public static class PipelineReaderExtensions
    {
        public static ValueTask<ReadResult> ReadAtLeastAsync(this IPipelineReader input, int minimumRequiredBytes) => ReadAtLeastAsync(input, minimumRequiredBytes, CancellationToken.None);

        // TODO: Pull this up to Channels. We should be able to do it there without allocating a Task<T> in any case (rather than here where we can avoid allocation
        // only if the buffer is already ready and has enough data)
        public static ValueTask<ReadResult> ReadAtLeastAsync(this IPipelineReader input, int minimumRequiredBytes, CancellationToken cancellationToken)
        {
            var awaiter = input.ReadAsync(/* cancellationToken */);

            // Short-cut path!
            if (awaiter.IsCompleted)
            {
                // We have a buffer, is it big enough?
                var result = awaiter.GetResult();

                if (result.IsCompleted || result.Buffer.Length >= minimumRequiredBytes)
                {
                    return new ValueTask<ReadResult>(result);
                }

                // Buffer wasn't big enough, mark it as examined and continue to the "slow" path below
                input.Advance(
                    consumed: result.Buffer.Start,
                    examined: result.Buffer.End);
            }
            return new ValueTask<ReadResult>(ReadAtLeastSlowAsync(awaiter, input, minimumRequiredBytes, cancellationToken));
        }

        private static async Task<ReadResult> ReadAtLeastSlowAsync(ReadableBufferAwaitable awaitable, IPipelineReader input, int minimumRequiredBytes, CancellationToken cancellationToken)
        {
            var result = await awaitable;
            while (!result.IsCompleted && result.Buffer.Length < minimumRequiredBytes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                input.Advance(
                    consumed: result.Buffer.Start,
                    examined: result.Buffer.End);
                result = await input.ReadAsync(/* cancelToken */);
            }
            return result;
        }
    }
}
