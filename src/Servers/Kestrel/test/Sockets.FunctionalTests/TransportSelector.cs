// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public static class TransportSelector
    {
        public static IHostBuilder GetHostBuilder(Func<MemoryPool<byte>> memoryPoolFactory = null,
                                                        long? maxReadBufferSize = null)
        {
            return new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseSockets(options =>
                        {
                            options.MemoryPoolFactory = memoryPoolFactory ?? options.MemoryPoolFactory;
                            options.MaxReadBufferSize = maxReadBufferSize;
                        });
                });
        }
    }
}
