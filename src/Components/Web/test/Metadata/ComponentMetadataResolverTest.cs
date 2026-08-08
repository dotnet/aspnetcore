// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Web;

[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class ComponentMetadataResolverTest
{
    [Fact]
    public void EmptyContextProducesEmptyMetadata()
    {
        var services = new ServiceCollection();
        services.AddComponentMetadata<EmptyContext>();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ComponentMetadataOptions>>().Value;
        var resolver = provider.GetRequiredService<ComponentMetadataResolver>();

        Assert.Empty(options.Components);
        Assert.Empty(options.BindableTypes);
        Assert.Empty(options.JSInvokableMethods);
        Assert.Empty(options.JsonTypeInfoResolvers);
        Assert.Empty(resolver.Components);
        Assert.Null(resolver.JsonTypeInfoResolver);
        Assert.False(resolver.TryGetComponentDescriptor(typeof(object), out _));
        Assert.False(resolver.TryGetBindableTypeDescriptor(typeof(object), out _));
    }

    [Fact]
    public void SingleJsonResolverPreservesIdentity()
    {
        var jsonResolver = new RecordingResolver("only", [], resolvesString: true);
        var options = new ComponentMetadataOptions();
        options.JsonTypeInfoResolvers.Add(jsonResolver);

        var resolver = new ComponentMetadataResolver(Options.Create(options));

        Assert.Same(jsonResolver, resolver.JsonTypeInfoResolver);
    }

    [Fact]
    public void MultipleJsonResolversRunInRegistrationOrder()
    {
        var calls = new List<string>();
        var first = new RecordingResolver("first", calls, resolvesString: false);
        var second = new RecordingResolver("second", calls, resolvesString: true);
        var options = new ComponentMetadataOptions();
        options.JsonTypeInfoResolvers.Add(first);
        options.JsonTypeInfoResolvers.Add(second);
        var resolver = new ComponentMetadataResolver(Options.Create(options));

        var typeInfo = resolver.JsonTypeInfoResolver!.GetTypeInfo(typeof(string), new JsonSerializerOptions());

        Assert.NotNull(typeInfo);
        Assert.Equal(["first", "second"], calls);
    }

    public sealed class EmptyContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<ComponentDescriptor> Components => [];

        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes => [];

        public override IReadOnlyList<Microsoft.JSInterop.Infrastructure.JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver? JsonTypeInfoResolver => null;
    }

    private sealed class RecordingResolver(
        string name,
        List<string> calls,
        bool resolvesString) : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            calls.Add(name);
            return resolvesString && type == typeof(string)
                ? new DefaultJsonTypeInfoResolver().GetTypeInfo(type, options)
                : null;
        }
    }
}
