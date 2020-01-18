// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers
{
    internal static class BufferExtensions
    {
        private const int _maxULongByteLength = 20;

        [ThreadStatic]
        private static byte[] _numericBytesScratch;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> ToSpan(in this ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                return buffer.First.Span;
            }
            return buffer.ToArray();
        }

        public static ArraySegment<byte> GetArray(this Memory<byte> buffer)
        {
            return ((ReadOnlyMemory<byte>)buffer).GetArray();
        }

        public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory)
        {
            if (!MemoryMarshal.TryGetArray(memory, out var result))
            {
                throw new InvalidOperationException("Buffer backed by array was expected");
            }
            return result;
        }

        internal static unsafe void WriteAscii(ref this BufferWriter<PipeWriter> buffer, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            var dataLength = data.Length;
            var bytes = buffer.Span;
            var bytesLength = bytes.Length;

            // Fast path, try encoding to the available memory directly
            if (dataLength <= bytesLength)
            {
                fixed (char* charsPtr = data)
                fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                {
                    Encoding.ASCII.GetBytes(charsPtr, dataLength, bytesPtr, bytesLength);
                    buffer.Advance(dataLength);
                }
            }
            else
            {
                WriteAsciiMultiWrite(ref buffer, data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteNumeric(ref this BufferWriter<PipeWriter> buffer, ulong number)
        {
            const byte AsciiDigitStart = (byte)'0';

            var span = buffer.Span;
            var bytesLeftInBlock = span.Length;

            // Fast path, try copying to the available memory directly
            var simpleWrite = true;
            fixed (byte* output = span)
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
                WriteNumericMultiWrite(ref buffer, number);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteNumericMultiWrite(ref this BufferWriter<PipeWriter> buffer, ulong number)
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe static void WriteAsciiMultiWrite(ref this BufferWriter<PipeWriter> buffer, string data)
        {
            Debug.Assert(!string.IsNullOrEmpty(data));

            fixed (char* charsPtr = data)
            {
                var dataLength = data.Length;
                var offset = 0;
                var bytes = buffer.Span;
                var bytesLength = bytes.Length;
                do
                {
                    var writable = Math.Min(dataLength - offset, bytesLength);
                    // Zero length spans are possible
                    if (writable > 0)
                    {
                        fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
                        {
                            Encoding.ASCII.GetBytes(charsPtr + offset, writable, bytesPtr, bytesLength);

                            buffer.Advance(writable);
                            offset += writable;
                        }
                    }

                    // Get new span if more to encode, and reset bytesLength
                    if (offset < dataLength)
                    {
                        buffer.Ensure();
                        bytes = buffer.Span;
                        bytesLength = bytes.Length;
                        continue;
                    }
                    else
                    {
                        // Encoded everything
                        break;
                    }
                } while (true);
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
