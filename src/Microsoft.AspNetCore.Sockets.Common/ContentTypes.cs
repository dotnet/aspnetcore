using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Sockets
{
    public static class ContentTypes
    {
        public static readonly string TextContentType = "application/vnd.microsoft.aspnetcore.endpoint-messages.v1+text";
        public static readonly string BinaryContentType = "application/vnd.microsoft.aspnetcore.endpoint-messages.v1+binary";

        public static string GetContentType(MessageFormat messageFormat)
        {
            switch (messageFormat)
            {
                case MessageFormat.Text: return TextContentType;
                case MessageFormat.Binary: return BinaryContentType;
                default: throw new ArgumentException($"Invalid message format: {messageFormat}", nameof(messageFormat));
            }
        }
    }
}
