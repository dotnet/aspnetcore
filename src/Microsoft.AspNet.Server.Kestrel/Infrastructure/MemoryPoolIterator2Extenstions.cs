// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNet.Server.Kestrel.Infrastructure
{
    public static class MemoryPoolIterator2Extenstions
    {
        private const int _maxStackAllocBytes = 16384;

        private static Encoding _utf8 = Encoding.UTF8;

        private static unsafe string GetAsciiStringStack(byte[] input, int inputOffset, int length)
        {
            // avoid declaring other local vars, or doing work with stackalloc
            // to prevent the .locals init cil flag , see: https://github.com/dotnet/coreclr/issues/1279
            char* output = stackalloc char[length];

            return GetAsciiStringImplementation(output, input, inputOffset, length);
        }
        private static unsafe string GetAsciiStringImplementation(char* output, byte[] input, int inputOffset, int length)
        {
            for (var i = 0; i < length; i++)
            {
                output[i] = (char)input[inputOffset + i];
            }

            return new string(output, 0, length);
        }

        private static unsafe string GetAsciiStringStack(MemoryPoolBlock2 start, MemoryPoolIterator2 end, int inputOffset, int length)
        {
            // avoid declaring other local vars, or doing work with stackalloc
            // to prevent the .locals init cil flag , see: https://github.com/dotnet/coreclr/issues/1279
            char* output = stackalloc char[length];

            return GetAsciiStringImplementation(output, start, end, inputOffset, length);
        }

        private unsafe static string GetAsciiStringHeap(MemoryPoolBlock2 start, MemoryPoolIterator2 end, int inputOffset, int length)
        {
            var buffer = new char[length];

            fixed (char* output = buffer)
            {
                return GetAsciiStringImplementation(output, start, end, inputOffset, length);
            }
        }

        private static unsafe string GetAsciiStringImplementation(char* output, MemoryPoolBlock2 start, MemoryPoolIterator2 end, int inputOffset, int length)
        {
            var outputOffset = 0;
            var block = start;
            var remaining = length;

            var endBlock = end.Block;
            var endIndex = end.Index;

            while (true)
            {
                int following = (block != endBlock ? block.End : endIndex) - inputOffset;

                if (following > 0)
                {
                    var input = block.Array;
                    for (var i = 0; i < following; i++)
                    {
                        output[i + outputOffset] = (char)input[i + inputOffset];
                    }

                    remaining -= following;
                    outputOffset += following;
                }

                if (remaining == 0)
                {
                    return new string(output, 0, length);
                }

                block = block.Next;
                inputOffset = block.Start;
            }
        }

        public static string GetAsciiString(this MemoryPoolIterator2 start, MemoryPoolIterator2 end)
        {
            if (start.IsDefault || end.IsDefault)
            {
                return default(string);
            }

            var length = start.GetLength(end);

            // Bytes out of the range of ascii are treated as "opaque data" 
            // and kept in string as a char value that casts to same input byte value
            // https://tools.ietf.org/html/rfc7230#section-3.2.4
            if (end.Block == start.Block)
            {
                return GetAsciiStringStack(start.Block.Array, start.Index, length);
            }

            if (length > _maxStackAllocBytes)
            {
                return GetAsciiStringHeap(start.Block, end, start.Index, length);
            }

            return GetAsciiStringStack(start.Block, end, start.Index, length);
        }

        public static string GetUtf8String(this MemoryPoolIterator2 start, MemoryPoolIterator2 end)
        {
            if (start.IsDefault || end.IsDefault)
            {
                return default(string);
            }
            if (end.Block == start.Block)
            {
                return _utf8.GetString(start.Block.Array, start.Index, end.Index - start.Index);
            }

            var decoder = _utf8.GetDecoder();

            var length = start.GetLength(end);
            var charLength = length * 2;
            var chars = new char[charLength];
            var charIndex = 0;

            var block = start.Block;
            var index = start.Index;
            var remaining = length;
            while (true)
            {
                int bytesUsed;
                int charsUsed;
                bool completed;
                var following = block.End - index;
                if (remaining <= following)
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        remaining,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        true,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    return new string(chars, 0, charIndex + charsUsed);
                }
                else if (block.Next == null)
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        following,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        true,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    return new string(chars, 0, charIndex + charsUsed);
                }
                else
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        following,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        false,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    charIndex += charsUsed;
                    remaining -= following;
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public static ArraySegment<byte> GetArraySegment(this MemoryPoolIterator2 start, MemoryPoolIterator2 end)
        {
            if (start.IsDefault || end.IsDefault)
            {
                return default(ArraySegment<byte>);
            }
            if (end.Block == start.Block)
            {
                return new ArraySegment<byte>(start.Block.Array, start.Index, end.Index - start.Index);
            }

            var length = start.GetLength(end);
            var array = new byte[length];
            start.CopyTo(array, 0, length, out length);
            return new ArraySegment<byte>(array, 0, length);
        }
    }
}
