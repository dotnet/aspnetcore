// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test
{
    public class ParameterCollectionTest
    {
        [Fact]
        public void CanInitializeUsingComponentWithNoDescendants()
        {
            // Arrange
            var frames = new[]
            {
                RenderTreeFrame.ChildComponent(0, typeof(FakeComponent)).WithComponentSubtreeLength(1)
            };
            var parameterCollection = new ParameterCollection(frames, 0);

            // Assert
            Assert.Empty(ToEnumerable(parameterCollection));
        }

        [Fact]
        public void CanInitializeUsingElementWithNoDescendants()
        {
            // Arrange
            var frames = new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(1)
            };
            var parameterCollection = new ParameterCollection(frames, 0);

            // Assert
            Assert.Empty(ToEnumerable(parameterCollection));
        }

        [Fact]
        public void EnumerationStopsAtEndOfOwnerDescendants()
        {
            // Arrange
            var attribute1Value = new object();
            var attribute2Value = new object();
            var frames = new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(3),
                RenderTreeFrame.Attribute(1, "attribute 1", attribute1Value),
                RenderTreeFrame.Attribute(2, "attribute 2", attribute2Value),
                // Although RenderTreeBuilder doesn't let you add orphaned attributes like this,
                // still want to verify that ParameterCollection doesn't attempt to read past the
                // end of the owner's descendants
                RenderTreeFrame.Attribute(3, "orphaned attribute", "value")
            };
            var parameterCollection = new ParameterCollection(frames, 0);

            // Assert
            Assert.Collection(ToEnumerable(parameterCollection),
                AssertParameter("attribute 1", attribute1Value),
                AssertParameter("attribute 2", attribute2Value));
        }

        [Fact]
        public void EnumerationStopsAtEndOfOwnerAttributes()
        {
            // Arrange
            var attribute1Value = new object();
            var attribute2Value = new object();
            var frames = new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(3),
                RenderTreeFrame.Attribute(1, "attribute 1", attribute1Value),
                RenderTreeFrame.Attribute(2, "attribute 2", attribute2Value),
                RenderTreeFrame.Element(3, "child element").WithElementSubtreeLength(2),
                RenderTreeFrame.Attribute(4, "child attribute", "some value")
            };
            var parameterCollection = new ParameterCollection(frames, 0);

            // Assert
            Assert.Collection(ToEnumerable(parameterCollection),
                AssertParameter("attribute 1", attribute1Value),
                AssertParameter("attribute 2", attribute2Value));
        }

        [Fact]
        public void CanTryGetNonExistingValue()
        {
            // Arrange
            var parameterCollection = new ParameterCollection(new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
                RenderTreeFrame.Attribute(1, "some other entry", new object())
            }, 0);

            // Act
            var didFind = parameterCollection.TryGetValue<string>("nonexisting entry", out var value);

            // Assert
            Assert.False(didFind);
            Assert.Null(value);
        }

        [Fact]
        public void CanTryGetExistingValueWithCorrectType()
        {
            // Arrange
            var parameterCollection = new ParameterCollection(new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
                RenderTreeFrame.Attribute(1, "my entry", "hello")
            }, 0);

            // Act
            var didFind = parameterCollection.TryGetValue<string>("my entry", out var value);

            // Assert
            Assert.True(didFind);
            Assert.Equal("hello", value);
        }

        [Fact]
        public void CanGetValueOrDefault_WithExistingValue()
        {
            // Arrange
            var myEntryValue = new object();
            var parameterCollection = new ParameterCollection(new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
                RenderTreeFrame.Attribute(1, "my entry", myEntryValue),
                RenderTreeFrame.Attribute(1, "my other entry", new object())
            }, 0);

            // Act
            var result = parameterCollection.GetValueOrDefault<object>("my entry");

            // Assert
            Assert.Same(myEntryValue, result);
        }

        [Fact]
        public void CanGetValueOrDefault_WithNonExistingValue()
        {
            // Arrange
            var parameterCollection = new ParameterCollection(new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
                RenderTreeFrame.Attribute(1, "some other entry", new object())
            }, 0);

            // Act
            var result = parameterCollection.GetValueOrDefault<DateTime>("nonexisting entry");

            // Assert
            Assert.Equal(default, result);
        }

        [Fact]
        public void CanGetValueOrDefault_WithNonExistingValueAndExplicitDefault()
        {
            // Arrange
            var explicitDefaultValue = new DateTime(2018, 3, 20);
            var parameterCollection = new ParameterCollection(new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
                RenderTreeFrame.Attribute(1, "some other entry", new object())
            }, 0);

            // Act
            var result = parameterCollection.GetValueOrDefault("nonexisting entry", explicitDefaultValue);

            // Assert
            Assert.Equal(explicitDefaultValue, result);
        }

        [Fact]
        public void ThrowsIfTryGetExistingValueWithIncorrectType()
        {
            // Arrange
            var parameterCollection = new ParameterCollection(new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(2),
                RenderTreeFrame.Attribute(1, "my entry", "hello")
            }, 0);

            // Act/Assert
            Assert.Throws<InvalidCastException>(() =>
            {
                parameterCollection.TryGetValue<bool>("my entry", out var value);
            });
        }

        [Fact]
        public void CanConvertToReadOnlyDictionary()
        {
            // Arrange
            var entry2Value = new object();
            var parameterCollection = new ParameterCollection(new[]
            {
                RenderTreeFrame.Element(0, "some element").WithElementSubtreeLength(3),
                RenderTreeFrame.Attribute(0, "entry 1", "value 1"),
                RenderTreeFrame.Attribute(0, "entry 2", entry2Value),
            }, 0);

            // Act
            IReadOnlyDictionary<string, object> dict = parameterCollection.ToDictionary();

            // Assert
            Assert.Collection(dict,
                entry =>
                {
                    Assert.Equal("entry 1", entry.Key);
                    Assert.Equal("value 1", entry.Value);
                },
                entry =>
                {
                    Assert.Equal("entry 2", entry.Key);
                    Assert.Same(entry2Value, entry.Value);
                });
        }

        private Action<Parameter> AssertParameter(string expectedName, object expectedValue)
        {
            return parameter =>
            {
                Assert.Equal(expectedName, parameter.Name);
                Assert.Same(expectedValue, parameter.Value);
            };
        }

        public IEnumerable<Parameter> ToEnumerable(ParameterCollection parameterCollection)
        {
            foreach (var item in parameterCollection)
            {
                yield return item;
            }
        }

        private class FakeComponent : IComponent
        {
            public void Init(RenderHandle renderHandle)
                => throw new NotImplementedException();

            public void SetParameters(ParameterCollection parameters)
                => throw new NotImplementedException();
        }
    }
}
