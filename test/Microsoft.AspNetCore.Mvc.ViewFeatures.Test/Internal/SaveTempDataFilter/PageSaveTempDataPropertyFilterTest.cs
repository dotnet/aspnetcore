// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class PageSaveTempDataPropertyFilterTest
    {
        [Fact]
        public void OnTempDataSaving_PopulatesTempDataWithValuesFromPageProperty()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "TestString" }
            };
            tempData.Save();

            var page = new TestPageString()
            {
                Test = "TestString",
                ViewContext = CreateViewContext(httpContext, tempData)
            };

            var provider = CreatePageSaveTempDataPropertyFilter(httpContext, tempData: tempData);
            provider.Subject = page;

            var pageType = page.GetType();

            var testProp = pageType.GetProperty(nameof(TestPageString.Test));
            var test2Prop = pageType.GetProperty(nameof(TestPageString.Test2));

            provider.TempDataProperties = new List<TempDataProperty>{
                new TempDataProperty(testProp, testProp.GetValue, testProp.SetValue),
                new TempDataProperty(test2Prop, test2Prop.GetValue, test2Prop.SetValue)
            };

            // Act
            provider.OnTempDataSaving(tempData);

            // Assert
            Assert.Equal("TestString", page.Test);
            Assert.Equal("TestString", page.TempData["TempDataProperty-Test"]);
        }

        [Fact]
        public void SetSubject_NullFilterFactory_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            tempData.Save();

            var page = new TestPageString()
            {
                ViewContext = CreateViewContext(httpContext, tempData)
            };

            var provider = CreatePageSaveTempDataPropertyFilter(httpContext, tempData: tempData, filterFactory: false);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.Subject = page);
            Assert.Contains("FilterFactory", ex.Message);
        }

        [Fact]
        public void SetSubject_ModifiesFactoryAndFilter()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            tempData.Save();

            var page = new TestPageString()
            {
                Test = "TestString",
                ViewContext = CreateViewContext(httpContext, tempData)
            };

            var provider = CreatePageSaveTempDataPropertyFilter(httpContext, tempData: tempData);
            provider.FilterFactory = new PageSaveTempDataPropertyFilterFactory();

            // Act
            provider.Subject = page;

            // Assert
            Assert.Collection(provider.TempDataProperties,
                property => Assert.Equal("Test", property.PropertyInfo.Name),
                property => Assert.Equal("Test2", property.PropertyInfo.Name));
        }

        [Fact]
        public void ApplyTempDataChanges_ToPageModel_SetsPropertyValue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "Value" }
            };
            tempData.Save();

            var page = new TestPageString()
            {
                ViewContext = CreateViewContext(httpContext, tempData)
            };

            var provider = CreatePageSaveTempDataPropertyFilter(httpContext, tempData: tempData);
            provider.Subject = page;

            var pageType = typeof(TestPageString);
            var testProp = pageType.GetProperty("Test");
            var test2Prop = pageType.GetProperty("Test2");

            provider.TempDataProperties = new List<TempDataProperty> {
                new TempDataProperty(testProp, testProp.GetValue, testProp.SetValue),
                new TempDataProperty(test2Prop, test2Prop.GetValue, test2Prop.SetValue)
            };

            // Act
            provider.ApplyTempDataChanges(httpContext);

            // Assert
            Assert.Equal("Value", page.Test);
            Assert.Null(page.Test2);
        }

        [Fact]
        public void ApplyTempDataChanges_ToPage_SetsPropertyValue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            {
                { "TempDataProperty-Test", "Value" }
            };
            tempData.Save();

            var page = new TestPageString()
            {
                ViewContext = CreateViewContext(httpContext, tempData)
            };

            var provider = CreatePageSaveTempDataPropertyFilter(httpContext, tempData: tempData);
            provider.Subject = page;

            var pageType = page.GetType();

            var testProp = pageType.GetProperty(nameof(TestPageString.Test));
            var test2Prop = pageType.GetProperty(nameof(TestPageString.Test2));

            provider.TempDataProperties = new List<TempDataProperty> {
                new TempDataProperty(testProp, testProp.GetValue, testProp.SetValue),
                new TempDataProperty(test2Prop, test2Prop.GetValue, test2Prop.SetValue)
            };

            // Act
            provider.ApplyTempDataChanges(httpContext);

            // Assert
            Assert.Equal("Value", page.Test);
            Assert.Null(page.Test2);
        }

        private static PageContext CreateViewContext(HttpContext httpContext, ITempDataDictionary tempData)
        {
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
            var viewContext = new PageContext(
                actionContext,
                viewData,
                tempData,
                new HtmlHelperOptions());

            return viewContext;
        }

        private PageSaveTempDataPropertyFilter CreatePageSaveTempDataPropertyFilter(
            HttpContext httpContext,
            TempDataDictionary tempData,
            bool filterFactory = true)
        {
            var factory = new Mock<ITempDataDictionaryFactory>();
            factory.Setup(f => f.GetTempData(httpContext))
                .Returns(tempData);

            var propertyFilter = new PageSaveTempDataPropertyFilter(factory.Object);

            if (filterFactory)
            {
                propertyFilter.FilterFactory = new Mock<PageSaveTempDataPropertyFilterFactory>().Object;
            }

            return propertyFilter;
        }

        public class TestPageString : Page
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

        public class TestPageStringWithModel : Page
        {
            public PageModel TestPageModelWithString { get; set; }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        public class TestPageModelWithString : PageModel
        {
            [TempData]
            public string Test { get; set; }

            [TempData]
            public string Test2 { get; set; }
        }
    }
}
