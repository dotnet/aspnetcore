// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public static class SocketInputExtensions
    {
        public static ValueTask<int> ReadAsync(this SocketInput input, byte[] buffer, int offset, int count)
        {
            while (input.IsCompleted)
            {
                var fin = input.RemoteIntakeFin;

                var begin = input.ConsumingStart();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.ConsumingComplete(end, end);

                if (actual != 0)
                {
                    return new ValueTask<int>(actual);
                }
                else if (fin)
                {
                    return new ValueTask<int>(0);
                }
            }

            return new ValueTask<int>(input.ReadAsyncAwaited(buffer, offset, count));
        }

        private static async Task<int> ReadAsyncAwaited(this SocketInput input, byte[] buffer, int offset, int count)
        {
            while (true)
            {
                await input;

                var fin = input.RemoteIntakeFin;

                var begin = input.ConsumingStart();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.ConsumingComplete(end, end);

                if (actual != 0)
                {
                    return actual;
                }
                else if (fin)
                {
                    return 0;
                }
            }
        }
    }
}
