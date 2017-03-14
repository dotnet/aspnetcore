// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Binary;
using System.Buffers;
using System.Text;
using System.Text.Formatting;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    internal static class TextMessageFormatter
    {
        internal const char FieldDelimiter = ':';
        internal const char MessageDelimiter = ';';
        internal const char TextTypeFlag = 'T';
        internal const char BinaryTypeFlag = 'B';

        internal const char CloseTypeFlag = 'C';
        internal const char ErrorTypeFlag = 'E';

        public static bool TryWriteMessage(Message message, IOutput output)
        {
            // Calculate the length, it's the number of characters for text messages, but number of base64 characters for binary
            var length = message.Payload.Length;
            if (message.Type == MessageType.Binary)
            {
                length = Base64.ComputeEncodedLength(length);
            }

            // Get the type indicator
            var typeIndicator = GetTypeIndicator(message.Type);

            // Write the length as a string
            output.Append(length, TextEncoder.Utf8);

            // Write the field delimiter ':'
            output.Append(FieldDelimiter, TextEncoder.Utf8);

            // Write the type
            output.Append(typeIndicator, TextEncoder.Utf8);

            // Write the field delimiter ':'
            output.Append(FieldDelimiter, TextEncoder.Utf8);

            // Write the payload
            if (!TryWritePayload(message, output, length))
            {
                return false;
            }

            // Terminator
            output.Append(MessageDelimiter, TextEncoder.Utf8);
            return true;
        }

        private static bool TryWritePayload(Message message, IOutput output, int length)
        {
            // Payload
            if (message.Type == MessageType.Binary)
            {
                // TODO: Base64 writer that works with IOutput would be amazing!
                var arr = new byte[Base64.ComputeEncodedLength(message.Payload.Length)];
                Base64.Encode(message.Payload, arr);
                return output.TryWrite(arr);
            }
            else
            {
                return output.TryWrite(message.Payload);
            }
        }

        private static char GetTypeIndicator(MessageType type)
        {
            switch (type)
            {
                case MessageType.Text: return TextTypeFlag;
                case MessageType.Binary: return BinaryTypeFlag;
                case MessageType.Close: return CloseTypeFlag;
                case MessageType.Error: return ErrorTypeFlag;
                default: throw new FormatException($"Invalid message type: {type}");
            }
        }
    }
}
