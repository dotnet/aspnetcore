// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class AsciiBytesToStringBenchmark
    {
        private const int Iterations = 100;

        private byte[] _asciiBytes;
        private string _asciiString = new string('\0', 1024);

        [Params(
            BenchmarkTypes.KeepAlive,
            BenchmarkTypes.Accept,
            BenchmarkTypes.UserAgent,
            BenchmarkTypes.Cookie
        )]
        public BenchmarkTypes Type { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            switch (Type)
            {
                case BenchmarkTypes.KeepAlive:
                    _asciiBytes = Encoding.ASCII.GetBytes("keep-alive");
                    break;
                case BenchmarkTypes.Accept:
                    _asciiBytes = Encoding.ASCII.GetBytes("text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7");
                    break;
                case BenchmarkTypes.UserAgent:
                    _asciiBytes = Encoding.ASCII.GetBytes("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36");
                    break;
                case BenchmarkTypes.Cookie:
                    _asciiBytes = Encoding.ASCII.GetBytes("prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric");
                    break;
            }

            Verify();
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public unsafe string EncodingAsciiGetChars()
        {
            for (uint i = 0; i < Iterations; i++)
            {
                fixed (byte* pBytes = &_asciiBytes[0])
                fixed (char* pString = _asciiString)
                {
                    Encoding.ASCII.GetChars(pBytes, _asciiBytes.Length, pString, _asciiBytes.Length);
                }
            }

            return _asciiString;
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Iterations)]
        public unsafe byte[] KestrelBytesToString()
        {
            for (uint i = 0; i < Iterations; i++)
            {
                fixed (byte* pBytes = &_asciiBytes[0])
                fixed (char* pString = _asciiString)
                {
                    TryGetAsciiString(pBytes, pString, _asciiBytes.Length);
                }
            }

            return _asciiBytes;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public unsafe byte[] AsciiBytesToStringVectorCheck()
        {
            for (uint i = 0; i < Iterations; i++)
            {
                fixed (byte* pBytes = &_asciiBytes[0])
                fixed (char* pString = _asciiString)
                {
                    TryGetAsciiStringVectorCheck(pBytes, pString, _asciiBytes.Length);
                }
            }

            return _asciiBytes;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public unsafe byte[] AsciiBytesToStringVectorWiden()
        {
            // Widen Acceleration is post netcoreapp2.0
            for (uint i = 0; i < Iterations; i++)
            {
                fixed (byte* pBytes = &_asciiBytes[0])
                fixed (char* pString = _asciiString)
                {
                    TryGetAsciiStringVectorWiden(pBytes, pString, _asciiBytes.Length);
                }
            }

            return _asciiBytes;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public unsafe byte[] AsciiBytesToStringSpanWiden()
        {
            // Widen Acceleration is post netcoreapp2.0
            for (uint i = 0; i < Iterations; i++)
            {
                fixed (char* pString = _asciiString)
                {
                    TryGetAsciiStringWidenSpan(_asciiBytes, new Span<char>(pString, _asciiString.Length));
                }
            }

            return _asciiBytes;
        }

        public static bool TryGetAsciiStringWidenSpan(ReadOnlySpan<byte> input, Span<char> output)
        {
            // Start as valid
            var isValid = true;

            do
            {
                // If Vector not-accelerated or remaining less than vector size
                if (!Vector.IsHardwareAccelerated || input.Length < Vector<sbyte>.Count)
                {
                    if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
                    {
                        // 64-bit: Loop longs by default
                        while ((uint)sizeof(long) <= (uint)input.Length)
                        {
                            isValid &= CheckBytesInAsciiRange(MemoryMarshal.Cast<byte, long>(input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];
                            output[4] = (char)input[4];
                            output[5] = (char)input[5];
                            output[6] = (char)input[6];
                            output[7] = (char)input[7];

                            input = input.Slice(sizeof(long));
                            output = output.Slice(sizeof(long));
                        }
                        if ((uint)sizeof(int) <= (uint)input.Length)
                        {
                            isValid &= CheckBytesInAsciiRange(MemoryMarshal.Cast<byte, int>(input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];

                            input = input.Slice(sizeof(int));
                            output = output.Slice(sizeof(int));
                        }
                    }
                    else
                    {
                        // 32-bit: Loop ints by default
                        while ((uint)sizeof(int) <= (uint)input.Length)
                        {
                            isValid &= CheckBytesInAsciiRange(MemoryMarshal.Cast<byte, int>(input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];

                            input = input.Slice(sizeof(int));
                            output = output.Slice(sizeof(int));
                        }
                    }
                    if ((uint)sizeof(short) <= (uint)input.Length)
                    {
                        isValid &= CheckBytesInAsciiRange(MemoryMarshal.Cast<byte, short>(input)[0]);

                        output[0] = (char)input[0];
                        output[1] = (char)input[1];

                        input = input.Slice(sizeof(short));
                        output = output.Slice(sizeof(short));
                    }
                    if ((uint)sizeof(byte) <= (uint)input.Length)
                    {
                        isValid &= CheckBytesInAsciiRange((sbyte)input[0]);
                        output[0] = (char)input[0];
                    }

                    return isValid;
                }

                // do/while as entry condition already checked
                do
                {
                    var vector = MemoryMarshal.Cast<byte, Vector<sbyte>>(input)[0];
                    isValid &= CheckBytesInAsciiRange(vector);
                    Vector.Widen(
                        vector,
                        out MemoryMarshal.Cast<char, Vector<short>>(output)[0],
                        out MemoryMarshal.Cast<char, Vector<short>>(output)[1]);

                    input = input.Slice(Vector<sbyte>.Count);
                    output = output.Slice(Vector<sbyte>.Count);
                } while (input.Length >= Vector<sbyte>.Count);

                // Vector path done, loop back to do non-Vector
                // If is a exact multiple of vector size, bail now
            } while (input.Length > 0);

            return isValid;
        }

        public static unsafe bool TryGetAsciiStringVectorWiden(byte* input, char* output, int count)
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

        public static unsafe bool TryGetAsciiStringVectorCheck(byte* input, char* output, int count)
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
                    isValid &= CheckBytesInAsciiRange(Unsafe.AsRef<Vector<sbyte>>(input));

                    // Vector.Widen is only netcoreapp2.1+ so let's do this manually
                    var i = 0;
                    do
                    {
                        // Vectors are min 16 byte, so lets do 16 byte loops
                        i += 16;
                        // Unrolled byte-wise widen
                        output[0] = (char)input[0];
                        output[1] = (char)input[1];
                        output[2] = (char)input[2];
                        output[3] = (char)input[3];
                        output[4] = (char)input[4];
                        output[5] = (char)input[5];
                        output[6] = (char)input[6];
                        output[7] = (char)input[7];
                        output[8] = (char)input[8];
                        output[9] = (char)input[9];
                        output[10] = (char)input[10];
                        output[11] = (char)input[11];
                        output[12] = (char)input[12];
                        output[13] = (char)input[13];
                        output[14] = (char)input[14];
                        output[15] = (char)input[15];

                        input += 16;
                        output += 16;
                    } while (i < Vector<sbyte>.Count);
                } while (input <= end - Vector<sbyte>.Count);

                // Vector path done, loop back to do non-Vector
                // If is a exact multiple of vector size, bail now
            } while (input < end);

            return isValid;
        }

        public static unsafe bool TryGetAsciiString(byte* input, char* output, int count)
        {
            var i = 0;
            sbyte* signedInput = (sbyte*)input;

            bool isValid = true;
            while (i < count - 11)
            {
                isValid = isValid && *signedInput > 0 && *(signedInput + 1) > 0 && *(signedInput + 2) > 0 &&
                    *(signedInput + 3) > 0 && *(signedInput + 4) > 0 && *(signedInput + 5) > 0 && *(signedInput + 6) > 0 &&
                    *(signedInput + 7) > 0 && *(signedInput + 8) > 0 && *(signedInput + 9) > 0 && *(signedInput + 10) > 0 &&
                    *(signedInput + 11) > 0;

                i += 12;
                *(output) = (char)*(signedInput);
                *(output + 1) = (char)*(signedInput + 1);
                *(output + 2) = (char)*(signedInput + 2);
                *(output + 3) = (char)*(signedInput + 3);
                *(output + 4) = (char)*(signedInput + 4);
                *(output + 5) = (char)*(signedInput + 5);
                *(output + 6) = (char)*(signedInput + 6);
                *(output + 7) = (char)*(signedInput + 7);
                *(output + 8) = (char)*(signedInput + 8);
                *(output + 9) = (char)*(signedInput + 9);
                *(output + 10) = (char)*(signedInput + 10);
                *(output + 11) = (char)*(signedInput + 11);
                output += 12;
                signedInput += 12;
            }
            if (i < count - 5)
            {
                isValid = isValid && *signedInput > 0 && *(signedInput + 1) > 0 && *(signedInput + 2) > 0 &&
                    *(signedInput + 3) > 0 && *(signedInput + 4) > 0 && *(signedInput + 5) > 0;

                i += 6;
                *(output) = (char)*(signedInput);
                *(output + 1) = (char)*(signedInput + 1);
                *(output + 2) = (char)*(signedInput + 2);
                *(output + 3) = (char)*(signedInput + 3);
                *(output + 4) = (char)*(signedInput + 4);
                *(output + 5) = (char)*(signedInput + 5);
                output += 6;
                signedInput += 6;
            }
            if (i < count - 3)
            {
                isValid = isValid && *signedInput > 0 && *(signedInput + 1) > 0 && *(signedInput + 2) > 0 &&
                    *(signedInput + 3) > 0;

                i += 4;
                *(output) = (char)*(signedInput);
                *(output + 1) = (char)*(signedInput + 1);
                *(output + 2) = (char)*(signedInput + 2);
                *(output + 3) = (char)*(signedInput + 3);
                output += 4;
                signedInput += 4;
            }

            while (i < count)
            {
                isValid = isValid && *signedInput > 0;

                i++;
                *output = (char)*signedInput;
                output++;
                signedInput++;
            }

            return isValid;
        }

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

        private void Verify()
        {
            var verification = EncodingAsciiGetChars().Substring(0, _asciiBytes.Length);

            BlankString('\0');
            EncodingAsciiGetChars();
            VerifyString(verification, '\0');
            BlankString(' ');
            EncodingAsciiGetChars();
            VerifyString(verification, ' ');

            BlankString('\0');
            KestrelBytesToString();
            VerifyString(verification, '\0');
            BlankString(' ');
            KestrelBytesToString();
            VerifyString(verification, ' ');

            BlankString('\0');
            AsciiBytesToStringVectorCheck();
            VerifyString(verification, '\0');
            BlankString(' ');
            AsciiBytesToStringVectorCheck();
            VerifyString(verification, ' ');

            BlankString('\0');
            AsciiBytesToStringVectorWiden();
            VerifyString(verification, '\0');
            BlankString(' ');
            AsciiBytesToStringVectorWiden();
            VerifyString(verification, ' ');

            BlankString('\0');
            AsciiBytesToStringSpanWiden();
            VerifyString(verification, '\0');
            BlankString(' ');
            AsciiBytesToStringSpanWiden();
            VerifyString(verification, ' ');
        }

        private unsafe void BlankString(char ch)
        {
            fixed (char* pString = _asciiString)
            {
                for (var i = 0; i < _asciiString.Length; i++)
                {
                    *(pString + i) = ch;
                }
            }
        }

        private unsafe void VerifyString(string verification, char ch)
        {
            fixed (char* pString = _asciiString)
            {
                var i = 0;
                for (; i < verification.Length; i++)
                {
                    if (*(pString + i) != verification[i]) throw new Exception($"Verify failed, saw {(int)*(pString + i)} expected {(int)verification[i]} at position {i}");
                }
                for (; i < _asciiString.Length; i++)
                {
                    if (*(pString + i) != ch) throw new Exception($"Verify failed, saw {(int)*(pString + i)} expected {(int)ch} at position {i}"); ;
                }
            }
        }

        public enum BenchmarkTypes
        {
            KeepAlive,
            Accept,
            UserAgent,
            Cookie,
        }
    }
}
