// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Microsoft.AspNet.Server.Kestrel.Infrastructure
{
    public struct MemoryPoolIterator2
    {
        /// <summary>
        /// Array of "minus one" bytes of the length of SIMD operations on the current hardware. Used as an argument in the
        /// vector dot product that counts matching character occurrence.
        /// </summary>
        private static Vector<byte> _dotCount = new Vector<byte>(Byte.MaxValue);

        /// <summary>
        /// Array of negative numbers starting at 0 and continuing for the length of SIMD operations on the current hardware.
        /// Used as an argument in the vector dot product that determines matching character index.
        /// </summary>
        private static Vector<byte> _dotIndex = new Vector<byte>(Enumerable.Range(0, Vector<byte>.Count).Select(x => (byte)-x).ToArray());

        private MemoryPoolBlock2 _block;
        private int _index;

        public MemoryPoolIterator2(MemoryPoolBlock2 block)
        {
            _block = block;
            _index = _block?.Start ?? 0;
        }
        public MemoryPoolIterator2(MemoryPoolBlock2 block, int index)
        {
            _block = block;
            _index = index;
        }

        public bool IsDefault => _block == null;

        public bool IsEnd
        {
            get
            {
                if (_block == null)
                {
                    return true;
                }
                else if (_index < _block.End)
                {
                    return false;
                }
                else
                {
                    var block = _block.Next;
                    while (block != null)
                    {
                        if (block.Start < block.End)
                        {
                            return false; // subsequent block has data - IsEnd is false
                        }
                        block = block.Next;
                    }
                    return true;
                }
            }
        }

        public MemoryPoolBlock2 Block => _block;

        public int Index => _index;

        public int Take()
        {
            if (_block == null)
            {
                return -1;
            }
            else if (_index < _block.End)
            {
                return _block.Array[_index++];
            }

            var block = _block;
            var index = _index;
            while (true)
            {
                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    return block.Array[index];
                }
                else if (block.Next == null)
                {
                    return -1;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public int Peek()
        {
            if (_block == null)
            {
                return -1;
            }
            else if (_index < _block.End)
            {
                return _block.Array[_index];
            }
            else if (_block.Next == null)
            {
                return -1;
            }

            var block = _block.Next;
            var index = block.Start;
            while (true)
            {
                if (index < block.End)
                {
                    return block.Array[index];
                }
                else if (block.Next == null)
                {
                    return -1;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public int Seek(int char0)
        {
            if (IsDefault)
            {
                return -1;
            }

            var byte0 = (byte)char0;
            var vectorStride = Vector<byte>.Count;
            var ch0Vector = new Vector<byte>(byte0);

            var block = _block;
            var index = _index;
            var array = block.Array;
            while (true)
            {
                while (block.End == index)
                {
                    if (block.Next == null)
                    {
                        _block = block;
                        _index = index;
                        return -1;
                    }
                    block = block.Next;
                    index = block.Start;
                    array = block.Array;
                }
                while (block.End != index)
                {
                    var following = block.End - index;
                    if (following >= vectorStride)
                    {
                        var data = new Vector<byte>(array, index);
                        var ch0Equals = Vector.Equals(data, ch0Vector);
                        var ch0Count = Vector.Dot(ch0Equals, _dotCount);

                        if (ch0Count == 0)
                        {
                            index += vectorStride;
                            continue;
                        }
                        else if (ch0Count == 1)
                        {
                            _block = block;
                            _index = index + Vector.Dot(ch0Equals, _dotIndex);
                            return char0;
                        }
                        else
                        {
                            following = vectorStride;
                        }
                    }
                    while (following > 0)
                    {
                        if (block.Array[index] == byte0)
                        {
                            _block = block;
                            _index = index;
                            return char0;
                        }
                        following--;
                        index++;
                    }
                }
            }
        }

        public int Seek(int char0, int char1)
        {
            if (IsDefault)
            {
                return -1;
            }

            var byte0 = (byte)char0;
            var byte1 = (byte)char1;
            var vectorStride = Vector<byte>.Count;
            var ch0Vector = new Vector<byte>(byte0);
            var ch1Vector = new Vector<byte>(byte1);

            var block = _block;
            var index = _index;
            var array = block.Array;
            while (true)
            {
                while (block.End == index)
                {
                    if (block.Next == null)
                    {
                        _block = block;
                        _index = index;
                        return -1;
                    }
                    block = block.Next;
                    index = block.Start;
                    array = block.Array;
                }
                while (block.End != index)
                {
                    var following = block.End - index;
                    if (following >= vectorStride)
                    {
                        var data = new Vector<byte>(array, index);
                        var ch0Equals = Vector.Equals(data, ch0Vector);
                        var ch0Count = Vector.Dot(ch0Equals, _dotCount);
                        var ch1Equals = Vector.Equals(data, ch1Vector);
                        var ch1Count = Vector.Dot(ch1Equals, _dotCount);

                        if (ch0Count == 0 && ch1Count == 0)
                        {
                            index += vectorStride;
                            continue;
                        }
                        else if (ch0Count < 2 && ch1Count < 2)
                        {
                            var ch0Index = ch0Count == 1 ? Vector.Dot(ch0Equals, _dotIndex) : byte.MaxValue;
                            var ch1Index = ch1Count == 1 ? Vector.Dot(ch1Equals, _dotIndex) : byte.MaxValue;
                            if (ch0Index < ch1Index)
                            {
                                _block = block;
                                _index = index + ch0Index;
                                return char0;
                            }
                            else
                            {
                                _block = block;
                                _index = index + ch1Index;
                                return char1;
                            }
                        }
                        else
                        {
                            following = vectorStride;
                        }
                    }
                    while (following > 0)
                    {
                        var byteIndex = block.Array[index];
                        if (byteIndex == byte0)
                        {
                            _block = block;
                            _index = index;
                            return char0;
                        }
                        else if (byteIndex == byte1)
                        {
                            _block = block;
                            _index = index;
                            return char1;
                        }
                        following--;
                        index++;
                    }
                }
            }
        }

        public int Seek(int char0, int char1, int char2)
        {
            if (IsDefault)
            {
                return -1;
            }

            var byte0 = (byte)char0;
            var byte1 = (byte)char1;
            var byte2 = (byte)char2;
            var vectorStride = Vector<byte>.Count;
            var ch0Vector = new Vector<byte>(byte0);
            var ch1Vector = new Vector<byte>(byte1);
            var ch2Vector = new Vector<byte>(byte2);

            var block = _block;
            var index = _index;
            var array = block.Array;
            while (true)
            {
                while (block.End == index)
                {
                    if (block.Next == null)
                    {
                        _block = block;
                        _index = index;
                        return -1;
                    }
                    block = block.Next;
                    index = block.Start;
                    array = block.Array;
                }
                while (block.End != index)
                {
                    var following = block.End - index;
                    if (following >= vectorStride)
                    {
                        var data = new Vector<byte>(array, index);
                        var ch0Equals = Vector.Equals(data, ch0Vector);
                        var ch0Count = Vector.Dot(ch0Equals, _dotCount);
                        var ch1Equals = Vector.Equals(data, ch1Vector);
                        var ch1Count = Vector.Dot(ch1Equals, _dotCount);
                        var ch2Equals = Vector.Equals(data, ch2Vector);
                        var ch2Count = Vector.Dot(ch2Equals, _dotCount);

                        if (ch0Count == 0 && ch1Count == 0 && ch2Count == 0)
                        {
                            index += vectorStride;
                            continue;
                        }
                        else if (ch0Count < 2 && ch1Count < 2 && ch2Count < 2)
                        {
                            var ch0Index = ch0Count == 1 ? Vector.Dot(ch0Equals, _dotIndex) : byte.MaxValue;
                            var ch1Index = ch1Count == 1 ? Vector.Dot(ch1Equals, _dotIndex) : byte.MaxValue;
                            var ch2Index = ch2Count == 1 ? Vector.Dot(ch2Equals, _dotIndex) : byte.MaxValue;

                            int toReturn, toMove;
                            if (ch0Index < ch1Index)
                            {
                                if (ch0Index < ch2Index)
                                {
                                    toReturn = char0;
                                    toMove = ch0Index;
                                }
                                else
                                {
                                    toReturn = char2;
                                    toMove = ch2Index;
                                }
                            }
                            else
                            {
                                if (ch1Index < ch2Index)
                                {
                                    toReturn = char1;
                                    toMove = ch1Index;
                                }
                                else
                                {
                                    toReturn = char2;
                                    toMove = ch2Index;
                                }
                            }

                            _block = block;
                            _index = index + toMove;
                            return toReturn;
                        }
                        else
                        {
                            following = vectorStride;
                        }
                    }
                    while (following > 0)
                    {
                        var byteIndex = block.Array[index];
                        if (byteIndex == byte0)
                        {
                            _block = block;
                            _index = index;
                            return char0;
                        }
                        else if (byteIndex == byte1)
                        {
                            _block = block;
                            _index = index;
                            return char1;
                        }
                        else if (byteIndex == byte2)
                        {
                            _block = block;
                            _index = index;
                            return char2;
                        }
                        following--;
                        index++;
                    }
                }
            }
        }

        /// <summary>
        /// Save the data at the current location then move to the next available space.
        /// </summary>
        /// <param name="data">The byte to be saved.</param>
        /// <returns>true if the operation successes. false if can't find available space.</returns>
        public bool Put(byte data)
        {
            if (_block == null)
            {
                return false;
            }
            else if (_index < _block.End)
            {
                _block.Array[_index++] = data;
                return true;
            }

            var block = _block;
            var index = _index;
            while (true)
            {
                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    block.Array[index] = data;
                    return true;
                }
                else if (block.Next == null)
                {
                    return false;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public int GetLength(MemoryPoolIterator2 end)
        {
            if (IsDefault || end.IsDefault)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var length = 0;
            while (true)
            {
                if (block == end._block)
                {
                    return length + end._index - index;
                }
                else if (block.Next == null)
                {
                    throw new InvalidOperationException("end did not follow iterator");
                }
                else
                {
                    length += block.End - index;
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public MemoryPoolIterator2 CopyTo(byte[] array, int offset, int count, out int actual)
        {
            if (IsDefault)
            {
                actual = 0;
                return this;
            }

            var block = _block;
            var index = _index;
            var remaining = count;
            while (true)
            {
                var following = block.End - index;
                if (remaining <= following)
                {
                    actual = count;
                    if (array != null)
                    {
                        Buffer.BlockCopy(block.Array, index, array, offset, remaining);
                    }
                    return new MemoryPoolIterator2(block, index + remaining);
                }
                else if (block.Next == null)
                {
                    actual = count - remaining + following;
                    if (array != null)
                    {
                        Buffer.BlockCopy(block.Array, index, array, offset, following);
                    }
                    return new MemoryPoolIterator2(block, index + following);
                }
                else
                {
                    if (array != null)
                    {
                        Buffer.BlockCopy(block.Array, index, array, offset, following);
                    }
                    offset += following;
                    remaining -= following;
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public void CopyFrom(byte[] data)
        {
            CopyFrom(new ArraySegment<byte>(data));
        }

        public void CopyFrom(byte[] data, int offset, int count)
        {
            CopyFrom(new ArraySegment<byte>(data, offset, count));
        }

        public void CopyFrom(ArraySegment<byte> buffer)
        {
            Debug.Assert(_block != null);
            Debug.Assert(_block.Pool != null);
            Debug.Assert(_block.Next == null);
            Debug.Assert(_block.End == _index);

            var pool = _block.Pool;
            var block = _block;
            var blockIndex = _index;

            var bufferIndex = buffer.Offset;
            var remaining = buffer.Count;
            var bytesLeftInBlock = block.Data.Offset + block.Data.Count - blockIndex;

            while (remaining > 0)
            {
                if (bytesLeftInBlock == 0)
                {
                    var nextBlock = pool.Lease();
                    block.Next = nextBlock;
                    block = nextBlock;

                    blockIndex = block.Data.Offset;
                    bytesLeftInBlock = block.Data.Count;
                }

                var bytesToCopy = remaining < bytesLeftInBlock ? remaining : bytesLeftInBlock;

                Buffer.BlockCopy(buffer.Array, bufferIndex, block.Array, blockIndex, bytesToCopy);

                blockIndex += bytesToCopy;
                bufferIndex += bytesToCopy;
                remaining -= bytesToCopy;
                bytesLeftInBlock -= bytesToCopy;
                block.End = blockIndex;
            }

            _block = block;
            _index = blockIndex;
        }

        public unsafe void CopyFromAscii(string data)
        {
            Debug.Assert(_block != null);
            Debug.Assert(_block.Pool != null);
            Debug.Assert(_block.Next == null);
            Debug.Assert(_block.End == _index);

            var pool = _block.Pool;
            var block = _block;
            var blockIndex = _index;
            var length = data.Length;

            var bytesLeftInBlock = block.Data.Offset + block.Data.Count - blockIndex;
            var bytesLeftInBlockMinusSpan = bytesLeftInBlock - 3;

            fixed (char* pData = data)
            {
                var input = pData;
                var inputEnd = pData + length;
                var inputEndMinusSpan = inputEnd - 3;

                while (input < inputEnd)
                {
                    if (bytesLeftInBlock == 0)
                    {
                        var nextBlock = pool.Lease();
                        block.Next = nextBlock;
                        block = nextBlock;

                        blockIndex = block.Data.Offset;
                        bytesLeftInBlock = block.Data.Count;
                        bytesLeftInBlockMinusSpan = bytesLeftInBlock - 3;
                    }

                    fixed (byte* pOutput = block.Data.Array)
                    {
                        var output = pOutput + block.End;

                        var copied = 0;
                        for (; input < inputEndMinusSpan && copied < bytesLeftInBlockMinusSpan; copied += 4)
                        {
                            *(output) = (byte)*(input);
                            *(output + 1) = (byte)*(input + 1);
                            *(output + 2) = (byte)*(input + 2);
                            *(output + 3) = (byte)*(input + 3);
                            output += 4;
                            input += 4;
                        }
                        for (; input < inputEnd && copied < bytesLeftInBlock; copied++)
                        {
                            *(output++) = (byte)*(input++);
                        }

                        blockIndex += copied;
                        bytesLeftInBlockMinusSpan -= copied;
                        bytesLeftInBlock -= copied;
                    }
                    block.End = blockIndex;
                }
            }

            _block = block;
            _index = blockIndex;
        }
    }
}
