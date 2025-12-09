// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Http.HPack
{
    internal sealed class DynamicTable
    {
        private HeaderField[] _buffer;
        private int _maxSize;
        private int _size;
        private int _count;
        private int _insertIndex;
        private int _removeIndex;

        public DynamicTable(int maxSize)
        {
            _buffer = [];
            _maxSize = maxSize;
        }

        public int Count => _count;

        public int Size => _size;

        public int MaxSize => _maxSize;

        public ref readonly HeaderField this[int index]
        {
            get
            {
                if (index >= _count)
                {
#pragma warning disable CA2201 // Do not raise reserved exception types
                    // Helpful to act like static table (array)
                    throw new IndexOutOfRangeException();
#pragma warning restore CA2201
                }

                index = _insertIndex - index - 1;

                if (index < 0)
                {
                    // _buffer is circular; wrap the index back around.
                    index += _buffer.Length;
                }

                return ref _buffer[index];
            }
        }

        public void Insert(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            Insert(staticTableIndex: null, name, value);
        }

        public void Insert(int? staticTableIndex, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            int entryLength = HeaderField.GetLength(name.Length, value.Length);
            EnsureAvailable(entryLength);

            if (entryLength > _maxSize)
            {
                // http://httpwg.org/specs/rfc7541.html#rfc.section.4.4
                // It is not an error to attempt to add an entry that is larger than the maximum size;
                // an attempt to add an entry larger than the maximum size causes the table to be emptied
                // of all existing entries and results in an empty table.
                return;
            }

            // Ensure that we have at least one slot available.
            if (_count == _buffer.Length)
            {
                int maxCapacity = _maxSize / HeaderField.RfcOverhead;
                Debug.Assert(_count + 1 <= maxCapacity);

                // Double the size of the current buffer, starting with at least 16 entries.
                int newBufferSize = Math.Min(Math.Max(16, _buffer.Length * 2), maxCapacity);
                Debug.Assert(newBufferSize > _count);

                var newBuffer = new HeaderField[newBufferSize];

                int headCount = Math.Min(_buffer.Length - _removeIndex, _count);
                int tailCount = _count - headCount;

                Array.Copy(_buffer, _removeIndex, newBuffer, 0, headCount);
                Array.Copy(_buffer, 0, newBuffer, headCount, tailCount);

                _buffer = newBuffer;
                _removeIndex = 0;
                _insertIndex = _count;
            }

            var entry = new HeaderField(staticTableIndex, name, value);
            _buffer[_insertIndex] = entry;

            if (++_insertIndex == _buffer.Length)
            {
                _insertIndex = 0;
            }

            _size += entry.Length;
            _count++;
        }

        public void UpdateMaxSize(int maxSize)
        {
            int previousMax = _maxSize;
            _maxSize = maxSize;

            if (maxSize < previousMax)
            {
                EnsureAvailable(0);
            }
        }

        private void EnsureAvailable(int available)
        {
            while (_count > 0 && _maxSize - _size < available)
            {
                ref HeaderField field = ref _buffer[_removeIndex];
                _size -= field.Length;
                field = default;

                _count--;

                if (++_removeIndex == _buffer.Length)
                {
                    _removeIndex = 0;
                }
            }
        }
    }
}
