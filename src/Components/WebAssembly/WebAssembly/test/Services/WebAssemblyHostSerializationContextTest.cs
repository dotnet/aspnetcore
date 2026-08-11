// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebAssembly;

#pragma warning disable ASPNETCORE9004
public class WebAssemblyHostSerializationContextTest
{
    [Fact]
    public void ContextSnapshotsApplicationResolversPerProvider()
    {
        var services = new ServiceCollection();
        services.AddComponentMetadata<FirstContext>();
        using var firstProvider = services.BuildServiceProvider();
        var first = new WebAssemblyHostSerializationContext(new RootTypeCache(), firstProvider);

        services.AddComponentMetadata<SecondContext>();
        using var secondProvider = services.BuildServiceProvider();
        var second = new WebAssemblyHostSerializationContext(new RootTypeCache(), secondProvider);

        var firstComponentResolver = first.ComponentOptions.TypeInfoResolverChain[1];
        var secondComponentResolver = second.ComponentOptions.TypeInfoResolverChain[1];
        var firstJSResolver = first.JSInteropOptions.TypeInfoResolverChain[2];
        var secondJSResolver = second.JSInteropOptions.TypeInfoResolverChain[2];

        Assert.NotNull(firstComponentResolver.GetTypeInfo(typeof(FirstPayload), first.ComponentOptions));
        Assert.Null(firstComponentResolver.GetTypeInfo(typeof(SecondPayload), first.ComponentOptions));
        Assert.NotNull(secondComponentResolver.GetTypeInfo(typeof(FirstPayload), second.ComponentOptions));
        Assert.NotNull(secondComponentResolver.GetTypeInfo(typeof(SecondPayload), second.ComponentOptions));
        Assert.Null(firstJSResolver.GetTypeInfo(typeof(SecondPayload), first.JSInteropOptions));
        Assert.NotNull(secondJSResolver.GetTypeInfo(typeof(SecondPayload), second.JSInteropOptions));
    }

    public sealed class FirstContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes => [];

        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => FirstJsonContext.Default;
    }

    public sealed class SecondContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes => [];

        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => SecondJsonContext.Default;
    }

    internal sealed class FirstPayload;

    internal sealed class SecondPayload;
}

[JsonSerializable(typeof(WebAssemblyHostSerializationContextTest.FirstPayload))]
internal sealed partial class FirstJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(WebAssemblyHostSerializationContextTest.SecondPayload))]
internal sealed partial class SecondJsonContext : JsonSerializerContext;
#pragma warning restore ASPNETCORE9004
