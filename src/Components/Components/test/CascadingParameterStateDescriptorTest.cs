// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class CascadingParameterStateDescriptorTest
{
    [Fact]
    public void FindCascadingParameters_UsesOnlyCascadingDescriptors()
    {
        var unnamedSupplier = new TestSupplier(
            static info => info.PropertyName == "DescriptorOnly",
            isFixed: true);
        var wrongNamedSupplier = new TestSupplier(
            static info => info.Attribute is CascadingParameterAttribute { Name: "other" },
            isFixed: true);
        var namedSupplier = new TestSupplier(
            static info => info.Attribute is CascadingParameterAttribute { Name: "wanted" },
            isFixed: true);
        var singleDeliverySupplier = new TestSupplier(
            static info => info.Attribute is SingleDeliveryAttribute,
            isFixed: true);
        var services = new ServiceCollection()
            .AddSingleton<IComponentMetadataResolver>(new TestMetadataResolver(CreateDescriptor()))
            .AddSingleton<ICascadingValueSupplier>(unnamedSupplier)
            .AddSingleton<ICascadingValueSupplier>(wrongNamedSupplier)
            .AddSingleton<ICascadingValueSupplier>(namedSupplier)
            .AddSingleton<ICascadingValueSupplier>(singleDeliverySupplier)
            .BuildServiceProvider();
        using var renderer = new TestRenderer(services);
        var componentState = new ComponentState(renderer, 0, new DescriptorOnlyComponent(), parentComponentState: null);

        var result = CascadingParameterState.FindCascadingParameters(componentState, out var hasSingleDeliveryParameters);

        Assert.True(hasSingleDeliveryParameters);
        Assert.Collection(
            result,
            match =>
            {
                Assert.Equal("DescriptorOnly", match.ParameterInfo.PropertyName);
                Assert.Equal(typeof(string), match.ParameterInfo.PropertyType);
                Assert.Same(unnamedSupplier, match.ValueSupplier);
            },
            match =>
            {
                Assert.Equal("NamedDescriptorOnly", match.ParameterInfo.PropertyName);
                Assert.Equal(typeof(int), match.ParameterInfo.PropertyType);
                Assert.Same(namedSupplier, match.ValueSupplier);
            },
            match =>
            {
                Assert.Equal("SingleDeliveryDescriptorOnly", match.ParameterInfo.PropertyName);
                Assert.Equal(typeof(Guid), match.ParameterInfo.PropertyType);
                Assert.Same(singleDeliverySupplier, match.ValueSupplier);
            });
    }

    private static ComponentDescriptor CreateDescriptor()
        => new()
        {
            Type = typeof(DescriptorOnlyComponent),
            Parameters =
            [
                CreateParameter("Ordinary", typeof(bool), new ParameterAttribute()),
                CreateParameter("DescriptorOnly", typeof(string), new CascadingParameterAttribute()),
                CreateParameter("NamedDescriptorOnly", typeof(int), new CascadingParameterAttribute { Name = "wanted" }),
                CreateParameter("SingleDeliveryDescriptorOnly", typeof(Guid), new SingleDeliveryAttribute()),
            ],
        };

    private static ComponentParameterDescriptor CreateParameter(string name, Type type, Attribute attribute)
        => new()
        {
            Name = name,
            ParameterType = type,
            Attribute = attribute,
            GetValue = static _ => null,
            SetValue = static (_, _) => { },
        };

    private sealed class TestMetadataResolver(ComponentDescriptor descriptor) : IComponentMetadataResolver
    {
        public IReadOnlyList<ComponentDescriptor> Components => [descriptor];

        public bool TryGetComponentDescriptor(Type type, [NotNullWhen(true)] out ComponentDescriptor? result)
        {
            result = type == descriptor.Type ? descriptor : null;
            return result is not null;
        }
    }

    private sealed class TestSupplier(
        Func<CascadingParameterInfo, bool> canSupply,
        bool isFixed) : ICascadingValueSupplier
    {
        public bool IsFixed => isFixed;

        public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
            => canSupply(parameterInfo);

        public object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo)
            => null;

        public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
            => throw new NotImplementedException();

        public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
            => throw new NotImplementedException();
    }

    private sealed class SingleDeliveryAttribute : CascadingParameterAttributeBase
    {
        internal override bool SingleDelivery => true;
    }

    private sealed class DescriptorOnlyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
            => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters)
            => throw new NotImplementedException();
    }
}
