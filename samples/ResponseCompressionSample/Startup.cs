// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;

namespace ResponseCompressionSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseResponseCompression(new ResponseCompressionOptions()
            {
                ShouldCompressResponse = ResponseCompressionUtils.CreateShouldCompressResponseDelegate(new string[] { "text/plain" })
            });

            app.Run(async context =>
            {
                context.Response.Headers["Content-Type"] = "text/plain";
                await context.Response.WriteAsync(LoremIpsum.Text);
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
