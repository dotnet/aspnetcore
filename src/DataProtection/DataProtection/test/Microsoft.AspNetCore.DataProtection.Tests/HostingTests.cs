// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.Test;

public class HostingTests
{
    [Fact]
    public async Task WebhostLoadsKeyRingBeforeServerStarts()
    {
        var tcs = new TaskCompletionSource();
        var mockKeyRing = new Mock<IKeyRingProvider>();
        mockKeyRing.Setup(m => m.GetCurrentKeyRing())
            .Returns(Mock.Of<IKeyRing>())
            .Callback(() => tcs.TrySetResult());

        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>()
            .ConfigureServices(s =>
                s.AddDataProtection()
                .Services
                .Replace(ServiceDescriptor.Singleton(mockKeyRing.Object))
                .AddSingleton<IServer>(
                    new FakeServer(onStart: () => tcs.TrySetException(new InvalidOperationException("Server was started before key ring was initialized")))));

        using (var host = builder.Build())
        {
            await host.StartAsync();
        }

        await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
        mockKeyRing.VerifyAll();
    }

    [Fact]
    public async Task GenericHostLoadsKeyRingBeforeServerStarts()
    {
        var tcs = new TaskCompletionSource();
        var mockKeyRing = new Mock<IKeyRingProvider>();
        mockKeyRing.Setup(m => m.GetCurrentKeyRing())
            .Returns(Mock.Of<IKeyRing>())
            .Callback(() => tcs.TrySetResult());

        var builder = new HostBuilder()
            .ConfigureServices(s =>
                s.AddDataProtection()
                .Services
                .Replace(ServiceDescriptor.Singleton(mockKeyRing.Object))
                .AddSingleton<IServer>(
                    new FakeServer(onStart: () => tcs.TrySetException(new InvalidOperationException("Server was started before key ring was initialized")))))
            .ConfigureWebHost(b => b.UseStartup<TestStartup>());

        using (var host = builder.Build())
        {
            await host.StartAsync();
        }

        await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
        mockKeyRing.VerifyAll();
    }

    [Fact]
    public async Task StartupContinuesOnFailureToLoadKey()
    {
        var mockKeyRing = new Mock<IKeyRingProvider>();
        mockKeyRing.Setup(m => m.GetCurrentKeyRing())
            .Throws(new NotSupportedException("This mock doesn't actually work, but shouldn't kill the server"))
            .Verifiable();

        var mockServer = new Mock<IServer>();
        mockServer.Setup(m => m.Features).Returns(new FeatureCollection());

        var builder = new HostBuilder()
            .ConfigureServices(s =>
                s.AddDataProtection()
                .Services
                .Replace(ServiceDescriptor.Singleton(mockKeyRing.Object))
                .AddSingleton(mockServer.Object))
                .ConfigureWebHost(b => b.UseStartup<TestStartup>());

        using (var host = builder.Build())
        {
            await host.StartAsync();
        }

        mockKeyRing.VerifyAll();
    }

    private class TestStartup
    {
        public void Configure(IApplicationBuilder app)
        {
        }
    }

    public class FakeServer : IServer
    {
        private readonly Action _onStart;

        public FakeServer(Action onStart)
        {
            _onStart = onStart;
        }

        public IFeatureCollection Features => new FeatureCollection();

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            _onStart();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
