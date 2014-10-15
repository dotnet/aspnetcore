// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.OptionDescriptors
{
    public class DefaultViewLocationExpanderProviderTest
    {
        [Fact]
        public void ViewLocationExpanders_ReturnsActivatedListOfExpanders()
        {
            // Arrange
            var service = Mock.Of<ITestService>();
            var expander = Mock.Of<IViewLocationExpander>();
            var type = typeof(TestViewLocationExpander);
            var typeActivator = new TypeActivator();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);
            var options = new RazorViewEngineOptions();
            options.ViewLocationExpanders.Add(type);
            options.ViewLocationExpanders.Add(expander);
            var accessor = new Mock<IOptions<RazorViewEngineOptions>>();
            accessor.SetupGet(a => a.Options)
                    .Returns(options);
            var provider = new DefaultViewLocationExpanderProvider(accessor.Object,
                                                                   typeActivator,
                                                                   serviceProvider.Object);

            // Act
            var result = provider.ViewLocationExpanders;

            // Assert
            Assert.Equal(2, result.Count);
            var testExpander = Assert.IsType<TestViewLocationExpander>(result[0]);
            Assert.Same(service, testExpander.Service);
            Assert.Same(expander, result[1]);
        }

        private class TestViewLocationExpander : IViewLocationExpander
        {
            public TestViewLocationExpander(ITestService service)
            {
                Service = service;
            }

            public ITestService Service { get; private set; }

            public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
                                                           IEnumerable<string> viewLocations)
            {
                throw new NotImplementedException();
            }

            public void PopulateValues(ViewLocationExpanderContext context)
            {
                throw new NotImplementedException();
            }
        }

        public interface ITestService
        {
        }
    }
}
