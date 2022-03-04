// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.Http2
{
    internal class PipeReaderFactory
    {
        private static readonly Action<object> _cancelReader = state => ((PipeReader)state).CancelPendingRead();

        public static PipeReader CreateFromStream(PipeOptions options, Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanRead)
            {
                throw new NotSupportedException();
            }

            var pipe = new Pipe(options);
            _ = CopyToAsync(stream, pipe, cancellationToken);

            return pipe.Reader;
        }

        private static async Task CopyToAsync(Stream stream, Pipe pipe, CancellationToken cancellationToken)
        {
            // We manually register for cancellation here in case the Stream implementation ignores it
            using (var registration = cancellationToken.Register(_cancelReader, pipe.Reader))
            {
                try
                {
                    await stream.CopyToAsync(new DuplexPipeStream(null, pipe.Writer), bufferSize: 4096, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Ignore the cancellation signal (the pipe reader is already wired up for cancellation when the token trips)
                }
                catch (Exception ex)
                {
                    pipe.Writer.Complete(ex);
                    return;
                }
                pipe.Writer.Complete();
            }
        }
    }
}
