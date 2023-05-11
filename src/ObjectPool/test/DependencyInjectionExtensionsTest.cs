// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ObjectPool.TestResources;
using Xunit;

namespace Microsoft.Extensions.ObjectPool;

public class DependencyInjectionExtensionsTest
{
    [Fact]
    public void ConfiguresPoolOptions()
    {
        var services = new ServiceCollection()
            .Configure<PoolOptions>(typeof(TestClass).FullName, options => options.Capacity = 2048)
            .Configure<PoolOptions>(typeof(TestDependency).FullName, options => options.Capacity = 4096)
            ;
        using var provider = services.BuildServiceProvider();

        var sut = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.Equal(2048, sut.Get(typeof(TestClass).FullName!).Capacity);
        Assert.Equal(4096, sut.Get(typeof(TestDependency).FullName!).Capacity);
    }

    [Fact]
    public void AddPool_ServiceTypeOnly_AddsPool()
    {
        var services = new ServiceCollection().AddPool<TestDependency>();

        var sut = services.BuildServiceProvider().GetService<ObjectPool<TestDependency>>();
        using var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.NotNull(sut);
        Assert.Equal(1024, optionsMonitor.Get(typeof(TestDependency).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ServiceTypeOnlyWithCapacity_AddsPoolAndSetsCapacity()
    {
        var services = new ServiceCollection().AddPool<TestDependency>(options => options.Capacity = 64);

        var sut = services.BuildServiceProvider().GetService<ObjectPool<TestDependency>>();
        using var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.NotNull(sut);
        Assert.Equal(64, optionsMonitor.Get(typeof(TestDependency).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ServiceAndImplementationType_AddsPool()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPool<ITestClass, TestClass>();

        using var provider = services.BuildServiceProvider();
        var sut = provider.GetService<ObjectPool<ITestClass>>();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.NotNull(sut);
        Assert.Equal(TestDependency.Message, sut!.Get().ReadMessage());
        Assert.Equal(1024, optionsMonitor.Get(typeof(ITestClass).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ServiceAndImplementationTypeWithCapacity_AddsPoolAndSetsCapacity()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPool<ITestClass, TestClass>(options => options.Capacity = 64);

        using var provider = services.BuildServiceProvider();
        var sut = provider.GetService<ObjectPool<ITestClass>>();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.NotNull(sut);
        Assert.Equal(TestDependency.Message, sut!.Get().ReadMessage());
        Assert.Equal(64, optionsMonitor.Get(typeof(ITestClass).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ReturnedPooled_CallsTryReset()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPool<ITestClass, TestClass>();

        using var provider = services.BuildServiceProvider();
        var sut = provider.GetRequiredService<ObjectPool<ITestClass>>();

        var pooled = sut.Get();
        sut.Return(pooled);

        Assert.Equal(1, pooled.ResetCalled);
    }

    [Fact]
    public void ResolvePooledInstanceDirectly()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPool<ITestClass, TestClass>()
            .AddScoped(provider => provider.GetRequiredService<ObjectPool<ITestClass>>().Get())
            ;

        using var provider = services.BuildServiceProvider();

        ITestClass resolved;

        using (var scope = provider.CreateScope())
        {
            resolved = scope.ServiceProvider.GetRequiredService<ITestClass>();
        }

        Assert.NotNull(resolved);
        Assert.Equal(1, resolved.DisposedCalled);
    }

    [Fact]
    public void PooledHelperReturnsScopedInstances_SameScope()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPool<TestClass>()
            .AddScoped<Pooled<TestClass>>()
            .AddScoped<ITestClass>(provider => provider.GetRequiredService<Pooled<TestClass>>().Object)
            ;

        using var provider = services.BuildServiceProvider();

        ITestClass resolved1, resolved2;

        // Because these are scoped, resolved1 and resolved2 are the same instance
        // and a single reference is disposed.

        using (var scope = provider.CreateScope())
        {
            resolved1 = scope.ServiceProvider.GetRequiredService<ITestClass>();
            resolved2 = scope.ServiceProvider.GetRequiredService<ITestClass>();
        }

        Assert.NotNull(resolved1);
        Assert.NotNull(resolved2);
        Assert.Same(resolved1, resolved2);

        Assert.Equal(1, resolved1.DisposedCalled);
        Assert.Equal(1, resolved1.ResetCalled);
    }

    [Fact]
    public void PooledHelperReturnsScopedInstances_DifferentScopes()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPool<TestClass>()
            .AddScoped<Pooled<TestClass>>()
            .AddScoped<ITestClass>(provider => provider.GetRequiredService<Pooled<TestClass>>().Object)
            ;

        using var provider = services.BuildServiceProvider();

        ITestClass resolved1, resolved2;

        using (var scope = provider.CreateScope())
        {
            resolved1 = scope.ServiceProvider.GetRequiredService<ITestClass>();
        }

        // The object should be returned and recalled in the next scope

        using (var scope = provider.CreateScope())
        {
            resolved2 = scope.ServiceProvider.GetRequiredService<ITestClass>();
        }

        Assert.NotNull(resolved1);
        Assert.NotNull(resolved2);
        Assert.Same(resolved1, resolved2);

        Assert.Equal(2, resolved1.DisposedCalled);
        Assert.Equal(2, resolved1.ResetCalled);
    }
}
