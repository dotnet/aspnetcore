// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
{
    public class ParameterCollectionAssignmentExtensionsTest
    {
        [Fact]
        public void IncomingParameterMatchesAnnotatedPrivateProperty_SetsValue()
        {
            // Arrange
            var someObject = new object();
            var parameterCollection = new ParameterCollectionBuilder
            {
                { nameof(HasInstanceProperties.IntProp), 123 },
                { nameof(HasInstanceProperties.StringProp), "Hello" },
                { HasInstanceProperties.ObjectPropName, someObject },
            }.Build();
            var target = new HasInstanceProperties();

            // Act
            parameterCollection.SetParameterProperties(target);

            // Assert
            Assert.Equal(123, target.IntProp);
            Assert.Equal("Hello", target.StringProp);
            Assert.Same(someObject, target.ObjectPropCurrentValue);
        }

        [Fact]
        public void IncomingParameterMatchesDeclaredParameterCaseInsensitively_SetsValue()
        {
            // Arrange
            var parameterCollection = new ParameterCollectionBuilder
            {
                { nameof(HasInstanceProperties.IntProp).ToLowerInvariant(), 123 }
            }.Build();
            var target = new HasInstanceProperties();

            // Act
            parameterCollection.SetParameterProperties(target);

            // Assert
            Assert.Equal(123, target.IntProp);
        }

        [Fact]
        public void IncomingParameterMatchesInheritedDeclaredParameter_SetsValue()
        {
            // Arrange
            var parameterCollection = new ParameterCollectionBuilder
            {
                { nameof(HasInheritedProperties.IntProp), 123 },
                { nameof(HasInheritedProperties.DerivedClassIntProp), 456 },
            }.Build();
            var target = new HasInheritedProperties();

            // Act
            parameterCollection.SetParameterProperties(target);

            // Assert
            Assert.Equal(123, target.IntProp);
            Assert.Equal(456, target.DerivedClassIntProp);
        }

        [Fact]
        public void NoIncomingParameterMatchesDeclaredParameter_SetValuesDefault()
        {
            // Arrange
            var existingObjectValue = new object();
            var target = new HasInstanceProperties
            {
                IntProp = 456,
                StringProp = "Existing value",
                ObjectPropCurrentValue = existingObjectValue
            };

            var parameterCollection = new ParameterCollectionBuilder().Build();

            // Act
            parameterCollection.SetParameterProperties(target);

            // Assert
            Assert.Equal(0, target.IntProp);
            Assert.Null(target.StringProp);
            Assert.Null(target.ObjectPropCurrentValue);
        }

        [Fact]
        public void IncomingParameterMatchesNoDeclaredParameter_Throws()
        {
            // Arrange
            var target = new HasPropertyWithoutParameterAttribute();
            var parameterCollection = new ParameterCollectionBuilder
            {
                { "AnyOtherKey", 123 },
            }.Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => parameterCollection.SetParameterProperties(target));

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
            var parameterCollection = new ParameterCollectionBuilder
            {
                { nameof(HasPropertyWithoutParameterAttribute.IntProp), 123 },
            }.Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => parameterCollection.SetParameterProperties(target));

            // Assert
            Assert.Equal(default, target.IntProp);
            Assert.Equal(
                $"Object of type '{typeof(HasPropertyWithoutParameterAttribute).FullName}' has a property matching the name '{nameof(HasPropertyWithoutParameterAttribute.IntProp)}', " +
                $"but it does not have [{nameof(ParameterAttribute)}] or [{nameof(CascadingParameterAttribute)}] applied.",
                ex.Message);
        }

        [Fact]
        public void IncomingParameterValueMismatchesDeclaredParameterType_Throws()
        {
            // Arrange
            var someObject = new object();
            var parameterCollection = new ParameterCollectionBuilder
            {
                { nameof(HasInstanceProperties.IntProp), "string value" },
            }.Build();
            var target = new HasInstanceProperties();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => parameterCollection.SetParameterProperties(target));

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
            var parameterCollection = new ParameterCollectionBuilder
            {
                { nameof(HasPropertyWhoseSetterThrows.StringProp), "anything" },
            }.Build();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => parameterCollection.SetParameterProperties(target));

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
            var parameterCollection = new ParameterCollectionBuilder().Build();
            var target = new HasParametersVaryingOnlyByCase();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() =>
                parameterCollection.SetParameterProperties(target));

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
            var parameterCollection = new ParameterCollectionBuilder().Build();
            var target = new HasParameterClashingWithInherited();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() =>
                parameterCollection.SetParameterProperties(target));

            // Assert
            Assert.Equal(
                $"The type '{typeof(HasParameterClashingWithInherited).FullName}' declares more than one parameter matching the " +
                $"name '{nameof(HasParameterClashingWithInherited.IntProp).ToLowerInvariant()}'. Parameter names are case-insensitive and must be unique.",
                ex.Message);
        }


        class HasInstanceProperties
        {
            // "internal" to show we're not requiring public accessors, but also
            // to keep the assertions simple in the tests

            [Parameter] internal int IntProp { get; set; }
            [Parameter] internal string StringProp { get; set; }

            // Also a truly private one to show there's nothing special about 'internal'
            [Parameter] private object ObjectProp { get; set; }

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
            internal string StringProp
            {
                get => string.Empty;
                set => throw new InvalidOperationException("This setter throws");
            }
        }

        class HasInheritedProperties : HasInstanceProperties
        {
            [Parameter] internal int DerivedClassIntProp { get; set; }
        }

        class HasParametersVaryingOnlyByCase
        {
            [Parameter] internal object MyValue { get; set; }
            [Parameter] internal object Myvalue { get; set; }
        }

        class HasParameterClashingWithInherited : HasInstanceProperties
        {
            [Parameter] new int IntProp { get; set; }
        }

        class ParameterCollectionBuilder : IEnumerable
        {
            private readonly List<(string Name, object Value)> _keyValuePairs
                = new List<(string, object)>();

            public void Add(string name, object value)
                => _keyValuePairs.Add((name, value));

            public IEnumerator GetEnumerator()
                => throw new NotImplementedException();

            public ParameterCollection Build()
            {
                var builder = new RenderTreeBuilder(new TestRenderer());
                builder.OpenComponent<FakeComponent>(0);
                foreach (var kvp in _keyValuePairs)
                {
                    builder.AddAttribute(1, kvp.Name, kvp.Value);
                }
                builder.CloseComponent();
                return new ParameterCollection(builder.GetFrames().Array, ownerIndex: 0);
            }
        }

        class FakeComponent : IComponent
        {
            public void Init(RenderHandle renderHandle)
                => throw new NotImplementedException();

            public void SetParameters(ParameterCollection parameters)
                => throw new NotImplementedException();
        }
    }
}
