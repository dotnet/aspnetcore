// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class EquivalenceTests
{
    [Fact]
    public async Task WrappedNullValues_RespondTheSame()
    {
        using var host = new HostBuilder()
        .ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(e =>
                {
                    e.MapGet("/nullable", Data? () => Data.NullInstance);
                    e.MapGet("/results", IResult () => Results.Ok(Data.NullInstance));
                    e.MapGet("/typed_results", Ok<Data?> () => TypedResults.Ok<Data?>(Data.NullInstance));
                });
            }).UseTestServer();
        })
        .ConfigureServices(services =>
        {
            services.AddRouting();
        })
        .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();

        var client = server.CreateClient();
        var baselineResponse = await client.GetAsync("/nullable");
        var baselineBody = await baselineResponse.Content.ReadAsStringAsync();

        Assert.Equal("null", baselineBody);

        foreach (var e in new[] { "/results", "/typed_results" })
        {
            var r = await client.GetAsync(e);
            Assert.Equal(baselineResponse.StatusCode, r.StatusCode);
            Assert.Equal(baselineResponse.Content.Headers.ContentType, r.Content.Headers.ContentType);
            var body = await r.Content.ReadAsStringAsync();
            Assert.Equal(baselineBody, body);
        }
    }
}

public record Data
{
    public static Data? NullInstance => null;
}

