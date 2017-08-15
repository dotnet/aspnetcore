// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;

namespace Microsoft.AspNetCore.SignalR.Internal.Formatters
{
    public static class BinaryMessageParser
    {
        public static bool TryParseMessage(ref ReadOnlyBuffer<byte> buffer, out ReadOnlyBuffer<byte> payload)
        {
            long length = 0;
            payload = default(ReadOnlyBuffer<byte>);

            if (buffer.Length < sizeof(long))
            {
                return false;
            }

            // Read the length
            length = buffer.Span.Slice(0, sizeof(long)).ReadBigEndian<long>();

            if (length > Int32.MaxValue)
            {
                throw new FormatException("Messages over 2GB in size are not supported");
            }

            // Skip over the length
            var remaining = buffer.Slice(sizeof(long));

            // We don't have enough data
            while (remaining.Length < (int)length)
            {
                return false;
            }

            // Get the payload
            payload = remaining.Slice(0, (int)length);

            // Skip the payload
            buffer = remaining.Slice((int)length);
            return true;
        }
    }
}
