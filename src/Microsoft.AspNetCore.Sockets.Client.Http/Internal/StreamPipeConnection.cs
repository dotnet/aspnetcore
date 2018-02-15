// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.IO.Pipelines
{
    internal class StreamPipeConnection : IDuplexPipe
    {
        public StreamPipeConnection(PipeOptions options, Stream stream)
        {
            Input = CreateReader(options, stream);
            Output = CreateWriter(options, stream);
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public void Dispose()
        {
            Input.Complete();
            Output.Complete();
        }

        public static PipeReader CreateReader(PipeOptions options, Stream stream, CancellationToken cancellationToken = default)
        {
            if (!stream.CanRead)
            {
                throw new NotSupportedException();
            }

            var pipe = new Pipe(options);
            var ignore = stream.CopyToEndAsync(pipe.Writer, cancellationToken);

            return pipe.Reader;
        }

        public static PipeWriter CreateWriter(PipeOptions options, Stream stream)
        {
            if (!stream.CanWrite)
            {
                throw new NotSupportedException();
            }

            var pipe = new Pipe(options);
            var ignore = pipe.Reader.CopyToEndAsync(stream);

            return pipe.Writer;
        }
    }
}
