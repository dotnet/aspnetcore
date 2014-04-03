using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
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
