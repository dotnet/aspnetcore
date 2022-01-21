// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

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
