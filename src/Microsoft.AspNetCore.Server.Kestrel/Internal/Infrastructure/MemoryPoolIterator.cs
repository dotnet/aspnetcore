// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    public struct MemoryPoolIterator
    {
        private const ulong _xorPowerOfTwoToHighByte = (0x07ul       |
                                                        0x06ul <<  8 |
                                                        0x05ul << 16 |
                                                        0x04ul << 24 |
                                                        0x03ul << 32 |
                                                        0x02ul << 40 |
                                                        0x01ul << 48 ) + 1;

        private static readonly Encoding _utf8 = Encoding.UTF8;
        private static readonly int _vectorSpan = Vector<byte>.Count;

        private MemoryPoolBlock _block;
        private int _index;

        public MemoryPoolIterator(MemoryPoolBlock block)
        {
            _block = block;
            _index = _block?.Start ?? 0;
        }
        public MemoryPoolIterator(MemoryPoolBlock block, int index)
        {
            _block = block;
            _index = index;
        }

        public bool IsDefault => _block == null;

        public bool IsEnd
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    return IsEndMultiBlock();
                }
            }
        }

        private bool IsEndMultiBlock()
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

        public MemoryPoolBlock Block => _block;

        public int Index => _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            return TakeMultiBlock(block, index);
        }

        private int TakeMultiBlock(MemoryPoolBlock block, int index)
        {
            var wasLastBlock = block.Next == null;
            do
            {
                if (wasLastBlock)
                {
                    return -1;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }

                wasLastBlock = block.Next == null;

                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    return block.Array[index];
                }
            } while (true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            SkipMultiBlock(bytesToSkip, following);
        }

        private void SkipMultiBlock(int bytesToSkip, int following)
        {
            var wasLastBlock = _block.Next == null;
            var block = _block;
            var index = _index;
            while (true)
            {
                if (wasLastBlock)
                {
                    throw new InvalidOperationException("Attempted to skip more bytes than available.");
                }
                else
                {
                    bytesToSkip -= following;
                    block = block.Next;
                    index = block.Start;
                }

                wasLastBlock = block.Next == null;
                following = block.End - index;

                if (following >= bytesToSkip)
                {
                    _block = block;
                    _index = index + bytesToSkip;
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            return PeekMultiBlock(block, index);
        }

        private static int PeekMultiBlock(MemoryPoolBlock block, int index)
        {
            var wasLastBlock = block.Next == null;
            do
            {
                if (wasLastBlock)
                {
                    return -1;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }

                wasLastBlock = block.Next == null;

                if (index < block.End)
                {
                    return block.Array[index];
                }
            } while (true);
        }

        // NOTE: Little-endian only!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryPeekLong(out ulong longValue)
        {
            longValue = 0;

            if (_block == null)
            {
                return false;
            }

            var blockBytes = _block.End - _index;

            if (blockBytes >= sizeof(ulong))
            {
                longValue = *(ulong*)(_block.DataFixedPtr + _index);
                return true;
            }

            return TryPeekLongMultiBlock(ref longValue, blockBytes);
        }

        private unsafe bool TryPeekLongMultiBlock(ref ulong longValue, int blockBytes)
        {
            var wasLastBlock = _block.Next == null;
            if (wasLastBlock)
            {
                return false;
            }
            else
            {
                // Each block will be filled with at least 2048 bytes before the Next pointer is set, so a long
                // will cross at most one block boundary assuming there are at least 8 bytes following the iterator.
                var nextBytes = sizeof(ulong) - blockBytes;

                if (_block.Next.End - _block.Next.Start < nextBytes)
                {
                    return false;
                }

                var nextLong = *(ulong*)(_block.Next.DataFixedPtr + _block.Next.Start);

                if (blockBytes == 0)
                {
                    // This case can not fall through to the else block since that would cause a 64-bit right shift
                    // on blockLong which is equivalent to no shift at all instead of shifting in all zeros.
                    // https://msdn.microsoft.com/en-us/library/xt18et0d.aspx
                    longValue = nextLong;
                }
                else
                {
                    var blockLong = *(ulong*)(_block.DataFixedPtr + _block.End - sizeof(ulong));

                    // Ensure that the right shift has a ulong operand so a logical shift is performed.
                    longValue = (blockLong >> nextBytes * 8) | (nextLong << blockBytes * 8);
                }

                return true;
            }
        }

        public int Seek(byte byte0)
        {
            int bytesScanned;
            return Seek(byte0, out bytesScanned);
        }

        public unsafe int Seek(
            byte byte0,
            out int bytesScanned,
            int limit = int.MaxValue)
        {
            bytesScanned = 0;

            if (IsDefault || limit <= 0)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var wasLastBlock = block.Next == null;
            var following = block.End - index;
            byte[] array;
            var byte0Vector = GetVector(byte0);

            while (true)
            {
                while (following == 0)
                {
                    if (bytesScanned >= limit || wasLastBlock)
                    {
                        _block = block;
                        _index = index;
                        return -1;
                    }

                    block = block.Next;
                    index = block.Start;
                    wasLastBlock = block.Next == null;
                    following = block.End - index;
                }
                array = block.Array;
                while (following > 0)
                {
                    // Need unit tests to test Vector path
#if !DEBUG
                    // Check will be Jitted away https://github.com/dotnet/coreclr/issues/1079
                    if (Vector.IsHardwareAccelerated)
                    {
#endif
                    if (following >= _vectorSpan)
                    {
                        var byte0Equals = Vector.Equals(new Vector<byte>(array, index), byte0Vector);

                        if (byte0Equals.Equals(Vector<byte>.Zero))
                        {
                            if (bytesScanned + _vectorSpan >= limit)
                            {
                                _block = block;
                                // Ensure iterator is left at limit position
                                _index = index + (limit - bytesScanned);
                                bytesScanned = limit;
                                return -1;
                            }

                            bytesScanned += _vectorSpan;
                            following -= _vectorSpan;
                            index += _vectorSpan;
                            continue;
                        }

                        _block = block;

                        var firstEqualByteIndex = LocateFirstFoundByte(byte0Equals);
                        var vectorBytesScanned = firstEqualByteIndex + 1;

                        if (bytesScanned + vectorBytesScanned > limit)
                        {
                            // Ensure iterator is left at limit position
                            _index = index + (limit - bytesScanned);
                            bytesScanned = limit;
                            return -1;
                        }

                        _index = index + firstEqualByteIndex;
                        bytesScanned += vectorBytesScanned;

                        return byte0;
                    }
                    // Need unit tests to test Vector path
#if !DEBUG
                    }
#endif

                    var pCurrent = (block.DataFixedPtr + index);
                    var pEnd = pCurrent + Math.Min(following, limit - bytesScanned);
                    do
                    {
                        bytesScanned++;
                        if (*pCurrent == byte0)
                        {
                            _block = block;
                            _index = index;
                            return byte0;
                        }
                        pCurrent++;
                        index++;
                    } while (pCurrent < pEnd);

                    following = 0;
                    break;
                }
            }
        }

        public unsafe int Seek(
            byte byte0,
            ref MemoryPoolIterator limit)
        {
            if (IsDefault)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var wasLastBlock = block.Next == null;
            var following = block.End - index;

            while (true)
            {
                while (following == 0)
                {
                    if ((block == limit.Block && index > limit.Index) ||
                        wasLastBlock)
                    {
                        _block = block;
                        // Ensure iterator is left at limit position
                        _index = limit.Index;
                        return -1;
                    }

                    block = block.Next;
                    index = block.Start;
                    wasLastBlock = block.Next == null;
                    following = block.End - index;
                }
                var array = block.Array;
                while (following > 0)
                {
// Need unit tests to test Vector path
#if !DEBUG
                    // Check will be Jitted away https://github.com/dotnet/coreclr/issues/1079
                    if (Vector.IsHardwareAccelerated)
                    {
#endif
                        if (following >= _vectorSpan)
                        {
                            var byte0Equals = Vector.Equals(new Vector<byte>(array, index), GetVector(byte0));

                            if (byte0Equals.Equals(Vector<byte>.Zero))
                            {
                                if (block == limit.Block && index + _vectorSpan > limit.Index)
                                {
                                    _block = block;
                                    // Ensure iterator is left at limit position
                                    _index = limit.Index;
                                    return -1;
                                }

                                following -= _vectorSpan;
                                index += _vectorSpan;
                                continue;
                            }

                            _block = block;

                            var firstEqualByteIndex = LocateFirstFoundByte(byte0Equals);

                            if (_block == limit.Block && index + firstEqualByteIndex > limit.Index)
                            {
                                // Ensure iterator is left at limit position
                                _index = limit.Index;
                                return -1;
                            }

                            _index = index + firstEqualByteIndex;

                            return byte0;
                        }
// Need unit tests to test Vector path
#if !DEBUG
                    }
#endif

                    var pCurrent = (block.DataFixedPtr + index);
                    var pEnd = block == limit.Block ? block.DataFixedPtr + limit.Index + 1 : pCurrent + following;
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

                    following = 0;
                    break;
                }
            }
        }

        public int Seek(byte byte0, byte byte1)
        {
            var limit = new MemoryPoolIterator();
            return Seek(byte0, byte1, ref limit);
        }

        public unsafe int Seek(
            byte byte0,
            byte byte1,
            ref MemoryPoolIterator limit)
        {
            if (IsDefault)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var wasLastBlock = block.Next == null;
            var following = block.End - index;
            int byteIndex = int.MaxValue;

            while (true)
            {
                while (following == 0)
                {
                    if ((block == limit.Block && index > limit.Index) ||
                        wasLastBlock)
                    {
                        _block = block;
                        // Ensure iterator is left at limit position
                        _index = limit.Index;
                        return -1;
                    }
                    block = block.Next;
                    index = block.Start;
                    wasLastBlock = block.Next == null;
                    following = block.End - index;
                }
                var array = block.Array;
                while (following > 0)
                {

// Need unit tests to test Vector path
#if !DEBUG
                    // Check will be Jitted away https://github.com/dotnet/coreclr/issues/1079
                    if (Vector.IsHardwareAccelerated)
                    {
#endif
                        if (following >= _vectorSpan)
                        {
                            var data = new Vector<byte>(array, index);

                            var byteEquals = Vector.Equals(data, GetVector(byte0));
                            byteEquals = Vector.ConditionalSelect(byteEquals, byteEquals, Vector.Equals(data, GetVector(byte1)));

                            if (!byteEquals.Equals(Vector<byte>.Zero))
                            {
                                byteIndex = LocateFirstFoundByte(byteEquals);
                            }

                            if (byteIndex == int.MaxValue)
                            {
                                following -= _vectorSpan;
                                index += _vectorSpan;

                                if (block == limit.Block && index > limit.Index)
                                {
                                    _block = block;
                                    // Ensure iterator is left at limit position
                                    _index = limit.Index;
                                    return -1;
                                }

                                continue;
                            }

                            _block = block;

                            _index = index + byteIndex;

                            if (block == limit.Block && _index > limit.Index)
                            {
                                // Ensure iterator is left at limit position
                                _index = limit.Index;
                                return -1;
                            }

                            _index = index + byteIndex;

                            if (block == limit.Block && _index > limit.Index)
                            {
                                // Ensure iterator is left at limit position
                                _index = limit.Index;
                                return -1;
                            }

                            return block.Array[index + byteIndex];
                        }
// Need unit tests to test Vector path
#if !DEBUG
                    }
#endif
                    var pCurrent = (block.DataFixedPtr + index);
                    var pEnd = block == limit.Block ? block.DataFixedPtr + limit.Index + 1 : pCurrent + following;
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

                    following = 0;
                    break;
                }
            }
        }

        public int Seek(byte byte0, byte byte1, byte byte2)
        {
            var limit = new MemoryPoolIterator();
            return Seek(byte0, byte1, byte2, ref limit);
        }

        public unsafe int Seek(
            byte byte0,
            byte byte1,
            byte byte2,
            ref MemoryPoolIterator limit)
        {
            if (IsDefault)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var wasLastBlock = block.Next == null;
            var following = block.End - index;
            int byteIndex = int.MaxValue;

            while (true)
            {
                while (following == 0)
                {
                    if ((block == limit.Block && index > limit.Index) ||
                        wasLastBlock)
                    {
                        _block = block;
                        // Ensure iterator is left at limit position
                        _index = limit.Index;
                        return -1;
                    }
                    block = block.Next;
                    index = block.Start;
                    wasLastBlock = block.Next == null;
                    following = block.End - index;
                }
                var array = block.Array;
                while (following > 0)
                {
// Need unit tests to test Vector path
#if !DEBUG
                    // Check will be Jitted away https://github.com/dotnet/coreclr/issues/1079
                    if (Vector.IsHardwareAccelerated)
                    {
#endif
                        if (following >= _vectorSpan)
                        {
                            var data = new Vector<byte>(array, index);

                            var byteEquals = Vector.Equals(data, GetVector(byte0));
                            byteEquals = Vector.ConditionalSelect(byteEquals, byteEquals, Vector.Equals(data, GetVector(byte1)));
                            byteEquals = Vector.ConditionalSelect(byteEquals, byteEquals, Vector.Equals(data, GetVector(byte2)));

                            if (!byteEquals.Equals(Vector<byte>.Zero))
                            {
                                byteIndex = LocateFirstFoundByte(byteEquals);
                            }

                            if (byteIndex == int.MaxValue)
                            {
                                following -= _vectorSpan;
                                index += _vectorSpan;

                                if (block == limit.Block && index > limit.Index)
                                {
                                    _block = block;
                                    // Ensure iterator is left at limit position
                                    _index = limit.Index;
                                    return -1;
                                }

                                continue;
                            }

                            _block = block;

                            _index = index + byteIndex;

                            if (block == limit.Block && _index > limit.Index)
                            {
                                // Ensure iterator is left at limit position
                                _index = limit.Index;
                                return -1;
                            }

                            return block.Array[index + byteIndex];
                        }
// Need unit tests to test Vector path
#if !DEBUG
                    }
#endif
                    var pCurrent = (block.DataFixedPtr + index);
                    var pEnd = block == limit.Block ? block.DataFixedPtr + limit.Index + 1 : pCurrent + following;
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

                    following = 0;
                    break;
                }
            }
        }

        /// <summary>
        /// Locate the first of the found bytes
        /// </summary>
        /// <param  name="byteEquals"></param >
        /// <returns>The first index of the result vector</returns>
        // Force inlining (64 IL bytes, 91 bytes asm) Issue: https://github.com/dotnet/coreclr/issues/7386
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LocateFirstFoundByte(Vector<byte> byteEquals)
        {
            var vector64 = Vector.AsVectorInt64(byteEquals);
            long longValue = 0;
            var i = 0;
            for (; i < Vector<long>.Count; i++)
            {
                longValue = vector64[i];
                if (longValue == 0) continue;
                break;
            }

            // Flag least significant power of two bit
            var powerOfTwoFlag = (ulong)(longValue ^ (longValue - 1));
            // Shift all powers of two into the high byte and extract
            var foundByteIndex = (int)((powerOfTwoFlag * _xorPowerOfTwoToHighByte) >> 57);
            // Single LEA instruction with jitted const (using function result)
            return i * 8 + foundByteIndex;
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

            var block = _block;
            var index = _index;
            while (true)
            {
                var wasLastBlock = block.Next == null;

                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    block.Array[index] = data;
                    return true;
                }
                else if (wasLastBlock)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetLength(MemoryPoolIterator end)
        {
            if (IsDefault || end.IsDefault)
            {
                return -1;
            }

            if (_block == end._block)
            {
                return end._index - _index;
            }

            return GetLengthMultiBlock(end);
        }

        public int GetLengthMultiBlock(MemoryPoolIterator end)
        {
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

        public MemoryPoolIterator CopyTo(byte[] array, int offset, int count, out int actual)
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
                // Determine if we might attempt to copy data from block.Next before
                // calculating "following" so we don't risk skipping data that could
                // be added after block.End when we decide to copy from block.Next.
                // block.End will always be advanced before block.Next is set.
                var wasLastBlock = block.Next == null;
                var following = block.End - index;
                if (remaining <= following)
                {
                    actual = count;
                    if (array != null)
                    {
                        Buffer.BlockCopy(block.Array, index, array, offset, remaining);
                    }
                    return new MemoryPoolIterator(block, index + remaining);
                }
                else if (wasLastBlock)
                {
                    actual = count - remaining + following;
                    if (array != null)
                    {
                        Buffer.BlockCopy(block.Array, index, array, offset, following);
                    }
                    return new MemoryPoolIterator(block, index + following);
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
            if (IsDefault)
            {
                return;
            }

            Debug.Assert(_block != null);
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
                    Volatile.Write(ref block.Next, nextBlock);
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
            if (IsDefault)
            {
                return;
            }

            Debug.Assert(_block != null);
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
                        Volatile.Write(ref block.Next, nextBlock);
                        block = nextBlock;

                        blockIndex = block.Data.Offset;
                        bytesLeftInBlock = block.Data.Count;
                        bytesLeftInBlockMinusSpan = bytesLeftInBlock - 3;
                    }

                    var output = (block.DataFixedPtr + block.End);
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

            block.End = blockIndex;
            _block = block;
            _index = blockIndex;
        }

        public unsafe string GetAsciiString(ref MemoryPoolIterator end)
        {
            if (IsDefault || end.IsDefault)
            {
                return null;
            }

            var length = GetLength(end);

            if (length == 0)
            {
                return null;
            }

            var inputOffset = Index;
            var block = Block;

            var asciiString = new string('\0', length);

            fixed (char* outputStart = asciiString)
            {
                var output = outputStart;
                var remaining = length;

                var endBlock = end.Block;
                var endIndex = end.Index;

                var outputOffset = 0;
                while (true)
                {
                    int following = (block != endBlock ? block.End : endIndex) - inputOffset;

                    if (following > 0)
                    {
                        if (!AsciiUtilities.TryGetAsciiString(block.DataFixedPtr + inputOffset, output + outputOffset, following))
                        {
                            throw BadHttpRequestException.GetException(RequestRejectionReason.NonAsciiOrNullCharactersInInputString);
                        }

                        outputOffset += following;
                        remaining -= following;
                    }

                    if (remaining == 0)
                    {
                        break;
                    }

                    block = block.Next;
                    inputOffset = block.Start;
                }
            }

            return asciiString;
        }

        public string GetUtf8String(ref MemoryPoolIterator end)
        {
            if (IsDefault || end.IsDefault)
            {
                return default(string);
            }
            if (end.Block == Block)
            {
                return _utf8.GetString(Block.Array, Index, end.Index - Index);
            }

            var decoder = _utf8.GetDecoder();

            var length = GetLength(end);
            var charLength = length;
            // Worse case is 1 byte = 1 char
            var chars = new char[charLength];
            var charIndex = 0;

            var block = Block;
            var index = Index;
            var remaining = length;
            while (true)
            {
                int bytesUsed;
                int charsUsed;
                bool completed;
                var following = block.End - index;
                if (remaining <= following)
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        remaining,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        true,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    return new string(chars, 0, charIndex + charsUsed);
                }
                else if (block.Next == null)
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        following,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        true,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    return new string(chars, 0, charIndex + charsUsed);
                }
                else
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        following,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        false,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    charIndex += charsUsed;
                    remaining -= following;
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<byte> GetArraySegment(MemoryPoolIterator end)
        {
            if (IsDefault || end.IsDefault)
            {
                return default(ArraySegment<byte>);
            }
            if (end.Block == Block)
            {
                return new ArraySegment<byte>(Block.Array, Index, end.Index - Index);
            }

            return GetArraySegmentMultiBlock(ref end);
        }

        private ArraySegment<byte> GetArraySegmentMultiBlock(ref MemoryPoolIterator end)
        {
            var length = GetLength(end);
            var array = new byte[length];
            CopyTo(array, 0, length, out length);
            return new ArraySegment<byte>(array, 0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<byte> GetVector(byte vectorByte)
        {
            // Vector<byte> .ctor doesn't become an intrinsic due to detection issue
            // However this does cause it to become an intrinsic (with additional multiply and reg->reg copy)
            // https://github.com/dotnet/coreclr/issues/7459#issuecomment-253965670
            return Vector.AsVectorByte(new Vector<ulong>(vectorByte * 0x0101010101010101ul));
        }

    }
}
