// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageSaveTempDataPropertyFilterTest
    {
        [Fact]
        public void OnTempDataSaving_PopulatesTempDataWithNewValuesFromPageProperties()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var page = new TestPage()
            {
                Test = "TestString",
                Test2 = "Test2",
            };

            var filter = CreatePageSaveTempDataPropertyFilter(tempData);
            filter.Subject = page;

            var pageType = page.GetType();

            var testProperty = pageType.GetProperty(nameof(TestPage.Test));
            var test2Property = pageType.GetProperty(nameof(TestPage.Test2));

            filter.OriginalValues[testProperty] = "SomeValue";
            filter.OriginalValues[test2Property] = "Test2";

            filter.Properties = new List<TempDataProperty>
            {
                new TempDataProperty("TempDataProperty-Test", testProperty, testProperty.GetValue, testProperty.SetValue),
                new TempDataProperty("TempDataProperty-Test2", test2Property, test2Property.GetValue, test2Property.SetValue)
            };

            // Act
            filter.OnTempDataSaving(tempData);

            // Assert
            Assert.Equal("TestString", page.Test);
            Assert.Equal("TestString", tempData["TempDataProperty-Test"]);
            Assert.False(tempData.ContainsKey("TestDataProperty-Test2"));
        }

        [Fact]
        public void OnPageExecuting_NullFilterFactory_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            tempData.Save();

            var page = new TestPage();

            var filter = CreatePageSaveTempDataPropertyFilter(tempData, filterFactory: false);

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
                page);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => filter.OnPageHandlerExecuting(context));
            Assert.Contains("FilterFactory", ex.Message);
        }

        [Fact]
        public void OnPageExecuting_ToPageModel_SetsPropertyValue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "Value" }
            };
            tempData.Save();

            var pageModel = new TestPageModel();

            var filter = CreatePageSaveTempDataPropertyFilter(tempData);
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
            Assert.Equal("Value", pageModel.Test);
            Assert.Null(pageModel.Test2);
        }

        [Fact]
        public void OnPageExecuting_ToPage_SetsPropertyValue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "Value" }
            };
            tempData.Save();

            var page = new TestPage()
            {
                ViewContext = CreateViewContext(httpContext, tempData)
            };

            var filter = CreatePageSaveTempDataPropertyFilter(tempData);
            filter.Subject = page;

            var pageType = page.GetType();
            var testProperty = pageType.GetProperty(nameof(TestPage.Test));
            var test2Property = pageType.GetProperty(nameof(TestPage.Test2));

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
                page);

            // Act
            filter.OnPageHandlerExecuting(context);

            // Assert
            Assert.Equal("Value", page.Test);
            Assert.Null(page.Test2);
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

            var filter = CreatePageSaveTempDataPropertyFilter(tempData);
            filter.Subject = pageModel;

            var factory = filter.FilterFactory;

            var pageType = typeof(TestPageModel);
            var testProperty = pageType.GetProperty(nameof(TestPageModel.Test));
            var test2Property = pageType.GetProperty(nameof(TestPageModel.Test2));

            filter.Properties = new List<TempDataProperty>
            {
                new TempDataProperty("TempDataProperty-Test", testProperty, testProperty.GetValue, testProperty.SetValue),
                new TempDataProperty("TempDataProperty-Test2", test2Property, test2Property.GetValue, test2Property.SetValue)
            };

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

            Assert.Same(filter.Properties, factory.Properties);
        }

        private static ViewContext CreateViewContext(HttpContext httpContext, ITempDataDictionary tempData)
        {
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
            var viewContext = new ViewContext(
                actionContext,
                NullView.Instance,
                viewData,
                tempData,
                TextWriter.Null,
                new HtmlHelperOptions());

            return viewContext;
        }

        private PageSaveTempDataPropertyFilter CreatePageSaveTempDataPropertyFilter(
            TempDataDictionary tempData,
            bool filterFactory = true)
        {
            var factory = new Mock<ITempDataDictionaryFactory>();
            factory
                .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(tempData);

            var propertyFilter = new PageSaveTempDataPropertyFilter(factory.Object);

            if (filterFactory)
            {
                propertyFilter.FilterFactory = Mock.Of<PageSaveTempDataPropertyFilterFactory>();
            }

            return propertyFilter;
        }

        public class TestPage : Page
        {
            [TempData]
            public string Test { get; set; }

            [TempData]
            public string Test2 { get; set; }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        public class TestPageModel : PageModel
        {
            [TempData]
            public string Test { get; set; }

            [TempData]
            public string Test2 { get; set; }
        }
    }
}
