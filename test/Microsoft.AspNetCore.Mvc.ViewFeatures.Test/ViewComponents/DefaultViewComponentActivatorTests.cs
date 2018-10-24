// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    public class DefaultViewComponentActivatorTests
    {
        [Fact]
        public void DefaultViewComponentActivator_ActivatesViewComponentContext()
        {
            // Arrange
            var expectedInstance = new TestViewComponent();

            var typeActivator = new Mock<ITypeActivatorCache>();
            typeActivator
                .Setup(ta => ta.CreateInstance<object>(It.IsAny<IServiceProvider>(), It.IsAny<Type>()))
                .Returns(expectedInstance);

            var activator = new DefaultViewComponentActivator(typeActivator.Object);

            var context = CreateContext(typeof(TestViewComponent));
            expectedInstance.ViewComponentContext = context;

            // Act
            var instance = activator.Create(context) as ViewComponent;

            // Assert
            Assert.NotNull(instance);
            Assert.Same(context, instance.ViewComponentContext);
        }

        [Fact]
        public void DefaultViewComponentActivator_ActivatesViewComponentContext_IgnoresNonPublic()
        {
            // Arrange
            var expectedInstance = new VisibilityViewComponent();

            var typeActivator = new Mock<ITypeActivatorCache>();
            typeActivator
                .Setup(ta => ta.CreateInstance<object>(It.IsAny<IServiceProvider>(), It.IsAny<Type>()))
                .Returns(expectedInstance);

            var activator = new DefaultViewComponentActivator(typeActivator.Object);

            var context = CreateContext(typeof(VisibilityViewComponent));
            expectedInstance.ViewComponentContext = context;

            // Act
            var instance = activator.Create(context) as VisibilityViewComponent;

            // Assert
            Assert.NotNull(instance);
            Assert.Same(context, instance.ViewComponentContext);
            Assert.Null(instance.C);
        }

        private static ViewComponentContext CreateContext(Type componentType)
        {
            return new ViewComponentContext
            {
                ViewComponentDescriptor = new ViewComponentDescriptor
                {
                    TypeInfo = componentType.GetTypeInfo()
                },
                ViewContext = new ViewContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = Mock.Of<IServiceProvider>()
                    }
                }
            };
        }

        private class TestViewComponent : ViewComponent
        {
            public Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class VisibilityViewComponent : ViewComponent
        {
            [ViewComponentContext]
            protected internal ViewComponentContext C { get; set; }
        }
    }
}
