// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    internal static class BinaryMessageFormatter
    {
        internal const byte TextTypeFlag = 0x00;
        internal const byte BinaryTypeFlag = 0x01;
        internal const byte ErrorTypeFlag = 0x02;
        internal const byte CloseTypeFlag = 0x03;

        public static bool TryWriteMessage(Message message, IOutput output)
        {
            var typeIndicator = GetTypeIndicator(message.Type);

            // Try to write the data
            if (!output.TryWriteBigEndian((long)message.Payload.Length))
            {
                return false;
            }

            if (!output.TryWriteBigEndian(typeIndicator))
            {
                return false;
            }

            if (!output.TryWrite(message.Payload))
            {
                return false;
            }

            return true;
        }

        private static byte GetTypeIndicator(MessageType type)
        {
            switch (type)
            {
                case MessageType.Text:
                    return TextTypeFlag;
                case MessageType.Binary:
                    return BinaryTypeFlag;
                case MessageType.Close:
                    return CloseTypeFlag;
                case MessageType.Error:
                    return ErrorTypeFlag;
                default:
                    throw new FormatException($"Invalid Message Type: {type}");
            }
        }
    }
}