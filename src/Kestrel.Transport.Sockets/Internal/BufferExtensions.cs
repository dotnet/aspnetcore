// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    public static class BufferExtensions
    {
        public static ArraySegment<byte> GetArray(this Buffer<byte> buffer)
        {
            ArraySegment<byte> result;
            if (!buffer.TryGetArray(out result))
            {
                throw new InvalidOperationException("Buffer backed by array was expected");
            }
            return result;
        }
    }
}