// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Internal
{
    internal sealed class MemoryBufferWriter : Stream, IBufferWriter<byte>
    {
        [ThreadStatic]
        private static MemoryBufferWriter _cachedInstance;

#if DEBUG
        private bool _inUse;
#endif

        private readonly int _minimumSegmentSize;
        private int _bytesWritten;

        private List<byte[]> _fullSegments;
        private byte[] _currentSegment;
        private int _position;

        public MemoryBufferWriter(int minimumSegmentSize = 4096)
        {
            _minimumSegmentSize = minimumSegmentSize;
        }

        public override long Length => _bytesWritten;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public static MemoryBufferWriter Get()
        {
            var writer = _cachedInstance;
            if (writer == null)
            {
                writer = new MemoryBufferWriter();
            }
            else
            {
                // Taken off the thread static
                _cachedInstance = null;
            }
#if DEBUG
            if (writer._inUse)
            {
                throw new InvalidOperationException("The reader wasn't returned!");
            }

            writer._inUse = true;
#endif

            return writer;
        }

        public static void Return(MemoryBufferWriter writer)
        {
            _cachedInstance = writer;
#if DEBUG
            writer._inUse = false;
#endif
            writer.Reset();
        }

        public void Reset()
        {
            if (_fullSegments != null)
            {
                for (var i = 0; i < _fullSegments.Count; i++)
                {
                    ArrayPool<byte>.Shared.Return(_fullSegments[i]);
                }

                _fullSegments.Clear();
            }

            if (_currentSegment != null)
            {
                ArrayPool<byte>.Shared.Return(_currentSegment);
                _currentSegment = null;
            }

            _bytesWritten = 0;
            _position = 0;
        }

        public void Advance(int count)
        {
            _bytesWritten += count;
            _position += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);

            return _currentSegment.AsMemory(_position, _currentSegment.Length - _position);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);

            return _currentSegment.AsSpan(_position, _currentSegment.Length - _position);
        }

        public void CopyTo(IBufferWriter<byte> destination)
        {
            if (_fullSegments != null)
            {
                // Copy full segments
                var count = _fullSegments.Count;
                for (var i = 0; i < count; i++)
                {
                    destination.Write(_fullSegments[i]);
                }
            }

            destination.Write(_currentSegment.AsSpan(0, _position));
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (_fullSegments == null)
            {
                // There is only one segment so write without async
                return destination.WriteAsync(_currentSegment, 0, _position);
            }

            return CopyToSlowAsync(destination);
        }

        private void EnsureCapacity(int sizeHint)
        {
            // TODO: Use sizeHint
            if (_currentSegment != null && _position < _currentSegment.Length)
            {
                // We have capacity in the current segment
                return;
            }

            AddSegment();
        }

        private void AddSegment()
        {
            if (_currentSegment != null)
            {
                // We're adding a segment to the list
                if (_fullSegments == null)
                {
                    _fullSegments = new List<byte[]>();
                }

                _fullSegments.Add(_currentSegment);
            }

            _currentSegment = ArrayPool<byte>.Shared.Rent(_minimumSegmentSize);
            _position = 0;
        }

        private async Task CopyToSlowAsync(Stream destination)
        {
            if (_fullSegments != null)
            {
                // Copy full segments                
                var count = _fullSegments.Count;
                for (var i = 0; i < count; i++)
                {
                    var segment = _fullSegments[i];
                    await destination.WriteAsync(segment, 0, segment.Length);
                }
            }

            await destination.WriteAsync(_currentSegment, 0, _position);
        }

        public byte[] ToArray()
        {
            if (_currentSegment == null)
            {
                return Array.Empty<byte>();
            }

            var result = new byte[_bytesWritten];

            var totalWritten = 0;

            if (_fullSegments != null)
            {
                // Copy full segments
                var count = _fullSegments.Count;
                for (var i = 0; i < count; i++)
                {
                    var segment = _fullSegments[i];
                    segment.CopyTo(result, totalWritten);
                    totalWritten += segment.Length;
                }
            }

            // Copy current incomplete segment
            _currentSegment.AsSpan(0, _position).CopyTo(result.AsSpan(totalWritten));

            return result;
        }

        public void CopyTo(Span<byte> span)
        {
            Debug.Assert(span.Length >= _bytesWritten);

            if (_currentSegment == null)
            {
                return;
            }

            var totalWritten = 0;

            if (_fullSegments != null)
            {
                // Copy full segments
                var count = _fullSegments.Count;
                for (var i = 0; i < count; i++)
                {
                    var segment = _fullSegments[i];
                    segment.AsSpan().CopyTo(span.Slice(totalWritten));
                    totalWritten += segment.Length;
                }
            }

            // Copy current incomplete segment
            _currentSegment.AsSpan(0, _position).CopyTo(span.Slice(totalWritten));

            Debug.Assert(_bytesWritten == totalWritten + _position);
        }

        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void WriteByte(byte value)
        {
            if (_currentSegment != null && (uint)_position < (uint)_currentSegment.Length)
            {
                _currentSegment[_position] = value;
            }
            else
            {
                AddSegment();
                _currentSegment[0] = value;
            }

            _position++;
            _bytesWritten++;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var position = _position;
            if (_currentSegment != null && position < _currentSegment.Length - count)
            {
                Buffer.BlockCopy(buffer, offset, _currentSegment, position, count);

                _position = position + count;
                _bytesWritten += count;
            }
            else
            {
                BuffersExtensions.Write(this, buffer.AsSpan(offset, count));
            }
        }

#if NETCOREAPP2_1
        public override void Write(ReadOnlySpan<byte> span)
        {
            if (_currentSegment != null && span.TryCopyTo(_currentSegment.AsSpan(_position)))
            {
                _position += span.Length;
                _bytesWritten += span.Length;
            }
            else
            {
                BuffersExtensions.Write(this, span);
            }
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reset();
            }
        }
    }
}