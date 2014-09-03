// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Razor.OptionDescriptors;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class ViewLocationExpanderDescriptorExtensionsTest
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void Insert_WithType_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ViewLocationExpanderDescriptor>
            {
                new ViewLocationExpanderDescriptor(Mock.Of<IViewLocationExpander>()),
                new ViewLocationExpanderDescriptor(Mock.Of<IViewLocationExpander>())
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index",
                                                       () => collection.Insert(index, typeof(IViewLocationExpander)));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(3)]
        public void Insert_WithInstance_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ViewLocationExpanderDescriptor>
            {
                new ViewLocationExpanderDescriptor(Mock.Of<IViewLocationExpander>()),
                new ViewLocationExpanderDescriptor(Mock.Of<IViewLocationExpander>())
            };
            var expander = Mock.Of<IViewLocationExpander>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, expander));
        }

        [InlineData]
        public void ViewLocationExpanderDescriptors_AddsTypesAndInstances()
        {
            // Arrange
            var expander = Mock.Of<IViewLocationExpander>();
            var type = typeof(TestViewLocationExpander);
            var collection = new List<ViewLocationExpanderDescriptor>();

            // Act
            collection.Add(expander);
            collection.Insert(0, type);

            // Assert
            Assert.Equal(2, collection.Count);
            Assert.IsType<TestViewLocationExpander>(collection[0].Instance);
            Assert.Same(expander, collection[0].Instance);
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
