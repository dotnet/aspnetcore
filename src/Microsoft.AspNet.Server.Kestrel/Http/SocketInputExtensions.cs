// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public static class SocketInputExtensions
    {
        public static async Task<int> ReadAsync(this SocketInput input, ArraySegment<byte> buffer)
        {
            while (true)
            {
                await input;

                var begin = input.ConsumingStart();
                int actual;
                var end = begin.CopyTo(buffer.Array, buffer.Offset, buffer.Count, out actual);
                input.ConsumingComplete(end, end);

                if (actual != 0)
                {
                    return actual;
                }
                if (input.RemoteIntakeFin)
                {
                    return 0;
                }
            }
        }
    }
}
