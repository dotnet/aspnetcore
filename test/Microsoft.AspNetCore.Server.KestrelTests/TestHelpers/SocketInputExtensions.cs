// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{
    public static class SocketInputExtensions
    {
        public static void IncomingData(this SocketInput input, byte[] buffer, int offset, int count)
        {
            var bufferIndex = offset;
            var remaining = count;

            while (remaining > 0)
            {
                var block = input.IncomingStart();

                var bytesLeftInBlock = block.Data.Offset + block.Data.Count - block.End;
                var bytesToCopy = remaining < bytesLeftInBlock ? remaining : bytesLeftInBlock;

                Buffer.BlockCopy(buffer, bufferIndex, block.Array, block.End, bytesToCopy);

                bufferIndex += bytesToCopy;
                remaining -= bytesToCopy;

                input.IncomingComplete(bytesToCopy, null);
            }
        }

        public static void IncomingFin(this SocketInput input)
        {
            input.IncomingComplete(0, null);
        }
    }
}
