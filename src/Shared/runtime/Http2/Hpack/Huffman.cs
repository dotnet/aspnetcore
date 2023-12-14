// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Threading;

namespace System.Net.Http.HPack
{
    internal static class Huffman
    {
        // HPack static huffman code. see: https://httpwg.org/specs/rfc7541.html#huffman.code
        // Stored into two tables to optimize its initialization and memory consumption.
        private static ReadOnlySpan<uint> EncodingTableCodes => // 257
        [
            0b11111111_11000000_00000000_00000000,
            0b11111111_11111111_10110000_00000000,
            0b11111111_11111111_11111110_00100000,
            0b11111111_11111111_11111110_00110000,
            0b11111111_11111111_11111110_01000000,
            0b11111111_11111111_11111110_01010000,
            0b11111111_11111111_11111110_01100000,
            0b11111111_11111111_11111110_01110000,
            0b11111111_11111111_11111110_10000000,
            0b11111111_11111111_11101010_00000000,
            0b11111111_11111111_11111111_11110000,
            0b11111111_11111111_11111110_10010000,
            0b11111111_11111111_11111110_10100000,
            0b11111111_11111111_11111111_11110100,
            0b11111111_11111111_11111110_10110000,
            0b11111111_11111111_11111110_11000000,
            0b11111111_11111111_11111110_11010000,
            0b11111111_11111111_11111110_11100000,
            0b11111111_11111111_11111110_11110000,
            0b11111111_11111111_11111111_00000000,
            0b11111111_11111111_11111111_00010000,
            0b11111111_11111111_11111111_00100000,
            0b11111111_11111111_11111111_11111000,
            0b11111111_11111111_11111111_00110000,
            0b11111111_11111111_11111111_01000000,
            0b11111111_11111111_11111111_01010000,
            0b11111111_11111111_11111111_01100000,
            0b11111111_11111111_11111111_01110000,
            0b11111111_11111111_11111111_10000000,
            0b11111111_11111111_11111111_10010000,
            0b11111111_11111111_11111111_10100000,
            0b11111111_11111111_11111111_10110000,
            0b01010000_00000000_00000000_00000000,
            0b11111110_00000000_00000000_00000000,
            0b11111110_01000000_00000000_00000000,
            0b11111111_10100000_00000000_00000000,
            0b11111111_11001000_00000000_00000000,
            0b01010100_00000000_00000000_00000000,
            0b11111000_00000000_00000000_00000000,
            0b11111111_01000000_00000000_00000000,
            0b11111110_10000000_00000000_00000000,
            0b11111110_11000000_00000000_00000000,
            0b11111001_00000000_00000000_00000000,
            0b11111111_01100000_00000000_00000000,
            0b11111010_00000000_00000000_00000000,
            0b01011000_00000000_00000000_00000000,
            0b01011100_00000000_00000000_00000000,
            0b01100000_00000000_00000000_00000000,
            0b00000000_00000000_00000000_00000000,
            0b00001000_00000000_00000000_00000000,
            0b00010000_00000000_00000000_00000000,
            0b01100100_00000000_00000000_00000000,
            0b01101000_00000000_00000000_00000000,
            0b01101100_00000000_00000000_00000000,
            0b01110000_00000000_00000000_00000000,
            0b01110100_00000000_00000000_00000000,
            0b01111000_00000000_00000000_00000000,
            0b01111100_00000000_00000000_00000000,
            0b10111000_00000000_00000000_00000000,
            0b11111011_00000000_00000000_00000000,
            0b11111111_11111000_00000000_00000000,
            0b10000000_00000000_00000000_00000000,
            0b11111111_10110000_00000000_00000000,
            0b11111111_00000000_00000000_00000000,
            0b11111111_11010000_00000000_00000000,
            0b10000100_00000000_00000000_00000000,
            0b10111010_00000000_00000000_00000000,
            0b10111100_00000000_00000000_00000000,
            0b10111110_00000000_00000000_00000000,
            0b11000000_00000000_00000000_00000000,
            0b11000010_00000000_00000000_00000000,
            0b11000100_00000000_00000000_00000000,
            0b11000110_00000000_00000000_00000000,
            0b11001000_00000000_00000000_00000000,
            0b11001010_00000000_00000000_00000000,
            0b11001100_00000000_00000000_00000000,
            0b11001110_00000000_00000000_00000000,
            0b11010000_00000000_00000000_00000000,
            0b11010010_00000000_00000000_00000000,
            0b11010100_00000000_00000000_00000000,
            0b11010110_00000000_00000000_00000000,
            0b11011000_00000000_00000000_00000000,
            0b11011010_00000000_00000000_00000000,
            0b11011100_00000000_00000000_00000000,
            0b11011110_00000000_00000000_00000000,
            0b11100000_00000000_00000000_00000000,
            0b11100010_00000000_00000000_00000000,
            0b11100100_00000000_00000000_00000000,
            0b11111100_00000000_00000000_00000000,
            0b11100110_00000000_00000000_00000000,
            0b11111101_00000000_00000000_00000000,
            0b11111111_11011000_00000000_00000000,
            0b11111111_11111110_00000000_00000000,
            0b11111111_11100000_00000000_00000000,
            0b11111111_11110000_00000000_00000000,
            0b10001000_00000000_00000000_00000000,
            0b11111111_11111010_00000000_00000000,
            0b00011000_00000000_00000000_00000000,
            0b10001100_00000000_00000000_00000000,
            0b00100000_00000000_00000000_00000000,
            0b10010000_00000000_00000000_00000000,
            0b00101000_00000000_00000000_00000000,
            0b10010100_00000000_00000000_00000000,
            0b10011000_00000000_00000000_00000000,
            0b10011100_00000000_00000000_00000000,
            0b00110000_00000000_00000000_00000000,
            0b11101000_00000000_00000000_00000000,
            0b11101010_00000000_00000000_00000000,
            0b10100000_00000000_00000000_00000000,
            0b10100100_00000000_00000000_00000000,
            0b10101000_00000000_00000000_00000000,
            0b00111000_00000000_00000000_00000000,
            0b10101100_00000000_00000000_00000000,
            0b11101100_00000000_00000000_00000000,
            0b10110000_00000000_00000000_00000000,
            0b01000000_00000000_00000000_00000000,
            0b01001000_00000000_00000000_00000000,
            0b10110100_00000000_00000000_00000000,
            0b11101110_00000000_00000000_00000000,
            0b11110000_00000000_00000000_00000000,
            0b11110010_00000000_00000000_00000000,
            0b11110100_00000000_00000000_00000000,
            0b11110110_00000000_00000000_00000000,
            0b11111111_11111100_00000000_00000000,
            0b11111111_10000000_00000000_00000000,
            0b11111111_11110100_00000000_00000000,
            0b11111111_11101000_00000000_00000000,
            0b11111111_11111111_11111111_11000000,
            0b11111111_11111110_01100000_00000000,
            0b11111111_11111111_01001000_00000000,
            0b11111111_11111110_01110000_00000000,
            0b11111111_11111110_10000000_00000000,
            0b11111111_11111111_01001100_00000000,
            0b11111111_11111111_01010000_00000000,
            0b11111111_11111111_01010100_00000000,
            0b11111111_11111111_10110010_00000000,
            0b11111111_11111111_01011000_00000000,
            0b11111111_11111111_10110100_00000000,
            0b11111111_11111111_10110110_00000000,
            0b11111111_11111111_10111000_00000000,
            0b11111111_11111111_10111010_00000000,
            0b11111111_11111111_10111100_00000000,
            0b11111111_11111111_11101011_00000000,
            0b11111111_11111111_10111110_00000000,
            0b11111111_11111111_11101100_00000000,
            0b11111111_11111111_11101101_00000000,
            0b11111111_11111111_01011100_00000000,
            0b11111111_11111111_11000000_00000000,
            0b11111111_11111111_11101110_00000000,
            0b11111111_11111111_11000010_00000000,
            0b11111111_11111111_11000100_00000000,
            0b11111111_11111111_11000110_00000000,
            0b11111111_11111111_11001000_00000000,
            0b11111111_11111110_11100000_00000000,
            0b11111111_11111111_01100000_00000000,
            0b11111111_11111111_11001010_00000000,
            0b11111111_11111111_01100100_00000000,
            0b11111111_11111111_11001100_00000000,
            0b11111111_11111111_11001110_00000000,
            0b11111111_11111111_11101111_00000000,
            0b11111111_11111111_01101000_00000000,
            0b11111111_11111110_11101000_00000000,
            0b11111111_11111110_10010000_00000000,
            0b11111111_11111111_01101100_00000000,
            0b11111111_11111111_01110000_00000000,
            0b11111111_11111111_11010000_00000000,
            0b11111111_11111111_11010010_00000000,
            0b11111111_11111110_11110000_00000000,
            0b11111111_11111111_11010100_00000000,
            0b11111111_11111111_01110100_00000000,
            0b11111111_11111111_01111000_00000000,
            0b11111111_11111111_11110000_00000000,
            0b11111111_11111110_11111000_00000000,
            0b11111111_11111111_01111100_00000000,
            0b11111111_11111111_11010110_00000000,
            0b11111111_11111111_11011000_00000000,
            0b11111111_11111111_00000000_00000000,
            0b11111111_11111111_00001000_00000000,
            0b11111111_11111111_10000000_00000000,
            0b11111111_11111111_00010000_00000000,
            0b11111111_11111111_11011010_00000000,
            0b11111111_11111111_10000100_00000000,
            0b11111111_11111111_11011100_00000000,
            0b11111111_11111111_11011110_00000000,
            0b11111111_11111110_10100000_00000000,
            0b11111111_11111111_10001000_00000000,
            0b11111111_11111111_10001100_00000000,
            0b11111111_11111111_10010000_00000000,
            0b11111111_11111111_11100000_00000000,
            0b11111111_11111111_10010100_00000000,
            0b11111111_11111111_10011000_00000000,
            0b11111111_11111111_11100010_00000000,
            0b11111111_11111111_11111000_00000000,
            0b11111111_11111111_11111000_01000000,
            0b11111111_11111110_10110000_00000000,
            0b11111111_11111110_00100000_00000000,
            0b11111111_11111111_10011100_00000000,
            0b11111111_11111111_11100100_00000000,
            0b11111111_11111111_10100000_00000000,
            0b11111111_11111111_11110110_00000000,
            0b11111111_11111111_11111000_10000000,
            0b11111111_11111111_11111000_11000000,
            0b11111111_11111111_11111001_00000000,
            0b11111111_11111111_11111011_11000000,
            0b11111111_11111111_11111011_11100000,
            0b11111111_11111111_11111001_01000000,
            0b11111111_11111111_11110001_00000000,
            0b11111111_11111111_11110110_10000000,
            0b11111111_11111110_01000000_00000000,
            0b11111111_11111111_00011000_00000000,
            0b11111111_11111111_11111001_10000000,
            0b11111111_11111111_11111100_00000000,
            0b11111111_11111111_11111100_00100000,
            0b11111111_11111111_11111001_11000000,
            0b11111111_11111111_11111100_01000000,
            0b11111111_11111111_11110010_00000000,
            0b11111111_11111111_00100000_00000000,
            0b11111111_11111111_00101000_00000000,
            0b11111111_11111111_11111010_00000000,
            0b11111111_11111111_11111010_01000000,
            0b11111111_11111111_11111111_11010000,
            0b11111111_11111111_11111100_01100000,
            0b11111111_11111111_11111100_10000000,
            0b11111111_11111111_11111100_10100000,
            0b11111111_11111110_11000000_00000000,
            0b11111111_11111111_11110011_00000000,
            0b11111111_11111110_11010000_00000000,
            0b11111111_11111111_00110000_00000000,
            0b11111111_11111111_10100100_00000000,
            0b11111111_11111111_00111000_00000000,
            0b11111111_11111111_01000000_00000000,
            0b11111111_11111111_11100110_00000000,
            0b11111111_11111111_10101000_00000000,
            0b11111111_11111111_10101100_00000000,
            0b11111111_11111111_11110111_00000000,
            0b11111111_11111111_11110111_10000000,
            0b11111111_11111111_11110100_00000000,
            0b11111111_11111111_11110101_00000000,
            0b11111111_11111111_11111010_10000000,
            0b11111111_11111111_11101000_00000000,
            0b11111111_11111111_11111010_11000000,
            0b11111111_11111111_11111100_11000000,
            0b11111111_11111111_11111011_00000000,
            0b11111111_11111111_11111011_01000000,
            0b11111111_11111111_11111100_11100000,
            0b11111111_11111111_11111101_00000000,
            0b11111111_11111111_11111101_00100000,
            0b11111111_11111111_11111101_01000000,
            0b11111111_11111111_11111101_01100000,
            0b11111111_11111111_11111111_11100000,
            0b11111111_11111111_11111101_10000000,
            0b11111111_11111111_11111101_10100000,
            0b11111111_11111111_11111101_11000000,
            0b11111111_11111111_11111101_11100000,
            0b11111111_11111111_11111110_00000000,
            0b11111111_11111111_11111011_10000000,
            0b11111111_11111111_11111111_11111100,
        ];

