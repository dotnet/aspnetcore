using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// Block tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
    /// individual blocks are then treated as independant array segments.
    /// </summary>
    public class MemoryPoolBlock2
    {
        /// <summary>
        /// Array of "minus one" bytes of the length of SIMD operations on the current hardware. Used as an argument in the
        /// vector dot product that counts matching character occurence.
        /// </summary>
        private static Vector<byte> _dotCount = new Vector<byte>(Byte.MaxValue);

        /// <summary>
        /// Array of negative numbers starting at 0 and continuing for the length of SIMD operations on the current hardware.
        /// Used as an argument in the vector dot product that determines matching character index.
        /// </summary>
        private static Vector<byte> _dotIndex = new Vector<byte>(Enumerable.Range(0, Vector<byte>.Count).Select(x => (byte)-x).ToArray());

        /// <summary>
        /// If this block represents a one-time-use memory object, this GCHandle will hold that memory object at a fixed address
        /// so it can be used in native operations.
        /// </summary>
        private GCHandle _pinHandle;

        /// <summary>
        /// Native address of the first byte of this block's Data memory. It is null for one-time-use memory, or copied from 
        /// the Slab's ArrayPtr for a slab-block segment. The byte it points to corresponds to Data.Array[0], and in practice you will always
        /// use the _dataArrayPtr + Start or _dataArrayPtr + End, which point to the start of "active" bytes, or point to just after the "active" bytes.
        /// </summary>
        private IntPtr _dataArrayPtr;

        /// <summary>
        /// The array segment describing the range of memory this block is tracking. The caller which has leased this block may only read and
        /// modify the memory in this range.
        /// </summary>
        public ArraySegment<byte> Data;

        /// <summary>
        /// This object cannot be instantiated outside of the static Create method
        /// </summary>
        protected MemoryPoolBlock2()
        {
        }

        /// <summary>
        /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
        /// </summary>
        public MemoryPool2 Pool { get; private set; }

        /// <summary>
        /// Back-reference to the slab from which this block was taken, or null if it is one-time-use memory.
        /// </summary>
        public MemoryPoolSlab2 Slab { get; private set; }

        /// <summary>
        /// Convenience accessor
        /// </summary>
        public byte[] Array => Data.Array;

        /// <summary>
        /// The Start represents the offset into Array where the range of "active" bytes begins. At the point when the block is leased
        /// the Start is guaranteed to be equal to Array.Offset. The value of Start may be assigned anywhere between Data.Offset and
        /// Data.Offset + Data.Count, and must be equal to or less than End.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The End represents the offset into Array where the range of "active" bytes ends. At the point when the block is leased
        /// the End is guaranteed to be equal to Array.Offset. The value of Start may be assigned anywhere between Data.Offset and
        /// Data.Offset + Data.Count, and must be equal to or less than End.
        /// </summary>
        public int End { get; set; }


        /// <summary>
        /// Reference to the next block of data when the overall "active" bytes spans multiple blocks. At the point when the block is
        /// leased Next is guaranteed to be null. Start, End, and Next are used together in order to create a linked-list of discontiguous 
        /// working memory. The "active" memory is grown when bytes are copied in, End is increased, and Next is assigned. The "active" 
        /// memory is shrunk when bytes are consumed, Start is increased, and blocks are returned to the pool.
        /// </summary>
        public MemoryPoolBlock2 Next { get; set; }

        ~MemoryPoolBlock2()
        {
            if (_pinHandle.IsAllocated)
            {
                // if this is a one-time-use block, ensure that the GCHandle does not leak
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

        /// <summary>
        /// Called to ensure that a block is pinned, and return the pointer to native memory just after
        /// the range of "active" bytes. This is where arriving data is read into.
        /// </summary>
        /// <returns></returns>
        public IntPtr Pin()
        {
            Debug.Assert(!_pinHandle.IsAllocated);

            if (_dataArrayPtr != IntPtr.Zero)
            {
                // this is a slab managed block - use the native address of the slab which is always locked
                return _dataArrayPtr + End;
            }
            else
            {
                // this is one-time-use memory - lock the managed memory until Unpin is called
                _pinHandle = GCHandle.Alloc(Data.Array, GCHandleType.Pinned);
                return _pinHandle.AddrOfPinnedObject() + End;
            }
        }

        public void Unpin()
        {
            if (_dataArrayPtr == IntPtr.Zero)
            {
                // this is one-time-use memory - unlock the managed memory
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

        /// <summary>
        /// called when the block is returned to the pool. mutable values are re-assigned to their guaranteed initialized state.
        /// </summary>
        public void Reset()
        {
            Next = null;
            Start = Data.Offset;
            End = Data.Offset;
        }

        /// <summary>
        /// ToString overridden for debugger convenience. This displays the "active" byte information in this block as ASCII characters.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Encoding.ASCII.GetString(Array, Start, End - Start);
        }

        /// <summary>
        /// acquires a cursor pointing into this block at the Start of "active" byte information
        /// </summary>
        /// <returns></returns>
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
                        for (var block = _block.Next; block != null; block = block.Next)
                        {
                            if (block.Start < block.End)
                            {
                                return true;
                            }
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
                        for (; following != 0; following--, index++)
                        {
                            if (block.Array[index] == byte0)
                            {
                                _block = block;
                                _index = index;
                                return char0;
                            }
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
                        for (; following != 0; following--, index++)
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
                        }
                    }
                }
            }

            public int GetLength(Iterator end)
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

            public string GetString(Iterator end)
            {
                if (IsDefault || end.IsDefault)
                {
                    return default(string);
                }
                if (end._block == _block)
                {
                    return Encoding.UTF8.GetString(_block.Array, _index, end._index - _index);
                }

                var decoder = Encoding.ASCII.GetDecoder();

                var length = GetLength(end);
                var charLength = length * 2;
                var chars = new char[charLength];
                var charIndex = 0;

                var block = _block;
                var index = _index;
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

                var length = GetLength(end);
                var array = new byte[length];
                CopyTo(array, 0, length, out length);
                return new ArraySegment<byte>(array, 0, length);
            }

            public Iterator CopyTo(byte[] array, int offset, int count, out int actual)
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
                        Buffer.BlockCopy(block.Array, index, array, offset, remaining);
                        return new Iterator(block, index + remaining);
                    }
                    else if (block.Next == null)
                    {
                        actual = count - remaining + following;
                        Buffer.BlockCopy(block.Array, index, array, offset, following);
                        return new Iterator(block, index + following);
                    }
                    else
                    {
                        Buffer.BlockCopy(block.Array, index, array, offset, following);
                        remaining -= following;
                        block = block.Next;
                        index = block.Start;
                    }
                }
            }
        }
    }
}
