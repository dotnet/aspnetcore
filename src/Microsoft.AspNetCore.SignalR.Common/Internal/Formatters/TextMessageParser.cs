// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Internal.Formatters
{
    public static class TextMessageParser
    {
        public static bool TryParseMessage(ref ReadOnlyMemory<byte> buffer, out ReadOnlyMemory<byte> payload)
        {
            var index = buffer.Span.IndexOf(TextMessageFormatter.RecordSeparator);
            if (index == -1)
            {
                payload = default;
                return false;
            }

            payload = buffer.Slice(0, index);

            // Skip record separator
            buffer = buffer.Slice(index + 1);

            return true;
        }
    }
}