        private static ReadOnlySpan<byte> EncodingTableBitLengths => // 257
        [
            13,
            23,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            24,
            30,
            28,
            28,
            30,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            30,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            28,
            6,
            10,
            10,
            12,
            13,
            6,
            8,
            11,
            10,
            10,
            8,
            11,
            8,
            6,
            6,
            6,
            5,
            5,
            5,
            6,
            6,
            6,
            6,
            6,
            6,
            6,
            7,
            8,
            15,
            6,
            12,
            10,
            13,
            6,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            7,
            8,
            7,
            8,
            13,
            19,
            13,
            14,
            6,
            15,
            5,
            6,
            5,
            6,
            5,
            6,
            6,
            6,
            5,
            7,
            7,
            6,
            6,
            6,
            5,
            6,
            7,
            6,
            5,
            5,
            6,
            7,
            7,
            7,
            7,
            7,
            15,
            11,
            14,
            13,
            28,
            20,
            22,
            20,
            20,
            22,
            22,
            22,
            23,
            22,
            23,
            23,
            23,
            23,
            23,
            24,
            23,
            24,
            24,
            22,
            23,
            24,
            23,
            23,
            23,
            23,
            21,
            22,
            23,
            22,
            23,
            23,
            24,
            22,
            21,
            20,
            22,
            22,
            23,
            23,
            21,
            23,
            22,
            22,
            24,
            21,
            22,
            23,
            23,
            21,
            21,
            22,
            21,
            23,
            22,
            23,
            23,
            20,
            22,
            22,
            22,
            23,
            22,
            22,
            23,
            26,
            26,
            20,
            19,
            22,
            23,
            22,
            25,
            26,
            26,
            26,
            27,
            27,
            26,
            24,
            25,
            19,
            21,
            26,
            27,
            27,
            26,
            27,
            24,
            21,
            21,
            26,
            26,
            28,
            27,
            27,
            27,
            20,
            24,
            20,
            21,
            22,
            21,
            21,
            23,
            22,
            22,
            25,
            25,
            24,
            24,
            26,
            23,
            26,
            27,
            26,
            26,
            27,
            27,
            27,
            27,
            27,
            28,
            27,
            27,
            27,
            27,
            27,
            26,
            30
        ];

