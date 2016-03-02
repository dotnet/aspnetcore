using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    /// <summary>
    /// Block tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
    /// individual blocks are then treated as independant array segments.
    /// </summary>
    public class MemoryPoolBlock
    {
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
        protected MemoryPoolBlock()
        {
        }

        /// <summary>
        /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
        /// </summary>
        public MemoryPool Pool { get; private set; }

        /// <summary>
        /// Back-reference to the slab from which this block was taken, or null if it is one-time-use memory.
        /// </summary>
        public MemoryPoolSlab Slab { get; private set; }

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
        public MemoryPoolBlock Next { get; set; }

        ~MemoryPoolBlock()
        {
            Debug.Assert(!_pinHandle.IsAllocated, "Ad-hoc memory block wasn't unpinned");
            Debug.Assert(Slab == null || !Slab.IsActive, "Block being garbage collected instead of returned to pool");

            if (_pinHandle.IsAllocated)
            {
                // if this is a one-time-use block, ensure that the GCHandle does not leak
                _pinHandle.Free();
            }

            if (Slab != null && Slab.IsActive)
            {
                Pool.Return(new MemoryPoolBlock
                {
                    _dataArrayPtr = _dataArrayPtr,
                    Data = Data,
                    Pool = Pool,
                    Slab = Slab,
                });
            }
        }

        /// <summary>
        /// Called to ensure that a block is pinned, and return the pointer to the native address
        /// of the first byte of this block's Data memory. Arriving data is read into Pin() + End.
        /// Outgoing data is read from Pin() + Start.
        /// </summary>
        /// <returns></returns>
        public IntPtr Pin()
        {
            Debug.Assert(!_pinHandle.IsAllocated);

            if (_dataArrayPtr != IntPtr.Zero)
            {
                // this is a slab managed block - use the native address of the slab which is always locked
                return _dataArrayPtr;
            }
            else
            {
                // this is one-time-use memory - lock the managed memory until Unpin is called
                _pinHandle = GCHandle.Alloc(Data.Array, GCHandleType.Pinned);
                return _pinHandle.AddrOfPinnedObject();
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

        public static MemoryPoolBlock Create(
            ArraySegment<byte> data,
            IntPtr dataPtr,
            MemoryPool pool,
            MemoryPoolSlab slab)
        {
            return new MemoryPoolBlock
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
        public MemoryPoolIterator GetIterator()
        {
            return new MemoryPoolIterator(this);
        }
    }
}
