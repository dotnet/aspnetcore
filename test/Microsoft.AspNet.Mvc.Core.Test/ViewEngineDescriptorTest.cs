// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class ViewEngineDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotViewEngine()
        {
            // Arrange
            var viewEngineType = typeof(IViewEngine).FullName;
            var type = typeof(string);
            var expected = string.Format("The type '{0}' must derive from '{1}'.",
                                         type.FullName, viewEngineType);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new ViewEngineDescriptor(type), "type", expected);
        }

        [Fact]
        public void ConstructorSetsViewEngineType()
        {
            // Arrange
            var type = typeof(TestViewEngine);

            // Act
            var descriptor = new ViewEngineDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.ViewEngineType);
            Assert.Null(descriptor.ViewEngine);
        }

        [Fact]
        public void ConstructorSetsViewEngineAndViewEngineType()
        {
            // Arrange
            var viewEngine = new TestViewEngine();

            // Act
            var descriptor = new ViewEngineDescriptor(viewEngine);

            // Assert
            Assert.Same(viewEngine, descriptor.ViewEngine);
            Assert.Equal(viewEngine.GetType(), descriptor.ViewEngineType);
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