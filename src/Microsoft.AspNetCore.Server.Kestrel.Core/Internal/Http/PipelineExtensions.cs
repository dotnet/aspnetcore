// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public static class PipelineExtensions
    {
        private const int _maxULongByteLength = 20;

        [ThreadStatic]
        private static byte[] _numericBytesScratch;

        public static ValueTask<ArraySegment<byte>> PeekAsync(this IPipeReader pipelineReader)
        {
            var input = pipelineReader.ReadAsync();
            while (input.IsCompleted)
            {
                var result = input.GetResult();
                try
                {
                    if (!result.Buffer.IsEmpty)
                    {
                        var segment = result.Buffer.First;
                        var data = segment.GetArray();

                        return new ValueTask<ArraySegment<byte>>(data);
                    }
                    else if (result.IsCompleted)
                    {
                        return default(ValueTask<ArraySegment<byte>>);
                    }
                }
                finally
                {
                    pipelineReader.Advance(result.Buffer.Start, result.Buffer.Start);
                }
                input = pipelineReader.ReadAsync();
            }

            return new ValueTask<ArraySegment<byte>>(pipelineReader.PeekAsyncAwaited(input));
        }

        private static async Task<ArraySegment<byte>> PeekAsyncAwaited(this IPipeReader pipelineReader, ReadableBufferAwaitable readingTask)
        {
            while (true)
            {
                var result = await readingTask;

                try
                {
                    if (!result.Buffer.IsEmpty)
                    {
                        var segment = result.Buffer.First;
                        return segment.GetArray();
                    }
                    else if (result.IsCompleted)
                    {
                        return default(ArraySegment<byte>);
                    }
                }
                finally
                {
                    pipelineReader.Advance(result.Buffer.Start, result.Buffer.Start);
                }

                readingTask = pipelineReader.ReadAsync();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> ToSpan(this ReadableBuffer buffer)
        {
            if (buffer.IsSingleSpan)
            {
                return buffer.First.Span;
            }
            return buffer.ToArray();
        }

        public static ArraySegment<byte> GetArray(this Buffer<byte> buffer)
        {
            ArraySegment<byte> result;
            if (!buffer.TryGetArray(out result))
            {
                throw new InvalidOperationException("Buffer backed by array was expected");
            }
            return result;
        }

        // Temporary until the fast write implementation propagates from corefx
        public unsafe static void WriteFast(this WritableBuffer buffer, byte[] source)
        {
            buffer.WriteFast(source, 0, source.Length);
        }

        public unsafe static void WriteFast(this WritableBuffer buffer, ArraySegment<byte> source)
        {
            buffer.WriteFast(source.Array, source.Offset, source.Count);
        }

        public unsafe static void WriteFast(this WritableBuffer buffer, byte[] source, int offset, int length)
        {
            var dest = buffer.Buffer.Span;
            var destLength = dest.Length;

            if (destLength == 0)
            {
                buffer.Ensure();

                // Get the new span and length
                dest = buffer.Buffer.Span;
                destLength = dest.Length;
            }

            var sourceLength = length;
            if (sourceLength <= destLength)
            {
                ref byte pSource = ref source[offset];
                ref byte pDest = ref dest.DangerousGetPinnableReference();
                Unsafe.CopyBlockUnaligned(ref pDest, ref pSource, (uint)sourceLength);
                buffer.Advance(sourceLength);
                return;
            }

            buffer.WriteMultiBuffer(source, offset, length);
        }

        private static unsafe void WriteMultiBuffer(this WritableBuffer buffer, byte[] source, int offset, int length)
        {
            var remaining = length;

            while (remaining > 0)
            {
                var writable = Math.Min(remaining, buffer.Buffer.Length);

                buffer.Ensure(writable);

                if (writable == 0)
                {
                    continue;
                }

                ref byte pSource = ref source[offset];
                ref byte pDest = ref buffer.Buffer.Span.DangerousGetPinnableReference();

                Unsafe.CopyBlockUnaligned(ref pDest, ref pSource, (uint)writable);

                remaining -= writable;
                offset += writable;

                buffer.Advance(writable);
            }
        }

        /// <summary>
        /// Write string characters as ASCII without validating that characters fall in the ASCII range
        /// </summary>
        /// <remarks>
        /// ASCII character validation is done by <see cref="FrameHeaders.ValidateHeaderCharacters(string)"/>
        /// </remarks>
        /// <param name="buffer">the buffer</param>
        /// <param name="data">The string to write</param>
        public unsafe static void WriteAsciiNoValidation(this WritableBuffer buffer, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            var dest = buffer.Buffer.Span;
            var destLength = dest.Length;
            var sourceLength = data.Length;

            if (destLength == 0)
            {
                buffer.Ensure();

                dest = buffer.Buffer.Span;
                destLength = dest.Length;
            }

            // Fast path, try copying to the available memory directly
            if (sourceLength <= destLength)
            {
                fixed (char* input = data)
                fixed (byte* output = &dest.DangerousGetPinnableReference())
                {
                    EncodeAsciiCharsToBytes(input, output, sourceLength);
                }

                buffer.Advance(sourceLength);
            }
            else
            {
                buffer.WriteAsciiMultiWrite(data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void WriteNumeric(this WritableBuffer buffer, ulong number)
        {
            const byte AsciiDigitStart = (byte)'0';

            var span = buffer.Buffer.Span;
            var bytesLeftInBlock = span.Length;

            if (bytesLeftInBlock == 0)
            {
                buffer.Ensure();

                span = buffer.Buffer.Span;
                bytesLeftInBlock = span.Length;
            }

            // Fast path, try copying to the available memory directly
            var simpleWrite = true;
            fixed (byte* output = &span.DangerousGetPinnableReference())
            {
                var start = output;
                if (number < 10 && bytesLeftInBlock >= 1)
                {
                    *(start) = (byte)(((uint)number) + AsciiDigitStart);
                    buffer.Advance(1);
                }
                else if (number < 100 && bytesLeftInBlock >= 2)
                {
                    var val = (uint)number;
                    var tens = (byte)((val * 205u) >> 11); // div10, valid to 1028

                    *(start) = (byte)(tens + AsciiDigitStart);
                    *(start + 1) = (byte)(val - (tens * 10) + AsciiDigitStart);
                    buffer.Advance(2);
                }
                else if (number < 1000 && bytesLeftInBlock >= 3)
                {
                    var val = (uint)number;
                    var digit0 = (byte)((val * 41u) >> 12); // div100, valid to 1098
                    var digits01 = (byte)((val * 205u) >> 11); // div10, valid to 1028

                    *(start) = (byte)(digit0 + AsciiDigitStart);
                    *(start + 1) = (byte)(digits01 - (digit0 * 10) + AsciiDigitStart);
                    *(start + 2) = (byte)(val - (digits01 * 10) + AsciiDigitStart);
                    buffer.Advance(3);
                }
                else
                {
                    simpleWrite = false;
                }
            }

            if (!simpleWrite)
            {
                buffer.WriteNumericMultiWrite(number);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void WriteNumericMultiWrite(this WritableBuffer buffer, ulong number)
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
            buffer.WriteFast(new ArraySegment<byte>(byteBuffer, position, length));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe static void WriteAsciiMultiWrite(this WritableBuffer buffer, string data)
        {
            var remaining = data.Length;

            fixed (char* input = data)
            {
                var inputSlice = input;

                while (remaining > 0)
                {
                    var writable = Math.Min(remaining, buffer.Buffer.Length);

                    buffer.Ensure(writable);

                    if (writable == 0)
                    {
                        continue;
                    }

                    fixed (byte* output = &buffer.Buffer.Span.DangerousGetPinnableReference())
                    {
                        EncodeAsciiCharsToBytes(inputSlice, output, writable);
                    }

                    inputSlice += writable;
                    remaining -= writable;

                    buffer.Advance(writable);
                }
            }
        }

        private unsafe static void EncodeAsciiCharsToBytes(char* input, byte* output, int length)
        {
            // Note: Not BIGENDIAN or check for non-ascii
            const int Shift16Shift24 = (1 << 16) | (1 << 24);
            const int Shift8Identity = (1 << 8) | (1);

            // Encode as bytes upto the first non-ASCII byte and return count encoded
            int i = 0;
            // Use Intrinsic switch
            if (IntPtr.Size == 8) // 64 bit
            {
                if (length < 4) goto trailing;

                int unaligned = (int)(((ulong)input) & 0x7) >> 1;
                // Unaligned chars
                for (; i < unaligned; i++)
                {
                    char ch = *(input + i);
                    *(output + i) = (byte)ch; // Cast convert
                }

                // Aligned
                int ulongDoubleCount = (length - i) & ~0x7;
                for (; i < ulongDoubleCount; i += 8)
                {
                    ulong inputUlong0 = *(ulong*)(input + i);
                    ulong inputUlong1 = *(ulong*)(input + i + 4);
                    // Pack 16 ASCII chars into 16 bytes
                    *(uint*)(output + i) =
                        ((uint)((inputUlong0 * Shift16Shift24) >> 24) & 0xffff) |
                        ((uint)((inputUlong0 * Shift8Identity) >> 24) & 0xffff0000);
                    *(uint*)(output + i + 4) =
                        ((uint)((inputUlong1 * Shift16Shift24) >> 24) & 0xffff) |
                        ((uint)((inputUlong1 * Shift8Identity) >> 24) & 0xffff0000);
                }
                if (length - 4 > i)
                {
                    ulong inputUlong = *(ulong*)(input + i);
                    // Pack 8 ASCII chars into 8 bytes
                    *(uint*)(output + i) =
                        ((uint)((inputUlong * Shift16Shift24) >> 24) & 0xffff) |
                        ((uint)((inputUlong * Shift8Identity) >> 24) & 0xffff0000);
                    i += 4;
                }

                trailing:
                for (; i < length; i++)
                {
                    char ch = *(input + i);
                    *(output + i) = (byte)ch; // Cast convert
                }
            }
            else // 32 bit
            {
                // Unaligned chars
                if ((unchecked((int)input) & 0x2) != 0)
                {
                    char ch = *input;
                    i = 1;
                    *(output) = (byte)ch; // Cast convert
                }

                // Aligned
                int uintCount = (length - i) & ~0x3;
                for (; i < uintCount; i += 4)
                {
                    uint inputUint0 = *(uint*)(input + i);
                    uint inputUint1 = *(uint*)(input + i + 2);
                    // Pack 4 ASCII chars into 4 bytes
                    *(ushort*)(output + i) = (ushort)(inputUint0 | (inputUint0 >> 8));
                    *(ushort*)(output + i + 2) = (ushort)(inputUint1 | (inputUint1 >> 8));
                }
                if (length - 1 > i)
                {
                    uint inputUint = *(uint*)(input + i);
                    // Pack 2 ASCII chars into 2 bytes
                    *(ushort*)(output + i) = (ushort)(inputUint | (inputUint >> 8));
                    i += 2;
                }

                if (i < length)
                {
                    char ch = *(input + i);
                    *(output + i) = (byte)ch; // Cast convert
                    i = length;
                }
            }
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