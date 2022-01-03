// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Test.ApplicationModels;

public class PageRouteTransformerConventionTest
{
    [Fact]
    public void Apply_SetTransformer()
    {
        // Arrange
        var transformer = new TestParameterTransformer();
        var convention = new PageRouteTransformerConvention(transformer);

        var model = new PageRouteModel(string.Empty, string.Empty);

        // Act
        convention.Apply(model);

        // Assert
        Assert.Same(transformer, model.RouteParameterTransformer);
    }

    [Fact]
    public void Apply_ShouldApplyFalse_NoOp()
    {
        // Arrange
        var transformer = new TestParameterTransformer();
        var convention = new CustomPageRouteTransformerConvention(transformer);

        var model = new PageRouteModel(string.Empty, string.Empty);

        // Act
        convention.Apply(model);

        // Assert
        Assert.Null(model.RouteParameterTransformer);
    }

    private class TestParameterTransformer : IOutboundParameterTransformer
    {
        public string TransformOutbound(object value)
        {
            return value?.ToString();
        }
    }

    private class CustomPageRouteTransformerConvention : PageRouteTransformerConvention
    {
        public CustomPageRouteTransformerConvention(IOutboundParameterTransformer parameterTransformer) : base(parameterTransformer)
        {
        }

        protected override bool ShouldApply(PageRouteModel action)
        {
            return false;
        }
    }
}
