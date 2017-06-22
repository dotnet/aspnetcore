// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public class TextMessageParser
    {
        private const int Int32OverflowLength = 10;

        private ParserState _state;

        public void Reset()
        {
            _state = default(ParserState);
        }

        /// <summary>
        /// Attempts to parse a message from the buffer. Returns 'false' if there is not enough data to complete a message. Throws an
        /// exception if there is a format error in the provided data.
        /// </summary>
        public bool TryParseMessage(ref ReadOnlySpan<byte> buffer, out ReadOnlyBuffer<byte> payload)
        {
            while (buffer.Length > 0)
            {
                switch (_state.Phase)
                {
                    case ParsePhase.ReadingLength:
                        if (!TryReadLength(ref buffer))
                        {
                            payload = default(ReadOnlyBuffer<byte>);
                            return false;
                        }

                        break;
                    case ParsePhase.LengthComplete:
                        if (!TryReadDelimiter(ref buffer, TextMessageFormatter.FieldDelimiter, ParsePhase.ReadingPayload, "length"))
                        {
                            payload = default(ReadOnlyBuffer<byte>);
                            return false;
                        }
                        break;
                    case ParsePhase.ReadingPayload:
                        ReadPayload(ref buffer);

                        break;
                    case ParsePhase.PayloadComplete:
                        if (!TryReadDelimiter(ref buffer, TextMessageFormatter.MessageDelimiter, ParsePhase.ReadingPayload, "payload"))
                        {
                            payload = default(ReadOnlyBuffer<byte>);
                            return false;
                        }

                        // We're done!
                        payload = _state.Payload;
                        Reset();
                        return true;
                    default:
                        throw new InvalidOperationException($"Invalid parser phase: {_state.Phase}");
                }
            }

            payload = default(ReadOnlyBuffer<byte>);
            return false;
        }

        private bool TryReadLength(ref ReadOnlySpan<byte> buffer)
        {
            // Read until the first ':' to find the length
            var found = buffer.IndexOf((byte)TextMessageFormatter.FieldDelimiter);

            if (found == -1)
            {
                // Insufficient data
                return false;
            }

            var lengthSpan = buffer.Slice(0, found);

            if (!TryParseInt32(lengthSpan, out var length, out var bytesConsumed) || bytesConsumed < lengthSpan.Length)
            {
                throw new FormatException($"Invalid length: '{Encoding.UTF8.GetString(lengthSpan.ToArray())}'");
            }

            buffer = buffer.Slice(found);

            _state.Length = length;
            _state.Phase = ParsePhase.LengthComplete;
            return true;
        }

        private bool TryReadDelimiter(ref ReadOnlySpan<byte> buffer, char delimiter, ParsePhase nextPhase, string field)
        {
            if (buffer.Length == 0)
            {
                return false;
            }

            if (buffer[0] != delimiter)
            {
                throw new FormatException($"Missing delimiter '{delimiter}' after {field}");
            }

            buffer = buffer.Slice(1);

            _state.Phase = nextPhase;
            return true;
        }

        private void ReadPayload(ref ReadOnlySpan<byte> buffer)
        {
            if (_state.Payload == null)
            {
                _state.Payload = new byte[_state.Length];
            }

            if (_state.Read == _state.Length)
            {
                _state.Phase = ParsePhase.PayloadComplete;
            }
            else
            {
                // Copy as much as possible from the Unread buffer
                var toCopy = Math.Min(_state.Length - _state.Read, buffer.Length);

                buffer.Slice(0, toCopy).CopyTo(new Span<byte>(_state.Payload, _state.Read));
                _state.Read += toCopy;
                buffer = buffer.Slice(toCopy);
            }
        }

        public static bool TryParseInt32(ReadOnlySpan<byte> text, out int value, out int bytesConsumed)
        {
            if (text.Length < 1)
            {
                bytesConsumed = 0;
                value = default(int);
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
                value = default(int);
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
                        value = default(int);
                        return false;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }

            bytesConsumed = text.Length;
            value = parsedValue * sign;
            return true;
        }

        private struct ParserState
        {
            public ParsePhase Phase;
            public int Length;
            public byte[] Payload;
            public int Read;
        }

        private enum ParsePhase
        {
            ReadingLength = 0,
            LengthComplete,
            ReadingPayload,
            PayloadComplete
        }
    }
}
