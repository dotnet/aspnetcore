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

        public static readonly string TextContentType = "application/vnd.microsoft.aspnetcore.endpoint-messages.v1+text";
        public static readonly string BinaryContentType = "application/vnd.microsoft.aspnetcore.endpoint-messages.v1+binary";

        public static bool TryWriteMessage(Message message, IOutput output, MessageFormat format)
        {
            if (!message.EndOfMessage)
            {
                // This is a truely exceptional condition since we EXPECT callers to have already
                // buffered incomplete messages and synthesized the correct, complete message before
                // giving it to us. Hence we throw, instead of returning false.
                throw new ArgumentException("Cannot format message where endOfMessage is false using this format", nameof(message));
            }

            return format == MessageFormat.Text ?
                TextMessageFormatter.TryWriteMessage(message, output) :
                BinaryMessageFormatter.TryWriteMessage(message, output);
        }

        public static string GetContentType(MessageFormat messageFormat)
        {
            switch (messageFormat)
            {
                case MessageFormat.Text: return TextContentType;
                case MessageFormat.Binary: return BinaryContentType;
                default: throw new ArgumentException($"Invalid message format: {messageFormat}", nameof(messageFormat));
            }
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
