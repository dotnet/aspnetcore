// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ViewComponentTests
    {
        [Fact]
        public void ViewComponent_ViewBag_UsesViewData()
        {
            // Arrange
            var viewComponent = new TestViewComponent();

            // Act
            viewComponent.ViewBag.A = "Alice";
            viewComponent.ViewBag.B = "Bob";

            // Assert
            Assert.Equal(2, viewComponent.ViewData.Count);
            Assert.Equal("Alice", viewComponent.ViewData["A"]);
            Assert.Equal("Bob", viewComponent.ViewData["B"]);
        }

        [Fact]
        public void ViewComponent_ViewData_StoresDataForViewBag()
        {
            // Arrange
            var viewComponent = new TestViewComponent();

            // Act
            viewComponent.ViewData["A"] = "Alice";
            viewComponent.ViewData["B"] = "Bob";

            // Assert
            Assert.Equal(2, viewComponent.ViewData.Count);
            Assert.Equal("Alice", viewComponent.ViewBag.A);
            Assert.Equal("Bob", viewComponent.ViewBag.B);
        }

        [Fact]
        public void ViewComponent_Content_SetsResultContentAndEncodedContent()
        {
            // Arrange
            var viewComponent = new TestViewComponent();
            var expectedContent = "TestContent&";
            var expectedEncodedContent = new HtmlString(HtmlEncoder.Default.Encode(expectedContent));

            // Act
            var actualResult = viewComponent.Content(expectedContent);

            // Assert
            Assert.IsType<ContentViewComponentResult>(actualResult);
            Assert.Same(expectedContent, actualResult.Content);
            Assert.Equal(expectedEncodedContent.ToString(), actualResult.EncodedContent.ToString());
        }

        [Fact]
        public void ViewComponent_Json_SetsResultData()
        {
            // Arrange
            var viewComponent = new TestViewComponent();
            var testData = new object();

            // Act
            var actualResult = viewComponent.Json(testData);

            // Assert
            Assert.IsType<JsonViewComponentResult>(actualResult);
            Assert.Same(testData, actualResult.Value);
        }

        [Fact]
        public void ViewComponent_View_WithEmptyParameter_SetsResultViewWithDefaultViewName()
        {
            // Arrange
            var viewComponent = new TestViewComponent();

            // Act
            var actualResult = viewComponent.View();

            // Assert
            Assert.IsType<ViewViewComponentResult>(actualResult);
            Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
            Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
            Assert.Null(actualResult.ViewData.Model);
            Assert.Null(actualResult.ViewName);
        }

        [Fact]
        public void ViewComponent_View_WithViewNameParameter_SetsResultViewWithCustomViewName()
        {
            // Arrange
            var viewComponent = new TestViewComponent();

            // Act
            var actualResult = viewComponent.View("CustomViewName");

            // Assert
            Assert.IsType<ViewViewComponentResult>(actualResult);
            Assert.IsType<ViewDataDictionary<object>>(actualResult.ViewData);
            Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
            Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
            Assert.Null(actualResult.ViewData.Model);
            Assert.Equal("CustomViewName", actualResult.ViewName);
        }

        [Fact]
        public void ViewComponent_View_WithModelParameter_SetsResultViewWithDefaultViewNameAndModel()
        {
            // Arrange
            var viewComponent = new TestViewComponent();

            var model = new object();

            // Act
            var actualResult = viewComponent.View(model);

            // Assert
            Assert.IsType<ViewViewComponentResult>(actualResult);
            Assert.IsType<ViewDataDictionary<object>>(actualResult.ViewData);
            Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
            Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
            Assert.Same(model, actualResult.ViewData.Model);
            Assert.Null(actualResult.ViewName);
        }

        [Fact]
        public void ViewComponent_View_WithViewNameAndModelParameters_SetsResultViewWithCustomViewNameAndModel()
        {
            // Arrange
            var viewComponent = new TestViewComponent();

            var model = new object();

            // Act
            var actualResult = viewComponent.View("CustomViewName", model);

            // Assert
            Assert.IsType<ViewViewComponentResult>(actualResult);
            Assert.IsType<ViewDataDictionary<object>>(actualResult.ViewData);
            Assert.NotSame(viewComponent.ViewData, actualResult.ViewData);
            Assert.Equal(new ViewDataDictionary<object>(viewComponent.ViewData), actualResult.ViewData);
            Assert.Same(model, actualResult.ViewData.Model);
            Assert.Equal("CustomViewName", actualResult.ViewName);
        }

        [Fact]
        public void ViewComponent_ViewContext_ViewData_ReturnsDefaultInstanceIfNull()
        {
            // Arrange && Act
            var viewComponent = new TestViewComponent();

            // Assert
            // ViewComponent.ViewContext returns the default instance for the unit test scenarios
            Assert.NotNull(viewComponent.ViewContext);
            Assert.NotNull(viewComponent.ViewContext.ViewData);

            // ViewComponent.ViewData returns the default instance for the unit test scenarios
            Assert.Empty(viewComponent.ViewContext.ViewData);
            Assert.NotNull(viewComponent.ViewData);
            Assert.Empty(viewComponent.ViewData);
            Assert.Same(viewComponent.ViewData, viewComponent.ViewContext.ViewData);
        }

        private class TestViewComponent : ViewComponent
        {
        }
    }
}
