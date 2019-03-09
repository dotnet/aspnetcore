// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class StringUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe bool TryGetAsciiString(byte* input, char* output, int count)
        {
            // Calculate end position
            var end = input + count;
            // Start as valid
            var isValid = true;

            do
            {
                // If Vector not-accelerated or remaining less than vector size
                if (!Vector.IsHardwareAccelerated || input > end - Vector<sbyte>.Count)
                {
                    if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
                    {
                        // 64-bit: Loop longs by default
                        while (input <= end - sizeof(long))
                        {
                            isValid &= CheckBytesInAsciiRange(((long*)input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];
                            output[4] = (char)input[4];
                            output[5] = (char)input[5];
                            output[6] = (char)input[6];
                            output[7] = (char)input[7];

                            input += sizeof(long);
                            output += sizeof(long);
                        }
                        if (input <= end - sizeof(int))
                        {
                            isValid &= CheckBytesInAsciiRange(((int*)input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];

                            input += sizeof(int);
                            output += sizeof(int);
                        }
                    }
                    else
                    {
                        // 32-bit: Loop ints by default
                        while (input <= end - sizeof(int))
                        {
                            isValid &= CheckBytesInAsciiRange(((int*)input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];

                            input += sizeof(int);
                            output += sizeof(int);
                        }
                    }
                    if (input <= end - sizeof(short))
                    {
                        isValid &= CheckBytesInAsciiRange(((short*)input)[0]);

                        output[0] = (char)input[0];
                        output[1] = (char)input[1];

                        input += sizeof(short);
                        output += sizeof(short);
                    }
                    if (input < end)
                    {
                        isValid &= CheckBytesInAsciiRange(((sbyte*)input)[0]);
                        output[0] = (char)input[0];
                    }

                    return isValid;
                }

                // do/while as entry condition already checked
                do
                {
                    var vector = Unsafe.AsRef<Vector<sbyte>>(input);
                    isValid &= CheckBytesInAsciiRange(vector);
                    Vector.Widen(
                        vector,
                        out Unsafe.AsRef<Vector<short>>(output),
                        out Unsafe.AsRef<Vector<short>>(output + Vector<short>.Count));

                    input += Vector<sbyte>.Count;
                    output += Vector<sbyte>.Count;
                } while (input <= end - Vector<sbyte>.Count);

                // Vector path done, loop back to do non-Vector
                // If is a exact multiple of vector size, bail now
            } while (input < end);

            return isValid;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe static bool BytesOrdinalEqualsStringAndAscii(string previousValue, Span<byte> newValue)
        {
            // We just widen the bytes to char for comparision, if either the string or the bytes are not ascii
            // this will result in non-equality, so we don't need to specifically test for non-ascii.
            Debug.Assert(previousValue.Length == newValue.Length);

            // Use IntPtr values rather than int, to avoid unnessary 32 -> 64 movs on 64-bit.
            // Unfortunately this means we also need to cast to byte* for comparisions as IntPtr doesn't
            // support operator comparisions (e.g. <=, >, etc).
            // Note: Pointer comparision is unsigned, so we use the compare pattern (offset + length <= count)
            // rather than (offset <= count - length) which we'd do with signed comparision to avoid overflow.
            var count = (IntPtr)newValue.Length;
            var offset = (IntPtr)0;

            ref var bytes = ref MemoryMarshal.GetReference(newValue);
            ref var str = ref MemoryMarshal.GetReference(previousValue.AsSpan());

            do
            {
                // If Vector not-accelerated or remaining less than vector size
                if (!Vector.IsHardwareAccelerated || (byte*)(offset + Vector<byte>.Count) > (byte*)count)
                {
                    if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
                    {
                        // 64-bit: Loop longs by default
                        while ((byte*)(offset + sizeof(long)) <= (byte*)count)
                        {
                            if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset) ||
                                Unsafe.Add(ref str, offset + 1) != (char)Unsafe.Add(ref bytes, offset + 1) ||
                                Unsafe.Add(ref str, offset + 2) != (char)Unsafe.Add(ref bytes, offset + 2) ||
                                Unsafe.Add(ref str, offset + 3) != (char)Unsafe.Add(ref bytes, offset + 3) ||
                                Unsafe.Add(ref str, offset + 4) != (char)Unsafe.Add(ref bytes, offset + 4) ||
                                Unsafe.Add(ref str, offset + 5) != (char)Unsafe.Add(ref bytes, offset + 5) ||
                                Unsafe.Add(ref str, offset + 6) != (char)Unsafe.Add(ref bytes, offset + 6) ||
                                Unsafe.Add(ref str, offset + 7) != (char)Unsafe.Add(ref bytes, offset + 7))
                            {
                                goto NotEqual;
                            }

                            offset += sizeof(long);
                        }
                        if ((byte*)(offset + sizeof(int)) <= (byte*)count)
                        {
                            if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset) ||
                                Unsafe.Add(ref str, offset + 1) != (char)Unsafe.Add(ref bytes, offset + 1) ||
                                Unsafe.Add(ref str, offset + 2) != (char)Unsafe.Add(ref bytes, offset + 2) ||
                                Unsafe.Add(ref str, offset + 3) != (char)Unsafe.Add(ref bytes, offset + 3))
                            {
                                goto NotEqual;
                            }

                            offset += sizeof(int);
                        }
                    }
                    else
                    {
                        // 32-bit: Loop ints by default
                        while ((byte*)(offset + sizeof(int)) <= (byte*)count)
                        {
                            if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset) ||
                                Unsafe.Add(ref str, offset + 1) != (char)Unsafe.Add(ref bytes, offset + 1) ||
                                Unsafe.Add(ref str, offset + 2) != (char)Unsafe.Add(ref bytes, offset + 2) ||
                                Unsafe.Add(ref str, offset + 3) != (char)Unsafe.Add(ref bytes, offset + 3))
                            {
                                goto NotEqual;
                            }

                            offset += sizeof(int);
                        }
                    }
                    if ((byte*)(offset + sizeof(short)) <= (byte*)count)
                    {
                        if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset) ||
                            Unsafe.Add(ref str, offset + 1) != (char)Unsafe.Add(ref bytes, offset + 1))
                        {
                            goto NotEqual;
                        }

                        offset += sizeof(short);
                    }
                    if ((byte*)offset < (byte*)count)
                    {
                        if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset))
                        {
                            goto NotEqual;
                        }
                    }

                    return true;
                }

                // do/while as entry condition already checked
                var AllTrue = new Vector<ushort>(ushort.MaxValue);
                do
                {
                    var vector = Unsafe.As<byte, Vector<byte>>(ref Unsafe.Add(ref bytes, offset));
                    Vector.Widen(vector, out var vector0, out var vector1);
                    var compare0 = Unsafe.As<char, Vector<ushort>>(ref Unsafe.Add(ref str, offset));
                    var compare1 = Unsafe.As<char, Vector<ushort>>(ref Unsafe.Add(ref str, offset + Vector<ushort>.Count));

                    if (!AllTrue.Equals(
                        Vector.BitwiseAnd(
                            Vector.Equals(compare0, vector0),
                            Vector.Equals(compare1, vector1))))
                    {
                        goto NotEqual;
                    }

                    offset += Vector<byte>.Count;
                } while ((byte*)(offset + Vector<byte>.Count) <= (byte*)count);

                // Vector path done, loop back to do non-Vector
                // If is a exact multiple of vector size, bail now
            } while ((byte*)offset < (byte*)count);

            return true;
        NotEqual:
            return false;
        }

        private static readonly char[] s_encode16Chars = "0123456789ABCDEF".ToCharArray();

        /// <summary>
        /// A faster version of String.Concat(<paramref name="str"/>, <paramref name="separator"/>, <paramref name="number"/>.ToString("X8"))
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ConcatAsHexSuffix(string str, char separator, uint number)
        {
            var length = 1 + 8;
            if (str != null)
            {
                length += str.Length;
            }

            return string.Create(length, (str, separator, number), (buffer, tuple) =>
            {
                var (tupleStr, tupleSeparator, tupleNumber) = tuple;
                char[] encode16Chars = s_encode16Chars;

                var i = 0;
                if (tupleStr != null)
                {
                    tupleStr.AsSpan().CopyTo(buffer);
                    i = tupleStr.Length;
                }

                buffer[i + 8] = encode16Chars[tupleNumber & 0xF];
                buffer[i + 7] = encode16Chars[(tupleNumber >> 4) & 0xF];
                buffer[i + 6] = encode16Chars[(tupleNumber >> 8) & 0xF];
                buffer[i + 5] = encode16Chars[(tupleNumber >> 12) & 0xF];
                buffer[i + 4] = encode16Chars[(tupleNumber >> 16) & 0xF];
                buffer[i + 3] = encode16Chars[(tupleNumber >> 20) & 0xF];
                buffer[i + 2] = encode16Chars[(tupleNumber >> 24) & 0xF];
                buffer[i + 1] = encode16Chars[(tupleNumber >> 28) & 0xF];
                buffer[i] = tupleSeparator;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
        private static bool CheckBytesInAsciiRange(Vector<sbyte> check)
        {
            // Vectorized byte range check, signed byte > 0 for 1-127
            return Vector.GreaterThanAll(check, Vector<sbyte>.Zero);
        }

        // Validate: bytes != 0 && bytes <= 127
        //  Subtract 1 from all bytes to move 0 to high bits
        //  bitwise or with self to catch all > 127 bytes
        //  mask off high bits and check if 0

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Needs a push
        private static bool CheckBytesInAsciiRange(long check)
        {
            const long HighBits = unchecked((long)0x8080808080808080L);
            return (((check - 0x0101010101010101L) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(int check)
        {
            const int HighBits = unchecked((int)0x80808080);
            return (((check - 0x01010101) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(short check)
        {
            const short HighBits = unchecked((short)0x8080);
            return (((short)(check - 0x0101) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(sbyte check)
            => check > 0;
    }
}
