// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.SignalR.Internal.Formatters
{
    public static class BinaryMessageFormatter
    {
        public static void WriteLengthPrefix(long length, Stream output)
        {
            // This code writes length prefix of the message as a VarInt. Read the comment in
            // the BinaryMessageParser.TryParseMessage for details.

#if NETCOREAPP2_1
            Span<byte> lenBuffer = stackalloc byte[5];
#else
            var lenBuffer = new byte[5];
#endif
            var lenNumBytes = 0;
            do
            {
                ref var current = ref lenBuffer[lenNumBytes];
                current = (byte)(length & 0x7f);
                length >>= 7;
                if (length > 0)
                {
                    current |= 0x80;
                }
                lenNumBytes++;
            }
            while (length > 0);

#if NETCOREAPP2_1
            output.Write(lenBuffer.Slice(0, lenNumBytes));
#else
            output.Write(lenBuffer, 0, lenNumBytes);
#endif
        }
    }
}
