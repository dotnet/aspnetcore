// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ViewEngineDescriptorExtensionsTest
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
            Assert.IsType<TestViewEngine>(collection[0].ViewEngine);
            Assert.Same(viewEngine, collection[0].ViewEngineType);
        }

        [Fact]
        public void InputviewEngines_InstanceOf_ThrowsInvalidOperationExceptionIfMoreThanOnceInstance()
        {
            // Arrange
            var viewEngines = new MvcOptions().ViewEngines;
            viewEngines.Add(new TestViewEngine());
            viewEngines.Add(Mock.Of<IViewEngine>());
            viewEngines.Add(new TestViewEngine());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => viewEngines.InstanceOf<TestViewEngine>());
        }

        [Fact]
        public void InputviewEngines_InstanceOf_ThrowsInvalidOperationExceptionIfNoInstance()
        {
            // Arrange
            var viewEngines = new MvcOptions().ViewEngines;
            viewEngines.Add(Mock.Of<IViewEngine>());
            viewEngines.Add(typeof(TestViewEngine));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => viewEngines.InstanceOf<TestViewEngine>());
        }

        [Fact]
        public void InputviewEngines_InstanceOf_ReturnsInstanceOfIInputFormatterIfOneExists()
        {
            // Arrange
            var viewEngines = new MvcOptions().ViewEngines;
            viewEngines.Add(Mock.Of<IViewEngine>());
            var testEngine = new TestViewEngine();
            viewEngines.Add(testEngine);
            viewEngines.Add(typeof(TestViewEngine));

            // Act
            var formatter = viewEngines.InstanceOf<TestViewEngine>();

            // Assert
            Assert.NotNull(formatter);
            Assert.IsType<TestViewEngine>(formatter);
            Assert.Same(testEngine, formatter);
        }

        [Fact]
        public void InputviewEngines_InstanceOfOrDefault_ThrowsInvalidOperationExceptionIfMoreThanOnceInstance()
        {
            // Arrange
            var viewEngines = new MvcOptions().ViewEngines;
            viewEngines.Add(new TestViewEngine());
            viewEngines.Add(Mock.Of<IViewEngine>());
            viewEngines.Add(new TestViewEngine());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => viewEngines.InstanceOfOrDefault<TestViewEngine>());
        }

        [Fact]
        public void InputviewEngines_InstanceOfOrDefault_ReturnsNullIfNoInstance()
        {
            // Arrange
            var viewEngines = new MvcOptions().ViewEngines;
            viewEngines.Add(Mock.Of<IViewEngine>());
            viewEngines.Add(typeof(TestViewEngine));

            // Act
            var formatter = viewEngines.InstanceOfOrDefault<TestViewEngine>();

            // Assert
            Assert.Null(formatter);
        }

        [Fact]
        public void InputviewEngines_InstanceOfOrDefault_ReturnsInstanceOfIInputFormatterIfOneExists()
        {
            // Arrange
            var viewEngines = new MvcOptions().ViewEngines;
            viewEngines.Add(Mock.Of<IViewEngine>());
            viewEngines.Add(typeof(TestViewEngine));
            var testEngine = new TestViewEngine();
            viewEngines.Add(testEngine);

            // Act
            var formatter = viewEngines.InstanceOfOrDefault<TestViewEngine>();

            // Assert
            Assert.NotNull(formatter);
            Assert.IsType<TestViewEngine>(formatter);
            Assert.Same(testEngine, formatter);
        }

        [Fact]
        public void InputviewEngines_InstancesOf_ReturnsEmptyCollectionIfNoneExist()
        {
            // Arrange
            var viewEngines = new MvcOptions().ViewEngines;
            viewEngines.Add(Mock.Of<IViewEngine>());
            viewEngines.Add(typeof(TestViewEngine));

            // Act
            var result = viewEngines.InstancesOf<TestViewEngine>();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void InputviewEngines_InstancesOf_ReturnsNonEmptyCollectionIfSomeExist()
        {
            // Arrange
            var viewEngines = new MvcOptions().ViewEngines;
            viewEngines.Add(typeof(TestViewEngine));
            var viewEngine1 = new TestViewEngine();
            var viewEngine2 = Mock.Of<IViewEngine>();
            var viewEngine3 = new TestViewEngine();
            var viewEngine4 = Mock.Of<IViewEngine>();
            viewEngines.Add(viewEngine1);
            viewEngines.Add(viewEngine2);
            viewEngines.Add(viewEngine3);
            viewEngines.Add(viewEngine4);

            var expectedviewEngines = new List<TestViewEngine> { viewEngine1, viewEngine3 };

            // Act
            var result = viewEngines.InstancesOf<TestViewEngine>().ToList();

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(result, expectedviewEngines);
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
