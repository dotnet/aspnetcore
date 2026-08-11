// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;
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

        services.AddComponentMetadata<SecondContext>();
        using var secondProvider = services.BuildServiceProvider();
        var secondResolver = secondProvider.GetRequiredService<IComponentJsonMetadataResolver>().JsonTypeInfoResolver!;

        Assert.NotNull(firstResolver.GetTypeInfo(typeof(FirstPayload), new JsonSerializerOptions()));
        Assert.Null(firstResolver.GetTypeInfo(typeof(SecondPayload), new JsonSerializerOptions()));
        Assert.NotNull(secondResolver.GetTypeInfo(typeof(FirstPayload), new JsonSerializerOptions()));
        Assert.NotNull(secondResolver.GetTypeInfo(typeof(SecondPayload), new JsonSerializerOptions()));
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
        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => FirstJsonContext.Default;
    }

    public sealed class SecondContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => SecondJsonContext.Default;
    }

    internal sealed class FirstPayload;

    internal sealed class SecondPayload;
}

[JsonSerializable(typeof(ComponentMetadataServiceCollectionExtensionsTest.FirstPayload))]
internal sealed partial class FirstJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(ComponentMetadataServiceCollectionExtensionsTest.SecondPayload))]
internal sealed partial class SecondJsonContext : JsonSerializerContext;
#pragma warning restore ASPNETCORE9004
