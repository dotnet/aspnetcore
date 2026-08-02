// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

int classTestResult = RunClassTest();
if (classTestResult != 100)
{
    return classTestResult;
}

return RunStructTest();

static int RunClassTest()
{
    var builder = Host.CreateDefaultBuilder();
    builder.UseServiceProviderFactory(new MyContainerClassFactory());

    builder.ConfigureWebHost(webBuilder =>
    {
        webBuilder.UseStartup(typeof(MyStartupWithClass));
    });

    builder.Build();

    if (!MyStartupWithClass.ConfigureServicesCalled)
    {
        return -1;
    }
    if (!MyStartupWithClass.ConfigureContainerCalled)
    {
        return -2;
    }

    return 100;
}

static int RunStructTest()
{
    var builder = Host.CreateDefaultBuilder();
    builder.UseServiceProviderFactory(new MyContainerStructFactory());

    builder.ConfigureWebHost(webBuilder =>
    {
        webBuilder.UseStartup(typeof(MyStartupWithStruct));
    });

    try
    {
        builder.Build();
        return -3;
    }
    catch (InvalidOperationException e)
    {
        if (!e.Message.StartsWith("A ValueType TContainerBuilder isn't supported with AOT", StringComparison.Ordinal))
        {
            return -4;
        }
    }

    if (!MyStartupWithStruct.ConfigureServicesCalled)
    {
        return -5;
    }

    // ConfigureContainer should not have been called, since the exception should have been raised
    if (MyStartupWithStruct.ConfigureContainerCalled)
    {
        return -6;
    }

    return 100;
}

public class MyStartupWithClass
{
    public static bool ConfigureServicesCalled;
    public static bool ConfigureContainerCalled;

    public void ConfigureServices(IServiceCollection _) => ConfigureServicesCalled = true;
    public void ConfigureContainer(MyContainerClass _) => ConfigureContainerCalled = true;
    public void Configure(IApplicationBuilder _) { }
}

public class MyContainerClassFactory : IServiceProviderFactory<MyContainerClass>
{
    public MyContainerClass CreateBuilder(IServiceCollection services) => new MyContainerClass(services);

    public IServiceProvider CreateServiceProvider(MyContainerClass containerBuilder)
    {
        containerBuilder.Build();
        return containerBuilder;
    }
}

public class MyContainerClass : IServiceProvider
{
    private IServiceProvider _inner;
    private IServiceCollection _services;

    public MyContainerClass(IServiceCollection services) => _services = services;
    public void Build() => _inner = _services.BuildServiceProvider();
    public object GetService(Type serviceType) => _inner.GetService(serviceType);
}

public class MyStartupWithStruct
{
    public static bool ConfigureServicesCalled;
    public static bool ConfigureContainerCalled;

    public void ConfigureServices(IServiceCollection _) => ConfigureServicesCalled = true;
    public void ConfigureContainer(MyContainerStruct _) => ConfigureContainerCalled = true;
    public void Configure(IApplicationBuilder _) { }
}

public class MyContainerStructFactory : IServiceProviderFactory<MyContainerStruct>
{
    public MyContainerStruct CreateBuilder(IServiceCollection services) => new MyContainerStruct(services);

    public IServiceProvider CreateServiceProvider(MyContainerStruct containerBuilder)
    {
        containerBuilder.Build();
        return containerBuilder;
    }
}

public struct MyContainerStruct : IServiceProvider
{
    private IServiceProvider _inner;
    private IServiceCollection _services;

    public MyContainerStruct(IServiceCollection services) => _services = services;
    public void Build() => _inner = _services.BuildServiceProvider();
    public object GetService(Type serviceType) => _inner.GetService(serviceType);
}
