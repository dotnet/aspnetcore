// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable ASPNETCORE9004
public class ComponentMetadataServiceCollectionExtensionsTest
{
    [Fact]
    public void MultipleContextsComposeResolversAndRemainEnumerable()
    {
        var services = new ServiceCollection();
        services.AddComponentMetadata<FirstContext>();
        services.AddComponentMetadata<SecondContext>();
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IComponentJsonMetadataResolver>().JsonTypeInfoResolver!;
        Assert.NotNull(resolver.GetTypeInfo(typeof(FirstPayload), new JsonSerializerOptions()));
        Assert.NotNull(resolver.GetTypeInfo(typeof(SecondPayload), new JsonSerializerOptions()));
        var bindableResolver = provider.GetRequiredService<IBindableTypeResolver>();
        Assert.True(bindableResolver.TryGetBindableTypeDescriptor(typeof(FirstPayload), out _));
        Assert.True(bindableResolver.TryGetBindableTypeDescriptor(typeof(SecondPayload), out _));
        var componentResolver = provider.GetRequiredService<IComponentMetadataResolver>();
        Assert.True(componentResolver.TryGetComponentDescriptor(typeof(FirstComponent), out _));
        Assert.True(componentResolver.TryGetComponentDescriptor(typeof(SecondComponent), out _));
        var typeInfoResolver = provider.GetRequiredService<IComponentTypeInfoResolver>();
        Assert.Equal(typeof(FirstComponent), typeInfoResolver.GetRequiredTypeInfo(typeof(FirstComponent)).Type);
        Assert.Equal(typeof(SecondComponent), typeInfoResolver.GetRequiredTypeInfo(typeof(SecondComponent)).Type);
        Assert.Collection(
            provider.GetServices<RazorComponentsMetadataContext>(),
            context => Assert.IsType<FirstContext>(context),
            context => Assert.IsType<SecondContext>(context));
    }

    [Fact]
    public void ExistingProviderDoesNotObserveLaterRegistrations()
    {
        var services = new ServiceCollection();
        services.AddComponentMetadata<FirstContext>();
        using var firstProvider = services.BuildServiceProvider();
        var firstResolver = firstProvider.GetRequiredService<IComponentJsonMetadataResolver>().JsonTypeInfoResolver!;
        var firstBindableResolver = firstProvider.GetRequiredService<IBindableTypeResolver>();

        services.AddComponentMetadata<SecondContext>();
        using var secondProvider = services.BuildServiceProvider();
        var secondResolver = secondProvider.GetRequiredService<IComponentJsonMetadataResolver>().JsonTypeInfoResolver!;
        var secondBindableResolver = secondProvider.GetRequiredService<IBindableTypeResolver>();

        Assert.NotNull(firstResolver.GetTypeInfo(typeof(FirstPayload), new JsonSerializerOptions()));
        Assert.Null(firstResolver.GetTypeInfo(typeof(SecondPayload), new JsonSerializerOptions()));
        Assert.NotNull(secondResolver.GetTypeInfo(typeof(FirstPayload), new JsonSerializerOptions()));
        Assert.NotNull(secondResolver.GetTypeInfo(typeof(SecondPayload), new JsonSerializerOptions()));
        Assert.True(firstBindableResolver.TryGetBindableTypeDescriptor(typeof(FirstPayload), out _));
        Assert.False(firstBindableResolver.TryGetBindableTypeDescriptor(typeof(SecondPayload), out _));
        Assert.True(secondBindableResolver.TryGetBindableTypeDescriptor(typeof(FirstPayload), out _));
        Assert.True(secondBindableResolver.TryGetBindableTypeDescriptor(typeof(SecondPayload), out _));
    }

    [Fact]
    public void NullServicesThrows()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ComponentMetadataServiceCollectionExtensions.AddComponentMetadata<FirstContext>(null!));

        Assert.Equal("services", exception.ParamName);
    }

    public sealed class FirstContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<ComponentDescriptor> Components =>
        [
            new() { Type = typeof(FirstComponent) },
        ];

        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes =>
        [
            new()
            {
                Type = typeof(FirstPayload),
            },
        ];

        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => FirstJsonContext.Default;
    }

    public sealed class SecondContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<ComponentDescriptor> Components =>
        [
            new() { Type = typeof(SecondComponent) },
        ];

        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes =>
        [
            new()
            {
                Type = typeof(SecondPayload),
            },
        ];

        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => SecondJsonContext.Default;
    }

    internal sealed class FirstPayload;

    internal sealed class SecondPayload;

    internal sealed class FirstComponent : ComponentBase;

    internal sealed class SecondComponent : ComponentBase;
}

[JsonSerializable(typeof(ComponentMetadataServiceCollectionExtensionsTest.FirstPayload))]
internal sealed partial class FirstJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(ComponentMetadataServiceCollectionExtensionsTest.SecondPayload))]
internal sealed partial class SecondJsonContext : JsonSerializerContext;
#pragma warning restore ASPNETCORE9004
