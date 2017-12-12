// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers.Text;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public static class LengthPrefixedTextMessageParser
    {
        private const char FieldDelimiter = ':';
        private const char MessageDelimiter = ';';

        /// <summary>
        /// Attempts to parse a message from the buffer. Returns 'false' if there is not enough data to complete a message. Throws an
        /// exception if there is a format error in the provided data.
        /// </summary>
        public static bool TryParseMessage(ref ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> payload)
        {
            payload = default;

            if (!TryReadLength(buffer, out var index, out var length))
            {
                return false;
            }

            var remaining = buffer.Slice(index);

            if (!TryReadDelimiter(remaining, FieldDelimiter, "length"))
            {
                return false;
            }

            // Skip the delimeter
            remaining = remaining.Slice(1);

            if (remaining.Length < length + 1)
            {
                return false;
            }

            payload = remaining.Slice(0, length);

            remaining = remaining.Slice(length);

            if (!TryReadDelimiter(remaining, MessageDelimiter, "payload"))
            {
                return false;
            }

            // Skip the delimeter
            buffer = remaining.Slice(1);
            return true;
        }

        private static bool TryReadLength(ReadOnlySpan<byte> buffer, out int index, out int length)
        {
            length = 0;
            // Read until the first ':' to find the length
            index = buffer.IndexOf((byte)FieldDelimiter);

            if (index == -1)
            {
                // Insufficient data
                return false;
            }

            var lengthSpan = buffer.Slice(0, index);

            if (!Utf8Parser.TryParse(buffer, out length, out var bytesConsumed) || bytesConsumed < lengthSpan.Length)
            {
                throw new FormatException($"Invalid length: '{Encoding.UTF8.GetString(lengthSpan.ToArray())}'");
            }

            return true;
        }

        private static bool TryReadDelimiter(ReadOnlySpan<byte> buffer, char delimiter, string field)
        {
            if (buffer.Length == 0)
            {
                return false;
            }

            if (buffer[0] != delimiter)
            {
                throw new FormatException($"Missing delimiter '{delimiter}' after {field}");
            }

            return true;
        }
    }
}
