// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

public class ViewDataAttributeApplicationModelProviderTest
{
    [Fact]
    public void OnProvidersExecuting_DoesNotAddFilter_IfTypeHasNoViewDataProperties()
    {
        // Arrange
        var type = typeof(TestController_NoViewDataProperties);
        var provider = new ViewDataAttributeApplicationModelProvider();
        var context = GetContext(type);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Empty(controller.Filters);
    }

    [Fact]
    public void AddsViewDataPropertyFilter_ForViewDataAttributeProperties()
    {
        // Arrange
        var type = typeof(TestController_NullableNonPrimitiveViewDataProperty);
        var provider = new ViewDataAttributeApplicationModelProvider();
        var context = GetContext(type);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.IsType<ControllerViewDataAttributeFilterFactory>(Assert.Single(controller.Filters));
    }

    [Fact]
    public void InitializeFilterFactory_WithExpectedPropertyHelpers_ForViewDataAttributeProperties()
    {
        // Arrange
        var expected = typeof(TestController_OneViewDataProperty).GetProperty(nameof(TestController_OneViewDataProperty.Test2));
        var provider = new ViewDataAttributeApplicationModelProvider();
        var context = GetContext(typeof(TestController_OneViewDataProperty));

        // Act
        provider.OnProvidersExecuting(context);
        var controller = context.Result.Controllers.SingleOrDefault();
        var filter = Assert.IsType<ControllerViewDataAttributeFilterFactory>(Assert.Single(controller.Filters));

        // Assert
        Assert.NotNull(filter);
        var property = Assert.Single(filter.Properties);
        Assert.Same(expected, property.PropertyInfo);
        Assert.Equal("Test2", property.Key);
    }

    private static ApplicationModelProviderContext GetContext(Type type)
    {
        var defaultProvider = new DefaultApplicationModelProvider(
            Options.Create(new MvcOptions()),
            new EmptyModelMetadataProvider());

        var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
        defaultProvider.OnProvidersExecuting(context);
        return context;
    }

    public class TestController_NoViewDataProperties
    {
        public DateTime? DateTime { get; set; }
    }

    public class TestController_NullableNonPrimitiveViewDataProperty
    {
        [ViewData]
        public DateTime? DateTime { get; set; }
    }

    public class TestController_OneViewDataProperty
    {
        public string Test { get; set; }

        [ViewData]
        public string Test2 { get; set; }
    }
}
