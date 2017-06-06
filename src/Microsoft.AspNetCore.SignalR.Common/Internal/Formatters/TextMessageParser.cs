// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Text;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public class TextMessageParser
    {
        private ParserState _state;

        public void Reset()
        {
            _state = default(ParserState);
        }

        /// <summary>
        /// Attempts to parse a message from the buffer. Returns 'false' if there is not enough data to complete a message. Throws an
        /// exception if there is a format error in the provided data.
        /// </summary>
        public bool TryParseMessage(ref BytesReader buffer, out ReadOnlyBuffer<byte> payload)
        {
            while (buffer.Unread.Length > 0)
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

        private bool TryReadLength(ref BytesReader buffer)
        {
            // Read until the first ':' to find the length
            var lengthBuffer = buffer.ReadBytesUntil((byte)TextMessageFormatter.FieldDelimiter);

            if (lengthBuffer == null)
            {
                // Insufficient data
                return false;
            }

            var lengthSpan = lengthBuffer.Value.ToSingleSpan();

            // Parse the length
            if (!PrimitiveParser.TryParseInt32(lengthSpan, out var length, out var consumedByLength, encoder: TextEncoder.Utf8) || consumedByLength < lengthSpan.Length)
            {
                if (TextEncoder.Utf8.TryDecode(lengthSpan, out var lengthString, out _))
                {
                    throw new FormatException($"Invalid length: '{lengthString}'");
                }

                throw new FormatException("Invalid length");
            }

            _state.Length = length;
            _state.Phase = ParsePhase.LengthComplete;
            return true;
        }

        private bool TryReadDelimiter(ref BytesReader buffer, char delimiter, ParsePhase nextPhase, string field)
        {
            if (buffer.Unread.Length == 0)
            {
                return false;
            }

            if (buffer.Unread[0] != delimiter)
            {
                throw new FormatException($"Missing delimiter '{delimiter}' after {field}");
            }
            buffer.Advance(1);

            _state.Phase = nextPhase;
            return true;
        }

        private void ReadPayload(ref BytesReader buffer)
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
                var toCopy = Math.Min(_state.Length - _state.Read, buffer.Unread.Length);
                buffer.Unread.Slice(0, toCopy).CopyTo(_state.Payload.Slice(_state.Read));
                _state.Read += toCopy;
                buffer.Advance(toCopy);
            }
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
