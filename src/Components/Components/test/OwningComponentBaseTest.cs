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

    [Fact]
    public async Task DisposeAsync_CallsDispose_WithDisposingTrue()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        services.AddTransient<MyService>();
        var serviceProvider = services.BuildServiceProvider();

        var renderer = new TestRenderer(serviceProvider);
        var component = (ComponentWithDispose)renderer.InstantiateComponent<ComponentWithDispose>();

        _ = component.MyService;
        await ((IAsyncDisposable)component).DisposeAsync();
        Assert.True(component.DisposingParameter);
    }

    [Fact]
    public async Task DisposeAsync_ThenDispose_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        services.AddTransient<MyService>();
        var serviceProvider = services.BuildServiceProvider();

        var counter = serviceProvider.GetRequiredService<Counter>();
        var renderer = new TestRenderer(serviceProvider);
        var component = (ComponentWithDispose)renderer.InstantiateComponent<ComponentWithDispose>();

        _ = component.MyService;
        
        // First disposal via DisposeAsync
        await ((IAsyncDisposable)component).DisposeAsync();
        var firstCallCount = component.DisposeCallCount;
        Assert.Equal(1, counter.DisposedCount);
        
        // Second disposal via Dispose - user override is called but base class prevents double-disposal
        ((IDisposable)component).Dispose();
        // User override is called again, but base.Dispose() returns early due to IsDisposed check
        Assert.True(component.DisposeCallCount >= firstCallCount); // Override may be called, but...
        Assert.Equal(1, counter.DisposedCount); // ...service should only be disposed once
    }

    [Fact]
    public async Task DisposeAsyncCore_Override_WithException_StillCallsDispose()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Counter>();
        services.AddTransient<MyService>();
        var serviceProvider = services.BuildServiceProvider();

        var renderer = new TestRenderer(serviceProvider);
        var component = (ComponentWithThrowingDisposeAsyncCore)renderer.InstantiateComponent<ComponentWithThrowingDisposeAsyncCore>();

        _ = component.MyService;
        
        // Even if DisposeAsyncCore throws, Dispose(true) should still be called
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await ((IAsyncDisposable)component).DisposeAsync());
        
        // Dispose should have been called due to try-finally
        Assert.True(component.DisposingParameter);
        Assert.True(component.IsDisposedPublic);
    }

    private class ComponentWithDispose : OwningComponentBase<MyService>
    {
        public MyService MyService => Service;
        public bool? DisposingParameter { get; private set; }
        public int DisposeCallCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            DisposingParameter = disposing;
            DisposeCallCount++;
            base.Dispose(disposing);
        }
    }

    private class ComponentWithThrowingDisposeAsyncCore : OwningComponentBase<MyService>
    {
        public MyService MyService => Service;
        public bool? DisposingParameter { get; private set; }
        public bool IsDisposedPublic => IsDisposed;

        protected override async ValueTask DisposeAsyncCore()
        {
            await base.DisposeAsyncCore();
            throw new InvalidOperationException("Something went wrong in async disposal");
        }

        protected override void Dispose(bool disposing)
        {
            DisposingParameter = disposing;
            base.Dispose(disposing);
        }
    }

    private class MyOwningComponent : OwningComponentBase<MyService>
    {
        public MyService MyService => Service;

        // Expose IsDisposed for testing
        public bool IsDisposedPublic => IsDisposed;
    }
}
