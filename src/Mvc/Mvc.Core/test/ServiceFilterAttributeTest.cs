// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class ServiceFilterAttributeTest
    {
        [Fact]
        public void CreateService_GetsFilterFromServiceProvider()
        {
            // Arrange
            var expected = new TestFilter();
            var serviceProvider = new ServiceCollection()
                .AddSingleton(expected)
                .BuildServiceProvider();

            var serviceFilter = new ServiceFilterAttribute(typeof(TestFilter));

            // Act
            var filter = serviceFilter.CreateInstance(serviceProvider);

            // Assert
            Assert.Same(expected, filter);
        }

        [Fact]
        public void CreateService_UnwrapsFilterFactory()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddSingleton(new TestFilterFactory())
                .BuildServiceProvider();

            var serviceFilter = new ServiceFilterAttribute(typeof(TestFilterFactory));

            // Act
            var filter = serviceFilter.CreateInstance(serviceProvider);

            // Assert
            Assert.IsType<TestFilter>(filter);
        }

        public class TestFilter : IFilterMetadata
        {
        }

        public class TestFilterFactory : IFilterFactory
        {
            public bool IsReusable => throw new NotImplementedException();

            public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
            {
                return new TestFilter();
            }
        }
    }
}
