// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

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

    class TestComponentBase : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
            => Task.CompletedTask;
    }
}
