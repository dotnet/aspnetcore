// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class StreamSocketOutput : ISocketOutput
    {
        private static readonly Task _emptyTask = Task.FromResult<object>(null);

        private readonly Stream _outputStream;

        public StreamSocketOutput(Stream outputStream)
        {
            _outputStream = outputStream;
        }

        void ISocketOutput.Write(ArraySegment<byte> buffer, bool immediate)
        {
            _outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
        }

        Task ISocketOutput.WriteAsync(ArraySegment<byte> buffer, bool immediate, CancellationToken cancellationToken)
        {
            // TODO: Use _outputStream.WriteAsync
            _outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
            return _emptyTask;
        }
    }
}
