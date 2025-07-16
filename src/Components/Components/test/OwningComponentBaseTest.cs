// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class OwningComponentBaseTest
{
    [Fact]
    public void CreatesScopeAndService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        services.AddTransient<MyService>();
        var serviceProvider = services.BuildServiceProvider();

        var counter = serviceProvider.GetRequiredService<Counter>();
        var renderer = new TestRenderer(serviceProvider);
        var component1 = (MyOwningComponent)renderer.InstantiateComponent<MyOwningComponent>();

        Assert.NotNull(component1.MyService);
        Assert.Equal(1, counter.CreatedCount);
        Assert.Equal(0, counter.DisposedCount);

        ((IDisposable)component1).Dispose();
        Assert.Equal(1, counter.CreatedCount);
        Assert.Equal(1, counter.DisposedCount);
    }

    [Fact]
    public async Task DisposeAsyncReleasesScopeAndService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        services.AddTransient<MyService>();
        var serviceProvider = services.BuildServiceProvider();

        var counter = serviceProvider.GetRequiredService<Counter>();
        var renderer = new TestRenderer(serviceProvider);
        var component1 = (MyOwningComponent)renderer.InstantiateComponent<MyOwningComponent>();

        Assert.NotNull(component1.MyService);
        Assert.Equal(1, counter.CreatedCount);
        Assert.Equal(0, counter.DisposedCount);
        Assert.False(component1.IsDisposedPublic);

        await ((IAsyncDisposable)component1).DisposeAsync();
        Assert.Equal(1, counter.CreatedCount);
        Assert.Equal(1, counter.DisposedCount);
        Assert.True(component1.IsDisposedPublic);
    }

    [Fact]
    public void ThrowsWhenAccessingScopedServicesAfterDispose()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        services.AddTransient<MyService>();
        var serviceProvider = services.BuildServiceProvider();

        var renderer = new TestRenderer(serviceProvider);
        var component1 = (MyOwningComponent)renderer.InstantiateComponent<MyOwningComponent>();

        // Access service first to create scope
        var service = component1.MyService;

        ((IDisposable)component1).Dispose();

        // Should throw when trying to access services after disposal
        Assert.Throws<ObjectDisposedException>(() => component1.MyService);
    }

    [Fact]
    public async Task ThrowsWhenAccessingScopedServicesAfterDisposeAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        services.AddTransient<MyService>();
        var serviceProvider = services.BuildServiceProvider();

        var renderer = new TestRenderer(serviceProvider);
        var component1 = (MyOwningComponent)renderer.InstantiateComponent<MyOwningComponent>();

        // Access service first to create scope
        var service = component1.MyService;

        await ((IAsyncDisposable)component1).DisposeAsync();

        // Should throw when trying to access services after disposal
        Assert.Throws<ObjectDisposedException>(() => component1.MyService);
    }

    private class Counter
    {
        public int CreatedCount { get; set; }
        public int DisposedCount { get; set; }
    }

    private class MyService : IDisposable
    {
        public MyService(Counter counter)
        {
            Counter = counter;
            Counter.CreatedCount++;
        }

        public Counter Counter { get; }

        void IDisposable.Dispose() => Counter.DisposedCount++;
    }

    private class MyOwningComponent : OwningComponentBase<MyService>
    {
        public MyService MyService => Service;

        // Expose IsDisposed for testing
        public bool IsDisposedPublic => IsDisposed;
    }
}
