using System;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public interface IMemoryPool
    {
        byte[] Empty { get; }

        byte[] AllocByte(int minimumSize);
        void FreeByte(byte[] memory);

        char[] AllocChar(int minimumSize);
        void FreeChar(char[] memory);

        /// <summary>
        ///   Acquires a sub-segment of a larger memory allocation. Used for async sends of write-behind
        ///   buffers to reduce number of array segments pinned
        /// </summary>
        /// <param name = "minimumSize">The smallest length of the ArraySegment.Count that may be returned</param>
        /// <returns>An array segment which is a sub-block of a larger allocation</returns>
        ArraySegment<byte> AllocSegment(int minimumSize);

        /// <summary>
        ///   Frees a sub-segment of a larger memory allocation produced by AllocSegment. The original ArraySegment
        ///   must be frees exactly once and must have the same offset and count that was returned by the Alloc.
        ///   If a segment is not freed it won't be re-used and has the same effect as a memory leak, so callers must be
        ///   implemented exactly correctly.
        /// </summary>
        /// <param name = "segment">The sub-block that was originally returned by a call to AllocSegment.</param>
        void FreeSegment(ArraySegment<byte> segment);
    }
}