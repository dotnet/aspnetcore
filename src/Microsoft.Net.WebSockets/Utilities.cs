using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Net.WebSockets
{
    public static class Utilities
    {
        // Copies the header and data into a new buffer and masks the data.
        public static byte[] MergeAndMask(int mask, ArraySegment<byte> header, ArraySegment<byte> data)
        {
            byte[] frame = new byte[header.Count + data.Count];
            Array.Copy(header.Array, header.Offset, frame, 0, header.Count);
            Array.Copy(data.Array, data.Offset, frame, header.Count, data.Count);

            Mask(mask, new ArraySegment<byte>(frame, header.Count, data.Count));
            return frame;
        }

        // Un/Masks the data in place
        public static void Mask(int mask, ArraySegment<byte> data)
        {
            if (mask == 0)
            {
                return;
            }

            byte[] maskBytes = new byte[]
            {
                (byte)(mask >> 24),
                (byte)(mask >> 16),
                (byte)(mask >> 8),
                (byte)mask,
            };
            int maskOffset = 0;

            for (int i = data.Offset; i < data.Offset + data.Count; i++)
            {
                data.Array[i] = (byte)(data.Array[i] ^ maskBytes[maskOffset]);
                maskOffset = (maskOffset + 1) % 4;
            }
        }
    }
}
