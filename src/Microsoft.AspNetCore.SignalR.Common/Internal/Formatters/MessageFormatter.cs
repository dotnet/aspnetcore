// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public class MessageFormatter
    {
        public static readonly char TextFormatIndicator = 'T';
        public static readonly char BinaryFormatIndicator = 'B';

        public static bool TryWriteMessage(Message message, IOutput output, MessageFormat format)
        {
            return format == MessageFormat.Text ?
                TextMessageFormatter.TryWriteMessage(message, output) :
                BinaryMessageFormatter.TryWriteMessage(message, output);
        }

        public static char GetFormatIndicator(MessageFormat messageFormat)
        {
            switch (messageFormat)
            {
                case MessageFormat.Text: return TextFormatIndicator;
                case MessageFormat.Binary: return BinaryFormatIndicator;
                default: throw new ArgumentException($"Invalid message format: {messageFormat}", nameof(messageFormat));
            }
        }
    }
}
