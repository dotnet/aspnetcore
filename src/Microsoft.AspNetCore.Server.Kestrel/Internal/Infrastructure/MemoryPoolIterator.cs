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
        private const int _maxULongByteLength = 20;
        private const ulong _xorPowerOfTwoToHighByte = (0x07ul       |
                                                        0x06ul <<  8 |
                                                        0x05ul << 16 |
                                                        0x04ul << 24 |
                                                        0x03ul << 32 |
                                                        0x02ul << 40 |
                                                        0x01ul << 48 ) + 1;

        private static readonly int _vectorSpan = Vector<byte>.Count;

        [ThreadStatic]
        private static byte[] _numericBytesScratch;

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
                var block = _block;
                if (block == null)
                {
                    return true;
                }
                else if (_index < block.End)
                {
                    return false;
                }
                else if (block.Next == null)
                {
                    return true;
                }
                else
                {
                    return IsEndMultiBlock();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool IsEndMultiBlock()
        {
            var block = _block.Next;
            do
            {
                if (block.Start < block.End)
                {
                    return false; // subsequent block has data - IsEnd is false
                }
                block = block.Next;
            } while (block != null);

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
            // Always set wasLastBlock before checking .End to avoid race which may cause data loss
            var wasLastBlock = block.Next == null;

            if (index < block.End)
            {
                _index = index + 1;
                return block.Array[index];
            }

            return wasLastBlock ? -1 : TakeMultiBlock();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int TakeMultiBlock()
        {
            var block = _block;
            do
            {
                block = block.Next;
                var index = block.Start;

                // Always set wasLastBlock before checking .End to avoid race which may cause data loss 
                var wasLastBlock = block.Next == null;

                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    return block.Array[index];
                }

                if (wasLastBlock)
                {
                    return -1;
                }
            } while (true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int bytesToSkip)
        {
            var block = _block;
            if (block == null && bytesToSkip > 0)
            {
                ThrowInvalidOperationException_SkipMoreThanAvailable();
            }

            // Always set wasLastBlock before checking .End to avoid race which may cause data loss
            var wasLastBlock = block.Next == null;
            var following = block.End - _index;

            if (following >= bytesToSkip)
            {
                _index += bytesToSkip;
                return;
            }

            if (wasLastBlock)
            {
                ThrowInvalidOperationException_SkipMoreThanAvailable();
            }

            SkipMultiBlock(bytesToSkip, following);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SkipMultiBlock(int bytesToSkip, int following)
        {
            var block = _block;
            do
            {
                bytesToSkip -= following;
                block = block.Next;
                var index = block.Start;

                // Always set wasLastBlock before checking .End to avoid race which may cause data loss
                var wasLastBlock = block.Next == null;
                following = block.End - index;

                if (following >= bytesToSkip)
                {
                    _block = block;
                    _index = index + bytesToSkip;
                    return;
                }

                if (wasLastBlock)
                {
                    ThrowInvalidOperationException_SkipMoreThanAvailable();
                }
            } while (true);
        }

        private static void ThrowInvalidOperationException_SkipMoreThanAvailable()
        {
            throw new InvalidOperationException("Attempted to skip more bytes than available.");
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

            // Always set wasLastBlock before checking .End to avoid race which may cause data loss
            var wasLastBlock = block.Next == null;
            if (index < block.End)
            {
                return block.Array[index];
            }

            return wasLastBlock ? -1 : PeekMultiBlock();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int PeekMultiBlock()
        {
            var block = _block;
            do
            {
                block = block.Next;
                var index = block.Start;

                // Always set wasLastBlock before checking .End to avoid race which may cause data loss 
                var wasLastBlock = block.Next == null;

                if (index < block.End)
                {
                    return block.Array[index];
                }
                if (wasLastBlock)
                {
                    return -1;
                }
            } while (true);
        }

        // NOTE: Little-endian only!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryPeekLong(out ulong longValue)
        {
            longValue = 0;

            var block = _block;
            if (block == null)
            {
                return false;
            }

            // Always set wasLastBlock before checking .End to avoid race which may cause data loss 
            var wasLastBlock = block.Next == null;
            var blockBytes = block.End - _index;

            if (blockBytes >= sizeof(ulong))
            {
                longValue = *(ulong*)(block.DataFixedPtr + _index);
                return true;
            }

            return wasLastBlock ? false : TryPeekLongMultiBlock(ref longValue, blockBytes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe bool TryPeekLongMultiBlock(ref ulong longValue, int blockBytes)
        {
            // Each block will be filled with at least 2048 bytes before the Next pointer is set, so a long
            // will cross at most one block boundary assuming there are at least 8 bytes following the iterator.
            var nextBytes = sizeof(ulong) - blockBytes;

            var block = _block;
            if (block.Next.End - block.Next.Start < nextBytes)
            {
                return false;
            }

            var nextLong = *(ulong*)(block.Next.DataFixedPtr + block.Next.Start);

            if (blockBytes == 0)
            {
                // This case can not fall through to the else block since that would cause a 64-bit right shift
                // on blockLong which is equivalent to no shift at all instead of shifting in all zeros.
                // https://msdn.microsoft.com/en-us/library/xt18et0d.aspx
                longValue = nextLong;
            }
            else
            {
                var blockLong = *(ulong*)(block.DataFixedPtr + block.End - sizeof(ulong));

                // Ensure that the right shift has a ulong operand so a logical shift is performed.
                longValue = (blockLong >> nextBytes * 8) | (nextLong << blockBytes * 8);
            }

            return true;
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

            var block = _block;
            if (block == null || limit <= 0)
            {
                return -1;
            }

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
            var block = _block;
            if (block == null)
            {
                return -1;
            }

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
            var block = _block;
            if (block == null)
            {
                return -1;
            }

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
            var block = _block;
            if (block == null)
            {
                return -1;
            }

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
            var vector64 = Vector.AsVectorUInt64(byteEquals);
            ulong longValue = 0;
            var i = 0;
            // Pattern unrolled by jit https://github.com/dotnet/coreclr/pull/8001
            for (; i < Vector<ulong>.Count; i++)
            {
                longValue = vector64[i];
                if (longValue == 0) continue;
                break;
            }

            // Flag least significant power of two bit
            var powerOfTwoFlag = (longValue ^ (longValue - 1));
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Put(byte data)
        {
            var block = _block;
            if (block == null)
            {
                ThrowInvalidOperationException_PutPassedEndOfBlock();
            }

            var index = _index;

            // Always set wasLastBlock before checking .End to avoid race which may cause data loss
            var wasLastBlock = block.Next == null;
            if (index < block.End)
            {
                _index = index + 1;
                block.Array[index] = data;
                return true;
            }

            if (wasLastBlock)
            {
                ThrowInvalidOperationException_PutPassedEndOfBlock();
            }

            return PutMultiBlock(data);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool PutMultiBlock(byte data)
        {
            var block = _block;
            do
            {
                block = block.Next;
                var index = block.Start;

                // Always set wasLastBlock before checking .End to avoid race which may cause data loss
                var wasLastBlock = block.Next == null;

                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    block.Array[index] = data;
                    break;
                }
                if (wasLastBlock)
                {
                    ThrowInvalidOperationException_PutPassedEndOfBlock();
                    return false;
                }
            } while (true);

            return true;
        }

        private static void ThrowInvalidOperationException_PutPassedEndOfBlock()
        {
            throw new InvalidOperationException("Attempted to put passed end of block.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetLength(MemoryPoolIterator end)
        {
            var block = _block;
            if (block == null || end.IsDefault)
            {
                ThrowInvalidOperationException_GetLengthNullBlock();
            }

            if (block == end._block)
            {
                return end._index - _index;
            }

            return GetLengthMultiBlock(ref end);
        }

        private static void ThrowInvalidOperationException_GetLengthNullBlock()
        {
            throw new InvalidOperationException("Attempted GetLength of non existent block.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetLengthMultiBlock(ref MemoryPoolIterator end)
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
            var block = _block;
            if (block == null)
            {
                actual = 0;
                return this;
            }

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
            var block = _block;
            if (block == null)
            {
                return;
            }

            Debug.Assert(block.Next == null);
            Debug.Assert(block.End == _index);

            var pool = block.Pool;
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
            var block = _block;
            if (block == null)
            {
                return;
            }

            Debug.Assert(block.Next == null);
            Debug.Assert(block.End == _index);

            var pool = block.Pool;
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

        private static byte[] NumericBytesScratch => _numericBytesScratch ?? CreateNumericBytesScratch();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] CreateNumericBytesScratch()
        {
            var bytes = new byte[_maxULongByteLength];
            _numericBytesScratch = bytes;
            return bytes;
        }

        public unsafe void CopyFromNumeric(ulong value)
        {
            const byte AsciiDigitStart = (byte)'0';

            var block = _block;
            if (block == null)
            {
                return;
            }

            var blockIndex = _index;
            var bytesLeftInBlock = block.Data.Offset + block.Data.Count - blockIndex;
            var start = block.DataFixedPtr + blockIndex;

            if (value < 10)
            {
                if (bytesLeftInBlock < 1)
                {
                    CopyFromNumericOverflow(value);
                    return;
                }
                _index = blockIndex + 1;
                block.End = blockIndex + 1;

                *(start) = (byte)(((uint)value) + AsciiDigitStart);
            }
            else if (value < 100)
            {
                if (bytesLeftInBlock < 2)
                {
                    CopyFromNumericOverflow(value);
                    return;
                }
                _index = blockIndex + 2;
                block.End = blockIndex + 2;

                var val = (uint)value;
                var tens = (byte)((val * 205u) >> 11); // div10, valid to 1028

                *(start)     = (byte)(tens + AsciiDigitStart);
                *(start + 1) = (byte)(val - (tens * 10) + AsciiDigitStart);
            }
            else if (value < 1000)
            {
                if (bytesLeftInBlock < 3)
                {
                    CopyFromNumericOverflow(value);
                    return;
                }
                _index = blockIndex + 3;
                block.End = blockIndex + 3;

                var val      = (uint)value;
                var digit0   = (byte)((val * 41u) >> 12); // div100, valid to 1098
                var digits01 = (byte)((val * 205u) >> 11); // div10, valid to 1028

                *(start)     = (byte)(digit0 + AsciiDigitStart);
                *(start + 1) = (byte)(digits01 - (digit0 * 10) + AsciiDigitStart);
                *(start + 2) = (byte)(val - (digits01 * 10) + AsciiDigitStart);
            }
            else
            {
                CopyFromNumericOverflow(value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void CopyFromNumericOverflow(ulong value)
        {
            const byte AsciiDigitStart = (byte)'0';

            var position = _maxULongByteLength;
            var byteBuffer = NumericBytesScratch;
            do
            {
                // Consider using Math.DivRem() if available
                var quotient = value / 10;
                byteBuffer[--position] = (byte)(AsciiDigitStart + (value - quotient * 10)); // 0x30 = '0'
                value = quotient;
            }
            while (value != 0);

            CopyFrom(byteBuffer, position, _maxULongByteLength - position);
        }

        public unsafe string GetAsciiString(ref MemoryPoolIterator end)
        {
            var block = _block;
            if (block == null || end.IsDefault)
            {
                return null;
            }

            var length = GetLength(end);

            if (length == 0)
            {
                return null;
            }

            var inputOffset = _index;

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
            var block = _block;
            if (block == null || end.IsDefault)
            {
                return default(string);
            }

            var index = _index;
            if (end.Block == block)
            {
                return Encoding.UTF8.GetString(block.Array, index, end.Index - index);
            }

            var decoder = Encoding.UTF8.GetDecoder();

            var length = GetLength(end);
            var charLength = length;
            // Worse case is 1 byte = 1 char
            var chars = new char[charLength];
            var charIndex = 0;

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
            var block = _block;
            if (block == null || end.IsDefault)
            {
                return default(ArraySegment<byte>);
            }

            var index = _index;
            if (end.Block == block)
            {
                return new ArraySegment<byte>(block.Array, index, end.Index - index);
            }

            return GetArraySegmentMultiBlock(ref end);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
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
            return Vector.AsVectorByte(new Vector<uint>(vectorByte * 0x01010101u));
        }
    }
}
