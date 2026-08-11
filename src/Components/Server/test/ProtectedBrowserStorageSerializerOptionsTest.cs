// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class ProtectedBrowserStorageSerializerOptionsTest
{
    [Fact]
    public void Options_AreTheSharedInstanceWhenTheApplicationRegisteredNoMetadata()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        var first = new ProtectedBrowserStorageSerializerOptions(services).Options;
        var second = new ProtectedBrowserStorageSerializerOptions(services).Options;

        // Nothing was contributed, so the shared instance is handed back untouched rather than copied.
        Assert.Same(first, second);
        Assert.Equal(JsonNamingPolicy.CamelCase, first.PropertyNamingPolicy);
    }

    [Fact]
    public void Options_PreserveTheSharedSerializationSettings()
    {
        var options = CreateOptions(new StubContext(new StubResolver()));

        Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.True(options.PropertyNameCaseInsensitive);
        Assert.True(options.IncludeFields);
    }

    [Fact]
    public void Options_OrderTheApplicationsContractsBeforeReflection()
    {
        var resolver = new StubResolver();

        var options = CreateOptions(new StubContext(resolver));

        Assert.Same(resolver, options.TypeInfoResolverChain[0]);

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            Assert.IsType<DefaultJsonTypeInfoResolver>(options.TypeInfoResolverChain[1]);
            Assert.Equal(2, options.TypeInfoResolverChain.Count);
        }
        else
        {
            Assert.Single(options.TypeInfoResolverChain);
        }
    }

    [Fact]
    public void Options_IncludeEveryRegisteredContext()
    {
        var first = new StubResolver(typeof(FirstPayload));
        var second = new StubResolver(typeof(SecondPayload));

        var options = CreateOptions(new StubContext(first), new StubContext(second));
        var applicationResolver = options.TypeInfoResolverChain[0];

        Assert.NotNull(applicationResolver.GetTypeInfo(typeof(FirstPayload), options));
        Assert.NotNull(applicationResolver.GetTypeInfo(typeof(SecondPayload), options));
        Assert.Contains(typeof(FirstPayload), first.Requested);
        Assert.Contains(typeof(SecondPayload), second.Requested);
    }

    [Fact]
    public void Options_SkipAContextThatSuppliesNoResolver()
    {
        var resolver = new StubResolver();

        var options = CreateOptions(new StubContext(null), new StubContext(resolver));

        Assert.Same(resolver, options.TypeInfoResolverChain[0]);
    }

    [Fact]
    public void Options_ResolveATypeOnlyTheApplicationDescribes()
    {
        var resolver = new StubResolver(typeof(Payload));

        var options = CreateOptions(new StubContext(resolver));
        var json = JsonSerializer.Serialize(new Payload { Value = "x" }, options);

        Assert.Equal("{\"value\":\"x\"}", json);
        Assert.Contains(typeof(Payload), resolver.Requested);
    }

    private static JsonSerializerOptions CreateOptions(params RazorComponentsMetadataContext[] contexts)
    {
        var services = new ServiceCollection();
        foreach (var context in contexts)
        {
            services.AddSingleton(context);
        }

        return new ProtectedBrowserStorageSerializerOptions(services.BuildServiceProvider()).Options;
    }

    private sealed class StubContext(IJsonTypeInfoResolver? resolver) : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor> Components => [];

        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes => [];

        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver? JsonTypeInfoResolver { get; } = resolver;
    }

    private sealed class StubResolver(params Type[] known) : IJsonTypeInfoResolver
    {
        private readonly HashSet<Type> _known = [.. known];

        public List<Type> Requested { get; } = [];

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            Requested.Add(type);
            return _known.Contains(type)
                ? new DefaultJsonTypeInfoResolver().GetTypeInfo(type, options)
                : null;
        }
    }

    private sealed class Payload
    {
        public string Value { get; set; } = "";
    }

    private sealed class FirstPayload;

    private sealed class SecondPayload;
}
