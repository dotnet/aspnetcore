// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class FilterContextTest
{
    [Fact]
    public void IsEffectivePolicy_FindsAnotherFilter_ReturnsFalse()
    {
        // Arrange
        var filters = new IFilterMetadata[]
        {
                Mock.Of<ITestFilterPolicy>(),
                Mock.Of<IAnotherTestFilterPolicy>(),
                Mock.Of<ITestFilterPolicy>(),
        };

        var context = new TestFilterContext(filters);

        // Act
        var result = context.IsEffectivePolicy((ITestFilterPolicy)filters.First());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsEffectivePolicy_FindsFilterOfInterest_ReturnsTrue()
    {
        // Arrange
        var filters = new IFilterMetadata[]
        {
                Mock.Of<ITestFilterPolicy>(),
                Mock.Of<IAnotherTestFilterPolicy>(),
                Mock.Of<ITestFilterPolicy>(),
        };

        var context = new TestFilterContext(filters);

        // Act
        var result = context.IsEffectivePolicy((ITestFilterPolicy)filters.Last());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsEffectivePolicy_NoMatch_ReturnsFalse()
    {
        // Arrange
        var filters = new IFilterMetadata[]
        {
                Mock.Of<ITestFilterPolicy>(),
                Mock.Of<ITestFilterPolicy>(),
        };

        var context = new TestFilterContext(filters);

        // Act
        var result = context.IsEffectivePolicy(Mock.Of<IAnotherTestFilterPolicy>());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FindEffectivePolicy_FindsLastFilter_ReturnsIt()
    {
        // Arrange
        var filters = new IFilterMetadata[]
        {
                Mock.Of<ITestFilterPolicy>(),
                Mock.Of<IAnotherTestFilterPolicy>(),
                Mock.Of<ITestFilterPolicy>(),
        };

        var context = new TestFilterContext(filters);

        // Act
        var result = context.FindEffectivePolicy<ITestFilterPolicy>();

        // Assert
        Assert.Same(filters.Last(), result);
    }

    [Fact]
    public void FindEffectivePolicy_NoMatch_ReturnsNull()
    {
        // Arrange
        var filters = new IFilterMetadata[]
        {
                Mock.Of<ITestFilterPolicy>(),
                Mock.Of<ITestFilterPolicy>(),
        };

        var context = new TestFilterContext(filters);

        // Act
        var result = context.FindEffectivePolicy<IAnotherTestFilterPolicy>();

        // Assert
        Assert.Null(result);
    }

    internal class ITestFilterPolicy : IFilterMetadata
    {
    }

    internal class IAnotherTestFilterPolicy : IFilterMetadata
    {
    }

    private class TestFilterContext : FilterContext
    {
        public TestFilterContext(IList<IFilterMetadata> filters)
            : base(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), filters)
        {
        }
    }
}
