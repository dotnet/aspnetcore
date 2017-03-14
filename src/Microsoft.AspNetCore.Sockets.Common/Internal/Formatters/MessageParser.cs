// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public class MessageParser
    {
        private TextMessageParser _textParser = new TextMessageParser();
        private BinaryMessageParser _binaryParser = new BinaryMessageParser();

        public void Reset()
        {
            _textParser.Reset();
            _binaryParser.Reset();
        }

        public bool TryParseMessage(ref BytesReader buffer, MessageFormat format, out Message message)
        {
            return format == MessageFormat.Text ?
                _textParser.TryParseMessage(ref buffer, out message) :
                _binaryParser.TryParseMessage(ref buffer, out message);
        }

        public static MessageFormat GetFormat(byte formatIndicator)
        {
            // Can't use switch because our "constants" are not consts, they're "static readonly" (which is good, because they are public)
            if (formatIndicator == MessageFormatter.TextFormatIndicator)
            {
                return MessageFormat.Text;
            }

            if (formatIndicator == MessageFormatter.BinaryFormatIndicator)
            {
                return MessageFormat.Binary;
            }

            throw new ArgumentException($"Invalid message format: 0x{formatIndicator:X}", nameof(formatIndicator));
        }
    }
}
