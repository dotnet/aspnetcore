// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public sealed class MemoryBufferWriter : IBufferWriter<byte>
    {
        [ThreadStatic]
        private static MemoryBufferWriter _cachedInstance;

#if DEBUG
        private bool _inUse;
#endif

        private readonly int _segmentSize;
        private int _bytesWritten;

        private List<byte[]> _segments;
        private int _position;

        private MemoryBufferWriter(int segmentSize = 2048)
        {
            _segmentSize = segmentSize;

            _segments = new List<byte[]>();
        }

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
            for (var i = 0; i < writer._segments.Count; i++)
            {
                ArrayPool<byte>.Shared.Return(writer._segments[i]);
            }
            writer._segments.Clear();
            writer._bytesWritten = 0;
            writer._position = 0;
        }

        public Memory<byte> CurrentSegment => _segments[_segments.Count - 1];

        public void Advance(int count)
        {
            _bytesWritten += count;
            _position += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            // TODO: Use sizeHint

            if (_segments.Count == 0 || _position == _segmentSize)
            {
                _segments.Add(ArrayPool<byte>.Shared.Rent(_segmentSize));
                _position = 0;
            }

            // Cache property access
            var currentSegment = CurrentSegment;
            return currentSegment.Slice(_position, currentSegment.Length - _position);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return GetMemory(sizeHint).Span;
        }

        public byte[] ToArray()
        {
            if (_segments.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var result = new byte[_bytesWritten];

            var totalWritten = 0;

            // Copy full segments
            for (var i = 0; i < _segments.Count - 1; i++)
            {
                _segments[i].AsMemory().CopyTo(result.AsMemory(totalWritten, _segmentSize));

                totalWritten += _segmentSize;
            }

            // Copy current incomplete segment
            CurrentSegment.Slice(0, _position).CopyTo(result.AsMemory(totalWritten, _position));

            return result;
        }
    }
}