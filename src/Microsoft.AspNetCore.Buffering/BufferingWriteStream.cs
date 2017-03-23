// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Buffering
{
    internal class BufferingWriteStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly MemoryStream _buffer = new MemoryStream();
        private bool _isBuffering = true;

        public BufferingWriteStream(Stream innerStream)
        {
            _innerStream = innerStream;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return _isBuffering; }
        }

        public override bool CanWrite
        {
            get { return _innerStream.CanWrite; }
        }

        public override long Length
        {
            get
            {
                if (_isBuffering)
                {
                    return _buffer.Length;
                }
                // May throw
                return _innerStream.Length;
            }
        }

        // Clear/Reset the buffer by setting Position, Seek, or SetLength to 0. Random access is not supported.
        public override long Position
        {
            get
            {
                if (_isBuffering)
                {
                    return _buffer.Position;
                }
                // May throw
                return _innerStream.Position;
            }
            set
            {
                if (_isBuffering)
                {
                    if (value != 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value, nameof(Position) + " can only be set to 0.");
                    }
                    _buffer.Position = value;
                    _buffer.SetLength(value);
                }
                else
                {
                    // May throw
                    _innerStream.Position = value;
                }
            }
        }

        // Clear/Reset the buffer by setting Position, Seek, or SetLength to 0. Random access is not supported.
        public override void SetLength(long value)
        {
            if (_isBuffering)
            {
                if (value != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, nameof(Length) + " can only be set to 0.");
                }
                _buffer.Position = value;
                _buffer.SetLength(value);
            }
            else
            {
                // May throw
                _innerStream.SetLength(value);
            }
        }

        // Clear/Reset the buffer by setting Position, Seek, or SetLength to 0. Random access is not supported.
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (_isBuffering)
            {
                if (origin != SeekOrigin.Begin)
                {
                    throw new ArgumentException(nameof(origin), nameof(Seek) + " can only be set to " + nameof(SeekOrigin.Begin) + ".");
                }
                if (offset != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), offset, nameof(Seek) + " can only be set to 0.");
                }
                _buffer.SetLength(offset);
                return _buffer.Seek(offset, origin);
            }
            // Try the inner stream instead, but this will usually fail.
            return _innerStream.Seek(offset, origin);
        }

        internal void DisableBuffering()
        {
            _isBuffering = false;
            if (_buffer.Length > 0)
            {
                Flush();
            }
        }

        internal Task DisableBufferingAsync(CancellationToken cancellationToken)
        {
            _isBuffering = false;
            if (_buffer.Length > 0)
            {
                return FlushAsync(cancellationToken);
            }
            return TaskCache.CompletedTask;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_isBuffering)
            {
                _buffer.Write(buffer, offset, count);
            }
            else
            {
                _innerStream.Write(buffer, offset, count);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_isBuffering)
            {
                return _buffer.WriteAsync(buffer, offset, count, cancellationToken);
            }
            else
            {
                return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }
#if NET46
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (_isBuffering)
            {
                return _buffer.BeginWrite(buffer, offset, count, callback, state);
            }
            else
            {
                return _innerStream.BeginWrite(buffer, offset, count, callback, state);
            }
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (_isBuffering)
            {
                _buffer.EndWrite(asyncResult);
            }
            else
            {
                _innerStream.EndWrite(asyncResult);
            }
        }
#elif NETSTANDARD1_3
#else
#error target frameworks need to be updated
#endif
        public override void Flush()
        {
            _isBuffering = false;
            if (_buffer.Length > 0)
            {
                _buffer.Seek(0, SeekOrigin.Begin);
                _buffer.CopyTo(_innerStream);
                _buffer.Seek(0, SeekOrigin.Begin);
                _buffer.SetLength(0);
            }
            _innerStream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            _isBuffering = false;
            if (_buffer.Length > 0)
            {
                _buffer.Seek(0, SeekOrigin.Begin);
                await _buffer.CopyToAsync(_innerStream, 1024 * 16, cancellationToken);
                _buffer.Seek(0, SeekOrigin.Begin);
                _buffer.SetLength(0);
            }
            await _innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("This Stream only supports Write operations.");
        }
    }
}
