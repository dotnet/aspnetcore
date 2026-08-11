// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

#nullable enable annotations

namespace Microsoft.AspNetCore.Components;

public partial class ParameterViewTest
{
    [Fact]
    public void DescribedComponent_BindsThroughDescriptorDelegates()
    {
        var parameters = new ParameterViewBuilder
            {
                { nameof(DescribedTarget.IntProp), 123 },
                { nameof(DescribedTarget.StringProp), "Hello" },
            }.Build();
        var target = new DescribedTarget();
        var resolver = CreateTypeInfoResolver(DescribedTarget.Descriptor);

        ComponentProperties.SetProperties(parameters, target, resolver);

        Assert.Equal(123, target.IntProp);
        Assert.Equal("Hello", target.StringProp);
        Assert.True(target.SetThroughDescriptor);
    }

    [Fact]
    public void DescribedComponent_MatchesParameterNamesCaseInsensitively()
    {
        var parameters = new ParameterViewBuilder
            {
                { nameof(DescribedCaseInsensitiveTarget.IntProp).ToLowerInvariant(), 123 },
            }.Build();
        var target = new DescribedCaseInsensitiveTarget();
        var resolver = CreateTypeInfoResolver(DescribedCaseInsensitiveTarget.Descriptor);

        ComponentProperties.SetProperties(parameters, target, resolver);

        Assert.Equal(123, target.IntProp);
    }

    [Fact]
    public void DescribedComponent_ThrowsForUnknownParameterName()
    {
        var parameters = new ParameterViewBuilder
            {
                { "NoSuchParameter", 123 },
            }.Build();
        var target = new DescribedUnknownNameTarget();
        var resolver = CreateTypeInfoResolver(DescribedUnknownNameTarget.Descriptor);

        var ex = Assert.Throws<InvalidOperationException>(
            () => ComponentProperties.SetProperties(parameters, target, resolver));

        Assert.Contains("does not have a property matching the name 'NoSuchParameter'", ex.Message);
    }

    [Fact]
    public void DescribedComponent_RejectsCascadingValueForDirectParameter()
    {
        var parameters = new ParameterViewBuilder
            {
                { nameof(DescribedCascadingRejectionTarget.IntProp), 123, true },
            }.Build();
        var target = new DescribedCascadingRejectionTarget();
        var resolver = CreateTypeInfoResolver(DescribedCascadingRejectionTarget.Descriptor);

        var ex = Assert.Throws<InvalidOperationException>(
            () => ComponentProperties.SetProperties(parameters, target, resolver));

        Assert.Contains("cannot be set using a cascading value", ex.Message);
    }

    [Fact]
    public void DescribedComponent_RejectsDirectValueForCascadingParameter()
    {
        var parameters = new ParameterViewBuilder
            {
                { nameof(DescribedCascadingOnlyTarget.CascadingProp), 123 },
            }.Build();
        var target = new DescribedCascadingOnlyTarget();
        var resolver = CreateTypeInfoResolver(DescribedCascadingOnlyTarget.Descriptor);

        var ex = Assert.Throws<InvalidOperationException>(
            () => ComponentProperties.SetProperties(parameters, target, resolver));

        Assert.Contains("cannot be set explicitly because it only accepts cascading values", ex.Message);
    }

    [Fact]
    public void DescribedComponent_CollectsUnmatchedValues()
    {
        var parameters = new ParameterViewBuilder
            {
                { nameof(DescribedCatchAllTarget.IntProp), 123 },
                { "Unmatched", "value" },
            }.Build();
        var target = new DescribedCatchAllTarget();
        var resolver = CreateTypeInfoResolver(DescribedCatchAllTarget.Descriptor);

        ComponentProperties.SetProperties(parameters, target, resolver);

        Assert.Equal(123, target.IntProp);
        Assert.Equal(new Dictionary<string, object> { ["Unmatched"] = "value" }, target.CatchAll);
    }

    [Fact]
    public void UndescribedComponent_FallsBackToReflection()    {
        var parameters = new ParameterViewBuilder
            {
                { nameof(UndescribedTarget.IntProp), 123 },
            }.Build();
        var target = new UndescribedTarget();
        var resolver = CreateTypeInfoResolver(DescribedTarget.Descriptor);

        ComponentProperties.SetProperties(parameters, target, resolver);

        Assert.Equal(123, target.IntProp);
    }

