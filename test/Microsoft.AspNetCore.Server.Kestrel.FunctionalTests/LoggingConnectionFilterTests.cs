// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class LoggingConnectionFilterTests
    {
        [Fact(Skip = "SslStream hanging on write after update to CoreFx 4.4 (https://github.com/dotnet/corefx/issues/14698)")]
        public async Task LoggingConnectionFilterCanBeAddedBeforeAndAfterHttpsFilter()
        {
            var host = new WebHostBuilder()
            .UseUrls($"https://127.0.0.1:0")
            .UseKestrel(options =>
            {
                options.UseConnectionLogging();
                options.UseHttps(@"TestResources/testCert.pfx", "testPassword");
            })
            .Configure(app =>
            {
                app.Run(context =>
                {
                    context.Response.ContentLength = 12;
                    return context.Response.WriteAsync("Hello World!");
                });
            })
            .Build();

            using (host)
            {
                host.Start();

                var response = await HttpClientSlim.GetStringAsync($"https://localhost:{host.GetPort()}/", validateCertificate: false)
                                                   .TimeoutAfter(TimeSpan.FromSeconds(10));

                Assert.Equal("Hello World!", response);
            }
        }
    }
}
