// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.DependencyInjection;

namespace DispatcherSample.Web
{
    public class Startup
    {
        private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddDispatcher(options =>
            {
                options.DataSources.Add(new DefaultEndpointDataSource(new[]
                {
                    new MatcherEndpoint((next) => (httpContext) =>
                    {
                        var response = httpContext.Response;
                        var payloadLength = _helloWorldPayload.Length;
                        response.StatusCode = 200;
                        response.ContentType = "text/plain";
                        response.ContentLength = payloadLength;
                        return response.Body.WriteAsync(_helloWorldPayload, 0, payloadLength);
                    },
                    "/plaintext", new { }, 0, EndpointMetadataCollection.Empty, "Plaintext"),
                }));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDispatcher();

            // Imagine some more stuff here...

            app.UseEndpoint();
        }
    }
}
