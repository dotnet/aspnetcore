// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

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
