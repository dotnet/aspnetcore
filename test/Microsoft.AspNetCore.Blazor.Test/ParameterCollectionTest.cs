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
                RenderTreeFrame.ChildComponent<FakeComponent>(0).WithComponentSubtreeLength(1)
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
