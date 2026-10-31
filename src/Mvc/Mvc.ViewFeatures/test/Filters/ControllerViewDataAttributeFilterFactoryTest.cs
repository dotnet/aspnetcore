// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

public class ControllerViewDataAttributeFilterFactoryTest
{
    [Fact]
    public void CreateInstance_CreatesFilter()
    {
        // Arrange
        var properties = new LifecycleProperty[]
        {
                new LifecycleProperty(),
                new LifecycleProperty(),
        };
        var filterFactory = new ControllerViewDataAttributeFilterFactory(properties);

        // Act
        var result = filterFactory.CreateInstance(Mock.Of<IServiceProvider>());

        // Assert
        var filter = Assert.IsType<ControllerViewDataAttributeFilter>(result);
        Assert.Same(properties, filter.Properties);
    }
}
