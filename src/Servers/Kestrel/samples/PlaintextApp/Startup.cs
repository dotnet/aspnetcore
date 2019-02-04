// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace PlaintextApp
{
    public class Startup
    {
        private static readonly byte[] _helloWorldBytes = Encoding.UTF8.GetBytes("Hello, World!");

        public void Configure(IApplicationBuilder app)
        {
            app.Run(Plaintext);
        }

        private static Task Plaintext(HttpContext httpContext)
        {
            var payload = _helloWorldBytes;
            var response = httpContext.Response;

            response.StatusCode = 200;
            response.ContentType = "text/plain";
            response.ContentLength = payload.Length;

            var vt = response.BodyPipe.WriteAsync(payload);
            if (vt.IsCompletedSuccessfully)
            {
                // Signal consumption to the IValueTaskSource
                vt.GetAwaiter().GetResult();
                return Task.CompletedTask;
            }
            else
            {
                return AwaitResult(vt);
            }

            async Task AwaitResult(ValueTask<FlushResult> flushResult)
            {
                await flushResult;
            }
        }

        public static Task Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 5001);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            return host.RunAsync();
        }
    }
}
