// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Wasm.Performance.Driver
{
    public class BenchmarkDriverStartup
    {

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(c => c.AddDefaultPolicy(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCors();

            app.Run(async context =>
            {
                var result = await JsonSerializer.DeserializeAsync<List<BenchmarkResult>>(context.Request.Body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
                await context.Response.WriteAsync("OK");
                Program.SetBenchmarkResult(result);
            });
        }
    }
}
