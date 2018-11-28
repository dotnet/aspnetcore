// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageModelActivatorProviderTest
    {
        [Fact]
        public void CreateActivator_ThrowsIfModelTypeInfoOnActionDescriptorIsNull()
        {
            // Arrange
            var activatorProvider = new DefaultPageModelActivatorProvider();
            var actionDescriptor = new CompiledPageActionDescriptor();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => activatorProvider.CreateActivator(actionDescriptor),
                "actionDescriptor",
                "The 'ModelTypeInfo' property of 'actionDescriptor' must not be null.");
        }

        [Fact]
        public void CreateActivator_CreatesModelInstance()
        {
            // Arrange
            var activatorProvider = new DefaultPageModelActivatorProvider();
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                ModelTypeInfo = typeof(SimpleModel).GetTypeInfo(),
            };
            var serviceCollection = new ServiceCollection();
            var generator = Mock.Of<IHtmlGenerator>();
            serviceCollection.AddSingleton(generator);
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceCollection.BuildServiceProvider(),
            };
            var pageContext = new PageContext
            {
                HttpContext = httpContext
            };

            // Act
            var activator = activatorProvider.CreateActivator(actionDescriptor);
            var model = activator(pageContext);

            // Assert
            var simpleModel = Assert.IsType<SimpleModel>(model);
            Assert.NotNull(simpleModel);
        }

        [Fact]
        public void CreateActivator_TypeActivatesModelType()
        {
            // Arrange
            var activatorProvider = new DefaultPageModelActivatorProvider();
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                ModelTypeInfo = typeof(ModelWithServices).GetTypeInfo(),
            };
            var serviceCollection = new ServiceCollection();
            var generator = Mock.Of<IHtmlGenerator>();
            serviceCollection.AddSingleton(generator);
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceCollection.BuildServiceProvider(),
            };
            var pageContext = new PageContext
            {
                HttpContext = httpContext
            };

            // Act
            var activator = activatorProvider.CreateActivator(actionDescriptor);
            var model = activator(pageContext);

            // Assert
            var modelWithServices = Assert.IsType<ModelWithServices>(model);
            Assert.Same(generator, modelWithServices.Generator);
        }

        [Theory]
        [InlineData(typeof(SimpleModel))]
        [InlineData(typeof(object))]
        public void CreateReleaser_ReturnsNullForModelsThatDoNotImplementDisposable(Type pageType)
        {
            // Arrange
            var context = new PageContext();
            var activator = new DefaultPageModelActivatorProvider();
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = pageType.GetTypeInfo(),
            };

            // Act
            var releaser = activator.CreateReleaser(actionDescriptor);

            // Assert
            Assert.Null(releaser);
        }

        [Fact]
        public void CreateReleaser_CreatesDelegateThatDisposesDisposableTypes()
        {
            // Arrange
            var context = new PageContext();

            var activator = new DefaultPageModelActivatorProvider();
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                ModelTypeInfo = typeof(DisposableModel).GetTypeInfo(),
            };

            var model = new DisposableModel();

            // Act & Assert
            var releaser = activator.CreateReleaser(actionDescriptor);
            releaser(context, model);

            // Assert
            Assert.True(model.Disposed);
        }

        private class SimpleModel
        {
        }

        private class ModelWithServices
        {
            public ModelWithServices(IHtmlGenerator generator)
            {
                Generator = generator;
            }

            public IHtmlGenerator Generator { get; }
        }

        private class DisposableModel : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
