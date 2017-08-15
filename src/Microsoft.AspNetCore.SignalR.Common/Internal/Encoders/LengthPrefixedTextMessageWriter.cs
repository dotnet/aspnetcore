// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public static class LengthPrefixedTextMessageWriter
    {
        private const int Int32OverflowLength = 10;

        internal const char FieldDelimiter = ':';
        internal const char MessageDelimiter = ';';

        public static void WriteMessage(ReadOnlySpan<byte> payload, Stream output)
        {
            // Calculate the length, it's the number of characters for text messages, but number of base64 characters for binary

            // Write the length as a string

            // Super inefficient...
            var lengthString = payload.Length.ToString(CultureInfo.InvariantCulture);
            var buffer = ArrayPool<byte>.Shared.Rent(Int32OverflowLength);
            var encodedLength = Encoding.UTF8.GetBytes(lengthString, 0, lengthString.Length, buffer, 0);
            output.Write(buffer, 0, encodedLength);
            ArrayPool<byte>.Shared.Return(buffer);

            // Write the field delimiter ':'
            output.WriteByte((byte)FieldDelimiter);

            buffer = ArrayPool<byte>.Shared.Rent(payload.Length);
            payload.CopyTo(buffer);
            output.Write(buffer, 0, payload.Length);
            ArrayPool<byte>.Shared.Return(buffer);

            // Terminator
            output.WriteByte((byte)MessageDelimiter);
        }
    }
}
