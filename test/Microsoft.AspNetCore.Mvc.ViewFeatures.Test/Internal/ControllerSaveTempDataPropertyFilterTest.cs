// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
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
            var testProperty = controllerType.GetProperty(nameof(TestController.Test));
            var test2Property = controllerType.GetProperty(nameof(TestController.Test2));

            filter.Properties = new List<TempDataProperty>
            {
                new TempDataProperty("TempDataProperty-Test", testProperty, testProperty.GetValue, testProperty.SetValue),
                new TempDataProperty("TempDataProperty-Test2", test2Property, test2Property.GetValue, test2Property.SetValue)
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
            var testProperty = controllerType.GetProperty(nameof(TestController.Test));
            var test2Property = controllerType.GetProperty(nameof(TestController.Test2));

            filter.Properties = new List<TempDataProperty>
            {
                new TempDataProperty("TempDataProperty-Test", testProperty, testProperty.GetValue, testProperty.SetValue),
                new TempDataProperty("TempDataProperty-Test2", test2Property, test2Property.GetValue, test2Property.SetValue)
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
            filter.OnTempDataSaving(tempData);

            // Assert
            Assert.Equal("FirstValue", controller.Test);
            Assert.Equal(0, controller.Test2);
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
}
