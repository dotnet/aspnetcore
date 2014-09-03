// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.OptionDescriptors
{
    public class ViewLocationExpanderDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotViewLocationExpander()
        {
            // Arrange
            var viewEngineType = typeof(IViewLocationExpander).FullName;
            var type = typeof(string);
            var expected = string.Format("The type '{0}' must derive from '{1}'.",
                                         type.FullName, viewEngineType);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new ViewLocationExpanderDescriptor(type), "type", expected);
        }

        [Fact]
        public void ConstructorSetsViewLocationExpanderType()
        {
            // Arrange
            var type = typeof(TestViewLocationExpander);

            // Act
            var descriptor = new ViewLocationExpanderDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.OptionType);
            Assert.Null(descriptor.Instance);
        }

        [Fact]
        public void ConstructorSetsViewLocationExpanderAndType()
        {
            // Arrange
            var expander = new TestViewLocationExpander();

            // Act
            var descriptor = new ViewLocationExpanderDescriptor(expander);

            // Assert
            Assert.Same(expander, descriptor.Instance);
            Assert.Equal(expander.GetType(), descriptor.OptionType);
        }

        private class TestViewLocationExpander : IViewLocationExpander
        {
            public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
                                                           IEnumerable<string> viewLocations)
            {
                throw new NotImplementedException();
            }

            public void PopulateValues(ViewLocationExpanderContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}