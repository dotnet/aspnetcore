// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Sockets.Formatters
{
    internal static class TextMessageFormatter
    {
        private const byte FieldDelimiter = (byte)':';
        private const byte MessageDelimiter = (byte)';';
        private const byte TextTypeFlag = (byte)'T';
        private const byte BinaryTypeFlag = (byte)'B';
        private const byte CloseTypeFlag = (byte)'C';
        private const byte ErrorTypeFlag = (byte)'E';

        internal static bool TryFormatMessage(Message message, Span<byte> buffer, out int bytesWritten)
        {
            // Calculate the length, it's the number of characters for text messages, but number of base64 characters for binary
            var length = message.Payload.Length;
            if (message.Type == MessageType.Binary)
            {
                length = (int)(4 * Math.Ceiling(((double)message.Payload.Length / 3)));
            }

            // Write the length as a string
            int written = 0;
            if (!length.TryFormat(buffer, out int lengthLen, default(TextFormat), TextEncoder.Utf8))
            {
                bytesWritten = 0;
                return false;
            }
            written += lengthLen;
            buffer = buffer.Slice(lengthLen);

            // We need at least 4 more characters of space (':', type flag, ':', and eventually the terminating ';')
            // We'll still need to double-check that we have space for the terminator after we write the payload,
            // but this way we can exit early if the buffer is way too small.
            if (buffer.Length < 4)
            {
                bytesWritten = 0;
                return false;
            }
            buffer[0] = FieldDelimiter;
            if (!TryFormatType(message.Type, buffer.Slice(1, 1)))
            {
                bytesWritten = 0;
                return false;
            }
            buffer[2] = FieldDelimiter;
            buffer = buffer.Slice(3);
            written += 3;

            // Payload
            if (message.Type == MessageType.Binary)
            {
                // Encode the payload. For now, we make it an array and use the old-fashioned types because we need to mirror packages
                // I've filed https://github.com/aspnet/SignalR/issues/192 to update this. -anurse
                var payload = Convert.ToBase64String(message.Payload);
                if (!TextEncoder.Utf8.TryEncode(payload, buffer, out int payloadWritten))
                {
                    bytesWritten = 0;
                    return false;
                }
                written += payloadWritten;
                buffer = buffer.Slice(payloadWritten);
            }
            else
            {
                if (buffer.Length < message.Payload.Length)
                {
                    bytesWritten = 0;
                    return false;
                }
                message.Payload.CopyTo(buffer.Slice(0, message.Payload.Length));
                written += message.Payload.Length;
                buffer = buffer.Slice(message.Payload.Length);
            }

            // Terminator
            if (buffer.Length < 1)
            {
                bytesWritten = 0;
                return false;
            }
            buffer[0] = MessageDelimiter;
            bytesWritten = written + 1;
            return true;
        }

        internal static bool TryParseMessage(ReadOnlySpan<byte> buffer, out Message message, out int bytesConsumed)
        {
            // Read until the first ':' to find the length
            var consumedSoFar = 0;
            var colonIndex = buffer.IndexOf(FieldDelimiter);
            if (colonIndex < 0)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }
            consumedSoFar += colonIndex;
            var lengthSpan = buffer.Slice(0, colonIndex);
            buffer = buffer.Slice(colonIndex);

            // Parse the length
            if (!PrimitiveParser.TryParseInt32(lengthSpan, out var length, out var consumedByLength, encoder: TextEncoder.Utf8) || consumedByLength < lengthSpan.Length)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            // Check if there's enough space in the buffer to even bother continuing
            // There are at least 4 characters we still expect to see: ':', type flag, ':', ';'.
            if (buffer.Length < 4)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            // Verify that we have the ':' after the type flag.
            if (buffer[0] != FieldDelimiter)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            // We already know that index 0 is the ':', so next is the type flag at index '1'.
            if (!TryParseType(buffer[1], out var messageType))
            {
                message = default(Message);
                bytesConsumed = 0;
            }

            // Verify that we have the ':' after the type flag.
            if (buffer[2] != FieldDelimiter)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            // Slice off ':[Type]:' and check the remaining length
            buffer = buffer.Slice(3);
            consumedSoFar += 3;

            // We expect to see <length>+1 more characters. Since <length> is the exact number of bytes in the text (even if base64-encoded)
            // and we expect to see the ';'
            if (buffer.Length < length + 1)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            // Grab the payload buffer
            var payloadBuffer = buffer.Slice(0, length);
            buffer = buffer.Slice(length);
            consumedSoFar += length;

            // Parse the payload. For now, we make it an array and use the old-fashioned types.
            // I've filed https://github.com/aspnet/SignalR/issues/192 to update this. -anurse
            var payload = payloadBuffer.ToArray();
            if (messageType == MessageType.Binary)
            {
                byte[] decoded;
                try
                {
                    var str = Encoding.UTF8.GetString(payload);
                    decoded = Convert.FromBase64String(str);
                }
                catch
                {
                    // Decoding failure
                    message = default(Message);
                    bytesConsumed = 0;
                    return false;
                }
                payload = decoded;
            }

            // Verify the trailer
            if (buffer.Length < 1 || buffer[0] != MessageDelimiter)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            message = new Message(payload, messageType);
            bytesConsumed = consumedSoFar + 1;
            return true;
        }

        private static bool TryParseType(byte type, out MessageType messageType)
        {
            switch (type)
            {
                case TextTypeFlag:
                    messageType = MessageType.Text;
                    return true;
                case BinaryTypeFlag:
                    messageType = MessageType.Binary;
                    return true;
                case CloseTypeFlag:
                    messageType = MessageType.Close;
                    return true;
                case ErrorTypeFlag:
                    messageType = MessageType.Error;
                    return true;
                default:
                    messageType = default(MessageType);
                    return false;
            }
        }

        private static bool TryFormatType(MessageType type, Span<byte> buffer)
        {
            switch (type)
            {
                case MessageType.Text:
                    buffer[0] = TextTypeFlag;
                    return true;
                case MessageType.Binary:
                    buffer[0] = BinaryTypeFlag;
                    return true;
                case MessageType.Close:
                    buffer[0] = CloseTypeFlag;
                    return true;
                case MessageType.Error:
                    buffer[0] = ErrorTypeFlag;
                    return true;
                default:
                    return false;
            }
        }
    }
}
