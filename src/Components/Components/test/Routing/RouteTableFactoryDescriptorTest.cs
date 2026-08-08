// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

#nullable enable annotations

namespace Microsoft.AspNetCore.Components.Routing;

public class RouteTableFactoryDescriptorTest
{
    [Fact]
    public void Create_UsesDescribedRoutesWhenTheApplicationRegisteredMetadata()
    {
        var services = BuildServices(new StubResolver(
            Describe(typeof(DescribedPage), new RouteAttribute("/described"))));
        var routeKey = new RouteKey(typeof(DescribedPage).Assembly, null);

        var table = new RouteTableFactory().Create(routeKey, services);

        Assert.Equal(typeof(DescribedPage), Match(table, "/described"));
    }

    [Fact]
    public void Create_HonorsExcludeFromInteractiveRoutingInDescribedMetadata()
    {
        var services = BuildServices(new StubResolver(
            Describe(typeof(DescribedPage), new RouteAttribute("/described")),
            Describe(typeof(ExcludedPage), new RouteAttribute("/excluded"), new ExcludeFromInteractiveRoutingAttribute())));
        var routeKey = new RouteKey(typeof(DescribedPage).Assembly, null);

        var table = new RouteTableFactory().Create(routeKey, services);

        Assert.Equal(typeof(DescribedPage), Match(table, "/described"));
        Assert.Null(Match(table, "/excluded"));
    }

    [Fact]
    public void Create_IgnoresDescriptorsFromAssembliesTheRouteKeyDoesNotCover()
    {
        var services = BuildServices(new StubResolver(
            Describe(typeof(DescribedPage), new RouteAttribute("/described"))));
        var routeKey = new RouteKey(typeof(string).Assembly, null);

        var table = new RouteTableFactory().Create(routeKey, services);

        Assert.Null(Match(table, "/described"));
    }

    [Fact]
    public void Create_FallsBackToScanningWhenNoMetadataIsRegistered()
    {
        var collection = new ServiceCollection();
        collection.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        collection.AddOptions();
        var services = collection.BuildServiceProvider();
        var routeKey = new RouteKey(typeof(RouteTableFactoryDescriptorTest).Assembly, null);

        var table = new RouteTableFactory().Create(routeKey, services);

        Assert.NotNull(table);
    }

    [Fact]
    public void Create_IsolatesCachedRoutesByProviderResolver()
    {
        var first = BuildServices(new StubResolver(
            Describe(typeof(DescribedPage), new RouteAttribute("/first"))));
        var second = BuildServices(new StubResolver(
            Describe(typeof(DescribedPage), new RouteAttribute("/second"))));
        var routeKey = new RouteKey(typeof(DescribedPage).Assembly, null);
        var factory = new RouteTableFactory();

        var firstTable = factory.Create(routeKey, first);
        var secondTable = factory.Create(routeKey, second);

        Assert.Equal(typeof(DescribedPage), Match(firstTable, "/first"));
        Assert.Null(Match(firstTable, "/second"));
        Assert.Equal(typeof(DescribedPage), Match(secondTable, "/second"));
        Assert.Null(Match(secondTable, "/first"));
    }

    private static IServiceProvider BuildServices(IComponentTypeInfoResolver resolver)
    {
        var services = new ServiceCollection();
        services.AddSingleton(resolver);
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddOptions();
        return services.BuildServiceProvider();
    }

    private static ComponentDescriptor Describe(Type type, params object[] metadata)
        => new() { Type = type, Metadata = metadata };

    private static Type Match(RouteTable table, string path)
    {
        var context = new RouteContext(path);
        table.Route(context);
        return context.Handler;
    }

    private sealed class StubResolver : IComponentTypeInfoResolver
    {
        private readonly Dictionary<Type, ComponentTypeInfo> _typeInfos;

        public StubResolver(params ComponentDescriptor[] descriptors)
        {
            _typeInfos = descriptors.ToDictionary(
                static descriptor => descriptor.Type,
                static descriptor => new ComponentTypeInfo(descriptor));
        }

        public ComponentTypeInfo? GetTypeInfo(Type componentType)
            => _typeInfos.GetValueOrDefault(componentType);

        public ComponentTypeInfo? GetTypeInfo(string assemblyName, string typeName)
            => _typeInfos.Values.FirstOrDefault(typeInfo =>
                typeInfo.Type.Assembly.GetName().Name == assemblyName &&
                typeInfo.Type.FullName == typeName);

        public IReadOnlyList<ComponentTypeInfo> GetTypeInfos(Assembly assembly)
            => [.. _typeInfos.Values.Where(typeInfo => typeInfo.Type.Assembly == assembly)];
    }

    private sealed class DescribedPage : IComponent
    {
        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private sealed class ExcludedPage : IComponent
    {
        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }
}
