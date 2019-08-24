// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public static class TransportSelector
    {
        public static IWebHostBuilder GetWebHostBuilder(Func<MemoryPool<byte>> memoryPoolFactory = null,
                                                        long? maxReadBufferSize = null)
        {
            return new WebHostBuilder().UseSockets(options =>
            {
                options.MemoryPoolFactory = memoryPoolFactory ?? options.MemoryPoolFactory;
                options.MaxReadBufferSize = maxReadBufferSize;
            });
        }
    }
}
