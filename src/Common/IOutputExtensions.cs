// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Binary;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal static class IOutputExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteBigEndian<[Primitive] T>(this IOutput self, T value) where T : struct
        {
            var size = Unsafe.SizeOf<T>();
            if (self.Buffer.Length < size)
            {
                self.Enlarge(size);
                if (self.Buffer.Length < size)
                {
                    return false;
                }
            }

            self.Buffer.WriteBigEndian(value);
            self.Advance(size);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWrite(this IOutput self, ReadOnlySpan<byte> data)
        {
            while (data.Length > 0)
            {
                if (self.Buffer.Length == 0)
                {
                    self.Enlarge(data.Length);
                    if (self.Buffer.Length == 0)
                    {
                        // Failed to enlarge
                        return false;
                    }
                }

                var toWrite = Math.Min(self.Buffer.Length, data.Length);

                // Slice based on what we can fit
                var chunk = data.Slice(0, toWrite);
                data = data.Slice(toWrite);

                // Copy the chunk
                chunk.CopyTo(self.Buffer);
                self.Advance(chunk.Length);
            }

            return true;
        }
    }
}
