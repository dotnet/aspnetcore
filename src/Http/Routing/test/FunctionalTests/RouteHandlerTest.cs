// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class RouteHandlerTest
{
    [Fact]
    public async Task MapPost_FromBodyWorksWithJsonPayload()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b =>
                            b.MapPost("/EchoTodo/{id}",
                                (int id, Todo todo) => todo with { Id = id }));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var todo = new Todo
        {
            Name = "Write tests!"
        };

        var response = await client.PostAsJsonAsync("/EchoTodo/42", todo);
        response.EnsureSuccessStatusCode();

        var echoedTodo = await response.Content.ReadFromJsonAsync<Todo>();

        Assert.NotNull(echoedTodo);
        Assert.Equal(todo.Name, echoedTodo?.Name);
        Assert.Equal(42, echoedTodo?.Id);
    }

    [Fact]
    public async Task CustomEndpointDataSource_IsDisposedIfResolved()
    {
        var testDisposeDataSource = new TestDisposeEndpointDataSource();
        var testGroupDisposeDataSource = new TestDisposeEndpointDataSource();

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b =>
                        {
                            b.DataSources.Add(testDisposeDataSource);

                            var group = b.MapGroup("");
                            ((IEndpointRouteBuilder)group).DataSources.Add(testGroupDisposeDataSource);
                        });
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();

        // Make a request to ensure data sources are resolved.
        var client = server.CreateClient();
        var response = await client.GetAsync("/");
        // We didn't define any endpoints.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.False(testDisposeDataSource.IsDisposed);
        Assert.False(testGroupDisposeDataSource.IsDisposed);

        await host.StopAsync();
        host.Dispose();

        Assert.True(testDisposeDataSource.IsDisposed);
        Assert.True(testGroupDisposeDataSource.IsDisposed);
    }

    private record Todo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Todo";
        public bool IsComplete { get; set; }
    }

    private class TestDisposeEndpointDataSource : EndpointDataSource, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public override IReadOnlyList<Endpoint> Endpoints => Array.Empty<Endpoint>();

        public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
