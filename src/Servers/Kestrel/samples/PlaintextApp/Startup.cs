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

namespace PlaintextApp
{
    public class Startup
    {
        private static readonly byte[] _helloWorldBytes = Encoding.UTF8.GetBytes("Hello, World!");

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async (httpContext) =>
            {
                var response = httpContext.Response;
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                response.ContentLength = _helloWorldBytes.Length;

                var pipe = response.BodyPipe;
                Write(pipe, _helloWorldBytes);
                await pipe.FlushAsync();
            });
        }

        private static void Write(PipeWriter pipe, byte[] payload)
        {
            var span = pipe.GetSpan(sizeHint: payload.Length);
            payload.CopyTo(span);
            pipe.Advance(payload.Length);
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
