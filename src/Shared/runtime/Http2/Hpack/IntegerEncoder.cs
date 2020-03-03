// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.HPack
{
    internal static class IntegerEncoder
    {
        /// <summary>
        /// The maximum bytes required to encode a 32-bit int, regardless of prefix length.
        /// </summary>
        public const int MaxInt32EncodedLength = 6;

        /// <summary>
        /// Encodes an integer into one or more bytes.
        /// </summary>
        /// <param name="value">The value to encode. Must not be negative.</param>
        /// <param name="numBits">The length of the prefix, in bits, to encode <paramref name="value"/> within. Must be between 1 and 8.</param>
        /// <param name="destination">The destination span to encode <paramref name="value"/> to.</param>
        /// <param name="bytesWritten">The number of bytes used to encode <paramref name="value"/>.</param>
        /// <returns>If <paramref name="destination"/> had enough storage to encode <paramref name="value"/>, true. Otherwise, false.</returns>
        public static bool Encode(int value, int numBits, Span<byte> destination, out int bytesWritten)
        {
            Debug.Assert(value >= 0);
            Debug.Assert(numBits >= 1 && numBits <= 8);

            if (destination.Length == 0)
            {
                bytesWritten = 0;
                return false;
            }

            destination[0] &= MaskHigh(8 - numBits);

            if (value < (1 << numBits) - 1)
            {
                destination[0] |= (byte)value;

                bytesWritten = 1;
                return true;
            }
            else
            {
                destination[0] |= (byte)((1 << numBits) - 1);

                if (1 == destination.Length)
                {
                    bytesWritten = 0;
                    return false;
                }

                value = value - ((1 << numBits) - 1);
                int i = 1;

                while (value >= 128)
                {
                    destination[i++] = (byte)(value % 128 + 128);

                    if (i >= destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    value = value / 128;
                }
                destination[i++] = (byte)value;

                bytesWritten = i;
                return true;
            }
        }

        private static byte MaskHigh(int n) => (byte)(sbyte.MinValue >> (n - 1));
    }
}
