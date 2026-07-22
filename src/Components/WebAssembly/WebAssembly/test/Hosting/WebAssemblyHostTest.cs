// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public class WebAssemblyHostTest
{
    // This won't happen in the product code, but we need to be able to safely call RunAsync
    // to be able to test a few of the other details.
    [Fact]
    public async Task RunAsync_CanExitBasedOnCancellationToken()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Act
        var task = host.RunAsyncCore(cts.Token, cultureProvider);

        cts.Cancel();
        await task.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert (does not throw)
    }

    [Fact]
    public async Task RunAsync_CallingTwiceCausesException()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();
        var task = host.RunAsyncCore(cts.Token, cultureProvider);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => host.RunAsyncCore(cts.Token));

        cts.Cancel();
        await task.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert
        Assert.Equal("The host has already started.", ex.Message);
    }

    [Fact]
    public async Task DisposeAsync_CanDisposeAfterCallingRunAsync()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        builder.Services.AddSingleton<DisposableService>();
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var disposable = host.Services.GetRequiredService<DisposableService>();

        var cts = new CancellationTokenSource();

        // Act
        await using (host)
        {
            var task = host.RunAsyncCore(cts.Token, cultureProvider);

            cts.Cancel();
            await task.TimeoutAfter(TimeSpan.FromSeconds(3));
        }

        // Assert
        Assert.Equal(1, disposable.DisposeCount);
    }

    [Fact]
    public async Task RunAsync_StartsHostedServices()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        var testHostedService = new TestHostedService();
        builder.Services.AddSingleton<IHostedService>(testHostedService);
        
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Act
        var task = host.RunAsyncCore(cts.Token, cultureProvider);
        
        // Give hosted services time to start
        await Task.Delay(100);
        cts.Cancel();
        await task.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert
        Assert.True(testHostedService.StartCalled);
        Assert.Equal(cts.Token, testHostedService.StartToken);
    }

    [Fact]
    public async Task DisposeAsync_StopsHostedServices()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        var testHostedService1 = new TestHostedService();
        var testHostedService2 = new TestHostedService();
        builder.Services.AddSingleton<IHostedService>(testHostedService1);
        builder.Services.AddSingleton<IHostedService>(testHostedService2);
        
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Start the host to initialize hosted services
        var runTask = host.RunAsyncCore(cts.Token, cultureProvider);
        await Task.Delay(100);

        // Act - dispose the host
        await host.DisposeAsync();
        cts.Cancel();
        await runTask.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert
        Assert.True(testHostedService1.StartCalled);
        Assert.True(testHostedService1.StopCalled);
        Assert.True(testHostedService2.StartCalled);
        Assert.True(testHostedService2.StopCalled);
    }

    [Fact]
    public async Task DisposeAsync_HandlesHostedServiceStopErrors()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        var goodService = new TestHostedService();
        var faultyService = new FaultyHostedService();
        builder.Services.AddSingleton<IHostedService>(goodService);
        builder.Services.AddSingleton<IHostedService>(faultyService);
        
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Start the host to initialize hosted services
        var runTask = host.RunAsyncCore(cts.Token, cultureProvider);
        await Task.Delay(100);

        // Act & Assert - dispose should not throw even if hosted service fails
        await host.DisposeAsync();
        cts.Cancel();
        await runTask.TimeoutAfter(TimeSpan.FromSeconds(3));

        Assert.True(goodService.StartCalled);
        Assert.True(goodService.StopCalled);
        Assert.True(faultyService.StartCalled);
        Assert.True(faultyService.StopCalled);
    }

    [Fact]
    public async Task RunAsync_SupportsAddHostedServiceExtension()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        // Test manual hosted service registration (equivalent to AddHostedService)
        builder.Services.AddSingleton<TestHostedService>();
        builder.Services.AddSingleton<IHostedService>(serviceProvider => serviceProvider.GetRequiredService<TestHostedService>());
        
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Act
        var task = host.RunAsyncCore(cts.Token, cultureProvider);
        
        // Give hosted services time to start
        await Task.Delay(100);
        cts.Cancel();
        await task.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert - verify the hosted service was started via service collection
        var hostedServices = host.Services.GetServices<IHostedService>();
        Assert.Single(hostedServices);
        
        var testService = hostedServices.First();
        Assert.IsType<TestHostedService>(testService);
        Assert.True(((TestHostedService)testService).StartCalled);
    }

    private class TestHostedService : IHostedService
    {
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public CancellationToken StartToken { get; private set; }
        public CancellationToken StopToken { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCalled = true;
            StartToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalled = true;
            StopToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private class FaultyHostedService : IHostedService
    {
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCalled = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalled = true;
            throw new InvalidOperationException("Simulated hosted service stop error");
        }
    }

    private class DisposableService : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return new ValueTask(Task.CompletedTask);
        }
    }

    private class TestSatelliteResourcesLoader : WebAssemblyCultureProvider
    {
        internal TestSatelliteResourcesLoader()
            : base(CultureInfo.CurrentCulture)
        {
        }

        public override ValueTask LoadCurrentCultureResourcesAsync() => default;
    }
}
