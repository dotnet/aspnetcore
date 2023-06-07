// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Moq;

namespace Microsoft.AspNetCore.Components;

public class CascadingParameterStateTest
{
    [Fact]
    public void FindCascadingParameters_IfHasNoParameters_ReturnsEmpty()
    {
        // Arrange
        var componentState = CreateComponentState(new ComponentWithNoParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(componentState);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_IfHasNoCascadingParameters_ReturnsEmpty()
    {
        // Arrange
        var componentState = CreateComponentState(new ComponentWithNoCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(componentState);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_IfHasNoAncestors_ReturnsEmpty()
    {
        // Arrange
        var componentState = CreateComponentState(new ComponentWithCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(componentState);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_IfHasNoMatchesInAncestors_ReturnsEmpty()
    {
        // Arrange: Build the ancestry list
        var states = CreateAncestry(
            new ComponentWithNoParams(),
            CreateCascadingValueComponent("Hello"),
            new ComponentWithNoParams(),
            new ComponentWithCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_IfHasPartialMatchesInAncestors_ReturnsMatches()
    {
        // Arrange
        var states = CreateAncestry(
            new ComponentWithNoParams(),
            CreateCascadingValueComponent(new ValueType2()),
            new ComponentWithNoParams(),
            new ComponentWithCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result, match =>
        {
            Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam2), match.LocalValueName);
            Assert.Same(states[1].Component, match.ValueSupplier);
        });
    }

    [Fact]
    public void FindCascadingParameters_IfHasMultipleMatchesInAncestors_ReturnsMatches()
    {
        // Arrange
        var states = CreateAncestry(
            new ComponentWithNoParams(),
            CreateCascadingValueComponent(new ValueType2()),
            new ComponentWithNoParams(),
            CreateCascadingValueComponent(new ValueType1()),
            new ComponentWithCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result.OrderBy(x => x.LocalValueName),
            match =>
            {
                Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam1), match.LocalValueName);
                Assert.Same(states[3].Component, match.ValueSupplier);
            },
            match =>
            {
                Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam2), match.LocalValueName);
                Assert.Same(states[1].Component, match.ValueSupplier);
            });
    }

    [Fact]
    public void FindCascadingParameters_InheritedParameters_ReturnsMatches()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new ValueType1()),
            CreateCascadingValueComponent(new ValueType3()),
            new ComponentWithInheritedCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result.OrderBy(x => x.LocalValueName),
            match =>
            {
                Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam1), match.LocalValueName);
                Assert.Same(states[0].Component, match.ValueSupplier);
            },
            match =>
            {
                Assert.Equal(nameof(ComponentWithInheritedCascadingParams.CascadingParam3), match.LocalValueName);
                Assert.Same(states[1].Component, match.ValueSupplier);
            });
    }

    [Fact]
    public void FindCascadingParameters_ComponentRequestsBaseType_ReturnsMatches()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new CascadingValueTypeDerivedClass()),
            new ComponentWithGenericCascadingParam<CascadingValueTypeBaseClass>());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result, match =>
        {
            Assert.Equal(nameof(ComponentWithGenericCascadingParam<object>.LocalName), match.LocalValueName);
            Assert.Same(states[0].Component, match.ValueSupplier);
        });
    }

    [Fact]
    public void FindCascadingParameters_ComponentRequestsImplementedInterface_ReturnsMatches()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new CascadingValueTypeDerivedClass()),
            new ComponentWithGenericCascadingParam<ICascadingValueTypeDerivedClassInterface>());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result, match =>
        {
            Assert.Equal(nameof(ComponentWithGenericCascadingParam<object>.LocalName), match.LocalValueName);
            Assert.Same(states[0].Component, match.ValueSupplier);
        });
    }

    [Fact]
    public void FindCascadingParameters_ComponentRequestsDerivedType_ReturnsEmpty()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new CascadingValueTypeBaseClass()),
            new ComponentWithGenericCascadingParam<CascadingValueTypeDerivedClass>());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_TypeAssignmentIsValidForNullValue_ReturnsMatches()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent((CascadingValueTypeDerivedClass)null),
            new ComponentWithGenericCascadingParam<CascadingValueTypeBaseClass>());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result, match =>
        {
            Assert.Equal(nameof(ComponentWithGenericCascadingParam<object>.LocalName), match.LocalValueName);
            Assert.Same(states[0].Component, match.ValueSupplier);
        });
    }

    [Fact]
    public void FindCascadingParameters_TypeAssignmentIsInvalidForNullValue_ReturnsEmpty()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent((object)null),
            new ComponentWithGenericCascadingParam<ValueType1>());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_SupplierSpecifiesNameButConsumerDoesNot_ReturnsEmpty()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new ValueType1(), "MatchOnName"),
            new ComponentWithCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_ConsumerSpecifiesNameButSupplierDoesNot_ReturnsEmpty()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new ValueType1()),
            new ComponentWithNamedCascadingParam());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_MismatchingNameButMatchingType_ReturnsEmpty()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new ValueType1(), "MismatchName"),
            new ComponentWithNamedCascadingParam());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_MatchingNameButMismatchingType_ReturnsEmpty()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new ValueType2(), "MatchOnName"),
            new ComponentWithNamedCascadingParam());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FindCascadingParameters_MatchingNameAndType_ReturnsMatches()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new ValueType1(), "matchonNAME"), // To show it's case-insensitive
            new ComponentWithNamedCascadingParam());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result, match =>
        {
            Assert.Equal(nameof(ComponentWithNamedCascadingParam.SomeLocalName), match.LocalValueName);
            Assert.Same(states[0].Component, match.ValueSupplier);
        });
    }

    [Fact]
    public void FindCascadingParameters_MultipleMatchingAncestors_ReturnsClosestMatches()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new ValueType1()),
            CreateCascadingValueComponent(new ValueType2()),
            CreateCascadingValueComponent(new ValueType1()),
            CreateCascadingValueComponent(new ValueType2()),
            new ComponentWithCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result.OrderBy(x => x.LocalValueName),
            match =>
            {
                Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam1), match.LocalValueName);
                Assert.Same(states[2].Component, match.ValueSupplier);
            },
            match =>
            {
                Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam2), match.LocalValueName);
                Assert.Same(states[3].Component, match.ValueSupplier);
            });
    }

    [Fact]
    public void FindCascadingParameters_CanOverrideNonNullValueWithNull()
    {
        // Arrange
        var states = CreateAncestry(
            CreateCascadingValueComponent(new ValueType1()),
            CreateCascadingValueComponent((ValueType1)null),
            new ComponentWithCascadingParams());

        // Act
        var result = CascadingParameterState.FindCascadingParameters(states.Last());

        // Assert
        Assert.Collection(result.OrderBy(x => x.LocalValueName),
            match =>
            {
                Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam1), match.LocalValueName);
                Assert.Same(states[1].Component, match.ValueSupplier);
                Assert.Null(match.ValueSupplier.CurrentValue);
            });
    }

    [Fact]
    public void FindCascadingParameters_HandlesSupplyParameterFromFormValues()
    {
        // Arrange
        var cascadingModelBinder = new CascadingModelBinder
        {
            FormValueSupplier = new TestFormValueSupplier()
            {
                FormName = "",
                ValueType = typeof(string),
                BindResult = true,
                BoundValue = "some value"
            },
            Navigation = Mock.Of<NavigationManager>(),
            Name = ""
        };

        cascadingModelBinder.UpdateBindingInformation("https://localhost/");

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
        var cascadingModelBinder = new CascadingModelBinder
        {
            FormValueSupplier = new TestFormValueSupplier()
            {
                FormName = "some-name",
                ValueType = typeof(string),
                BindResult = true,
                BoundValue = "some value"
            },
            Navigation = new TestNavigationManager(),
            Name = ""
        };

        cascadingModelBinder.UpdateBindingInformation("https://localhost/");

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

    static CascadingValue<T> CreateCascadingValueComponent<T>(T value, string name = null)
    {
        var supplier = new CascadingValue<T>();
        var renderer = new TestRenderer();
        supplier.Attach(new RenderHandle(renderer, 0));

        var supplierParams = new Dictionary<string, object>
            {
                { "Value", value }
            };

        if (name != null)
        {
            supplierParams.Add("Name", name);
        }

        renderer.Dispatcher.InvokeAsync((Action)(() => supplier.SetParametersAsync(ParameterView.FromDictionary(supplierParams))));
        return supplier;
    }

    class ComponentWithNoParams : TestComponentBase
    {
    }

    class FormParametersComponent : TestComponentBase
    {
        [SupplyParameterFromForm] public string FormParameter { get; set; }
    }

    class FormParametersComponentWithName : TestComponentBase
    {
        [SupplyParameterFromForm(Name = "some-name")] public string FormParameter { get; set; }
    }

    class ComponentWithNoCascadingParams : TestComponentBase
    {
        [Parameter] public bool SomeRegularParameter { get; set; }
    }

    class ComponentWithCascadingParams : TestComponentBase
    {
        [Parameter] public bool RegularParam { get; set; }
        [CascadingParameter] internal ValueType1 CascadingParam1 { get; set; }
        [CascadingParameter] internal ValueType2 CascadingParam2 { get; set; }
    }

    class ComponentWithInheritedCascadingParams : ComponentWithCascadingParams
    {
        [CascadingParameter] internal ValueType3 CascadingParam3 { get; set; }
    }

    class ComponentWithGenericCascadingParam<T> : TestComponentBase
    {
        [CascadingParameter] internal T LocalName { get; set; }
    }

    class ComponentWithNamedCascadingParam : TestComponentBase
    {
        [CascadingParameter(Name = "MatchOnName")]
        internal ValueType1 SomeLocalName { get; set; }
    }

    class TestComponentBase : IComponent
    {
        public void Attach(RenderHandle renderHandle)
            => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters)
            => throw new NotImplementedException();
    }

    class ValueType1 { }
    class ValueType2 { }
    class ValueType3 { }

    class CascadingValueTypeBaseClass { }
    class CascadingValueTypeDerivedClass : CascadingValueTypeBaseClass, ICascadingValueTypeDerivedClassInterface { }
    interface ICascadingValueTypeDerivedClassInterface { }

    private class TestFormValueSupplier : IFormValueSupplier
    {
        public string FormName { get; set; }

        public Type ValueType { get; set; }

        public object BoundValue { get; set; }

        public bool BindResult { get; set; }

        public bool CanBind(string formName, Type valueType)
        {
            return string.Equals(formName, FormName, StringComparison.Ordinal) &&
                valueType == ValueType;
        }

        public bool CanConvertSingleValue(Type type)
        {
            return type == ValueType;
        }

        public bool TryBind(string formName, Type valueType, [NotNullWhen(true)] out object boundValue)
        {
            boundValue = BoundValue;
            return BindResult;
        }
    }

    class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://localhost:85/subdir/", "https://localhost:85/subdir/path?query=value#hash");
        }
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SupplyParameterFromFormAttribute : Attribute, IHostEnvironmentCascadingParameter
{
    /// <summary>
    /// Gets or sets the name for the parameter. The name is used to match
    /// the form data and decide whether or not the value needs to be bound.
    /// </summary>
    public string Name { get; set; }
}
