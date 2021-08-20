// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Routing.FunctionalTests
{
    public class MapDelegateTest
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

        private record Todo
        {
            public int Id { get; set; }
            public string Name { get; set; } = "Todo";
            public bool IsComplete { get; set; }
        }
    }
}
