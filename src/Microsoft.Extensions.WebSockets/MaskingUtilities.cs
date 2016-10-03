using System;
using System.Binary;
using Channels;

namespace Microsoft.Extensions.WebSockets
{
    internal static class MaskingUtilities
    {
        // Plenty of optimization to be done here but not our immediate priority right now.
        // Including: Vectorization, striding by uints (even when not vectorized; we'd probably flip the
        // overload that does the implementation in that case and do it in the uint version).

        public static void ApplyMask(ref ReadableBuffer payload, uint maskingKey)
        {
            unsafe
            {
                // Write the masking key as bytes to simplify access. Use a stackalloc buffer because it's fixed-size
                var maskingKeyBytes = stackalloc byte[4];
                var maskingKeySpan = new Span<byte>(maskingKeyBytes, 4);
                maskingKeySpan.WriteBigEndian(maskingKey);

                ApplyMask(ref payload, maskingKeySpan);
            }
        }

        public static void ApplyMask(ref ReadableBuffer payload, Span<byte> maskingKey)
        {
            var offset = 0;
            foreach (var mem in payload)
            {
                var span = mem.Span;
                ApplyMask(span, maskingKey, ref offset);
                offset += span.Length;
            }
        }

        public static void ApplyMask(Span<byte> payload, Span<byte> maskingKey)
        {
            var i = 0;
            ApplyMask(payload, maskingKey, ref i);
        }

        private static void ApplyMask(Span<byte> payload, Span<byte> maskingKey, ref int maskingKeyOffset)
        {
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(payload[i] ^ maskingKey[maskingKeyOffset % 4]);
                maskingKeyOffset++;
            }
        }
    }
}
