// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    public static class IntegerEncoder
    {
        public static bool Encode(int i, int n, Span<byte> buffer, out int length)
        {
            var j = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            if (i < (1 << n) - 1)
            {
                buffer[j] &= MaskHigh(8 - n);
                buffer[j++] |= (byte)i;
            }
            else
            {
                buffer[j] &= MaskHigh(8 - n);
                buffer[j++] |= (byte)((1 << n) - 1);

                if (j == buffer.Length)
                {
                    return false;
                }

                i = i - ((1 << n) - 1);
                while (i >= 128)
                {
                    buffer[j++] = (byte)(i % 128 + 128);

                    if (j > buffer.Length)
                    {
                        return false;
                    }

                    i = i / 128;
                }
                buffer[j++] = (byte)i;
            }

            length = j;
            return true;
        }

        private static byte MaskHigh(int n)
        {
            return (byte)(sbyte.MinValue >> (n - 1));
        }
    }
}
