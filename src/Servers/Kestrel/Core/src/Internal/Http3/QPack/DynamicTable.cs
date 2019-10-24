// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    //   The size of the dynamic table is the sum of the size of its entries.
    //   The size of an entry is the sum of its name's length in bytes (as
    //   defined in Section 4.1.2), its value's length in bytes, and 32.

    internal class DynamicTable
    {
        private HeaderField[] _buffer;
        private int _maxSize;
        private int _size;
        private int _count;
        private int _insertIndex;
        private int _removeIndex;

        // The encoder sends a Set Dynamic Table Capacity
        // instruction(Section 4.3.1) with a non-zero capacity to begin using
        // the dynamic table.
        public DynamicTable(int maxSize)
        {
            // TODO confirm this.
            _buffer = new HeaderField[maxSize / HeaderField.RfcOverhead];
            _maxSize = maxSize;
        }

        public int Count => _count;

        public int Size => _size;

        public int MaxSize => _maxSize;

        public HeaderField this[int index]
        {
            get
            {
                // relative index 
                if (index >= _count)
                {
                    throw new IndexOutOfRangeException();
                }
                // TODO I think this was updated already outside of this class.
                return _buffer[_insertIndex - index];
            }
        }

        // TODO
        public void Insert(Span<byte> name, Span<byte> value)
        {
            var entryLength = HeaderField.GetLength(name.Length, value.Length);
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

        // TODO 
        public void Resize(int maxSize)
        {
            if (maxSize > _maxSize)
            {
                var newBuffer = new HeaderField[maxSize / HeaderField.RfcOverhead];

                for (var i = 0; i < Count; i++)
                {
                    newBuffer[i] = _buffer[i];
                }

                _buffer = newBuffer;
                _maxSize = maxSize;
            }
            else
            {
                _maxSize = maxSize;
                EnsureAvailable(0);
            }
        }

        // TODO 
        private void EnsureAvailable(int available)
        {
            while (_count > 0 && _maxSize - _size < available)
            {
                _size -= _buffer[_removeIndex].Length;
                _count--;
                _removeIndex = (_removeIndex + 1) % _buffer.Length;
            }
        }

        // TODO 
        internal void Duplicate(int index)
        {
            throw new NotImplementedException();
        }
    }
}
