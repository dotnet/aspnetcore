// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal sealed class LoggingStream : Stream
    {
        private readonly Stream _inner;
        private readonly ILogger _logger;

        public LoggingStream(Stream inner, ILogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public override bool CanRead
        {
            get
            {
                return _inner.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _inner.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _inner.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return _inner.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _inner.Position;
            }

            set
            {
                _inner.Position = value;
            }
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _inner.Read(buffer, offset, count);
            Log("Read", new ReadOnlySpan<byte>(buffer, offset, read));
            return read;
        }

        public override int Read(Span<byte> destination)
        {
            int read = _inner.Read(destination);
            Log("Read", destination.Slice(0, read));
            return read;
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
            Log("ReadAsync", new ReadOnlySpan<byte>(buffer, offset, read));
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            int read = await _inner.ReadAsync(destination, cancellationToken);
            Log("ReadAsync", destination.Span.Slice(0, read));
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Log("Write", new ReadOnlySpan<byte>(buffer, offset, count));
            _inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            Log("Write", source);
            _inner.Write(source);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Log("WriteAsync", new ReadOnlySpan<byte>(buffer, offset, count));
            return _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            Log("WriteAsync", source.Span);
            return _inner.WriteAsync(source, cancellationToken);
        }

        private void Log(string method, ReadOnlySpan<byte> buffer)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            var builder = new StringBuilder($"{method}[{buffer.Length}] ");

            // Write the hex
            for (int i = 0; i < buffer.Length; i++)
            {
                builder.Append(buffer[i].ToString("X2"));
                builder.Append(" ");
            }
            builder.AppendLine();
            builder.Append("{0}");

            var rawDataBuilder = new StringBuilder();
            // Write the bytes as if they were ASCII
            for (int i = 0; i < buffer.Length; i++)
            {
                var bufferChar = (char)buffer[i];
                if (Char.IsControl(bufferChar))
                {
                    rawDataBuilder.Append("\\x");
                    rawDataBuilder.Append(buffer[i].ToString("X2"));
                    continue;
                }
                rawDataBuilder.Append(bufferChar);
            }

            _logger.LogDebug(builder.ToString(), rawDataBuilder.ToString());
        }

        // The below APM methods call the underlying Read/WriteAsync methods which will still be logged.
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToApm.End(asyncResult);
        }
    }
}
