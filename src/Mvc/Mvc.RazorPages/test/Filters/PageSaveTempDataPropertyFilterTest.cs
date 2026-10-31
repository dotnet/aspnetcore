// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class PageSaveTempDataPropertyFilterTest
{
    [Fact]
    public void OnTempDataSaving_PopulatesTempDataWithNewValuesFromPageProperties()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            ["TempDataProperty-Test"] = "Old-Value",
        };
        var pageModel = new TestPageModel()
        {
            Test = "TestString",
            Test2 = "Test2",
        };

        var filter = CreatePageSaveTempDataPropertyFilter(tempData, "TempDataProperty-");
        filter.Subject = pageModel;

        // Act
        filter.OnTempDataSaving(tempData);

        // Assert
        Assert.Equal("TestString", tempData["TempDataProperty-Test"]);
        Assert.False(tempData.ContainsKey("TestDataProperty-Test2"));
    }

    [Fact]
    public void OnPageExecuting_SetsPropertyValue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "Value" }
            };

        var pageModel = new TestPageModel();

        var filter = CreatePageSaveTempDataPropertyFilter(tempData, "TempDataProperty-");
        filter.Subject = pageModel;

        var context = new PageHandlerExecutingContext(
            new PageContext()
            {
                ActionDescriptor = new CompiledPageActionDescriptor(),
                HttpContext = httpContext,
                RouteData = new RouteData(),
            },
            Array.Empty<IFilterMetadata>(),
            null,
            new Dictionary<string, object>(),
            pageModel);

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Equal("Value", pageModel.Test);
        Assert.Null(pageModel.Test2);
    }

    [Fact]
    public void OnPageExecuting_InitializesAndSavesProperties()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "Value" }
            };
        tempData.Save();

        var pageModel = new TestPageModel();

        var filter = CreatePageSaveTempDataPropertyFilter(tempData, "TempDataProperty-");
        filter.Subject = pageModel;

        var pageType = typeof(TestPageModel);
        var testProperty = pageType.GetProperty(nameof(TestPageModel.Test));
        var test2Property = pageType.GetProperty(nameof(TestPageModel.Test2));

        var context = new PageHandlerExecutingContext(
            new PageContext()
            {
                ActionDescriptor = new CompiledPageActionDescriptor(),
                HttpContext = httpContext,
                RouteData = new RouteData(),
            },
            Array.Empty<IFilterMetadata>(),
            null,
            new Dictionary<string, object>(),
            pageModel);

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Collection(
            filter.Properties.OrderBy(p => p.PropertyInfo.Name),
            p => Assert.Equal(testProperty, p.PropertyInfo),
            p => Assert.Equal(test2Property, p.PropertyInfo));
    }

    [Fact]
    public void OnPageExecuting_ReadsTempDataPropertiesWithoutPrefix()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "Prefix-Value" },
                { "Test", "Value" }
            };
        tempData.Save();

        var model = new TestPageModel();

        var filter = CreatePageSaveTempDataPropertyFilter(tempData, string.Empty);
        filter.Subject = model;

        var context = new PageHandlerExecutingContext(
            new PageContext()
            {
                ActionDescriptor = new CompiledPageActionDescriptor(),
                HttpContext = httpContext,
                RouteData = new RouteData(),
            },
            Array.Empty<IFilterMetadata>(),
            null,
            new Dictionary<string, object>(),
            model);

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Equal("Value", model.Test);
        Assert.Null(model.Test2);
    }

    [Fact]
    public void OnTempDataSaving_WritesToTempData_WithoutPrefix()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            ["Test"] = "Old-Value",
        };
        var pageModel = new TestPageModel
        {
            Test = "New-Value",
        };

        var filter = CreatePageSaveTempDataPropertyFilter(tempData, string.Empty);
        filter.Subject = pageModel;

        // Act
        filter.OnTempDataSaving(tempData);

        // Assert
        Assert.Collection(
            tempData,
            item =>
            {
                Assert.Equal("Test", item.Key);
                Assert.Equal("New-Value", item.Value);
            });
    }

    private PageSaveTempDataPropertyFilter CreatePageSaveTempDataPropertyFilter(TempDataDictionary tempData, string prefix)
    {
        var factory = new Mock<ITempDataDictionaryFactory>();
        factory
            .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(tempData);

        var pageModelType = typeof(TestPageModel);
        var property1 = pageModelType.GetProperty(nameof(TestPageModel.Test));
        var property2 = pageModelType.GetProperty(nameof(TestPageModel.Test2));

        var filter = new PageSaveTempDataPropertyFilter(factory.Object)
        {
            Properties = new[]
            {
                    new LifecycleProperty(property1, prefix + property1.Name),
                    new LifecycleProperty(property2, prefix + property2.Name),
                }
        };

        return filter;
    }

    public class TestPageModel : PageModel
    {
        [TempData]
        public string Test { get; set; }

        [TempData]
        public string Test2 { get; set; }
    }
}
