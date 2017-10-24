// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public static class LengthPrefixedTextMessageParser
    {
        private const int Int32OverflowLength = 10;

        /// <summary>
        /// Attempts to parse a message from the buffer. Returns 'false' if there is not enough data to complete a message. Throws an
        /// exception if there is a format error in the provided data.
        /// </summary>
        public static bool TryParseMessage(ref ReadOnlyBuffer<byte> buffer, out ReadOnlyBuffer<byte> payload)
        {
            payload = default;
            var span = buffer.Span;

            if (!TryReadLength(span, out var index, out var length))
            {
                return false;
            }

            var remaining = buffer.Slice(index);
            span = remaining.Span;

            if (!TryReadDelimiter(span, LengthPrefixedTextMessageWriter.FieldDelimiter, "length"))
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

            if (!TryReadDelimiter(remaining.Span, LengthPrefixedTextMessageWriter.MessageDelimiter, "payload"))
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
            index = buffer.IndexOf((byte)LengthPrefixedTextMessageWriter.FieldDelimiter);

            if (index == -1)
            {
                // Insufficient data
                return false;
            }

            var lengthSpan = buffer.Slice(0, index);

            if (!TryParseInt32(lengthSpan, out length, out var bytesConsumed) || bytesConsumed < lengthSpan.Length)
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

        private static bool TryParseInt32(ReadOnlySpan<byte> text, out int value, out int bytesConsumed)
        {
            if (text.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            int indexOfFirstDigit = 0;
            int sign = 1;
            if (text[0] == '-')
            {
                indexOfFirstDigit = 1;
                sign = -1;
            }
            else if (text[0] == '+')
            {
                indexOfFirstDigit = 1;
            }

            int overflowLength = Int32OverflowLength + indexOfFirstDigit;

            // Parse the first digit separately. If invalid here, we need to return false.
            int firstDigit = text[indexOfFirstDigit] - 48; // '0'
            if (firstDigit < 0 || firstDigit > 9)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            int parsedValue = firstDigit;

            if (text.Length < overflowLength)
            {
                // Length is less than Int32OverflowLength; overflow is not possible
                for (int index = indexOfFirstDigit + 1; index < text.Length; index++)
                {
                    int nextDigit = text[index] - 48; // '0'
                    if (nextDigit < 0 || nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue * sign;
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }
            else
            {
                // Length is greater than Int32OverflowLength; overflow is only possible after Int32OverflowLength
                // digits. There may be no overflow after Int32OverflowLength if there are leading zeroes.
                for (int index = indexOfFirstDigit + 1; index < overflowLength - 1; index++)
                {
                    int nextDigit = text[index] - 48; // '0'
                    if (nextDigit < 0 || nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue * sign;
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
                for (int index = overflowLength - 1; index < text.Length; index++)
                {
                    int nextDigit = text[index] - 48; // '0'
                    if (nextDigit < 0 || nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue * sign;
                        return true;
                    }
                    // If parsedValue > (int.MaxValue / 10), any more appended digits will cause overflow.
                    // if parsedValue == (int.MaxValue / 10), any nextDigit greater than 7 or 8 (depending on sign) implies overflow.
                    bool positive = sign > 0;
                    bool nextDigitTooLarge = nextDigit > 8 || (positive && nextDigit > 7);
                    if (parsedValue > int.MaxValue / 10 || parsedValue == int.MaxValue / 10 && nextDigitTooLarge)
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }

            bytesConsumed = text.Length;
            value = parsedValue * sign;
            return true;
        }
    }
}
