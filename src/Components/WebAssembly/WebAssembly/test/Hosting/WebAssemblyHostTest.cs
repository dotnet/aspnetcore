// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
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
            : base(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture)
        {
        }

        public override ValueTask LoadCurrentCultureResourcesAsync() => default;
    }
}
