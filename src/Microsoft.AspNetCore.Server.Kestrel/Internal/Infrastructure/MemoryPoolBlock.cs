using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    /// <summary>
    /// Block tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
    /// individual blocks are then treated as independent array segments.
    /// </summary>
    public class MemoryPoolBlock
    {
        /// <summary>
        /// Native address of the first byte of this block's Data memory. It is null for one-time-use memory, or copied from 
        /// the Slab's ArrayPtr for a slab-block segment. The byte it points to corresponds to Data.Array[0], and in practice you will always
        /// use the DataArrayPtr + Start or DataArrayPtr + End, which point to the start of "active" bytes, or point to just after the "active" bytes.
        /// </summary>
        public readonly IntPtr DataArrayPtr;

        internal unsafe readonly byte* DataFixedPtr;

        /// <summary>
        /// The array segment describing the range of memory this block is tracking. The caller which has leased this block may only read and
        /// modify the memory in this range.
        /// </summary>
        public ArraySegment<byte> Data;

        /// <summary>
        /// This object cannot be instantiated outside of the static Create method
        /// </summary>
        unsafe protected MemoryPoolBlock(IntPtr dataArrayPtr)
        {
            DataArrayPtr = dataArrayPtr;
            DataFixedPtr = (byte*)dataArrayPtr.ToPointer();
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
        public int Start;

        /// <summary>
        /// The End represents the offset into Array where the range of "active" bytes ends. At the point when the block is leased
        /// the End is guaranteed to be equal to Array.Offset. The value of Start may be assigned anywhere between Data.Offset and
        /// Data.Offset + Data.Count, and must be equal to or less than End.
        /// </summary>
        public volatile int End;

        /// <summary>
        /// Reference to the next block of data when the overall "active" bytes spans multiple blocks. At the point when the block is
        /// leased Next is guaranteed to be null. Start, End, and Next are used together in order to create a linked-list of discontinuous 
        /// working memory. The "active" memory is grown when bytes are copied in, End is increased, and Next is assigned. The "active" 
        /// memory is shrunk when bytes are consumed, Start is increased, and blocks are returned to the pool.
        /// </summary>
        public MemoryPoolBlock Next;

        ~MemoryPoolBlock()
        {
            Debug.Assert(Slab == null || !Slab.IsActive, "Block being garbage collected instead of returned to pool");

            if (Slab != null && Slab.IsActive)
            {
                Pool.Return(new MemoryPoolBlock(DataArrayPtr)
                {
                    Data = Data,
                    Pool = Pool,
                    Slab = Slab,
                });
            }
        }

        internal static MemoryPoolBlock Create(
            ArraySegment<byte> data,
            IntPtr dataPtr,
            MemoryPool pool,
            MemoryPoolSlab slab)
        {
            return new MemoryPoolBlock(dataPtr)
            {
                Data = data,
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
