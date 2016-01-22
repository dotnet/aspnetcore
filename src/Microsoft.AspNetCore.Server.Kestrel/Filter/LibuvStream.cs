// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    public class LibuvStream : Stream
    {
        private readonly static Task<int> _initialCachedTask = Task.FromResult(0);

        private readonly SocketInput _input;
        private readonly ISocketOutput _output;

        private Task<int> _cachedTask = _initialCachedTask;

        public LibuvStream(SocketInput input, ISocketOutput output)
        {
            _input = input;
            _output = output;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // ValueTask uses .GetAwaiter().GetResult() if necessary
            // https://github.com/dotnet/corefx/blob/f9da3b4af08214764a51b2331f3595ffaf162abe/src/System.Threading.Tasks.Extensions/src/System/Threading/Tasks/ValueTask.cs#L156
            return ReadAsync(new ArraySegment<byte>(buffer, offset, count)).Result;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var task = ReadAsync(new ArraySegment<byte>(buffer, offset, count));

            if (task.IsCompletedSuccessfully)
            {
                if (_cachedTask.Result != task.Result)
                {
                    // Needs .AsTask to match Stream's Async method return types
                    _cachedTask = task.AsTask();
                }
            }
            else
            {
                // Needs .AsTask to match Stream's Async method return types
                _cachedTask = task.AsTask();
            }

            return _cachedTask;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ArraySegment<byte> segment;
            if (buffer != null)
            {
                segment = new ArraySegment<byte>(buffer, offset, count);
            }
            else
            {
                segment = default(ArraySegment<byte>);
            }
            _output.Write(segment);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            ArraySegment<byte> segment;
            if (buffer != null)
            {
                segment = new ArraySegment<byte>(buffer, offset, count);
            }
            else
            {
                segment = default(ArraySegment<byte>);
            }
            return _output.WriteAsync(segment, cancellationToken: token);
        }

        public override void Flush()
        {
            // No-op since writes are immediate.
        }

        private ValueTask<int> ReadAsync(ArraySegment<byte> buffer)
        {
            return _input.ReadAsync(buffer.Array, buffer.Offset, buffer.Count);
        }
    }
}
