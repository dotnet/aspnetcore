// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class HandleInheritanceTests : TestApplicationErrorLoggerLoggedTest
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Fixed in 3.0 https://github.com/aspnet/KestrelHttpServer/issues/3040")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Fixed in 3.0 https://github.com/aspnet/KestrelHttpServer/issues/3040")]
        public async Task SpawnChildProcess_DoesNotInheritListenHandle()
        {
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .ConfigureServices(AddTestLogging)
                .UseUrls("http://127.0.0.1:0")
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello World");
                    });
                });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();

                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    CreateNoWindow = true,
                };
                using (var process = Process.Start(processInfo))
                {
                    var port = host.GetPort();
                    await host.StopAsync();

                    // We should not be able to connect if the handle was correctly closed and not inherited by the child process.
                    using (var client = new TcpClient())
                    {
                        await Assert.ThrowsAnyAsync<SocketException>(() => client.ConnectAsync("127.0.0.1", port));
                    }

                    process.Kill();
                }
            }
        }
    }
}
