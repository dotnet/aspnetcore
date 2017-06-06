// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Buffers;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public class BinaryMessageParser
    {
        private ParserState _state;

        public void Reset()
        {
            _state = default(ParserState);
        }

        public bool TryParseMessage(ref BytesReader buffer, out ReadOnlyBuffer<byte> payload)
        {
            if (_state.Length == null)
            {
                var lengthBuffer = buffer.TryReadBytes(sizeof(long));

                if (lengthBuffer == null)
                {
                    payload = default(ReadOnlyBuffer<byte>);
                    return false;
                }

                var length = lengthBuffer.Value.ToSingleSpan();

                if (length.Length < sizeof(long))
                {
                    payload = default(ReadOnlyBuffer<byte>);
                    return false;
                }

                var longLength = length.ReadBigEndian<long>();
                if (longLength > Int32.MaxValue)
                {
                    throw new FormatException("Messages over 2GB in size are not supported");
                }
                buffer.Advance(length.Length);
                _state.Length = (int)longLength;
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
                payload = _state.Payload;
                Reset();
                return true;
            }

            // There's still more to read.
            payload = default(ReadOnlyBuffer<byte>);
            return false;
        }

        private struct ParserState
        {
            public int? Length;
            public byte[] Payload;
            public int Read;
        }
    }
}
