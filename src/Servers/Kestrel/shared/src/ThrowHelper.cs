// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal class ThrowHelper
    {
        public static void ThrowArgumentOutOfRangeException(int sourceLength, int offset)
        {
            throw GetArgumentOutOfRangeException(sourceLength, offset);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(int sourceLength, int offset)
        {
            if ((uint)offset > (uint)sourceLength)
            {
                // Offset is negative or less than array length
                return new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.offset));
            }

            // The third parameter (not passed) length must be out of range
            return new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.length));
        }

        public static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
        {
            throw GetArgumentOutOfRangeException(argument);
        }
        
        public static void ThrowInvalidOperationException_ReferenceCountZero()
        {
            throw new InvalidOperationException("Can't release when reference count is already zero");
        }

        public static void ThrowInvalidOperationException_ReturningPinnedBlock()
        {
            throw new InvalidOperationException("Can't release when reference count is already zero");
        }

        public static void ThrowArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize)
        {
            throw GetArgumentOutOfRangeException_BufferRequestTooLarge(maxSize);
        }

        public static void ThrowObjectDisposedException(ExceptionArgument argument)
        {
            throw GetObjectDisposedException(argument);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument)
        {
            return new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize)
        {
            return new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.size), $"Cannot allocate more than {maxSize} bytes in a single buffer");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ObjectDisposedException GetObjectDisposedException(ExceptionArgument argument)
        {
            return new ObjectDisposedException(GetArgumentName(argument));
        }

        private static string GetArgumentName(ExceptionArgument argument)
        {
            Debug.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument), "The enum value is not defined, please check the ExceptionArgument Enum.");

            return argument.ToString();
        }
    }

    internal enum ExceptionArgument
    {
        size,
        offset,
        length,
        MemoryPoolBlock
    }
}
