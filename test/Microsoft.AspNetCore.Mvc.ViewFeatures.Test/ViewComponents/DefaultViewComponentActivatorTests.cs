// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Internal;
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

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(OpenGenericType<>))]
        [InlineData(typeof(AbstractType))]
        [InlineData(typeof(InterfaceType))]
        public void Create_ThrowsIfControllerCannotBeActivated(Type type)
        {
            // Arrange
            var actionDescriptor = new ViewComponentDescriptor
            {
                TypeInfo = type.GetTypeInfo()
            };

            var context = new ViewComponentContext
            {
                ViewComponentDescriptor = actionDescriptor,
                ViewContext = new ViewContext
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        RequestServices = Mock.Of<IServiceProvider>()
                    },
                }
            };

            var activator = new DefaultViewComponentActivator(new TypeActivatorCache());

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => activator.Create(context));
            Assert.Equal(
                $"The type '{type.FullName}' cannot be activated by '{typeof(DefaultViewComponentActivator).FullName}' " +
                "because it is either a value type, an interface, an abstract class or an open generic type.",
                exception.Message);
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

        private class OpenGenericType<T> : Controller
        {
        }

        private abstract class AbstractType : Controller
        {
        }

        private interface InterfaceType
        {
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
