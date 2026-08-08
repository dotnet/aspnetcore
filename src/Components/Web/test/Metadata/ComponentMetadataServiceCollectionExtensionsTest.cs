// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class ComponentMetadataServiceCollectionExtensionsTest
{
    [Fact]
    public void MultipleContextsAppendMetadataAndMergeComponentTypeInfo()
    {
        var services = new ServiceCollection();
        services.AddComponentMetadata<FirstContext>();
        services.AddComponentMetadata<SecondContext>();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ComponentMetadataOptions>>().Value;
        var resolver = provider.GetRequiredService<ComponentMetadataResolver>();

        Assert.Equal([FirstContext.Component, SecondContext.Component], options.Components);
        Assert.Equal([FirstContext.Bindable, SecondContext.Bindable], options.BindableTypes);
        Assert.Equal([FirstContext.JSMethod, SecondContext.JSMethod], options.JSInvokableMethods);
        Assert.Equal([FirstContext.Component, SecondContext.Component], resolver.Components);
        Assert.True(resolver.TryGetComponentDescriptor(typeof(SharedComponent), out var component));
        Assert.Same(SecondContext.Component, component);
        Assert.True(resolver.TryGetBindableTypeDescriptor(typeof(SharedModel), out var bindable));
        Assert.Same(SecondContext.Bindable, bindable);
        var typeInfo = provider.GetRequiredService<IComponentTypeInfoResolver>().GetRequiredTypeInfo(typeof(SharedComponent));
        Assert.Equal(["first", "second"], typeInfo.Metadata);
        Assert.Collection(
            provider.GetServices<RazorComponentsMetadataContext>(),
            context => Assert.IsType<FirstContext>(context),
            context => Assert.IsType<SecondContext>(context));
    }

    [Fact]
    public void RepeatedRegistrationSharesSingletonResolverAndPreservesContexts()
    {
        var services = new ServiceCollection();
        services.AddComponentMetadata<FirstContext>();
        services.AddComponentMetadata<FirstContext>();
        using var provider = services.BuildServiceProvider();

        var concrete = provider.GetRequiredService<ComponentMetadataResolver>();
        Assert.Same(concrete, provider.GetRequiredService<IComponentMetadataResolver>());
        Assert.Same(concrete, provider.GetRequiredService<IBindableTypeResolver>());
        Assert.Same(concrete, provider.GetRequiredService<IComponentJsonMetadataResolver>());
        Assert.Same(
            provider.GetRequiredService<IComponentTypeInfoResolver>(),
            provider.GetRequiredService<IComponentTypeInfoResolver>());
        Assert.Equal(2, provider.GetServices<RazorComponentsMetadataContext>().Count());
    }

    [Fact]
    public void NullServicesThrows()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ComponentMetadataServiceCollectionExtensions.AddComponentMetadata<FirstContext>(null!));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void ApplicationJsonResolverContributesToComponentMarkerSerialization()
    {
        var services = new ServiceCollection();
        services.AddComponentMetadata<MarkerContext>();

        var typeInfo = ComponentMarkerJsonTypeInfoResolver.Instance.GetTypeInfo(
            typeof(MarkerModel),
            new JsonSerializerOptions());

        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(MarkerModel), typeInfo.Type);
    }

    public sealed class FirstContext : RazorComponentsMetadataContext
    {
        public static readonly ComponentDescriptor Component = new()
        {
            Type = typeof(SharedComponent),
            Metadata = ["first"],
        };

        public static readonly BindableTypeDescriptor Bindable = new()
        {
            Type = typeof(SharedModel),
        };

        public static readonly JSInvokableMethodDescriptor JSMethod = CreateJSMethod("first");

        public override IReadOnlyList<ComponentDescriptor> Components => [Component];

        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes => [Bindable];

        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [JSMethod];

        public override IJsonTypeInfoResolver? JsonTypeInfoResolver => null;
    }

    public sealed class SecondContext : RazorComponentsMetadataContext
    {
        public static readonly ComponentDescriptor Component = new()
        {
            Type = typeof(SharedComponent),
            Metadata = ["second"],
        };

        public static readonly BindableTypeDescriptor Bindable = new()
        {
            Type = typeof(SharedModel),
        };

        public static readonly JSInvokableMethodDescriptor JSMethod = CreateJSMethod("second");

        public override IReadOnlyList<ComponentDescriptor> Components => [Component];

        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes => [Bindable];

        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [JSMethod];

        public override IJsonTypeInfoResolver? JsonTypeInfoResolver => null;
    }

    public sealed class MarkerContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<ComponentDescriptor> Components => [];

        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes => [];

        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => MarkerJsonContext.Default;
    }

    private static JSInvokableMethodDescriptor CreateJSMethod(string identifier)
        => new()
        {
            AssemblyName = "TestAssembly",
            TargetType = typeof(SharedComponent),
            Identifier = identifier,
            IsStatic = true,
            Invoke = static (_, _, _) => ValueTask.FromResult<string?>(null),
        };

    private sealed class SharedComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
            => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters)
            => throw new NotImplementedException();
    }

    private sealed class SharedModel;

    internal sealed class MarkerModel;
}

[JsonSerializable(typeof(ComponentMetadataServiceCollectionExtensionsTest.MarkerModel))]
internal sealed partial class MarkerJsonContext : JsonSerializerContext;
