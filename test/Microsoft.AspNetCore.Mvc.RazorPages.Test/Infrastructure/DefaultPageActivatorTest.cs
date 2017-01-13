// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageActivatorTest
    {
        [Fact]
        public void CreateActivator_ThrowsIfPageTypeInfoIsNull()
        {
            // Arrange
            var descriptor = new CompiledPageActionDescriptor();
            var activator = new DefaultPageActivator();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => activator.CreateActivator(descriptor),
                "actionDescriptor",
                "The 'PageTypeInfo' property of 'actionDescriptor' must not be null.");
        }

        [Theory]
        [InlineData(typeof(TestPage))]
        [InlineData(typeof(PageWithMultipleConstructors))]
        public void CreateActivator_ReturnsFactoryForPage(Type type)
        {
            // Arrange
            var pageContext = new PageContext();
            var descriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = type.GetTypeInfo(),
            };

            var activator = new DefaultPageActivator();

            // Act
            var factory = activator.CreateActivator(descriptor);
            var instance = factory(pageContext);

            // Assert
            Assert.NotNull(instance);
            Assert.IsType(type, instance);
        }

        [Fact]
        public void CreateActivator_ThrowsIfTypeDoesNotHaveParameterlessConstructor()
        {
            // Arrange
            var descriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithoutParameterlessConstructor).GetTypeInfo(),
            };
            var pageContext = new PageContext();
            var activator = new DefaultPageActivator();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => activator.CreateActivator(descriptor));
        }

        [Theory]
        [InlineData(typeof(TestPage))]
        [InlineData(typeof(object))]
        public void CreateReleaser_ReturnsNullForPagesThatDoNotImplementDisposable(Type pageType)
        {
            // Arrange
            var context = new PageContext();
            var activator = new DefaultPageActivator();
            var page = new TestPage();

            // Act
            var releaser = activator.CreateReleaser(new CompiledPageActionDescriptor
            {
                PageTypeInfo = pageType.GetTypeInfo()
            });

            // Assert
            Assert.Null(releaser);
        }

        [Fact]
        public void CreateReleaser_CreatesDelegateThatDisposesDisposableTypes()
        {
            // Arrange
            var context = new PageContext();
            var activator = new DefaultPageActivator();
            var page = new DisposablePage();

            // Act & Assert
            var disposer = activator.CreateReleaser(new CompiledPageActionDescriptor
            {
                PageTypeInfo = page.GetType().GetTypeInfo()
            });
            Assert.NotNull(disposer);
            disposer(context, page);

            // Assert
            Assert.True(page.Disposed);
        }

        private class TestPage : Page
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class PageWithMultipleConstructors : Page
        {
            public PageWithMultipleConstructors(int x)
            {

            }

            public PageWithMultipleConstructors()
            {

            }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class PageWithoutParameterlessConstructor : Page
        {
            public PageWithoutParameterlessConstructor(ILogger logger)
            {
            }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class DisposablePage : TestPage, IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
