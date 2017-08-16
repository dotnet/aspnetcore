// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    public class DynamicTable
    {
        private readonly HeaderField[] _buffer;
        private int _maxSize = 4096;
        private int _size;
        private int _count;
        private int _insertIndex;
        private int _removeIndex;

        public DynamicTable(int maxSize)
        {
            _buffer = new HeaderField[maxSize];
            _maxSize = maxSize;
        }

        public int Count => _count;

        public int Size => _size;

        public HeaderField this[int index]
        {
            get
            {
                if (index >= _count)
                {
                    throw new IndexOutOfRangeException();
                }

                return _buffer[_insertIndex == 0 ? _buffer.Length - 1 : _insertIndex - index - 1];
            }
        }

        public void Insert(string name, string value)
        {
            var entrySize = name.Length + value.Length + 32;
            EnsureSize(_maxSize - entrySize);

            if (_maxSize < entrySize)
            {
                throw new InvalidOperationException($"Unable to add entry of size {entrySize} to dynamic table of size {_maxSize}.");
            }

            _buffer[_insertIndex] = new HeaderField(name, value);
            _insertIndex = (_insertIndex + 1) % _buffer.Length;
            _size += entrySize;
            _count++;
        }

        public void Resize(int maxSize)
        {
            _maxSize = maxSize;
            EnsureSize(_maxSize);
        }

        public void EnsureSize(int size)
        {
            while (_count > 0 && _size > size)
            {
                _size -= _buffer[_removeIndex].Name.Length + _buffer[_removeIndex].Value.Length + 32;
                _count--;
                _removeIndex = (_removeIndex + 1) % _buffer.Length;
            }
        }
    }
}
