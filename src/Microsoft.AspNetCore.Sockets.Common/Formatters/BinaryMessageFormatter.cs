// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets.Formatters
{
    internal static class BinaryMessageFormatter
    {
        private const byte TextTypeFlag = 0x00;
        private const byte BinaryTypeFlag = 0x01;
        private const byte ErrorTypeFlag = 0x02;
        private const byte CloseTypeFlag = 0x03;

        internal static bool TryFormatMessage(Message message, Span<byte> buffer, out int bytesWritten)
        {
            // We can check the size needed right up front!
            var sizeNeeded = sizeof(long) + 1 + message.Payload.Length;
            if (buffer.Length < sizeNeeded)
            {
                bytesWritten = 0;
                return false;
            }

            buffer.WriteBigEndian((long)message.Payload.Length);
            if (!TryFormatType(message.Type, buffer.Slice(sizeof(long), 1)))
            {
                bytesWritten = 0;
                return false;
            }

            buffer = buffer.Slice(sizeof(long) + 1);

            message.Payload.CopyTo(buffer);
            bytesWritten = sizeNeeded;
            return true;
        }

        internal static bool TryParseMessage(ReadOnlySpan<byte> buffer, out Message message, out int bytesConsumed)
        {
            // Check if we have enough to read the size and type flag
            if (buffer.Length < sizeof(long) + 1)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            // REVIEW: The spec calls for 64-bit length but I'm thinking that's a little ridiculous.
            // REVIEW: We don't really have a primitive for storing that much data. For now, I'm using it
            // REVIEW: but throwing if the size is over 2GB.
            var longLength = buffer.ReadBigEndian<long>();
            if (longLength > Int32.MaxValue)
            {
                throw new FormatException("Messages over 2GB in size are not supported");
            }
            var length = (int)longLength;

            if (!TryParseType(buffer[sizeof(long)], out var messageType))
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            // Check if we actually have the whole payload
            if (buffer.Length < sizeof(long) + 1 + length)
            {
                message = default(Message);
                bytesConsumed = 0;
                return false;
            }

            // Copy the payload into the buffer
            // REVIEW: Copy! Noooooooooo! But how can we capture a segment of the span as an "Owned" reference?
            // REVIEW: If we do have to copy, we should at least use a pooled buffer
            var buf = new byte[length];
            buffer.Slice(sizeof(long) + 1, length).CopyTo(buf);

            message = new Message(buf, messageType, endOfMessage: true);
            bytesConsumed = sizeof(long) + 1 + length;
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