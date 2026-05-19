// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Wasm.Performance.Driver;

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
            var result = await JsonSerializer.DeserializeAsync<BenchmarkResult>(context.Request.Body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            await context.Response.WriteAsync("OK");
            Program.BenchmarkResultTask.TrySetResult(result);
        });
    }
}
