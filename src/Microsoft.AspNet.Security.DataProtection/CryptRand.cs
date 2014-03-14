using System;

namespace Microsoft.AspNet.Security.DataProtection
{
    /// <summary>
    /// Helper class to populate buffers with cryptographically random data.
    /// </summary>
    public static class CryptRand
    {
        /// <summary>
        /// Populates a buffer with cryptographically random data.
        /// </summary>
        /// <param name="buffer">The buffer to populate.</param>
        public static unsafe void FillBuffer(ArraySegment<byte> buffer)
        {
            // the ArraySegment<> ctor performs bounds checking
            var unused = new ArraySegment<byte>(buffer.Array, buffer.Offset, buffer.Count);

            if (buffer.Count != 0)
            {
                fixed (byte* pBuffer = &buffer.Array[buffer.Offset])
                {
                    BCryptUtil.GenRandom(pBuffer, buffer.Count);
                }
            }
        }
    }
}
