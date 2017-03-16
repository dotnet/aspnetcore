// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Sockets.Tests.Internal
{
    internal class ArrayOutput : IOutput
    {
        private IList<ArraySegment<byte>> _buffers = new List<ArraySegment<byte>>();

        private int _chunkSize;
        private byte[] _activeBuffer;
        private int _offset;

        public Span<byte> Buffer => _activeBuffer.Slice(_offset);

        public ArrayOutput(int chunkSize)
        {
            _chunkSize = chunkSize;
            AdvanceChunk();
        }

        public void Advance(int bytes)
        {
            // Determine the new location
            _offset += bytes;
            Debug.Assert(_offset <= _activeBuffer.Length, "How did we write more data than we had space?");
        }

        public void Enlarge(int desiredBufferLength = 0)
        {
            if (desiredBufferLength == 0 || _activeBuffer.Length - _offset < desiredBufferLength)
            {
                AdvanceChunk();
            }
        }

        public byte[] ToArray()
        {
            var totalLength = _buffers.Sum(b => b.Count) + _offset;

            var arr = new byte[totalLength];

            int offset = 0;
            foreach (var buffer in _buffers)
            {
                System.Buffer.BlockCopy(buffer.Array, 0, arr, offset, buffer.Count);
                offset += buffer.Count;
            }

            if (_offset > 0)
            {
                System.Buffer.BlockCopy(_activeBuffer, 0, arr, offset, _offset);
            }

            return arr;
        }

        private void AdvanceChunk()
        {
            if (_activeBuffer != null)
            {
                _buffers.Add(new ArraySegment<byte>(_activeBuffer, 0, _offset));
            }

            _activeBuffer = new byte[_chunkSize];
            _offset = 0;
        }
    }
}
