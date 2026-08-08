// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Discovery;

#nullable enable

public class DescribedComponentDiscoveryTest
{
    private static readonly System.Reflection.Assembly _assembly = typeof(DescribedComponentDiscoveryTest).Assembly;

    [Fact]
    public void AddAssembly_UsesNormalizedGeneratedMetadata()
    {
        var marker = new DescribedMarkerAttribute();
        var descriptor = new ComponentDescriptor
        {
            Type = typeof(DescribedRoutablePage),
            Metadata = [new RouteAttribute("/described-page"), marker],
        };
        var builder = new ComponentApplicationBuilder(CreateGeneratedResolver(descriptor));

        builder.AddAssembly(_assembly);
        var application = builder.Build();

        var page = Assert.Single(application.Pages);
        Assert.Equal("/described-page", page.Route);
        Assert.DoesNotContain(page.Metadata, static item => item is RouteAttribute);
        Assert.Same(marker, Assert.Single(page.Metadata));
        Assert.Equal(descriptor.Type, Assert.Single(application.Components).Type);
    }

    [Fact]
    public void AddAssembly_GeneratedMetadataWinsAndReflectionFillsMissingTypes()
    {
        var generated = new ComponentDescriptor
        {
            Type = typeof(DescribedRoutablePage),
            Metadata = [new RouteAttribute("/generated-page")],
        };
        using var resolver = new CompositeComponentTypeInfoResolver(
        [
            CreateGeneratedResolver(generated),
            new ReflectionComponentTypeInfoResolver(),
        ]);
        var builder = new ComponentApplicationBuilder(resolver);

        builder.AddAssembly(_assembly);
        var application = builder.Build();

        Assert.Contains(application.Pages, static page =>
            page.Type == typeof(DescribedRoutablePage) && page.Route == "/generated-page");
        Assert.Contains(application.Components, static component =>
            component.Type == typeof(DescribedPlainComponent));
    }

    private static SourceGeneratedComponentTypeInfoResolver CreateGeneratedResolver(
        params ComponentDescriptor[] descriptors)
        => new(new StubMetadataResolver(descriptors));

    public class DescribedRoutablePage : ComponentBase;

    public class DescribedPlainComponent : ComponentBase;

    private sealed class DescribedMarkerAttribute : Attribute;

    private sealed class StubMetadataResolver(IReadOnlyList<ComponentDescriptor> components)
        : IComponentMetadataResolver
    {
        public IReadOnlyList<ComponentDescriptor> Components { get; } = components;

        public bool TryGetComponentDescriptor(
            Type type,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ComponentDescriptor? descriptor)
        {
            descriptor = Components.FirstOrDefault(item => item.Type == type);
            return descriptor is not null;
        }
    }
}