        private static readonly ushort[] s_decodingTree = GenerateDecodingLookupTree();

        public static (uint encoded, int bitLength) Encode(int data)
        {
            return (EncodingTableCodes[data], EncodingTableBitLengths[data]);
        }

        private static ushort[] GenerateDecodingLookupTree()
        {
            // Decoding lookup tree is a tree of 8 bit lookup tables stored in
            // one dimensional array of ushort to reduce allocations.
            // First 256 ushort is lookup table with index 0, next 256 ushort is lookup table with index 1, etc...
            // lookup_value = [(lookup_table_index << 8) + lookup_index]

            // lookup_index is next 8 bits of huffman code, if there is less than 8 bits in source.
            // lookup_index MUST be aligned to 8 bits with LSB bits set to anything (zeros are recommended).

            // Lookup value is encoded in ushort as either.
            // -----------------------------------------------------------------
            //  15  14  13  12  11  10   9   8   7   6   5   4   3   2   1   0
            // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
            // | 1 |   next_lookup_table_index |            not_used           |
            // +---+---------------------------+-------------------------------+
            // or
            // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
            // | 0 |     number_of_used_bits   |              octet            |
            // +---+---------------------------+-------------------------------+

            // Bit 15 unset indicates a leaf value of decoding tree.
            // For example value 0x0241 means that we have reached end of huffman code
            // with result byte 0x41 'A' and from lookup bits only rightmost 2 bits were used
            // and rest of bits are part of next huffman code.

            // Bit 15 set indicates that code is not yet decoded and next lookup table index shall be used
            // for next n bits of huffman code.
            // 0 in 'next lookup table index' is considered as decoding error - invalid huffman code

            // Because HPack uses static huffman code defined in RFC https://httpwg.org/specs/rfc7541.html#huffman.code
            // it is guaranteed that for this huffman code generated decoding lookup tree MUST consist of exactly 15 lookup tables
            var decodingTree = new ushort[15 * 256];

            ReadOnlySpan<uint> encodingTableCodes = EncodingTableCodes;
            ReadOnlySpan<byte> encodingTableBitLengths = EncodingTableBitLengths;

            int allocatedLookupTableIndex = 0;
            // Create traverse path for all 0..256 octets, 256 is EOS, see: http://httpwg.org/specs/rfc7541.html#rfc.section.5.2
            for (int octet = 0; octet <= 256; octet++)
            {
                uint code = encodingTableCodes[octet];
                int bitLength = encodingTableBitLengths[octet];

                int lookupTableIndex = 0;
                int bitsLeft = bitLength;
                while (bitsLeft > 0)
                {
                    // read next 8 bits from huffman code
                    int indexInLookupTable = (int)(code >> (32 - 8));

                    if (bitsLeft <= 8)
                    {
                        // Reached last lookup table for this huffman code.

                        // Identical lookup value has to be stored for every combination of unused bits,
                        // For example: 12 bit code could be looked up during decoding as this:
                        // ---------------------------------
                        //   7   6   5   4   3   2   1   0
                        // +---+---+---+---+---+---+---+---+
                        // |last_code_bits | next_code_bits|
                        // +-------------------------------+
                        // next_code_bits are 'random' bits of next huffman code, so in order for lookup
                        // to work, lookup value has to be stored for all 4 unused bits, in this case for suffix 0..15
                        int suffixCount = 1 << (8 - bitsLeft);
                        for (int suffix = 0; suffix < suffixCount; suffix++)
                        {
                            if (octet == 256)
                            {
                                // EOS (in our case 256) have special meaning in HPack static huffman code
                                // see: http://httpwg.org/specs/rfc7541.html#rfc.section.5.2
                                // > A Huffman-encoded string literal containing the EOS symbol MUST be treated as a decoding error.
                                // To force decoding error we store 0 as 'next lookup table index' which MUST be treated as decoding error.

                                // Invalid huffman code - EOS
                                // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                                // | 1 | 0   0   0   0   0   0   0 | 1   1   1   1   1   1   1   1 |
                                // +---+---------------------------+-------------------------------+
                                decodingTree[(lookupTableIndex << 8) + (indexInLookupTable | suffix)] = 0x80ff;
                            }
                            else
                            {
                                // Leaf lookup value
                                // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                                // | 0 |     number_of_used_bits   |              code             |
                                // +---+---------------------------+-------------------------------+
                                decodingTree[(lookupTableIndex << 8) + (indexInLookupTable | suffix)] = (ushort)((bitsLeft << 8) | octet);
                            }
                        }
                    }
                    else
                    {
                        // More than 8 bits left in huffman code means that we need to traverse to another lookup table for next 8 bits
                        ushort lookupValue = decodingTree[(lookupTableIndex << 8) + indexInLookupTable];

                        // Because next_lookup_table_index can not be 0, as 0 is index of root table, default value of array element
                        // means that we have not initialized it yet => lookup table MUST be allocated and its index assigned to that lookup value
                        // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                        // | 1 |   next_lookup_table_index |            not_used           |
                        // +---+---------------------------+-------------------------------+
                        if (lookupValue == 0)
                        {
                            ++allocatedLookupTableIndex;
                            decodingTree[(lookupTableIndex << 8) + indexInLookupTable] = (ushort)((0x80 | allocatedLookupTableIndex) << 8);
                            lookupTableIndex = allocatedLookupTableIndex;
                        }
                        else
                        {
                            lookupTableIndex = (lookupValue & 0x7f00) >> 8;
                        }
                    }

                    bitsLeft -= 8;
                    code <<= 8;
                }
            }

            return decodingTree;
        }

