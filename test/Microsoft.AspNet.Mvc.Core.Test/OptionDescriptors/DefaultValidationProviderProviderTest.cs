// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class DefaultValidationProviderProviderTest
    {
        [Fact]
        public void ValidationProviders_ReturnsInstantiatedListOfValueProviders()
        {
            // Arrange
            var service = Mock.Of<ITestService>();
            var validationProvider = Mock.Of<IModelValidatorProvider>();
            var type = typeof(TestModelValidationProvider);
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);
            var typeActivatorCache = new DefaultTypeActivatorCache();
            var options = new MvcOptions();
            options.ModelValidatorProviders.Add(type);
            options.ModelValidatorProviders.Add(validationProvider);
            var accessor = new Mock<IOptions<MvcOptions>>();
            accessor.SetupGet(a => a.Options)
                    .Returns(options);
            var provider = new DefaultModelValidatorProviderProvider(accessor.Object,
                                                                     typeActivatorCache,
                                                                     serviceProvider.Object);

            // Act
            var result = provider.ModelValidatorProviders;

            // Assert
            Assert.Equal(2, result.Count);
            var testModelValidationProvider = Assert.IsType<TestModelValidationProvider>(result[0]);
            Assert.Same(service, testModelValidationProvider.Service);
            Assert.Same(validationProvider, result[1]);
        }

        private class TestModelValidationProvider : IModelValidatorProvider
        {
            public TestModelValidationProvider(ITestService service)
            {
                Service = service;
            }

            public ITestService Service { get; private set; }

            public void GetValidators(ModelValidatorProviderContext context)
            {
            }
        }

        public interface ITestService
        {
        }
    }
}
