// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Benchmarks
{
    public class StartupUsingRouter
    {
        private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouter(routes =>
            {
                routes.MapRoute("/plaintext", (httpContext) =>
                {
                    var response = httpContext.Response;
                    var payloadLength = _helloWorldPayload.Length;
                    response.StatusCode = 200;
                    response.ContentType = "text/plain";
                    response.ContentLength = payloadLength;
                    return response.Body.WriteAsync(_helloWorldPayload, 0, payloadLength);
                });
            });
        }
    }
}