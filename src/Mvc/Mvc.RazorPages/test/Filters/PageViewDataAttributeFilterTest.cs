// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class PageViewDataAttributeFilterTest
{
    [Fact]
    public void OnPageHandlerExecuting_AddsFeature()
    {
        // Arrange
        var filter = new PageViewDataAttributeFilter(Array.Empty<LifecycleProperty>());
        var handler = new object();
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var pageContext = new PageContext(actionContext);
        var context = new PageHandlerExecutingContext(pageContext, new IFilterMetadata[0], new HandlerMethodDescriptor(), new Dictionary<string, object>(), handler);

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        var feature = Assert.Single(httpContext.Features, f => f.Key == typeof(IViewDataValuesProviderFeature));
        Assert.Same(filter, feature.Value);
    }

    [Fact]
    public void OnPageHandlerExecuting_SetsSubject()
    {
        // Arrange
        var filter = new PageViewDataAttributeFilter(Array.Empty<LifecycleProperty>());
        var handler = new object();
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var pageContext = new PageContext(actionContext);
        var context = new PageHandlerExecutingContext(pageContext, new IFilterMetadata[0], new HandlerMethodDescriptor(), new Dictionary<string, object>(), handler);

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Same(handler, filter.Subject);
    }

    [Fact]
    public void ProvideValues_AddsNonNullPropertyValuesToViewData()
    {
        // Arrange
        var type = typeof(TestModel);
        var properties = new[]
        {
                new LifecycleProperty(type.GetProperty(nameof(TestModel.Prop1)), "Prop1"),
                new LifecycleProperty(type.GetProperty(nameof(TestModel.Prop2)), "Prop2"),
                new LifecycleProperty(type.GetProperty(nameof(TestModel.Prop3)), "Prop3"),
            };

        var controller = new TestModel();
        var filter = new PageViewDataAttributeFilter(properties)
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

    public class TestModel
    {
        public string Prop1 { get; set; }

        public string Prop2 => "Test";

        public string Prop3 { get; set; }
    }
}
