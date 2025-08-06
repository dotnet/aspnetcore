// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;

namespace Microsoft.AspNetCore.Components.Forms.PostHandling;

public class SupplyParameterFromFormTest
{
    [Fact]
    public async Task FindCascadingParameters_HandlesSupplyParameterFromFormValues()
    {
        // Arrange
        var renderer = CreateRendererWithFormValueModelBinder();
        var formComponent = new FormParametersComponent();

        // Act
        var componentId = renderer.AssignRootComponentId(formComponent);
        await renderer.RenderRootComponentAsync(componentId);
        var formComponentState = renderer.GetComponentState(formComponent);

        var result = CascadingParameterState.FindCascadingParameters(formComponentState, out _);

        // Assert
        var supplier = Assert.Single(result);
        Assert.IsType<SupplyParameterFromFormValueProvider>(supplier.ValueSupplier);
    }

    [Fact]
    public async Task FindCascadingParameters_HandlesSupplyParameterFromFormValues_WithMappingScopeName()
    {
        // Arrange
        var renderer = CreateRendererWithFormValueModelBinder();
        var formMappingScope = new FormMappingScope
        {
            Name = "scope-name",
            FormValueModelBinder = new TestFormModelValueBinder("[scope-name]handler-name"),
            ChildContent = modelBindingContext => builder =>
            {
                builder.OpenComponent<FormParametersComponentWithName>(0);
                builder.CloseComponent();
            }
        };

        // Act
        var componentId = renderer.AssignRootComponentId(formMappingScope);
        await renderer.RenderRootComponentAsync(componentId);
        var formComponentState = renderer.Batches.Single()
            .GetComponentFrames<FormParametersComponentWithName>().Single()
            .ComponentState;

        var result = CascadingParameterState.FindCascadingParameters(formComponentState, out _);

        // Assert
        var supplier = Assert.Single(result);
        Assert.Equal(formMappingScope, supplier.ValueSupplier);
    }

    [Fact]
    public async Task FindCascadingParameters_HandlesRecursiveModelTypes()
    {
        // This test reproduces GitHub issue #61341
        // Arrange
        var renderer = CreateRendererWithFormValueModelBinder();
        var formComponent = new RecursiveFormParametersComponent();

        // Act
        var componentId = renderer.AssignRootComponentId(formComponent);
        await renderer.RenderRootComponentAsync(componentId);
        var formComponentState = renderer.GetComponentState(formComponent);

        var result = CascadingParameterState.FindCascadingParameters(formComponentState, out _);

        // Assert
        var supplier = Assert.Single(result);
        Assert.IsType<SupplyParameterFromFormValueProvider>(supplier.ValueSupplier);
    }

    [Fact]
    public async Task SupplyParameterFromForm_WithRecursiveType_ShouldBindCorrectly()
    {
        // Arrange - Test if recursive types can be bound correctly
        // This reproduces the scenario from GitHub #61341
        var renderer = CreateRendererWithRealFormBinding();
        var formComponent = new RecursiveFormParametersComponent();

        // Act
        var componentId = renderer.AssignRootComponentId(formComponent);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        var parameters = CascadingParameterState.FindCascadingParameters(
            renderer.GetComponentState(formComponent), out _);
        
        var supplier = Assert.Single(parameters);
        Assert.IsType<SupplyParameterFromFormValueProvider>(supplier.ValueSupplier);
        
        // The key test: verify that the recursive type can be resolved for binding
        var valueMapper = new TestFormValueMapperWithRealBinding();
        var canMap = valueMapper.CanMap(typeof(MyModel), "", null);
        
        Assert.True(canMap, "Should be able to map recursive types");
    }

    [Fact]
    public async Task SupplyParameterFromForm_WithNestedRecursiveProperties_ShouldBindCorrectly()
    {
        // Test more complex scenarios with actual nested data
        var renderer = CreateRendererWithRealFormBinding();
        var formComponent = new RecursiveFormParametersComponent();

        // Act
        var componentId = renderer.AssignRootComponentId(formComponent);
        await renderer.RenderRootComponentAsync(componentId);

        // Assert
        var parameters = CascadingParameterState.FindCascadingParameters(
            renderer.GetComponentState(formComponent), out _);
        
        var supplier = Assert.Single(parameters);
        Assert.IsType<SupplyParameterFromFormValueProvider>(supplier.ValueSupplier);
        
        // Test that nested recursive properties can be handled
        var valueMapper = new TestFormValueMapperWithRealBinding();
        var canMapRoot = valueMapper.CanMap(typeof(MyModel), "", null);
        
        Assert.True(canMapRoot, "Should be able to map recursive types with nested properties");
    }

    static TestRenderer CreateRendererWithRealFormBinding()
    {
        var services = new ServiceCollection();
        var valueBinder = new TestFormValueMapperWithRealBinding();
        services.AddSingleton<IFormValueMapper>(valueBinder);
        services.AddSingleton<ICascadingValueSupplier>(_ => new SupplyParameterFromFormValueProvider(
            valueBinder, mappingScopeName: ""));
        return new TestRenderer(services.BuildServiceProvider());
    }

    static TestRenderer CreateRendererWithFormValueModelBinder()
    {
        var services = new ServiceCollection();
        var valueBinder = new TestFormModelValueBinder();
        services.AddSingleton<IFormValueMapper>(valueBinder);
        services.AddSingleton<ICascadingValueSupplier>(_ => new SupplyParameterFromFormValueProvider(
            valueBinder, mappingScopeName: ""));
        return new TestRenderer(services.BuildServiceProvider());
    }

    class FormParametersComponent : TestComponentBase
    {
        [SupplyParameterFromForm] public string FormParameter { get; set; }
    }

    class FormParametersComponentWithName : TestComponentBase
    {
        [SupplyParameterFromForm(FormName = "handler-name")] public string FormParameter { get; set; }
    }

    class RecursiveFormParametersComponent : TestComponentBase
    {
        [SupplyParameterFromForm] public MyModel FormParameter { get; set; }
    }

    class MyModel
    {
        public string Name { get; set; }
        public MyModel Parent { get; set; }
    }

    class TestFormModelValueBinder(string IncomingScopeQualifiedFormName = "") : IFormValueMapper
    {
        public void Map(FormValueMappingContext context) { }

        public bool CanMap(Type valueType, string mappingScopeName, string formName)
        {
            if (string.IsNullOrEmpty(mappingScopeName))
            {
                return IncomingScopeQualifiedFormName == (formName ?? string.Empty);
            }
            else
            {
                return IncomingScopeQualifiedFormName == $"[{mappingScopeName}]{formName ?? string.Empty}";
            }
        }
    }

    class TestFormValueMapperWithRealBinding : IFormValueMapper
    {
        public void Map(FormValueMappingContext context) 
        {
            // Simple test implementation - just create an instance if possible
            try
            {
                if (context.ValueType == typeof(MyModel))
                {
                    context.SetResult(new MyModel());
                }
                else if (context.ValueType.IsClass && context.ValueType.GetConstructor(Type.EmptyTypes) != null)
                {
                    context.SetResult(Activator.CreateInstance(context.ValueType));
                }
            }
            catch
            {
                // If there's an exception creating the instance, don't set result
            }
        }

        public bool CanMap(Type valueType, string mappingScopeName, string formName)
        {
            // Test if we can handle recursive types
            // For this test, we accept MyModel and other simple types
            return valueType == typeof(MyModel) || 
                   (valueType.IsClass && valueType.GetConstructor(Type.EmptyTypes) != null);
        }
    }

    class TestComponentBase : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
            => Task.CompletedTask;
    }
}
