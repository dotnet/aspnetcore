// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace MvcMinimalSample.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Example 1
            services
                .AddMvcCore()
                .AddAuthorization()
                .AddFormatterMappings(m => m.SetMediaTypeMappingForFormat("js", new MediaTypeHeaderValue("application/json")))
                .AddJsonFormatters(j => j.Formatting = Formatting.Indented);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
        }
    }
}
