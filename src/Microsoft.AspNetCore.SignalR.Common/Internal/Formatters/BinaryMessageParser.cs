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

            // We don't have enough data
            if (buffer.Length < (int)length + sizeof(long))
            {
                return false;
            }

            // Get the payload
            payload = buffer.Slice(sizeof(long), (int)length);

            // Skip the payload
            buffer = buffer.Slice((int)length + sizeof(long));
            return true;
        }
    }
}