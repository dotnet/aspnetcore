// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            _buffer = new HeaderField[maxSize / HeaderField.RfcOverhead];
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
                    throw new IndexOutOfRangeException();
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

            var entry = new HeaderField(name, value);
            _buffer[_insertIndex] = entry;
            _insertIndex = (_insertIndex + 1) % _buffer.Length;
            _size += entry.Length;
            _count++;
        }

        public void Resize(int maxSize)
        {
            if (maxSize > _maxSize)
            {
                var newBuffer = new HeaderField[maxSize / HeaderField.RfcOverhead];

                int headCount = Math.Min(_buffer.Length - _removeIndex, _count);
                int tailCount = _count - headCount;

                Array.Copy(_buffer, _removeIndex, newBuffer, 0, headCount);
                Array.Copy(_buffer, 0, newBuffer, headCount, tailCount);

                _buffer = newBuffer;
                _removeIndex = 0;
                _insertIndex = _count;
                _maxSize = maxSize;
            }
            else
            {
                _maxSize = maxSize;
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
                _removeIndex = (_removeIndex + 1) % _buffer.Length;
            }
        }
    }
}
