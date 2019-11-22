// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
{
    public class CascadingParameterStateTest
    {
        [Fact]
        public void FindCascadingParameters_IfHasNoParameters_ReturnsNull()
        {
            // Arrange
            var componentState = CreateComponentState(new ComponentWithNoParams());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(componentState);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindCascadingParameters_IfHasNoCascadingParameters_ReturnsNull()
        {
            // Arrange
            var componentState = CreateComponentState(new ComponentWithNoCascadingParams());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(componentState);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindCascadingParameters_IfHasNoAncestors_ReturnsNull()
        {
            // Arrange
            var componentState = CreateComponentState(new ComponentWithCascadingParams());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(componentState);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindCascadingParameters_IfHasNoMatchesInAncestors_ReturnsNull()
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
            Assert.Null(result);
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
                match => {
                    Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam1), match.LocalValueName);
                    Assert.Same(states[3].Component, match.ValueSupplier);
                },
                match => {
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
                match => {
                    Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam1), match.LocalValueName);
                    Assert.Same(states[0].Component, match.ValueSupplier);
                },
                match => {
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
            Assert.Collection(result, match => {
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
            Assert.Collection(result, match => {
                Assert.Equal(nameof(ComponentWithGenericCascadingParam<object>.LocalName), match.LocalValueName);
                Assert.Same(states[0].Component, match.ValueSupplier);
            });
        }

        [Fact]
        public void FindCascadingParameters_ComponentRequestsDerivedType_ReturnsNull()
        {
            // Arrange
            var states = CreateAncestry(
                CreateCascadingValueComponent(new CascadingValueTypeBaseClass()),
                new ComponentWithGenericCascadingParam<CascadingValueTypeDerivedClass>());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(states.Last());

            // Assert
            Assert.Null(result);
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
            Assert.Collection(result, match => {
                Assert.Equal(nameof(ComponentWithGenericCascadingParam<object>.LocalName), match.LocalValueName);
                Assert.Same(states[0].Component, match.ValueSupplier);
            });
        }

        [Fact]
        public void FindCascadingParameters_TypeAssignmentIsInvalidForNullValue_ReturnsNull()
        {
            // Arrange
            var states = CreateAncestry(
                CreateCascadingValueComponent((object)null),
                new ComponentWithGenericCascadingParam<ValueType1>());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(states.Last());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindCascadingParameters_SupplierSpecifiesNameButConsumerDoesNot_ReturnsNull()
        {
            // Arrange
            var states = CreateAncestry(
                CreateCascadingValueComponent(new ValueType1(), "MatchOnName"),
                new ComponentWithCascadingParams());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(states.Last());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindCascadingParameters_ConsumerSpecifiesNameButSupplierDoesNot_ReturnsNull()
        {
            // Arrange
            var states = CreateAncestry(
                CreateCascadingValueComponent(new ValueType1()),
                new ComponentWithNamedCascadingParam());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(states.Last());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindCascadingParameters_MismatchingNameButMatchingType_ReturnsNull()
        {
            // Arrange
            var states = CreateAncestry(
                CreateCascadingValueComponent(new ValueType1(), "MismatchName"),
                new ComponentWithNamedCascadingParam());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(states.Last());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindCascadingParameters_MatchingNameButMismatchingType_ReturnsNull()
        {
            // Arrange
            var states = CreateAncestry(
                CreateCascadingValueComponent(new ValueType2(), "MatchOnName"),
                new ComponentWithNamedCascadingParam());

            // Act
            var result = CascadingParameterState.FindCascadingParameters(states.Last());

            // Assert
            Assert.Null(result);
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
            Assert.Collection(result, match => {
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
                match => {
                    Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam1), match.LocalValueName);
                    Assert.Same(states[2].Component, match.ValueSupplier);
                },
                match => {
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
                match => {
                    Assert.Equal(nameof(ComponentWithCascadingParams.CascadingParam1), match.LocalValueName);
                    Assert.Same(states[1].Component, match.ValueSupplier);
                    Assert.Null(match.ValueSupplier.CurrentValue);
                });
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
    }
}
