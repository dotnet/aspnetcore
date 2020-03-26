using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal static class MemoryPoolExtensions
    {
        /// <summary>
        /// Computes a minimum segment size
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public static int GetMinimumSegmentSize(this MemoryPool<byte> pool)
        {
            if (pool == null)
            {
                return 4096;
            }

            return Math.Min(4096, pool.MaxBufferSize);
        }

        public static int GetMinimumAllocSize(this MemoryPool<byte> pool)
        {
            // 1/2 of a segment
            return pool.GetMinimumSegmentSize() / 2;
        }
    }
}
