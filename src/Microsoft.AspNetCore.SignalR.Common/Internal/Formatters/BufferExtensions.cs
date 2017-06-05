// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Buffers
{
    internal static class BufferExtensions
    {
        public static ReadOnlySpan<byte> ToSingleSpan(this ReadOnlyBytes self)
        {
            if (self.Rest == null)
            {
                return self.First.Span;
            }
            else
            {
                return self.ToSpan();
            }
        }

        public static ReadOnlyBytes? TryReadBytes(this BytesReader self, int count)
        {
            try
            {
                return self.ReadBytes(count);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }
    }
}
