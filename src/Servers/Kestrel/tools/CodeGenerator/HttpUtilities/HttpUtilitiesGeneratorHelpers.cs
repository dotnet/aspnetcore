// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace CodeGenerator.HttpUtilities;

internal sealed class HttpUtilitiesGeneratorHelpers
{
    public sealed class ShiftInfo<TMask>
    {
        public TMask Mask;
        public byte Shift;
    }

    public static ShiftInfo<ulong>[] GetShifts(ulong mask)
    {
        var shifts = new List<ShiftInfo<ulong>>();

        const ulong one = 0x01;

        ulong currentMask = 0;

        int currentBitsCount = 0;
        int lastShift = 0;
        for (int i = 0; i < sizeof(ulong) * 8; i++)
        {
            var currentBitMask = one << i;
            bool isCurrentBit0 = (currentBitMask & mask) == 0;

            if (isCurrentBit0 == false)
            {
                currentMask |= currentBitMask;
                currentBitsCount++;
            }
            else if (currentBitsCount > 0)
            {
                var currentShift = (byte)(i - currentBitsCount - lastShift);
                shifts.Add(new ShiftInfo<ulong>
                {
                    Mask = currentMask,
                    Shift = currentShift
                });
                lastShift = currentShift;
                currentMask = 0;
                currentBitsCount = 0;
            }
        }

        return shifts.ToArray();
    }

    public static ulong? SearchKeyByLookThroughMaskCombinations(ulong[] values, byte bitsIndexStart, byte bitsLength, byte bitsCount)
    {
        if (bitsIndexStart + bitsLength > sizeof(ulong) * 8)
        {
            throw new ArgumentOutOfRangeException(nameof(bitsIndexStart));
        }

        if (bitsLength < bitsCount || bitsCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitsCount));
        }

        var bits = new byte[bitsLength];

        for (byte i = bitsIndexStart; i < bitsIndexStart + bitsLength; i++)
        {
            bits[i - bitsIndexStart] = i;
        }

        var combinations = new CombinationsWithoutRepetition<byte>(bits, bitsCount);

        ulong? maskFound = null;
        int bit1ChunksFoundMask = 0;

        int arrayLength = values.Length;

        var mashHash = new HashSet<ulong>();

        while (combinations.MoveNext())
        {
            var bitsCombination = combinations.Current;

            ulong currentMask = 0;

            for (int i = 0; i < bitsCombination.Length; i++)
            {
                var index = bitsCombination[i];

                const ulong oneBit = 0x01;

                currentMask |= oneBit << index;
            }

            mashHash.Clear();
            bool invalidMask = false;
            for (int j = 0; j < arrayLength; j++)
            {
                var tmp = values[j] & currentMask;

                bool alreadyExists = mashHash.Add(tmp) == false;
                if (alreadyExists)
                {
                    invalidMask = true;
                    break;
                }
            }

            if (invalidMask == false)
            {
                var bit1Chunks = CountBit1Chunks(currentMask);

                if (maskFound.HasValue)
                {
                    if (bit1ChunksFoundMask > bit1Chunks)
                    {
                        maskFound = currentMask;
                        bit1ChunksFoundMask = bit1Chunks;
                        if (bit1ChunksFoundMask == 0)
                        {
                            return maskFound;
                        }
                    }
                }
                else
                {
                    maskFound = currentMask;
                    bit1ChunksFoundMask = bit1Chunks;

                    if (bit1ChunksFoundMask == 0)
                    {
                        return maskFound;
                    }
                }
            }
        }

        return maskFound;
    }

    public static int CountBit1Chunks(ulong mask)
    {
        int currentBitsCount = 0;

        int chunks = 0;

        for (int i = 0; i < sizeof(ulong) * 8; i++)
        {
            const ulong oneBit = 0x01;

            var currentBitMask = oneBit << i;
            bool isCurrentBit0 = (currentBitMask & mask) == 0;

            if (isCurrentBit0 == false)
            {
                currentBitsCount++;
            }
            else if (currentBitsCount > 0)
            {
                chunks++;
                currentBitsCount = 0;
            }
        }

        return chunks;
    }

    public static string GeHexString(byte[] array, string prefix, string separator)
    {
        var result = new StringBuilder();
        int i = 0;
        for (; i < array.Length - 1; i++)
        {
            result.AppendFormat(CultureInfo.InvariantCulture, "{0}{1:x2}", prefix, array[i]);
            result.Append(separator);
        }

        if (array.Length > 0)
        {
            result.AppendFormat(CultureInfo.InvariantCulture, "{0}{1:x2}", prefix, array[i]);
        }

        return result.ToString();
    }

    public static string MaskToString(ulong mask)
    {
        var maskSizeInBIts = Math.Log(mask, 2);
        var hexMaskSize = Math.Ceiling(maskSizeInBIts / 4.0);
        return string.Format(CultureInfo.InvariantCulture, "0x{0:X" + hexMaskSize + "}", mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountBits(ulong v)
    {
        const ulong Mask01010101 = 0x5555555555555555UL;
        const ulong Mask00110011 = 0x3333333333333333UL;
        const ulong Mask00001111 = 0x0F0F0F0F0F0F0F0FUL;
        const ulong Mask00000001 = 0x0101010101010101UL;
        v = v - ((v >> 1) & Mask01010101);
        v = (v & Mask00110011) + ((v >> 2) & Mask00110011);
        return (int)(unchecked(((v + (v >> 4)) & Mask00001111) * Mask00000001) >> 56);
    }

    public static string MaskToHexString(ulong mask)
    {
        var maskSizeInBIts = Math.Log(mask, 2);
        var hexMaskSize = (byte)Math.Ceiling(maskSizeInBIts / 4);

        return string.Format(CultureInfo.InvariantCulture, "0x{0:X" + (hexMaskSize == 0 ? 1 : hexMaskSize) + "}", mask);
    }
}
