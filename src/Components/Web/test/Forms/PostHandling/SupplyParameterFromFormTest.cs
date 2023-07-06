// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Moq;

namespace Microsoft.AspNetCore.Components.Forms.PostHandling;

public class SupplyParameterFromFormTest
{
    [Fact]
    public void FindCascadingParameters_HandlesSupplyParameterFromFormValues()
    {
        // Arrange
        var provider = new TestCascadingFormModelBindingProvider
        {
            FormName = "",
            CurrentValue = "some value",
        };
        var cascadingModelBinder = new CascadingModelBinder
        {
            ModelBindingProviders = new[] { provider },
            Navigation = Mock.Of<NavigationManager>(),
            Name = ""
        };

        cascadingModelBinder.UpdateBindingInformation();

        var states = CreateAncestry(
            cascadingModelBinder,
            new FormParametersComponent());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        var supplier = Assert.Single(result);
        Assert.Equal(cascadingModelBinder, supplier.ValueSupplier);
    }

    [Fact]
    public void FindCascadingParameters_HandlesSupplyParameterFromFormValues_WithName()
    {
        // Arrange
        var provider = new TestCascadingFormModelBindingProvider
        {
            FormName = "some-name",
            CurrentValue = "some value",
        };
        var cascadingModelBinder = new CascadingModelBinder
        {
            ModelBindingProviders = new[] { provider },
            Navigation = new TestNavigationManager(),
            Name = "some-name"
        };

        cascadingModelBinder.UpdateBindingInformation();

        var states = CreateAncestry(
            cascadingModelBinder,
            new FormParametersComponentWithName());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        var supplier = Assert.Single(result);
        Assert.Equal(cascadingModelBinder, supplier.ValueSupplier);
    }

    static ComponentState[] CreateAncestry(params IComponent[] components)
    {
        var result = new ComponentState[components.Length];

        for (var i = 0; i < components.Length; i++)
        {
            result[i] = CreateComponentState(
                components[i],
                i == 0 ? null : result[i - 1]);
        }

        return result;
    }

    static ComponentState CreateComponentState(
        IComponent component, ComponentState parentComponentState = null)
    {
        return new ComponentState(new TestRenderer(), 0, component, parentComponentState);
    }

    class FormParametersComponent : TestComponentBase
    {
        [SupplyParameterFromForm] public string FormParameter { get; set; }
    }

    class FormParametersComponentWithName : TestComponentBase
    {
        [SupplyParameterFromForm(Handler = "some-name")] public string FormParameter { get; set; }
    }

    class TestComponentBase : IComponent
    {
        public void Attach(RenderHandle renderHandle)
            => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters)
            => throw new NotImplementedException();
    }

    class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://localhost:85/subdir/", "https://localhost:85/subdir/path?query=value#hash");
        }
    }

    private class TestCascadingFormModelBindingProvider : CascadingModelBindingProvider
    {
        public required string FormName { get; init; }

        public required string CurrentValue { get; init; }

        protected internal override bool AreValuesFixed => true;

        protected internal override bool CanSupplyValue(ModelBindingContext bindingContext, in CascadingParameterInfo parameterInfo)
            => string.Equals(bindingContext.Name, FormName, StringComparison.Ordinal);

        protected internal override object GetCurrentValue(ModelBindingContext bindingContext, in CascadingParameterInfo parameterInfo)
            => CurrentValue;

        protected internal override bool SupportsCascadingParameterAttributeType(Type attributeType)
            => attributeType == typeof(SupplyParameterFromFormAttribute);

        protected internal override bool SupportsParameterType(Type parameterType)
            => parameterType == typeof(string);
    }
}
