// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
                new Mock<IServiceProvider>().Object,
                new Mock<ITypeActivator>().Object);

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
                new Mock<IServiceProvider>().Object,
                new Mock<ITypeActivator>().Object);

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
