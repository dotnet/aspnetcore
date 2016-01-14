// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders.Testing;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace SimpleWebSite
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
            services.AddSingleton<UrlEncoder, UrlTestEncoder>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseStartup<Startup>()
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Build();

            host.Run();
        }
    }
}
