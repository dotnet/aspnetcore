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

            var filter = CreateSaveTempDataPropertyFilter(httpContext, tempData);

            var controller = new TestController();

            filter.PropertyHelpers = BuildPropertyHelpers<TestController>();
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

            var filter = CreateSaveTempDataPropertyFilter(httpContext, tempData: tempData);
            var controller = new TestController();

            filter.PropertyHelpers = BuildPropertyHelpers<TestController>();

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

        [Fact]
        public void ApplyTempDataChanges_SetsPropertyValue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "Value" }
            };
            tempData.Save();

            var controller = new TestControllerStrings()
            {
                TempData = tempData,
            };

            var provider = CreateSaveTempDataPropertyFilter(httpContext, tempData: tempData);
            provider.Subject = controller;
            provider.PropertyHelpers = BuildPropertyHelpers<TestControllerStrings>();

            // Act
            provider.ApplyTempDataChanges(httpContext);

            // Assert
            Assert.Equal("Value", controller.Test);
            Assert.Null(controller.Test2);
        }

        private IList<PropertyHelper> BuildPropertyHelpers<TSubject>()
        {
            var subjectType = typeof(TSubject);

            var properties = subjectType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var result = new List<PropertyHelper>();

            foreach (var property in properties)
            {
                result.Add(new PropertyHelper(property));
            }

            return result;
        }

        private SaveTempDataPropertyFilter CreateSaveTempDataPropertyFilter(
            HttpContext httpContext,
            TempDataDictionary tempData)
        {
            var factory = new Mock<ITempDataDictionaryFactory>();
            factory.Setup(f => f.GetTempData(httpContext))
                .Returns(tempData);

            return new SaveTempDataPropertyFilter(factory.Object);
        }

        public class TestControllerStrings : Controller
        {
            [TempData]
            public string Test { get; set; }

            [TempData]
            public string Test2 { get; set; }
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
