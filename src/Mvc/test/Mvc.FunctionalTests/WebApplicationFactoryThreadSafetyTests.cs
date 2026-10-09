// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

// Regression tests for https://github.com/dotnet/aspnetcore/issues/67223:
// concurrent List.Add on the factory's _clients / _derivedFactories tore the
// backing array, surfacing as a NullReferenceException on dispose. Each test
// repeats a few times so the race reproduces reliably against the unfixed code.
public class WebApplicationFactoryThreadSafetyTests
{
    private static int WorkerCount => Math.Max(Environment.ProcessorCount, 4);

    [Fact]
    public async Task WithWebHostBuilder_ConcurrentCalls_DoNotCorruptDerivedFactories()
    {
        const int perWorker = 50;
        var expected = WorkerCount * perWorker;

        for (var iteration = 0; iteration < 10; iteration++)
        {
            // No server needed: WithWebHostBuilder just appends a derived factory.
            await using var root = new WebApplicationFactory<BasicWebSite.Startup>();

            await RunConcurrently(() =>
            {
                for (var i = 0; i < perWorker; i++)
                {
                    root.WithWebHostBuilder(_ => { });
                }
            });

            var factories = root.Factories;

            // A short count means an entry was dropped; a null slot means the array tore.
            Assert.Equal(expected, factories.Count);
            Assert.All(factories, factory => Assert.NotNull(factory));
        }
    }

    [Fact]
    public async Task CreateClient_ConcurrentCalls_DoNotCorruptClientList()
    {
        const int perWorker = 20;

        for (var iteration = 0; iteration < 5; iteration++)
        {
            await using var factory = new WebApplicationFactory<BasicWebSite.Startup>();

            // Start the server once so the concurrent calls race only _clients.Add, not StartServer.
            factory.CreateClient().Dispose();

            await RunConcurrently(() =>
            {
                for (var i = 0; i < perWorker; i++)
                {
                    factory.CreateClient().Dispose();
                }
            });
        }
    }

    // Lines all workers up on a barrier so the Add calls collide as tightly as possible.
    private static async Task RunConcurrently(Action body)
    {
        var workerCount = WorkerCount;
        using var gate = new Barrier(workerCount);
        var workers = new Task[workerCount];

        for (var w = 0; w < workerCount; w++)
        {
            workers[w] = Task.Run(() =>
            {
                gate.SignalAndWait();
                body();
            });
        }

        await Task.WhenAll(workers);
    }
}
