// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class SaveTempDataPropertyFilterTest
    {
        [Fact]
        public void SaveTempDataPropertyFilter_PopulatesTempDataWithValuesFromControllerProperty()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                ["TempDataProperty-Test"] = "FirstValue"
            };

            var factory = new Mock<ITempDataDictionaryFactory>();
            factory.Setup(f => f.GetTempData(httpContext))
                .Returns(tempData);

            var filter = new SaveTempDataPropertyFilter(factory.Object);

            var controller = new TestController();
            var controllerType = controller.GetType().GetTypeInfo();

            var propertyHelper1 = new PropertyHelper(controllerType.GetProperty(nameof(TestController.Test)));
            var propertyHelper2 = new PropertyHelper(controllerType.GetProperty(nameof(TestController.Test2)));
            var propertyHelpers = new List<PropertyHelper>
            {
                propertyHelper1,
                propertyHelper2,
            };

            filter.PropertyHelpers = propertyHelpers;
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
        public void SaveTempDataPropertyFilter_ReadsTempDataFromTempDataDictionary()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                ["TempDataProperty-Test"] = "FirstValue"
            };

            var factory = new Mock<ITempDataDictionaryFactory>();
            factory.Setup(f => f.GetTempData(httpContext))
                .Returns(tempData);

            var filter = new SaveTempDataPropertyFilter(factory.Object);
            var controller = new TestController();
            var controllerType = controller.GetType().GetTypeInfo();

            var propertyHelper1 = new PropertyHelper(controllerType.GetProperty(nameof(TestController.Test)));
            var propertyHelper2 = new PropertyHelper(controllerType.GetProperty(nameof(TestController.Test2)));
            var propertyHelpers = new List<PropertyHelper>
            {
                propertyHelper1,
                propertyHelper2,
            };

            filter.PropertyHelpers = propertyHelpers;

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

        public class TestController : Controller
        {
            [TempData]
            public string Test { get; set; }

            [TempData]
            public int Test2 { get; set; }
        }
    }
}
