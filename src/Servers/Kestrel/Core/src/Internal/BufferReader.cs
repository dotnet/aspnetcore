// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal ref struct BufferReader
    {
        private ReadOnlySpan<byte> _currentSpan;
        private int _index;

        private ReadOnlySequence<byte> _sequence;
        private SequencePosition _currentSequencePosition;
        private SequencePosition _nextSequencePosition;

        private int _consumedBytes;
        private bool _end;

        public BufferReader(ReadOnlySequence<byte> buffer)
        {
            _end = false;
            _index = 0;
            _consumedBytes = 0;
            _sequence = buffer;
            _currentSequencePosition = _sequence.Start;
            _nextSequencePosition = _currentSequencePosition;
            _currentSpan = ReadOnlySpan<byte>.Empty;
            MoveNext();
        }

        public bool End => _end;

        public int CurrentSegmentIndex => _index;

        public SequencePosition Position => _sequence.GetPosition(_index, _currentSequencePosition);

        public ReadOnlySpan<byte> CurrentSegment => _currentSpan;

        public ReadOnlySpan<byte> UnreadSegment => _currentSpan.Slice(_index);

        public int ConsumedBytes => _consumedBytes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Peek()
        {
            if (_end)
            {
                return -1;
            }
            return _currentSpan[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read()
        {
            if (_end)
            {
                return -1;
            }

            var value = _currentSpan[_index];
            _index++;
            _consumedBytes++;

            if (_index >= _currentSpan.Length)
            {
                MoveNext();
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MoveNext()
        {
            var previous = _nextSequencePosition;
            while (_sequence.TryGet(ref _nextSequencePosition, out var memory, true))
            {
                _currentSequencePosition = previous;
                _currentSpan = memory.Span;
                _index = 0;
                if (_currentSpan.Length > 0)
                {
                    return;
                }
            }
            _end = true;
        }

        public void Advance(int byteCount)
        {
            if (byteCount < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }

            _consumedBytes += byteCount;

            while (!_end && byteCount > 0)
            {
                if ((_index + byteCount) < _currentSpan.Length)
                {
                    _index += byteCount;
                    byteCount = 0;
                    break;
                }

                var remaining = (_currentSpan.Length - _index);

                _index += remaining;
                byteCount -= remaining;

                MoveNext();
            }

            if (byteCount > 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }
        }
    }
}
