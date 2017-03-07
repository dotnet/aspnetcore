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
    }
}
