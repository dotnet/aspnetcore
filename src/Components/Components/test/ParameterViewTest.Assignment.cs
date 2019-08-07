// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public partial class ParameterViewTest
    {
        [Fact]
        public void IncomingParameterMatchesAnnotatedPrivateProperty_SetsValue()
        {
            // Arrange
            var someObject = new object();
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasInstanceProperties.IntProp), 123 },
                { nameof(HasInstanceProperties.StringProp), "Hello" },
                { HasInstanceProperties.ObjectPropName, someObject },
            }.Build();
            var target = new HasInstanceProperties();

            // Act
            parameters.SetParameterProperties(target);

            // Assert
            Assert.Equal(123, target.IntProp);
            Assert.Equal("Hello", target.StringProp);
            Assert.Same(someObject, target.ObjectPropCurrentValue);
        }

        [Fact]
        public void IncomingParameterMatchesDeclaredParameterCaseInsensitively_SetsValue()
        {
            // Arrange
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasInstanceProperties.IntProp).ToLowerInvariant(), 123 }
            }.Build();
            var target = new HasInstanceProperties();

            // Act
            parameters.SetParameterProperties(target);

            // Assert
            Assert.Equal(123, target.IntProp);
        }

        [Fact]
        public void IncomingParameterMatchesInheritedDeclaredParameter_SetsValue()
        {
            // Arrange
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasInheritedProperties.IntProp), 123 },
                { nameof(HasInheritedProperties.DerivedClassIntProp), 456 },
            }.Build();
            var target = new HasInheritedProperties();

            // Act
            parameters.SetParameterProperties(target);

            // Assert
            Assert.Equal(123, target.IntProp);
            Assert.Equal(456, target.DerivedClassIntProp);
        }

        [Fact]
        public void NoIncomingParameterMatchesDeclaredParameter_LeavesValueUnchanged()
        {
            // Arrange
            var existingObjectValue = new object();
            var target = new HasInstanceProperties
            {
                IntProp = 456,
                StringProp = "Existing value",
                ObjectPropCurrentValue = existingObjectValue
            };

            var parameters = new ParameterViewBuilder().Build();

            // Act
            parameters.SetParameterProperties(target);

            // Assert
            Assert.Equal(456, target.IntProp);
            Assert.Equal("Existing value", target.StringProp);
            Assert.Same(existingObjectValue, target.ObjectPropCurrentValue);
        }

        [Fact]
        public void IncomingParameterMatchesNoDeclaredParameter_Throws()
        {
            // Arrange
            var target = new HasPropertyWithoutParameterAttribute();
            var parameters = new ParameterViewBuilder
            {
                { "AnyOtherKey", 123 },
            }.Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"Object of type '{typeof(HasPropertyWithoutParameterAttribute).FullName}' does not have a property " +
                $"matching the name 'AnyOtherKey'.",
                ex.Message);
        }

        [Fact]
        public void IncomingParameterMatchesPropertyNotDeclaredAsParameter_Throws()
        {
            // Arrange
            var target = new HasPropertyWithoutParameterAttribute();
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasPropertyWithoutParameterAttribute.IntProp), 123 },
            }.Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(default, target.IntProp);
            Assert.Equal(
                $"Object of type '{typeof(HasPropertyWithoutParameterAttribute).FullName}' has a property matching the name '{nameof(HasPropertyWithoutParameterAttribute.IntProp)}', " +
                $"but it does not have [{nameof(ParameterAttribute)}] or [{nameof(CascadingParameterAttribute)}] applied.",
                ex.Message);
        }

        [Fact]
        public void SettingCaptureUnmatchedValuesParameterExplicitlyWorks()
        {
            // Arrange
            var target = new HasCaptureUnmatchedValuesProperty();
            var value = new Dictionary<string, object>();
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasCaptureUnmatchedValuesProperty.CaptureUnmatchedValues), value },
            }.Build();

            // Act
            parameters.SetParameterProperties(target);

            // Assert
            Assert.Same(value, target.CaptureUnmatchedValues);
        }

        [Fact]
        public void SettingCaptureUnmatchedValuesParameterWithUnmatchedValuesWorks()
        {
            // Arrange
            var target = new HasCaptureUnmatchedValuesProperty();
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasCaptureUnmatchedValuesProperty.StringProp), "hi" },
                { "test1", 123 },
                { "test2", 456 },
            }.Build();

            // Act
            parameters.SetParameterProperties(target);

            // Assert
            Assert.Equal("hi", target.StringProp);
            Assert.Collection(
                target.CaptureUnmatchedValues.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("test1", kvp.Key);
                    Assert.Equal(123, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("test2", kvp.Key);
                    Assert.Equal(456, kvp.Value);
                });
        }

        [Fact]
        public void SettingCaptureUnmatchedValuesParameterExplicitlyAndImplicitly_Throws()
        {
            // Arrange
            var target = new HasCaptureUnmatchedValuesProperty();
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasCaptureUnmatchedValuesProperty.CaptureUnmatchedValues), new Dictionary<string, object>() },
                { "test1", 123 },
                { "test2", 456 },
            }.Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"The property '{nameof(HasCaptureUnmatchedValuesProperty.CaptureUnmatchedValues)}' on component type '{typeof(HasCaptureUnmatchedValuesProperty).FullName}' cannot be set explicitly when " +
                $"also used to capture unmatched values. Unmatched values:" + Environment.NewLine +
                $"test1" + Environment.NewLine +
                $"test2",
                ex.Message);
        }

        [Fact]
        public void SettingCaptureUnmatchedValuesParameterExplicitlyAndImplicitly_ReverseOrder_Throws()
        {
            // Arrange
            var target = new HasCaptureUnmatchedValuesProperty();
            var parameters = new ParameterViewBuilder
            {
                { "test2", 456 },
                { "test1", 123 },
                { nameof(HasCaptureUnmatchedValuesProperty.CaptureUnmatchedValues), new Dictionary<string, object>() },
            }.Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"The property '{nameof(HasCaptureUnmatchedValuesProperty.CaptureUnmatchedValues)}' on component type '{typeof(HasCaptureUnmatchedValuesProperty).FullName}' cannot be set explicitly when " +
                $"also used to capture unmatched values. Unmatched values:" + Environment.NewLine +
                $"test1" + Environment.NewLine +
                $"test2",
                ex.Message);
        }

        [Fact]
        public void HasDuplicateCaptureUnmatchedValuesParameters_Throws()
        {
            // Arrange
            var target = new HasDupliateCaptureUnmatchedValuesProperty();
            var parameters = new ParameterViewBuilder().Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"Multiple properties were found on component type '{typeof(HasDupliateCaptureUnmatchedValuesProperty).FullName}' " +
                $"with '{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureUnmatchedValues)}'. " +
                $"Only a single property per type can use '{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureUnmatchedValues)}'. " +
                $"Properties:" + Environment.NewLine +
                $"{nameof(HasDupliateCaptureUnmatchedValuesProperty.CaptureUnmatchedValuesProp1)}" + Environment.NewLine +
                $"{nameof(HasDupliateCaptureUnmatchedValuesProperty.CaptureUnmatchedValuesProp2)}",
                ex.Message);
        }

        [Fact]
        public void HasCaptureUnmatchedValuesParameteterWithWrongType_Throws()
        {
            // Arrange
            var target = new HasWrongTypeCaptureUnmatchedValuesProperty();
            var parameters = new ParameterViewBuilder().Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"The property '{nameof(HasWrongTypeCaptureUnmatchedValuesProperty.CaptureUnmatchedValuesProp)}' on component type '{typeof(HasWrongTypeCaptureUnmatchedValuesProperty).FullName}' cannot be used with " +
                $"'{nameof(ParameterAttribute)}.{nameof(ParameterAttribute.CaptureUnmatchedValues)}' because it has the wrong type. " +
                $"The property must be assignable from 'Dictionary<string, object>'.",
                ex.Message);
        }

        [Fact]
        public void IncomingParameterValueMismatchesDeclaredParameterType_Throws()
        {
            // Arrange
            var someObject = new object();
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasInstanceProperties.IntProp), "string value" },
            }.Build();
            var target = new HasInstanceProperties();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"Unable to set property '{nameof(HasInstanceProperties.IntProp)}' on object of " +
                $"type '{typeof(HasInstanceProperties).FullName}'. The error was: {ex.InnerException.Message}",
                ex.Message);
        }

        [Fact]
        public void PropertyExplicitSetterException_Throws()
        {
            // Arrange
            var target = new HasPropertyWhoseSetterThrows();
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasPropertyWhoseSetterThrows.StringProp), "anything" },
            }.Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"Unable to set property '{nameof(HasPropertyWhoseSetterThrows.StringProp)}' on object of " +
                $"type '{typeof(HasPropertyWhoseSetterThrows).FullName}'. The error was: {ex.InnerException.Message}",
                ex.Message);
        }

        [Fact]
        public void DeclaredParametersVaryOnlyByCase_Throws()
        {
            // Arrange
            var parameters = new ParameterViewBuilder().Build();
            var target = new HasParametersVaryingOnlyByCase();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() =>
                parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"The type '{typeof(HasParametersVaryingOnlyByCase).FullName}' declares more than one parameter matching the " +
                $"name '{nameof(HasParametersVaryingOnlyByCase.MyValue).ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.",
                ex.Message);
        }

        [Fact]
        public void DeclaredParameterClashesWithInheritedParameter_Throws()
        {
            // Even when the developer uses 'new' to shadow an inherited property, this is not
            // an allowed scenario because there would be no way for the consumer to specify
            // both property values, and it's no good leaving the shadowed one unset because the
            // base class can legitimately depend on it for correct functioning.

            // Arrange
            var parameters = new ParameterViewBuilder().Build();
            var target = new HasParameterClashingWithInherited();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() =>
                parameters.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"The type '{typeof(HasParameterClashingWithInherited).FullName}' declares more than one parameter matching the " +
                $"name '{nameof(HasParameterClashingWithInherited.IntProp).ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.",
                ex.Message);
        }

        [Fact]
        public void SupplyingNullWritesDefaultForType()
        {
            // Arrange
            var parameters = new ParameterViewBuilder
            {
                { nameof(HasInstanceProperties.IntProp), null },
                { nameof(HasInstanceProperties.StringProp), null },
            }.Build();
            var target = new HasInstanceProperties { IntProp = 123, StringProp = "Hello" };

            // Act
            parameters.SetParameterProperties(target);

            // Assert
            Assert.Equal(0, target.IntProp);
            Assert.Null(target.StringProp);
        }

        class HasInstanceProperties
        {
            // "internal" to show we're not requiring public accessors, but also
            // to keep the assertions simple in the tests

            [Parameter] public int IntProp { get; set; }
            [Parameter] public string StringProp { get; set; }

            // Also a truly private one to show there's nothing special about 'internal'
            [Parameter] public object ObjectProp { get; set; }

            public static string ObjectPropName => nameof(ObjectProp);
            public object ObjectPropCurrentValue
            {
                get => ObjectProp;
                set => ObjectProp = value;
            }
        }

        class HasPropertyWithoutParameterAttribute
        {
            internal int IntProp { get; set; }
        }

        class HasPropertyWhoseSetterThrows
        {
            [Parameter]
            public string StringProp
            {
                get => string.Empty;
                set => throw new InvalidOperationException("This setter throws");
            }
        }

        class HasInheritedProperties : HasInstanceProperties
        {
            [Parameter] public int DerivedClassIntProp { get; set; }
        }

        class HasParametersVaryingOnlyByCase
        {
            [Parameter] public object MyValue { get; set; }
            [Parameter] public object Myvalue { get; set; }
        }

        class HasParameterClashingWithInherited : HasInstanceProperties
        {
            [Parameter] public new int IntProp { get; set; }
        }

        class HasCaptureUnmatchedValuesProperty
        {
            [Parameter] public int IntProp { get; set; }
            [Parameter] public string StringProp { get; set; }
            [Parameter] public object ObjectProp { get; set; }
            [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object> CaptureUnmatchedValues { get; set; }
        }

        class HasDupliateCaptureUnmatchedValuesProperty
        {
            [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> CaptureUnmatchedValuesProp1 { get; set; }
            [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object> CaptureUnmatchedValuesProp2 { get; set; }
        }

        class HasWrongTypeCaptureUnmatchedValuesProperty
        {
            [Parameter(CaptureUnmatchedValues = true)] public KeyValuePair<string, object>[] CaptureUnmatchedValuesProp { get; set; }
        }

        class ParameterViewBuilder : IEnumerable
        {
            private readonly List<(string Name, object Value)> _keyValuePairs
                = new List<(string, object)>();

            public void Add(string name, object value)
                => _keyValuePairs.Add((name, value));

            public IEnumerator GetEnumerator()
                => throw new NotImplementedException();

            public ParameterView Build()
            {
                var builder = new RenderTreeBuilder();
                builder.OpenComponent<FakeComponent>(0);
                foreach (var kvp in _keyValuePairs)
                {
                    builder.AddAttribute(1, kvp.Name, kvp.Value);
                }
                builder.CloseComponent();
                return new ParameterView(builder.GetFrames().Array, ownerIndex: 0);
            }
        }
    }
}
