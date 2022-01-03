// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

public class ControllerSaveTempDataPropertyFilterTest
{
    [Fact]
    public void PopulatesTempDataWithValuesFromControllerProperty()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            ["TempDataProperty-Test"] = "FirstValue"
        };

        var filter = CreateControllerSaveTempDataPropertyFilter(httpContext, tempData);

        var controller = new TestController();

        var controllerType = controller.GetType();
        var property1 = controllerType.GetProperty(nameof(TestController.Test));
        var property2 = controllerType.GetProperty(nameof(TestController.Test2));

        filter.Properties = new[]
        {
                new LifecycleProperty(property1, "TempDataProperty-Test"),
                new LifecycleProperty(property1, "TempDataProperty-Test2"),
            };

        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor(),
            },
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller);

        // Act
        filter.OnActionExecuting(context);
        controller.Test = "SecondValue";
        filter.OnTempDataSaving(tempData);

        // Assert
        Assert.Equal("SecondValue", controller.Test);
        Assert.Equal("SecondValue", tempData["TempDataProperty-Test"]);
        Assert.Equal(0, controller.Test2);
    }

    [Fact]
    public void ReadsTempDataFromTempDataDictionary()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            ["TempDataProperty-Test"] = "FirstValue"
        };

        var filter = CreateControllerSaveTempDataPropertyFilter(httpContext, tempData: tempData);
        var controller = new TestController();

        var controllerType = controller.GetType();

        var property1 = controllerType.GetProperty(nameof(TestController.Test));
        var property2 = controllerType.GetProperty(nameof(TestController.Test2));

        filter.Properties = new[]
        {
                new LifecycleProperty(property1, "TempDataProperty-Test"),
                new LifecycleProperty(property2, "TempDataProperty-Test2"),
            };

        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor(),
            },
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller);

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Equal("FirstValue", controller.Test);
        Assert.Equal(0, controller.Test2);
    }

    [Fact]
    public void ReadsTempDataFromTempDataDictionary_WithoutKeyPrefix()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            ["TempDataProperty-Test"] = "ValueWithPrefix",
            ["Test"] = "Value"
        };

        var filter = CreateControllerSaveTempDataPropertyFilter(httpContext, tempData: tempData);
        var controller = new TestController();

        var controllerType = controller.GetType();
        var property1 = controllerType.GetProperty(nameof(TestController.Test));
        var property2 = controllerType.GetProperty(nameof(TestController.Test2));

        filter.Properties = new[]
        {
                new LifecycleProperty(property1, "Test"),
                new LifecycleProperty(property2, "Test2"),
            };

        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor(),
            },
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller);

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Equal("Value", controller.Test);
        Assert.Equal(0, controller.Test2);
    }

    [Fact]
    public void WritesTempDataFromTempDataDictionary_WithoutKeyPrefix()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var filter = CreateControllerSaveTempDataPropertyFilter(httpContext, tempData: tempData);
        var controller = new TestController();

        var controllerType = controller.GetType();
        var property1 = controllerType.GetProperty(nameof(TestController.Test));
        var property2 = controllerType.GetProperty(nameof(TestController.Test2));

        filter.Properties = new[]
        {
                new LifecycleProperty(property1, "Test"),
                new LifecycleProperty(property2, "Test2"),
            };

        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor(),
            },
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller);

        // Act
        filter.OnActionExecuting(context);
        controller.Test = "New-Value";
        controller.Test2 = 42;

        filter.OnTempDataSaving(tempData);

        // Assert
        Assert.Collection(
            tempData.OrderBy(i => i.Key),
            item =>
            {
                Assert.Equal(nameof(TestController.Test), item.Key);
                Assert.Equal("New-Value", item.Value);
            },
            item =>
            {
                Assert.Equal(nameof(TestController.Test2), item.Key);
                Assert.Equal(42, item.Value);
            });
    }

    private ControllerSaveTempDataPropertyFilter CreateControllerSaveTempDataPropertyFilter(
        HttpContext httpContext,
        TempDataDictionary tempData)
    {
        var factory = new Mock<ITempDataDictionaryFactory>();
        factory
            .Setup(f => f.GetTempData(httpContext))
            .Returns(tempData);

        return new ControllerSaveTempDataPropertyFilter(factory.Object);
    }

    public class TestController : Controller
    {
        [TempData]
        public string Test { get; set; }

        [TempData]
        public int Test2 { get; set; }
    }
}
