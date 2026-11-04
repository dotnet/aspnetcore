// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

public class ControllerViewDataAttributeFilterTest
{
    [Fact]
    public void OnActionExecuting_AddsFeature()
    {
        // Arrange
        var filter = new ControllerViewDataAttributeFilter(Array.Empty<LifecycleProperty>());
        var controller = new object();
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new IFilterMetadata[0], new Dictionary<string, object>(), controller);

        // Act
        filter.OnActionExecuting(context);

        // Assert
        var feature = Assert.Single(httpContext.Features, f => f.Key == typeof(IViewDataValuesProviderFeature));
        Assert.Same(filter, feature.Value);
    }

    [Fact]
    public void OnActionExecuting_SetsSubject()
    {
        // Arrange
        var filter = new ControllerViewDataAttributeFilter(Array.Empty<LifecycleProperty>());
        var controller = new object();
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new IFilterMetadata[0], new Dictionary<string, object>(), controller);

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Same(controller, filter.Subject);
    }

    [Fact]
    public void ProvideValues_AddsNonNullPropertyValuesToViewData()
    {
        // Arrange
        var type = typeof(TestController);
        var properties = new[]
        {
                new LifecycleProperty(type.GetProperty(nameof(TestController.Prop1)), "Prop1"),
                new LifecycleProperty(type.GetProperty(nameof(TestController.Prop2)), "Prop2"),
                new LifecycleProperty(type.GetProperty(nameof(TestController.Prop3)), "Prop3"),
            };

        var controller = new TestController();
        var filter = new ControllerViewDataAttributeFilter(properties)
        {
            Subject = controller,
        };
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());

        // Act
        controller.Prop1 = "New-Value";
        filter.ProvideViewDataValues(viewData);

        // Assert
        Assert.Collection(
            viewData.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Prop1", kvp.Key);
                Assert.Equal("New-Value", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("Prop2", kvp.Key);
                Assert.Equal("Test", kvp.Value);
            });
    }

    public class TestController
    {
        public string Prop1 { get; set; }

        public string Prop2 => "Test";

        public string Prop3 { get; set; }
    }
}
