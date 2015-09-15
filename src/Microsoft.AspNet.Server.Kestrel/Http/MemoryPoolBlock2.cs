using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class MemoryPoolBlock2
    {
        private static Vector<byte> _dotIndex = new Vector<byte>(Enumerable.Range(0, Vector<byte>.Count).Select(x => (byte)-x).ToArray());
        private static Vector<byte> _dotCount = new Vector<byte>(Byte.MaxValue);

        private GCHandle _pinHandle;
        private IntPtr _dataArrayPtr;

        public ArraySegment<byte> Data;

        protected MemoryPoolBlock2()
        {
        }

        public MemoryPool2 Pool { get; private set; }

        public MemoryPoolSlab2 Slab { get; private set; }

        public byte[] Array => Data.Array;
        public int Start { get; set; }
        public int End { get; set; }
        public MemoryPoolBlock2 Next { get; set; }

        ~MemoryPoolBlock2()
        {
            if (_pinHandle.IsAllocated)
            {
                _pinHandle.Free();
            }

            if (Slab != null && Slab.IsActive)
            {
                Pool.Return(new MemoryPoolBlock2
                {
                    _dataArrayPtr = _dataArrayPtr,
                    Data = Data,
                    Pool = Pool,
                    Slab = Slab,
                });
            }
        }

        public IntPtr Pin()
        {
            Debug.Assert(!_pinHandle.IsAllocated);

            if (_dataArrayPtr != IntPtr.Zero)
            {
                return _dataArrayPtr + End;
            }
            else
            {
                _pinHandle = GCHandle.Alloc(Data.Array, GCHandleType.Pinned);
                return _pinHandle.AddrOfPinnedObject() + End;
            }
        }

        public void Unpin()
        {
            if (_dataArrayPtr == IntPtr.Zero)
            {
                Debug.Assert(_pinHandle.IsAllocated);
                _pinHandle.Free();
            }
        }

        public static MemoryPoolBlock2 Create(int size, MemoryPool2 pool)
        {
            return new MemoryPoolBlock2
            {
                Data = new ArraySegment<byte>(new byte[size]),
                Pool = pool
            };
        }

        public static MemoryPoolBlock2 Create(
            ArraySegment<byte> data,
            IntPtr dataPtr,
            MemoryPool2 pool,
            MemoryPoolSlab2 slab)
        {
            return new MemoryPoolBlock2
            {
                Data = data,
                _dataArrayPtr = dataPtr,
                Pool = pool,
                Slab = slab,
                Start = data.Offset,
                End = data.Offset,
            };
        }

        public void Reset()
        {
            Next = null;
            Start = Data.Offset;
            End = Data.Offset;
        }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(Array, Start, End - Start);
        }

        public Iterator GetIterator()
        {
            return new Iterator(this);
        }

        public struct Iterator
        {
            private MemoryPoolBlock2 _block;
            private int _index;

            public Iterator(MemoryPoolBlock2 block)
            {
                _block = block;
                _index = _block?.Start ?? 0;
            }
            public Iterator(MemoryPoolBlock2 block, int index)
            {
                _block = block;
                _index = index;
            }

            public bool IsDefault => _block == null;

            public MemoryPoolBlock2 Block => _block;

            public int Index => _index;

            public bool HasAtLeast(int count)
            {
                var scan = _block;
                var index = _index;
                while (scan != null)
                {
                    if (count <= scan.End - index)
                    {
                        return true;
                    }
                    count -= scan.End - index;
                    scan = scan.Next;
                    index = scan?.Start ?? 0;
                }
                return false;
            }

            public Iterator Add(int count)
            {
                var block = _block;
                var index = _index;
                while (block != null)
                {
                    var tailCount = block.End - index;
                    if (count < tailCount)
                    {
                        return new Iterator(block, index + count);
                    }
                    count -= tailCount;
                    block = block.Next;
                    index = block?.Start ?? 0;
                }
                return new Iterator(block, index + count);
            }

            public Iterator CopyTo(byte[] array, int offset, int count, out int actual)
            {
                var block = _block;
                var index = _index;
                var remaining = count;
                while (block != null && remaining != 0)
                {
                    var copyLength = Math.Min(remaining, block.End - index);
                    Buffer.BlockCopy(block.Array, index, array, offset, copyLength);
                    index += copyLength;
                    offset += copyLength;
                    remaining -= copyLength;
                    if (index == block.End)
                    {
                        block = block.Next;
                        index = block?.Start ?? 0;
                    }
                }
                actual = count - remaining;
                return new Iterator(block, index);
            }

            public char MoveNext()
            {
                var block = _block;
                var index = _index;
                while (block != null && index == block.End)
                {
                    block = block.Next;
                    index = block?.Start ?? 0;
                }
                if (block != null)
                {
                    ++index;
                }
                while (block != null && index == block.End)
                {
                    block = block.Next;
                    index = block?.Start ?? 0;
                }
                _block = block;
                _index = index;
                return block != null ? (char)block.Array[index] : char.MinValue;
            }

            public int Peek()
            {
                while (_block != null)
                {
                    if (_index < _block.End)
                    {
                        return _block.Data.Array[_index];
                    }
                    _block = _block.Next;
                    _index = _block.Start;
                }
                return -1;
            }

            public Iterator IndexOf(char char0)
            {
                var byte0 = (byte)char0;
                var vectorStride = Vector<byte>.Count;
                var ch0Vector = new Vector<byte>(byte0);

                var scanBlock = _block;
                var scanArray = scanBlock?.Array;
                var scanIndex = _index;
                while (scanBlock != null)
                {
                    var tailCount = scanBlock.End - scanIndex;
                    if (tailCount == 0)
                    {
                        scanBlock = scanBlock.Next;
                        scanArray = scanBlock?.Array;
                        scanIndex = scanBlock?.Start ?? 0;
                        continue;
                    }
                    if (tailCount >= vectorStride)
                    {
                        var data = new Vector<byte>(scanBlock.Array, scanIndex);
                        var ch0Equals = Vector.Equals(data, ch0Vector);
                        var ch0Count = Vector.Dot(ch0Equals, _dotCount);

                        if (ch0Count == 0)
                        {
                            scanIndex += vectorStride;
                            continue;
                        }
                        else if (ch0Count == 1)
                        {
                            return new Iterator(scanBlock, scanIndex + Vector.Dot(ch0Equals, _dotIndex));
                        }
                        else
                        {
                            tailCount = vectorStride;
                        }
                    }
                    for (; tailCount != 0; tailCount--, scanIndex++)
                    {
                        var ch = scanBlock.Array[scanIndex];
                        if (ch == byte0)
                        {
                            return new Iterator(scanBlock, scanIndex);
                        }
                    }
                }
                return new Iterator(null, 0);
            }

            public Iterator IndexOfAny(char char0, char char1, out char chFound)
            {
                var byte0 = (byte)char0;
                var byte1 = (byte)char1;
                var vectorStride = Vector<byte>.Count;
                var ch0Vector = new Vector<byte>(byte0);
                var ch1Vector = new Vector<byte>(byte1);

                var scanBlock = _block;
                var scanArray = scanBlock?.Array;
                var scanIndex = _index;
                while (scanBlock != null)
                {
                    var tailCount = scanBlock.End - scanIndex;
                    if (tailCount == 0)
                    {
                        scanBlock = scanBlock.Next;
                        scanArray = scanBlock?.Array;
                        scanIndex = scanBlock?.Start ?? 0;
                        continue;
                    }
                    if (tailCount >= vectorStride)
                    {
                        var data = new Vector<byte>(scanBlock.Array, scanIndex);
                        var ch0Equals = Vector.Equals(data, ch0Vector);
                        var ch0Count = Vector.Dot(ch0Equals, _dotCount);
                        var ch1Equals = Vector.Equals(data, ch1Vector);
                        var ch1Count = Vector.Dot(ch1Equals, _dotCount);

                        if (ch0Count == 0 && ch1Count == 0)
                        {
                            scanIndex += vectorStride;
                            continue;
                        }
                        else if (ch0Count < 2 && ch1Count < 2)
                        {
                            var ch0Index = ch0Count == 1 ? Vector.Dot(ch0Equals, _dotIndex) : byte.MaxValue;
                            var ch1Index = ch1Count == 1 ? Vector.Dot(ch1Equals, _dotIndex) : byte.MaxValue;
                            if (ch0Index < ch1Index)
                            {
                                chFound = char0;
                                return new Iterator(scanBlock, scanIndex + ch0Index);
                            }
                            else
                            {
                                chFound = char1;
                                return new Iterator(scanBlock, scanIndex + ch1Index);
                            }
                        }
                        else
                        {
                            tailCount = vectorStride;
                        }
                    }
                    for (; tailCount != 0; tailCount--, scanIndex++)
                    {
                        var chIndex = scanBlock.Array[scanIndex];
                        if (chIndex == byte0)
                        {
                            chFound = char0;
                            return new Iterator(scanBlock, scanIndex);
                        }
                        else if (chIndex == byte1)
                        {
                            chFound = char1;
                            return new Iterator(scanBlock, scanIndex);
                        }
                    }
                }
                chFound = char.MinValue;
                return new Iterator(null, 0);
            }

            public int GetLength(Iterator end)
            {
                var length = 0;
                var block = _block;
                var index = _index;
                for (;;)
                {
                    if (block == end._block)
                    {
                        return length + end._index - index;
                    }
                    if (block == null)
                    {
                        throw new Exception("end was not after iterator");
                    }
                    length += block.End - index;
                    block = block.Next;
                    index = block?.Start ?? 0;
                }
            }

            public string GetString(Iterator end)
            {
                if (IsDefault || end.IsDefault)
                {
                    return default(string);
                }
                if (end._block == _block)
                {
                    return Encoding.ASCII.GetString(_block.Array, _index, end._index - _index);
                }
                if (end._block == _block.Next && end._index == end._block.Start)
                {
                    return Encoding.ASCII.GetString(_block.Array, _index, _block.End - _index);
                }

                var length = GetLength(end);
                var result = new char[length];
                var offset = 0;
                var decoder = Encoding.ASCII.GetDecoder();

                var block = _block;
                var index = _index;
                while (length != 0)
                {
                    if (block == null)
                    {
                        throw new Exception("Unexpected end of data");
                    }

                    var count = Math.Min(block.End - index, length);

                    int bytesUsed;
                    int textAdded;
                    bool completed;
                    decoder.Convert(
                        block.Array,
                        index,
                        count,
                        result,
                        offset,
                        length,
                        count == length,
                        out bytesUsed,
                        out textAdded,
                        out completed);

                    Debug.Assert(bytesUsed == count);
                    Debug.Assert(textAdded == count);
                    offset += count;
                    length -= count;

                    block = block.Next;
                    index = block?.Start ?? 0;
                }
                return new string(result);
            }

            public ArraySegment<byte> GetArraySegment(Iterator end)
            {
                if (IsDefault || end.IsDefault)
                {
                    return default(ArraySegment<byte>);
                }
                if (end._block == _block)
                {
                    return new ArraySegment<byte>(_block.Array, _index, end._index - _index);
                }
                if (end._block == _block.Next && end._index == end._block.Start)
                {
                    return new ArraySegment<byte>(_block.Array, _index, _block.End - _index);
                }

                var length = GetLength(end);
                var result = new ArraySegment<byte>(new byte[length]);
                var offset = result.Offset;

                var block = _block;
                var index = _index;
                while (length != 0)
                {
                    if (block == null)
                    {
                        throw new Exception("Unexpected end of data");
                    }

                    var count = Math.Min(block.End - index, length);
                    Buffer.BlockCopy(block.Array, index, result.Array, offset, count);
                    offset += count;
                    length -= count;

                    block = block.Next;
                    index = block?.Start ?? 0;
                }

                return result;
            }
        }
    }
}
