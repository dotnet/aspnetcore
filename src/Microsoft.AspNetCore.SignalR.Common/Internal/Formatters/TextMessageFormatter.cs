// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.SignalR.Internal.Formatters
{
    public static class TextMessageFormatter
    {
        // This record separator is supposed to be used only for JSON payloads where 0x1e character
        // will not occur (is not a valid character) and therefore it is safe to not escape it
        internal static readonly byte RecordSeparator = 0x1e;

        public static void WriteRecordSeparator(Stream output)
        {
            output.WriteByte(RecordSeparator);
        }
    }
}