        /// <summary>
        /// Decodes a Huffman encoded string from a byte array.
        /// </summary>
        /// <param name="src">The source byte array containing the encoded data.</param>
        /// <param name="dstArray">The destination byte array to store the decoded data.  This may grow if its size is insufficient.</param>
        /// <returns>The number of decoded symbols.</returns>
        public static int Decode(ReadOnlySpan<byte> src, ref byte[] dstArray)
        {
            // The code below implements the decoding logic for an HPack huffman encoded literal values.
            // https://httpwg.org/specs/rfc7541.html#string.literal.representation
            //
            // To decode a symbol, we traverse the decoding lookup table tree by 8 bits for each lookup
            // until we found a leaf - which contains decoded symbol (octet)
            //
            // see comments in GenerateDecodingLookupTree() describing decoding table

            Span<byte> dst = dstArray;
            Debug.Assert(dst != null && dst.Length > 0);

            ushort[] decodingTree = s_decodingTree;

            int lookupTableIndex = 0;
            int lookupIndex;

            uint acc = 0;
            int bitsInAcc = 0;

            int i = 0;
            int j = 0;
            while (i < src.Length)
            {
                // Load next 8 bits into accumulator.
                acc <<= 8;
                acc |= src[i++];
                bitsInAcc += 8;

                // Decode bits in accumulator.
                do
                {
                    lookupIndex = (byte)(acc >> (bitsInAcc - 8));

                    int lookupValue = decodingTree[(lookupTableIndex << 8) + lookupIndex];

                    if (lookupValue < 0x80_00)
                    {
                        // Octet found.
                        // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                        // | 0 |     number_of_used_bits   |              octet            |
                        // +---+---------------------------+-------------------------------+
                        if (j == dst.Length)
                        {
                            Array.Resize(ref dstArray, dst.Length * 2);
                            dst = dstArray;
                        }
                        dst[j++] = (byte)lookupValue;

                        // Start lookup of next symbol
                        lookupTableIndex = 0;
                        bitsInAcc -= lookupValue >> 8;
                    }
                    else
                    {
                        // Traverse to next lookup table.
                        // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                        // | 1 |   next_lookup_table_index |            not_used           |
                        // +---+---------------------------+-------------------------------+
                        lookupTableIndex = (lookupValue & 0x7f00) >> 8;
                        if (lookupTableIndex == 0)
                        {
                            // No valid symbol could be decoded or EOS was decoded
                            throw new HuffmanDecodingException(SR.net_http_hpack_huffman_decode_failed);
                        }
                        bitsInAcc -= 8;
                    }
                } while (bitsInAcc >= 8);
            }

            // Finish decoding last < 8 bits of src.
            // Processing of the last byte has to handle several corner cases
            // so it's extracted outside of the main loop for performance reasons.
            while (bitsInAcc > 0)
            {
                Debug.Assert(bitsInAcc < 8);

                // Check for correct EOS, which is padding with ones till end of byte
                // when we STARTED new huffman code in last 8 bits (lookupTableIndex was reset to 0 -> root lookup table).
                if (lookupTableIndex == 0)
                {
                    // Check if all remaining bits are ones.
                    uint ones = uint.MaxValue >> (32 - bitsInAcc);
                    if ((acc & ones) == ones)
                    {
                        // Is it a EOS. See: http://httpwg.org/specs/rfc7541.html#rfc.section.5.2
                        break;
                    }
                }

                // Lookup index has to be 8 bits aligned to MSB
                lookupIndex = (byte)(acc << (8 - bitsInAcc));

                int lookupValue = decodingTree[(lookupTableIndex << 8) + lookupIndex];

                if (lookupValue < 0x80_00)
                {
                    // Octet found.
                    // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                    // | 0 |     number_of_used_bits   |              octet            |
                    // +---+---------------------------+-------------------------------+
                    bitsInAcc -= lookupValue >> 8;

                    if (bitsInAcc < 0)
                    {
                        // Last looked up code had more bits than was left in accumulator which indicated invalid or incomplete source
                        throw new HuffmanDecodingException(SR.net_http_hpack_huffman_decode_failed);
                    }

                    if (j == dst.Length)
                    {
                        Array.Resize(ref dstArray, dst.Length * 2);
                        dst = dstArray;
                    }
                    dst[j++] = (byte)lookupValue;

                    // Set table index to root - start of new huffman code.
                    lookupTableIndex = 0;
                }
                else
                {
                    // Src was depleted in middle of lookup tree or EOS was decoded.
                    throw new HuffmanDecodingException(SR.net_http_hpack_huffman_decode_failed);
                }
            }

            if (lookupTableIndex != 0)
            {
                // Finished in middle of traversing - no valid symbol could be decoded
                // or too long EOS padding (7 bits plus). See: http://httpwg.org/specs/rfc7541.html#rfc.section.5.2
                throw new HuffmanDecodingException(SR.net_http_hpack_huffman_decode_failed);
            }

            return j;
        }
    }
}
