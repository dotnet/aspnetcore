// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class ReflectionComponentTypeInfoResolverTest
{
    [Fact]
    public void DescribesActivationParametersCascadesAndPersistentMembers()
    {
        using var resolver = new ReflectionComponentTypeInfoResolver();

        var typeInfo = resolver.GetRequiredTypeInfo(typeof(TestComponent));

        Assert.NotNull(typeInfo.CreateInstance);
        Assert.IsType<TestComponent>(typeInfo.CreateInstance!(new ServiceCollection().BuildServiceProvider()));
        Assert.Collection(
            typeInfo.Parameters.OrderBy(static parameter => parameter.Name),
            parameter =>
            {
                Assert.Equal(nameof(TestComponent.CascadingValue), parameter.Name);
                Assert.IsType<CascadingParameterAttribute>(parameter.Attribute);
            },
            parameter =>
            {
                Assert.Equal(nameof(TestComponent.PersistentValue), parameter.Name);
                Assert.IsType<PersistentStateAttribute>(parameter.Attribute);
            },
            parameter =>
            {
                Assert.Equal(nameof(TestComponent.Value), parameter.Name);
                Assert.IsType<ParameterAttribute>(parameter.Attribute);
            });
    }

    [Fact]
    public void ActivatorsConsumeReflectionTypeInfo()
    {
        var service = new TestService();
        var services = new ServiceCollection()
            .AddSingleton(service)
            .BuildServiceProvider();
        using var resolver = new ReflectionComponentTypeInfoResolver();
        var typeInfo = resolver.GetRequiredTypeInfo(typeof(TestComponent));
        var component = Assert.IsType<TestComponent>(
            new DefaultComponentActivator(services, resolver).CreateInstance(typeInfo));

        new DefaultComponentPropertyActivator(resolver).GetActivator(typeInfo)(services, component);

        Assert.Same(service, component.Service);
    }

    [Fact]
    public void ParameterAssignmentConsumesReflectionTypeInfo()
    {
        using var resolver = new ReflectionComponentTypeInfoResolver();
        var component = new TestComponent();
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(TestComponent.Value)] = "assigned",
        });

        ComponentProperties.SetProperties(parameters, component, resolver);

        Assert.Equal("assigned", component.Value);
    }

    private sealed class TestComponent : IComponent
    {
        [Inject]
        public TestService Service { get; set; } = null!;

        [Parameter]
        public string Value { get; set; } = string.Empty;

        [CascadingParameter]
        public string CascadingValue { get; set; } = string.Empty;

        [PersistentState]
        public string PersistentValue { get; set; } = string.Empty;

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private sealed class TestService;
}
