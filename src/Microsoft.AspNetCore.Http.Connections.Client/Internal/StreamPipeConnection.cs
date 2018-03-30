// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Pipelines
{
    internal class StreamPipeConnection
    {
        public static PipeReader CreateReader(PipeOptions options, Stream stream)
        {
            if (!stream.CanRead)
            {
                throw new NotSupportedException();
            }

            var pipe = new Pipe(options);
            _ = stream.CopyToEndAsync(pipe.Writer);

            return pipe.Reader;
        }
    }
}
