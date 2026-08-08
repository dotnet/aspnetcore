// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class ComponentParameterValueGetterTest
{
    [Fact]
    public void Create_UsesDescriptorGetterFirst()
    {
        var component = new TestComponent();
        var typeInfo = CreateTypeInfo(
            new ComponentParameterDescriptor
            {
                Name = nameof(TestComponent.Value),
                ParameterType = typeof(string),
                Attribute = new SupplyParameterFromTempDataAttribute(),
                SetValue = static (_, _) => { },
                GetValue = static _ => "descriptor",
            });

        var getter = ComponentParameterValueGetter.Create(component, typeInfo, nameof(TestComponent.Value));

        Assert.Equal("descriptor", getter());
    }

    [Fact]
    public void Create_UsesSharedReflectionResolverForCompatibility()
    {
        var component = new TestComponent { Value = "reflection" };
        var getter = ComponentParameterValueGetter.Create(
            component,
            CreateTypeInfo(),
            nameof(TestComponent.Value));

        Assert.Equal("reflection", getter());
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void Create_ReflectionDisabled_DoesNotUseCompatibilityFallback()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(ComponentMetadataFeature.SwitchName, false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            Assert.False(ComponentMetadataFeature.IsReflectionEnabledByDefault);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ComponentParameterValueGetter.Create(
                    new TestComponent(),
                    CreateTypeInfo(),
                    nameof(TestComponent.Value)));
            Assert.Contains(nameof(TestComponent.Value), exception.Message);
        }, options);
    }

    private static ComponentTypeInfo CreateTypeInfo(params ComponentParameterDescriptor[] parameters)
        => new(new ComponentDescriptor
        {
            Type = typeof(TestComponent),
            Parameters = parameters,
        });

    private sealed class TestComponent : ComponentBase
    {
        [SupplyParameterFromTempData]
        public string? Value { get; set; }
    }
}
