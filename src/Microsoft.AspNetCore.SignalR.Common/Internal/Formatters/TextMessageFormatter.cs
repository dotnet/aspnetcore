// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Buffers;
using System.Text;
using System.Text.Formatting;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public static class TextMessageFormatter
    {
        internal const char FieldDelimiter = ':';
        internal const char MessageDelimiter = ';';
        
        public static bool TryWriteMessage(ReadOnlySpan<byte> payload, IOutput output)
        {
            // Calculate the length, it's the number of characters for text messages, but number of base64 characters for binary
            var length = payload.Length;

            // Write the length as a string
            output.Append(length, TextEncoder.Utf8);

            // Write the field delimiter ':'
            output.Append(FieldDelimiter, TextEncoder.Utf8);

            // Write the payload
            if (!output.TryWrite(payload))
            {
                return false;
            }

            // Terminator
            output.Append(MessageDelimiter, TextEncoder.Utf8);
            return true;
        }
    }
}
