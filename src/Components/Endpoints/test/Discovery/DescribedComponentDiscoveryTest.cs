// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Discovery;

#nullable enable

public class DescribedComponentDiscoveryTest
{
    private static readonly System.Reflection.Assembly _assembly = typeof(DescribedComponentDiscoveryTest).Assembly;

    [Fact]
    public void AddAssembly_UsesNormalizedTypeInfo()
    {
        var marker = new DescribedMarkerAttribute();
        var descriptor = new ComponentDescriptor
        {
            Type = typeof(DescribedRoutablePage),
            Metadata = [new RouteAttribute("/described-page"), marker],
        };
        var builder = new ComponentApplicationBuilder(new TestResolver([descriptor]));

        builder.AddAssembly(_assembly);
        var application = builder.Build();

        var page = Assert.Single(application.Pages);
        Assert.Equal("/described-page", page.Route);
        Assert.DoesNotContain(page.Metadata, static item => item is RouteAttribute);
        Assert.Same(marker, Assert.Single(page.Metadata));
        Assert.Equal(descriptor.Type, Assert.Single(application.Components).Type);
    }

    [Fact]
    public void AddAssembly_TypeInfoWinsAndReflectionFillsMissingTypes()
    {
        var generated = new ComponentDescriptor
        {
            Type = typeof(DescribedRoutablePage),
            Metadata = [new RouteAttribute("/generated-page")],
        };
        using var reflectionResolver = new ReflectionComponentTypeInfoResolver();
        var resolver = new TestResolver([generated], reflectionResolver);
        var builder = new ComponentApplicationBuilder(resolver);

        builder.AddAssembly(_assembly);
        var application = builder.Build();

        Assert.Contains(application.Pages, static page =>
            page.Type == typeof(DescribedRoutablePage) && page.Route == "/generated-page");
        Assert.Contains(application.Components, static component =>
            component.Type == typeof(DescribedPlainComponent));
    }

    public class DescribedRoutablePage : ComponentBase;

    public class DescribedPlainComponent : ComponentBase;

    private sealed class DescribedMarkerAttribute : Attribute;

    private sealed class TestResolver(
        IReadOnlyList<ComponentDescriptor> components,
        IComponentTypeInfoResolver? fallback = null) : IComponentTypeInfoResolver
    {
        private readonly ComponentTypeInfo[] _typeInfos =
            [.. components.Select(static descriptor => new ComponentTypeInfo(descriptor))];

        public ComponentTypeInfo? GetTypeInfo(Type componentType)
            => _typeInfos.FirstOrDefault(typeInfo => typeInfo.Type == componentType)
                ?? fallback?.GetTypeInfo(componentType);

        public ComponentTypeInfo? GetTypeInfo(string assemblyName, string typeName)
            => _typeInfos.FirstOrDefault(typeInfo =>
                    typeInfo.Type.Assembly.GetName().Name == assemblyName &&
                    typeInfo.Type.FullName == typeName)
                ?? fallback?.GetTypeInfo(assemblyName, typeName);

        public IReadOnlyList<ComponentTypeInfo> GetTypeInfos(System.Reflection.Assembly assembly)
        {
            var results = _typeInfos.Where(typeInfo => typeInfo.Type.Assembly == assembly).ToList();
            if (fallback is not null)
            {
                foreach (var typeInfo in fallback.GetTypeInfos(assembly))
                {
                    if (!results.Any(existing => existing.Type == typeInfo.Type))
                    {
                        results.Add(typeInfo);
                    }
                }
            }

            return results;
        }
    }
}
