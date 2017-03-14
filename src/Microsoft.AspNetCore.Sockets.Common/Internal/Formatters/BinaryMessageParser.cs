// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Buffers;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    internal class BinaryMessageParser
    {
        private ParserState _state;

        public void Reset()
        {
            _state = default(ParserState);
        }

        public bool TryParseMessage(ref BytesReader buffer, out Message message)
        {
            if (_state.Length == null)
            {
                var length = buffer.TryReadBytes(sizeof(long))?.ToSingleSpan();
                if (length == null || length.Value.Length < sizeof(long))
                {
                    message = default(Message);
                    return false;
                }

                var longLength = length.Value.ReadBigEndian<long>();
                if (longLength > Int32.MaxValue)
                {
                    throw new FormatException("Messages over 2GB in size are not supported");
                }
                buffer.Advance(length.Value.Length);
                _state.Length = (int)longLength;
            }

            if (_state.MessageType == null)
            {
                if (buffer.Unread.Length == 0)
                {
                    message = default(Message);
                    return false;
                }

                var typeByte = buffer.Unread[0];

                if (!TryParseType(typeByte, out var messageType))
                {
                    throw new FormatException($"Unknown type value: 0x{typeByte:X}");
                }

                buffer.Advance(1);
                _state.MessageType = messageType;
            }

            if (_state.Payload == null)
            {
                _state.Payload = new byte[_state.Length.Value];
            }

            while (_state.Read < _state.Payload.Length && buffer.Unread.Length > 0)
            {
                // Copy what we can from the current unread segment
                var toCopy = Math.Min(_state.Payload.Length - _state.Read, buffer.Unread.Length);
                buffer.Unread.Slice(0, toCopy).CopyTo(_state.Payload.Slice(_state.Read));
                _state.Read += toCopy;
                buffer.Advance(toCopy);
            }

            if (_state.Read == _state.Payload.Length)
            {
                message = new Message(_state.Payload, _state.MessageType.Value);
                Reset();
                return true;
            }

            // There's still more to read.
            message = default(Message);
            return false;
        }

        private static bool TryParseType(byte type, out MessageType messageType)
        {
            switch (type)
            {
                case BinaryMessageFormatter.TextTypeFlag:
                    messageType = MessageType.Text;
                    return true;
                case BinaryMessageFormatter.BinaryTypeFlag:
                    messageType = MessageType.Binary;
                    return true;
                case BinaryMessageFormatter.CloseTypeFlag:
                    messageType = MessageType.Close;
                    return true;
                case BinaryMessageFormatter.ErrorTypeFlag:
                    messageType = MessageType.Error;
                    return true;
                default:
                    messageType = default(MessageType);
                    return false;
            }
        }

        private struct ParserState
        {
            public int? Length;
            public MessageType? MessageType;
            public byte[] Payload;
            public int Read;
        }
    }
}
