// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace PlatformBenchmarks
{
    // Same as KestrelHttpServer\src\Kestrel.Core\Internal\Http\PipelineExtensions.cs
    // However methods accept T : struct, IBufferWriter<byte> rather than PipeWriter.
    // This allows a struct wrapper to turn CountingBufferWriter into a non-shared generic,
    // while still offering the WriteNumeric extension.

    public static class BufferExtensions
    {
        private const int _maxULongByteLength = 20;

        [ThreadStatic]
        private static byte[] _numericBytesScratch;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteNumeric<T>(ref this BufferWriter<T> buffer, uint number)
             where T : struct, IBufferWriter<byte>
        {
            const byte AsciiDigitStart = (byte)'0';

            var span = buffer.Span;
            var bytesLeftInBlock = span.Length;

            // Fast path, try copying to the available memory directly
            var advanceBy = 0;
            fixed (byte* output = span)
            {
                var start = output;
                if (number < 10 && bytesLeftInBlock >= 1)
                {
                    start[0] = (byte)(number + AsciiDigitStart);
                    advanceBy = 1;
                }
                else if (number < 100 && bytesLeftInBlock >= 2)
                {
                    var tens = (byte)((number * 205u) >> 11); // div10, valid to 1028

                    start[0] = (byte)(tens + AsciiDigitStart);
                    start[1] = (byte)(number - (tens * 10) + AsciiDigitStart);
                    advanceBy = 2;
                }
                else if (number < 1000 && bytesLeftInBlock >= 3)
                {
                    var digit0 = (byte)((number * 41u) >> 12); // div100, valid to 1098
                    var digits01 = (byte)((number * 205u) >> 11); // div10, valid to 1028

                    start[0] = (byte)(digit0 + AsciiDigitStart);
                    start[1] = (byte)(digits01 - (digit0 * 10) + AsciiDigitStart);
                    start[2] = (byte)(number - (digits01 * 10) + AsciiDigitStart);
                    advanceBy = 3;
                }
            }

            if (advanceBy > 0)
            {
                buffer.Advance(advanceBy);
            }
            else
            {
                WriteNumericMultiWrite(ref buffer, number);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteNumericMultiWrite<T>(ref this BufferWriter<T> buffer, uint number)
             where T : struct, IBufferWriter<byte>
        {
            const byte AsciiDigitStart = (byte)'0';

            var value = number;
            var position = _maxULongByteLength;
            var byteBuffer = NumericBytesScratch;
            do
            {
                // Consider using Math.DivRem() if available
                var quotient = value / 10;
                byteBuffer[--position] = (byte)(AsciiDigitStart + (value - quotient * 10)); // 0x30 = '0'
                value = quotient;
            }
            while (value != 0);

            var length = _maxULongByteLength - position;
            buffer.Write(new ReadOnlySpan<byte>(byteBuffer, position, length));
        }

        private static byte[] NumericBytesScratch => _numericBytesScratch ?? CreateNumericBytesScratch();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] CreateNumericBytesScratch()
        {
            var bytes = new byte[_maxULongByteLength];
            _numericBytesScratch = bytes;
            return bytes;
        }
    }
}
