// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.AspNet.Mvc.Rendering;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ViewEngineDescriptorExtensionTest
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void Insert_WithType_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ViewEngineDescriptor>
            {
                new ViewEngineDescriptor(Mock.Of<IViewEngine>()),
                new ViewEngineDescriptor(Mock.Of<IViewEngine>())
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, typeof(IViewEngine)));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(3)]
        public void Insert_WithInstance_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<ViewEngineDescriptor>
            {
                new ViewEngineDescriptor(Mock.Of<IViewEngine>()),
                new ViewEngineDescriptor(Mock.Of<IViewEngine>())
            };
            var viewEngine = Mock.Of<IViewEngine>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, viewEngine));
        }

        [InlineData]
        public void ViewEngineDescriptors_AddsTypesAndInstances()
        {
            // Arrange
            var viewEngine = Mock.Of<IViewEngine>();
            var type = typeof(TestViewEngine);
            var collection = new List<ViewEngineDescriptor>();

            // Act
            collection.Add(viewEngine);
            collection.Insert(0, type);

            // Assert
            Assert.Equal(2, collection.Count);
            Assert.IsType<TestViewEngine>(collection[0].Instance);
            Assert.Same(viewEngine, collection[0].Instance);
        }

        private class TestViewEngine : IViewEngine
        {
            public ViewEngineResult FindPartialView(ActionContext context, string partialViewName)
            {
                throw new NotImplementedException();
            }

            public ViewEngineResult FindView(ActionContext context, string viewName)
            {
                throw new NotImplementedException();
            }
        }
    }
}
