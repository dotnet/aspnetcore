// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public class BinaryMessageParser
    {
        private ParserState _state;

        public void Reset()
        {
            _state = default(ParserState);
        }

        public bool TryParseMessage(ref ReadOnlySpan<byte> buffer, out ReadOnlyBuffer<byte> payload)
        {
            if (_state.Length == null)
            {
                long length = 0;

                if (buffer.Length < sizeof(long))
                {
                    payload = default(ReadOnlyBuffer<byte>);
                    return false;
                }

                length = buffer.Slice(0, sizeof(long)).ReadBigEndian<long>();

                if (length > Int32.MaxValue)
                {
                    throw new FormatException("Messages over 2GB in size are not supported");
                }

                buffer = buffer.Slice(sizeof(long));
                _state.Length = (int)length;
            }

            if (_state.Payload == null)
            {
                _state.Payload = new byte[_state.Length.Value];
            }

            while (_state.Read < _state.Payload.Length && buffer.Length > 0)
            {
                // Copy what we can from the current unread segment
                var toCopy = Math.Min(_state.Payload.Length - _state.Read, buffer.Length);
                buffer.Slice(0, toCopy).CopyTo(new Span<byte>(_state.Payload, _state.Read));
                _state.Read += toCopy;
                buffer = buffer.Slice(toCopy);
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