    [Fact]
    public async Task ComponentBase_BindsThroughDescriptorResolvedFromTheRenderer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IComponentMetadataResolver>(
            new StubMetadataResolver(DescribedComponentBase.Descriptor));
        using var renderer = new TestRenderer(services.BuildServiceProvider());
        var component = new DescribedComponentBase();
        var componentId = renderer.AssignRootComponentId(component);

        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(
            componentId,
            ParameterView.FromDictionary(new Dictionary<string, object> { ["IntProp"] = 123 })));

        Assert.Equal(123, component.IntProp);
        Assert.True(component.SetThroughDescriptor);
    }

    private sealed class StubMetadataResolver : IComponentMetadataResolver    {
        private readonly Dictionary<Type, ComponentDescriptor> _descriptors;

        public StubMetadataResolver(params ComponentDescriptor[] descriptors)
        {
            _descriptors = descriptors.ToDictionary(d => d.Type);
        }

        public IReadOnlyList<ComponentDescriptor> Components => [.. _descriptors.Values];

        public bool TryGetComponentDescriptor(Type type, [NotNullWhen(true)] out ComponentDescriptor? descriptor)
            => _descriptors.TryGetValue(type, out descriptor);
    }

    private static IComponentTypeInfoResolver CreateTypeInfoResolver(
        params ComponentDescriptor[] descriptors)
        => new CompositeComponentTypeInfoResolver(
        [
            new SourceGeneratedComponentTypeInfoResolver(new StubMetadataResolver(descriptors)),
            new ReflectionComponentTypeInfoResolver(),
        ]);

    private class DescribedTarget
    {
        public int IntProp { get; set; }

        public string StringProp { get; set; }

        public bool SetThroughDescriptor { get; private set; }

        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(DescribedTarget),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(IntProp),
                    ParameterType = typeof(int),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) =>
                    {
                        var typed = (DescribedTarget)target;
                        typed.IntProp = (int)value;
                        typed.SetThroughDescriptor = true;
                    },
                    GetValue = static target => ((DescribedTarget)target).IntProp,
                },
                new ComponentParameterDescriptor
                {
                    Name = nameof(StringProp),
                    ParameterType = typeof(string),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) => ((DescribedTarget)target).StringProp = (string)value,
                    GetValue = static target => ((DescribedTarget)target).StringProp,
                },
            ],
        };
    }

    private class DescribedCaseInsensitiveTarget
    {
        public int IntProp { get; set; }

        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(DescribedCaseInsensitiveTarget),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(IntProp),
                    ParameterType = typeof(int),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) => ((DescribedCaseInsensitiveTarget)target).IntProp = (int)value,
                    GetValue = static target => ((DescribedCaseInsensitiveTarget)target).IntProp,
                },
            ],
        };
    }

    private class DescribedUnknownNameTarget
    {
        public int IntProp { get; set; }

        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(DescribedUnknownNameTarget),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(IntProp),
                    ParameterType = typeof(int),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) => ((DescribedUnknownNameTarget)target).IntProp = (int)value,
                    GetValue = static target => ((DescribedUnknownNameTarget)target).IntProp,
                },
            ],
        };
    }

    private class DescribedCascadingRejectionTarget
    {
        public int IntProp { get; set; }

        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(DescribedCascadingRejectionTarget),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(IntProp),
                    ParameterType = typeof(int),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) => ((DescribedCascadingRejectionTarget)target).IntProp = (int)value,
                    GetValue = static target => ((DescribedCascadingRejectionTarget)target).IntProp,
                },
            ],
        };
    }

    private class DescribedCascadingOnlyTarget
    {
        public int CascadingProp { get; set; }

        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(DescribedCascadingOnlyTarget),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(CascadingProp),
                    ParameterType = typeof(int),
                    Attribute = new CascadingParameterAttribute(),
                    SetValue = static (target, value) => ((DescribedCascadingOnlyTarget)target).CascadingProp = (int)value,
                    GetValue = static target => ((DescribedCascadingOnlyTarget)target).CascadingProp,
                },
            ],
        };
    }

    private class DescribedCatchAllTarget
    {
        public int IntProp { get; set; }

        public Dictionary<string, object> CatchAll { get; set; }

        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(DescribedCatchAllTarget),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(IntProp),
                    ParameterType = typeof(int),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) => ((DescribedCatchAllTarget)target).IntProp = (int)value,
                    GetValue = static target => ((DescribedCatchAllTarget)target).IntProp,
                },
                new ComponentParameterDescriptor
                {
                    Name = nameof(CatchAll),
                    ParameterType = typeof(Dictionary<string, object>),
                    Attribute = new ParameterAttribute { CaptureUnmatchedValues = true },
                    SetValue = static (target, value) => ((DescribedCatchAllTarget)target).CatchAll = (Dictionary<string, object>)value,
                    GetValue = static target => ((DescribedCatchAllTarget)target).CatchAll,
                },
            ],
        };
    }

    private class UndescribedTarget
    {
        [Parameter] public int IntProp { get; set; }
    }

    private class DescribedComponentBase : ComponentBase
    {
        [Parameter] public int IntProp { get; set; }

        public bool SetThroughDescriptor { get; private set; }

        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(DescribedComponentBase),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(IntProp),
                    ParameterType = typeof(int),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) =>
                    {
                        var typed = (DescribedComponentBase)target;
                        typed.IntProp = (int)value;
                        typed.SetThroughDescriptor = true;
                    },
                    GetValue = static target => ((DescribedComponentBase)target).IntProp,
                },
            ],
        };
    }
}
