using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    internal static class WritableBufferExtensions
    {
        public static void WriteFast(this WritableBuffer buffer, ReadOnlySpan<byte> source)
        {
            if (buffer.Memory.IsEmpty)
            {
                buffer.Ensure();
            }

            // Fast path, try copying to the available memory directly
            if (source.Length <= buffer.Memory.Length)
            {
                source.CopyToFast(buffer.Memory.Span);
                buffer.Advance(source.Length);
                return;
            }

            var remaining = source.Length;
            var offset = 0;

            while (remaining > 0)
            {
                var writable = Math.Min(remaining, buffer.Memory.Length);

                buffer.Ensure(writable);

                if (writable == 0)
                {
                    continue;
                }

                source.Slice(offset, writable).CopyToFast(buffer.Memory.Span);

                remaining -= writable;
                offset += writable;

                buffer.Advance(writable);
            }
        }

        private unsafe static void CopyToFast(this ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                throw new InvalidOperationException();
            }

            // Assume it fits
            fixed (byte* pSource = &source.DangerousGetPinnableReference())
            fixed (byte* pDest = &destination.DangerousGetPinnableReference())
            {
                Buffer.MemoryCopy(pSource, pDest, destination.Length, source.Length);
            }
        }
    }
}
