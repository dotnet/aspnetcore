// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace ServerComparison.TestSites
{
    public class StartupResponses
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Map("/contentlength", subApp =>
            {
                subApp.Run(context =>
                {
                    context.Response.ContentLength = 14;
                    return context.Response.WriteAsync("Content Length");
                });
            });

            app.Map("/connectionclose", subApp =>
            {
                subApp.Run(async context =>
                {
                    context.Response.Headers[HeaderNames.Connection] = "close";
                    await context.Response.WriteAsync("Connnection Close");
                    await context.Response.Body.FlushAsync(); // Bypass IIS write-behind buffering
                });
            });

            app.Map("/chunked", subApp =>
            {
                subApp.Run(async context =>
                {
                    await context.Response.WriteAsync("Chunked");
                    await context.Response.Body.FlushAsync(); // Bypass IIS write-behind buffering
                });
            });

            app.Map("/manuallychunked", subApp =>
            {
                subApp.Run(context =>
                {
                    context.Response.Headers[HeaderNames.TransferEncoding] = "chunked";
                    return context.Response.WriteAsync("10\r\nManually Chunked\r\n0\r\n\r\n");
                });
            });

            app.Map("/manuallychunkedandclose", subApp =>
            {
                subApp.Run(context =>
                {
                    context.Response.Headers[HeaderNames.Connection] = "close";
                    context.Response.Headers[HeaderNames.TransferEncoding] = "chunked";
                    return context.Response.WriteAsync("1A\r\nManually Chunked and Close\r\n0\r\n\r\n");
                });
            });

            app.Run(context =>
            {
                return context.Response.WriteAsync("Running");
            });
        }
    }
}
