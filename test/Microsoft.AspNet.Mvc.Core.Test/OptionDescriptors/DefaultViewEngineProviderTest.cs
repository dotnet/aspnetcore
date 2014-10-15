// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class DefaultViewEngineProviderTest
    {
        [Fact]
        public void ViewEngine_ReturnsInstantiatedListOfViewEngines()
        {
            // Arrange
            var service = Mock.Of<ITestService>();
            var viewEngine = Mock.Of<IViewEngine>();
            var type = typeof(TestViewEngine);
            var typeActivator = new TypeActivator();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);
            var options = new MvcOptions();
            options.ViewEngines.Add(viewEngine);
            options.ViewEngines.Add(type);
            var accessor = new Mock<IOptions<MvcOptions>>();
            accessor.SetupGet(a => a.Options)
                    .Returns(options);
            var provider = new DefaultViewEngineProvider(accessor.Object, typeActivator, serviceProvider.Object);

            // Act
            var result = provider.ViewEngines;

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Same(viewEngine, result[0]);
            var testViewEngine = Assert.IsType<TestViewEngine>(result[1]);
            Assert.Same(service, testViewEngine.Service);
        }

        private class TestViewEngine : IViewEngine
        {
            public TestViewEngine(ITestService service)
            {
                Service = service;
            }

            public ITestService Service { get; private set; }

            public ViewEngineResult FindPartialView(ActionContext context, string partialViewName)
            {
                throw new NotImplementedException();
            }

            public ViewEngineResult FindView(ActionContext context, string viewName)
            {
                throw new NotImplementedException();
            }
        }

        public interface ITestService
        {
        }
    }
}
