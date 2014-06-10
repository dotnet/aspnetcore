// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class DefaultControllerFactoryTest
    {
        [Fact]
        public void DefaultControllerFactory_DisposesIDisposableController()
        {
            // Arrange
            var factory = new DefaultControllerFactory(
                Mock.Of<IServiceProvider>(),
                Mock.Of<ITypeActivator>(),
                Mock.Of<IControllerActivator>());

            var controller = new MyController();

            // Act + Assert
            Assert.False(controller.Disposed);

            factory.ReleaseController(controller);

            Assert.True(controller.Disposed);
        }

        [Fact]
        public void DefaultControllerFactory_ReleasesNonIDisposableController()
        {
            // Arrange
            var factory = new DefaultControllerFactory(
                Mock.Of<IServiceProvider>(),
                Mock.Of<ITypeActivator>(),
                Mock.Of<IControllerActivator>());

            var controller = new Object();

            // Act + Assert
            Assert.DoesNotThrow(() => factory.ReleaseController(controller));
        }

        private class MyController : Controller, IDisposable
        {
            public bool Disposed { get; set; }
            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
