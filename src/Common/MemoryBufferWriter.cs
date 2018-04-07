// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Internal
{
    internal sealed class MemoryBufferWriter : IBufferWriter<byte>
    {
        [ThreadStatic]
        private static MemoryBufferWriter _cachedInstance;

#if DEBUG
        private bool _inUse;
#endif

        private readonly int _segmentSize;
        private int _bytesWritten;

        private List<byte[]> _fullSegments;
        private byte[] _currentSegment;
        private int _position;

        public MemoryBufferWriter(int segmentSize = 2048)
        {
            _segmentSize = segmentSize;
        }

        public int Length => _bytesWritten;

        public static MemoryBufferWriter Get()
        {
            var writer = _cachedInstance;
            if (writer == null)
            {
                writer = new MemoryBufferWriter();
            }

            // Taken off the thread static
            _cachedInstance = null;
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
            // TODO: Use sizeHint
            if (_currentSegment == null)
            {
                _currentSegment = ArrayPool<byte>.Shared.Rent(_segmentSize);
                _position = 0;
            }
            else if (_position == _segmentSize)
            {
                if (_fullSegments == null)
                {
                    _fullSegments = new List<byte[]>();
                }
                _fullSegments.Add(_currentSegment);
                _currentSegment = ArrayPool<byte>.Shared.Rent(_segmentSize);
                _position = 0;
            }

            return _currentSegment.AsMemory(_position, _currentSegment.Length - _position);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return GetMemory(sizeHint).Span;
        }

        public Task CopyToAsync(Stream stream)
        {
            if (_fullSegments == null)
            {
                // There is only one segment so write without async
                return stream.WriteAsync(_currentSegment, 0, _position);
            }

            return CopyToSlowAsync(stream);
        }

        private async Task CopyToSlowAsync(Stream stream)
        {
            if (_fullSegments != null)
            {
                // Copy full segments
                for (var i = 0; i < _fullSegments.Count - 1; i++)
                {
                    await stream.WriteAsync(_fullSegments[i], 0, _segmentSize);
                }
            }

            await stream.WriteAsync(_currentSegment, 0, _position);
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
                for (var i = 0; i < _fullSegments.Count; i++)
                {
                    _fullSegments[i].CopyTo(result, totalWritten);

                    totalWritten += _segmentSize;
                }
            }

            // Copy current incomplete segment
            _currentSegment.AsSpan(0, _position).CopyTo(result.AsSpan(totalWritten));

            return result;
        }
    }
}