// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class DefaultValueProviderFactoryProviderTest
    {
        [Fact]
        public void ViewEngine_ReturnsInstantiatedListOfViewEngines()
        {
            // Arrange
            var service = Mock.Of<ITestService>();
            var valueProviderFactory = Mock.Of<IValueProviderFactory>();
            var type = typeof(TestValueProviderFactory);
            var typeActivator = new TypeActivator();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);
            var options = new MvcOptions();
            options.ValueProviderFactories.Add(valueProviderFactory);
            options.ValueProviderFactories.Add(type);
            var accessor = new Mock<IOptions<MvcOptions>>();
            accessor.SetupGet(a => a.Options)
                    .Returns(options);
            var provider = new DefaultValueProviderFactoryProvider(accessor.Object,
                                                                   typeActivator,
                                                                   serviceProvider.Object);

            // Act
            var result = provider.ValueProviderFactories;

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Same(valueProviderFactory, result[0]);
            var testValueProviderFactory = Assert.IsType<TestValueProviderFactory>(result[1]);
            Assert.Same(service, testValueProviderFactory.Service);
        }

        private class TestValueProviderFactory : IValueProviderFactory
        {
            public TestValueProviderFactory(ITestService service)
            {
                Service = service;
            }

            public ITestService Service { get; private set; }

            public IValueProvider GetValueProvider(ValueProviderFactoryContext context)
            {
                throw new NotImplementedException();
            }
        }

        public interface ITestService
        {
        }
    }
}
