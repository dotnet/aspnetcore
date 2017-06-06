// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Sockets.Internal.Formatters
{
    public static class BinaryMessageFormatter
    {
        public static bool TryWriteMessage(ReadOnlySpan<byte> payload, IOutput output)
        {
            // Try to write the data
            if (!output.TryWriteBigEndian((long)payload.Length))
            {
                return false;
            }

            if (!output.TryWrite(payload))
            {
                return false;
            }

            return true;
        }
    }
}