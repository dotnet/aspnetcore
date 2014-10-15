// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class DefaultModelBindersProviderTest
    {
        [Fact]
        public void DefaultModelBindersProvider_ProvidesInstancesOfModelBinders()
        {
            // Arrange
            var service = Mock.Of<ITestService>();
            var binder = new TypeMatchModelBinder();
            var options = new MvcOptions();
            options.ModelBinders.Add(binder);
            options.ModelBinders.Add(typeof(TestModelBinder));
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options)
                           .Returns(options);
            var activator = new TypeActivator();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);

            var provider = new DefaultModelBindersProvider(optionsAccessor.Object, activator, serviceProvider.Object);

            // Act
            var binders = provider.ModelBinders;

            // Assert
            Assert.Equal(2, binders.Count);
            Assert.Same(binder, binders[0]);
            var testModelBinder = Assert.IsType<TestModelBinder>(binders[1]);
            Assert.Same(service, testModelBinder.Service);
        }

        private class TestModelBinder : IModelBinder
        {
            public TestModelBinder(ITestService service)
            {
                Service = service;
            }

            public ITestService Service { get; private set; }

            public Task<bool> BindModelAsync(ModelBindingContext bindingContext)
            {
                throw new NotImplementedException();
            }
        }

        public interface ITestService
        {
        }
    }
}
