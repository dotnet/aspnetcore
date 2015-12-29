// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Numerics;

namespace Microsoft.AspNet.Server.Kestrel.Infrastructure
{
    public struct MemoryPoolIterator2
    {
        private readonly static int _vectorSpan = Vector<byte>.Count; 

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
            var block = _block;
            if (block == null)
            {
                return -1;
            }

            var index = _index;

            if (index < block.End)
            {
                _index = index + 1;
                return block.Array[index];
            }

            do
            {
                if (block.Next == null)
                {
                    return -1;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }

                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    return block.Array[index];
                }
            } while (true);
        }

        public void Skip(int bytesToSkip)
        {
            if (_block == null)
            {
                return;
            }
            var following = _block.End - _index;
            if (following >= bytesToSkip)
            {
                _index += bytesToSkip;
                return;
            }

            var block = _block;
            var index = _index;
            while (true)
            {
                if (block.Next == null)
                {
                    return;
                }
                else
                {
                    bytesToSkip -= following;
                    block = block.Next;
                    index = block.Start;
                }
                following = block.End - index;
                if (following >= bytesToSkip)
                {
                    _block = block;
                    _index = index + bytesToSkip;
                    return;
                }
            }
        }

        public int Peek()
        {
            var block = _block;
            if (block == null)
            {
                return -1;
            }

            var index = _index;

            if (index < block.End)
            {
                return block.Array[index];
            }

            do
            {
                if (block.Next == null)
                {
                    return -1;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }

                if (index < block.End)
                {
                    return block.Array[index];
                }
            } while (true);
        }

        public unsafe long PeekLong()
        {
            if (_block == null)
            {
                return -1;
            }
            else if (_block.End - _index >= sizeof(long))
            {
                fixed (byte* ptr = _block.Array)
                {
                    return *(long*)(ptr + _index);
                }
            }
            else if (_block.Next == null)
            {
                return -1;
            }
            else
            {
                var blockBytes = _block.End - _index;
                var nextBytes = sizeof(long) - blockBytes;

                if (_block.Next.End - _block.Next.Start < nextBytes)
                {
                    return -1;
                }

                long blockLong;
                fixed (byte* ptr = _block.Array)
                {
                    blockLong = *(long*)(ptr + _block.End - sizeof(long));
                }

                long nextLong;
                fixed (byte* ptr = _block.Next.Array)
                {
                    nextLong = *(long*)(ptr + _block.Next.Start);
                }

                return (blockLong >> (sizeof(long) - blockBytes) * 8) | (nextLong << (sizeof(long) - nextBytes) * 8);
            }
        }

        public unsafe int Seek(ref Vector<byte> byte0Vector)
        {
            if (IsDefault)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var following = block.End - index;
            byte[] array;
            var byte0 = byte0Vector[0];

            while (true)
            {
                while (following == 0)
                {
                    var newBlock = block.Next;
                    if (newBlock == null)
                    {
                        _block = block;
                        _index = index;
                        return -1;
                    }
                    index = newBlock.Start;
                    following = newBlock.End - index;
                    block = newBlock;
                }
                array = block.Array;
                while (following > 0)
                {
#if !DEBUG // Need unit tests to test Vector path
                    // Check will be Jitted away https://github.com/dotnet/coreclr/issues/1079
                    if (Vector.IsHardwareAccelerated)
                    {
#endif
                        if (following >= _vectorSpan)
                        {
                            var byte0Equals = Vector.Equals(new Vector<byte>(array, index), byte0Vector);

                            if (byte0Equals.Equals(Vector<byte>.Zero))
                            {
                                following -= _vectorSpan;
                                index += _vectorSpan;
                                continue;
                            }

                            _block = block;
                            _index = index + FindFirstEqualByte(ref byte0Equals);
                            return byte0;
                        }
#if !DEBUG // Need unit tests to test Vector path
                    }
#endif
                    fixed (byte* ptr = &block.Array[index])
                    {
                        var pCurrent = ptr;
                        var pEnd = pCurrent + following;
                        do
                        {
                            if (*pCurrent == byte0)
                            {
                                _block = block;
                                _index = index;
                                return byte0;
                            }
                            pCurrent++;
                            index++;
                        } while (pCurrent < pEnd);
                    }

                    following = 0;
                    break;
                }
            }
        }

        public unsafe int Seek(ref Vector<byte> byte0Vector, ref Vector<byte> byte1Vector)
        {
            if (IsDefault)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var following = block.End - index;
            byte[] array;
            int byte0Index = int.MaxValue;
            int byte1Index = int.MaxValue;
            var byte0 = byte0Vector[0];
            var byte1 = byte1Vector[0];

            while (true)
            {
                while (following == 0)
                {
                    var newBlock = block.Next;
                    if (newBlock == null)
                    {
                        _block = block;
                        _index = index;
                        return -1;
                    }
                    index = newBlock.Start;
                    following = newBlock.End - index;
                    block = newBlock;
                }
                array = block.Array;
                while (following > 0)
                {

#if !DEBUG // Need unit tests to test Vector path
                    // Check will be Jitted away https://github.com/dotnet/coreclr/issues/1079
                    if (Vector.IsHardwareAccelerated)
                    {
#endif
                        if (following >= _vectorSpan)
                        {
                            var data = new Vector<byte>(array, index);
                            var byte0Equals = Vector.Equals(data, byte0Vector);
                            var byte1Equals = Vector.Equals(data, byte1Vector);

                            if (!byte0Equals.Equals(Vector<byte>.Zero))
                            {
                                byte0Index = FindFirstEqualByte(ref byte0Equals);
                            }
                            if (!byte1Equals.Equals(Vector<byte>.Zero))
                            {
                                byte1Index = FindFirstEqualByte(ref byte1Equals);
                            }

                            if (byte0Index == int.MaxValue && byte1Index == int.MaxValue)
                            {
                                following -= _vectorSpan;
                                index += _vectorSpan;
                                continue;
                            }

                            _block = block;

                            if (byte0Index < byte1Index)
                            {
                                _index = index + byte0Index;
                                return byte0;
                            }

                            _index = index + byte1Index;
                            return byte1;
                        }
#if !DEBUG // Need unit tests to test Vector path
                    }
#endif
                    fixed (byte* ptr = &block.Array[index])
                    {
                        var pCurrent = ptr;
                        var pEnd = pCurrent + following;
                        do
                        {
                            if (*pCurrent == byte0)
                            {
                                _block = block;
                                _index = index;
                                return byte0;
                            }
                            if (*pCurrent == byte1)
                            {
                                _block = block;
                                _index = index;
                                return byte1;
                            }
                            pCurrent++;
                            index++;
                        } while (pCurrent != pEnd);
                    }

                    following = 0;
                    break;
                }
            }
        }

        public unsafe int Seek(ref Vector<byte> byte0Vector, ref Vector<byte> byte1Vector, ref Vector<byte> byte2Vector)
        {
            if (IsDefault)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var following = block.End - index;
            byte[] array;
            int byte0Index = int.MaxValue;
            int byte1Index = int.MaxValue;
            int byte2Index = int.MaxValue;
            var byte0 = byte0Vector[0];
            var byte1 = byte1Vector[0];
            var byte2 = byte2Vector[0];

            while (true)
            {
                while (following == 0)
                {
                    var newBlock = block.Next;
                    if (newBlock == null)
                    {
                        _block = block;
                        _index = index;
                        return -1;
                    }
                    index = newBlock.Start;
                    following = newBlock.End - index;
                    block = newBlock;
                }
                array = block.Array;
                while (following > 0)
                {
#if !DEBUG // Need unit tests to test Vector path
                    // Check will be Jitted away https://github.com/dotnet/coreclr/issues/1079
                    if (Vector.IsHardwareAccelerated)
                    {
#endif
                        if (following >= _vectorSpan)
                        {
                            var data = new Vector<byte>(array, index);
                            var byte0Equals = Vector.Equals(data, byte0Vector);
                            var byte1Equals = Vector.Equals(data, byte1Vector);
                            var byte2Equals = Vector.Equals(data, byte2Vector);

                            if (!byte0Equals.Equals(Vector<byte>.Zero))
                            {
                                byte0Index = FindFirstEqualByte(ref byte0Equals);
                            }
                            if (!byte1Equals.Equals(Vector<byte>.Zero))
                            {
                                byte1Index = FindFirstEqualByte(ref byte1Equals);
                            }
                            if (!byte2Equals.Equals(Vector<byte>.Zero))
                            {
                                byte2Index = FindFirstEqualByte(ref byte2Equals);
                            }

                            if (byte0Index == int.MaxValue && byte1Index == int.MaxValue && byte2Index == int.MaxValue)
                            {
                                following -= _vectorSpan;
                                index += _vectorSpan;
                                continue;
                            }

                            _block = block;

                            int toReturn, toMove;
                            if (byte0Index < byte1Index)
                            {
                                if (byte0Index < byte2Index)
                                {
                                    toReturn = byte0;
                                    toMove = byte0Index;
                                }
                                else
                                {
                                    toReturn = byte2;
                                    toMove = byte2Index;
                                }
                            }
                            else
                            {
                                if (byte1Index < byte2Index)
                                {
                                    toReturn = byte1;
                                    toMove = byte1Index;
                                }
                                else
                                {
                                    toReturn = byte2;
                                    toMove = byte2Index;
                                }
                            }

                            _index = index + toMove;
                            return toReturn;
                        }
#if !DEBUG // Need unit tests to test Vector path
                    }
#endif
                    fixed (byte* ptr = &block.Array[index])
                    {
                        var pCurrent = ptr;
                        var pEnd = pCurrent + following;
                        do
                        {
                            if (*pCurrent == byte0)
                            {
                                _block = block;
                                _index = index;
                                return byte0;
                            }
                            if (*pCurrent == byte1)
                            {
                                _block = block;
                                _index = index;
                                return byte1;
                            }
                            if (*pCurrent == byte2)
                            {
                                _block = block;
                                _index = index;
                                return byte2;
                            }
                            pCurrent++;
                            index++;
                        } while (pCurrent != pEnd);
                    }

                    following = 0;
                    break;
                }
            }
        }

        private static int FindFirstEqualByte(ref Vector<byte> byteEquals)
        {
            // Quasi-tree search
            var vector64 = Vector.AsVectorInt64(byteEquals);
            for (var i = 0; i < Vector<long>.Count; i++)
            {
                var longValue = vector64[i];
                if (longValue == 0) continue;

                var shift = i << 1;
                var offset = shift << 2;
                var vector32 = Vector.AsVectorInt32(byteEquals);
                if (vector32[shift] != 0)
                {
                    if (byteEquals[offset] != 0) return offset;
                    if (byteEquals[offset + 1] != 0) return offset + 1;
                    if (byteEquals[offset + 2] != 0) return offset + 2;
                    return offset + 3;
                }
                if (byteEquals[offset + 4] != 0) return offset + 4;
                if (byteEquals[offset + 5] != 0) return offset + 5;
                if (byteEquals[offset + 6] != 0) return offset + 6;
                return offset + 7;
            }
            throw new InvalidOperationException();
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
            checked
            {
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
            CopyFrom(data, 0, data.Length);
        }

        public void CopyFrom(ArraySegment<byte> buffer)
        {
            CopyFrom(buffer.Array, buffer.Offset, buffer.Count);
        }

        public void CopyFrom(byte[] data, int offset, int count)
        {
            Debug.Assert(_block != null);
            Debug.Assert(_block.Pool != null);
            Debug.Assert(_block.Next == null);
            Debug.Assert(_block.End == _index);

            var pool = _block.Pool;
            var block = _block;
            var blockIndex = _index;

            var bufferIndex = offset;
            var remaining = count;
            var bytesLeftInBlock = block.Data.Offset + block.Data.Count - blockIndex;

            while (remaining > 0)
            {
                if (bytesLeftInBlock == 0)
                {
                    var nextBlock = pool.Lease();
                    block.End = blockIndex;
                    block.Next = nextBlock;
                    block = nextBlock;

                    blockIndex = block.Data.Offset;
                    bytesLeftInBlock = block.Data.Count;
                }

                var bytesToCopy = remaining < bytesLeftInBlock ? remaining : bytesLeftInBlock;

                Buffer.BlockCopy(data, bufferIndex, block.Array, blockIndex, bytesToCopy);

                blockIndex += bytesToCopy;
                bufferIndex += bytesToCopy;
                remaining -= bytesToCopy;
                bytesLeftInBlock -= bytesToCopy;
            }

            block.End = blockIndex;
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
                        block.End = blockIndex;
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
                }
            }

            block.End = blockIndex;
            _block = block;
            _index = blockIndex;
        }
    }
}
