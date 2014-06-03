// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ViewComponentTests
    {
        [Fact]
        public void ViewComponent_ViewBag_UsesViewData()
        {
            // Arrange
            var viewComponent = new TestViewComponent()
            {
                ViewData = new ViewDataDictionary(metadataProvider: null),
            };

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
            var viewComponent = new TestViewComponent()
            {
                ViewData = new ViewDataDictionary(metadataProvider: null),
            };

            // Act
            viewComponent.ViewData["A"] = "Alice";
            viewComponent.ViewData["B"] = "Bob";

            // Assert
            Assert.Equal(2, viewComponent.ViewData.Count);
            Assert.Equal("Alice", viewComponent.ViewBag.A);
            Assert.Equal("Bob", viewComponent.ViewBag.B);
        }

        [Fact]
        public void ViewComponent_Content_CallsResultContentWithTestContent()
        {
            // Arrange
            var viewComponent = new TestViewComponent();
            var resultHelperMock = new Mock<DefaultViewComponentResultHelper>(It.IsAny<IViewEngine>());
            var resultMock = new Mock<ContentViewComponentResult>("TestContent");
            resultHelperMock.Setup(r => r.Content(It.IsAny<string>()))
                            .Returns(resultMock.Object);
            viewComponent.Initialize(resultHelperMock.Object);

            // Act
            var actualResult = viewComponent.Content("TestContent");

            // Assert
            resultHelperMock.Verify(r => r.Content("TestContent"));
            Assert.Same(resultMock.Object, actualResult);
        }

        [Fact]
        public void ViewComponent_Json_CallsResultJsonWithTestValue()
        {
            // Arrange
            var viewComponent = new TestViewComponent();
            var resultHelperMock = new Mock<DefaultViewComponentResultHelper>(It.IsAny<IViewEngine>());
            var resultMock = new Mock<JsonViewComponentResult>(It.IsAny<object>());
            resultHelperMock.Setup(r => r.Json(It.IsAny<object>()))
                            .Returns(resultMock.Object);
            viewComponent.Initialize(resultHelperMock.Object);
            var testValue = new object();

            // Act
            var actualResult = viewComponent.Json(testValue);

            // Assert
            resultHelperMock.Verify(r => r.Json(testValue));
            Assert.Same(resultMock.Object, actualResult);
        }

        [Fact]
        public void ViewComponent_View_WithEmptyParameter_CallsResultViewWithDefaultViewName()
        {
            // Arrange
            var viewComponent = new TestViewComponent()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            };
            var resultHelperMock = new Mock<DefaultViewComponentResultHelper>(It.IsAny<IViewEngine>());
            var resultMock = new Mock<ViewViewComponentResult>(It.IsAny<IViewEngine>(),
                                                               It.IsAny<string>(),
                                                               It.IsAny<ViewDataDictionary>());
            resultHelperMock.Setup(r => r.View(It.IsAny<string>(), It.IsAny<ViewDataDictionary>()))
                            .Returns(resultMock.Object);
            viewComponent.Initialize(resultHelperMock.Object);

            // Act
            var actualResult = viewComponent.View();

            // Assert
            resultHelperMock.Verify(r => r.View("Default", viewComponent.ViewData));
            Assert.Same(resultMock.Object, actualResult);
        }

        [Fact]
        public void ViewComponent_View_WithViewNameParameter_CallsResultViewWithCustomViewName()
        {
            // Arrange
            var viewComponent = new TestViewComponent()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            };
            var resultHelperMock = new Mock<DefaultViewComponentResultHelper>(It.IsAny<IViewEngine>());
            var resultMock = new Mock<ViewViewComponentResult>(It.IsAny<IViewEngine>(),
                                                               It.IsAny<string>(),
                                                               It.IsAny<ViewDataDictionary>());
            resultHelperMock.Setup(r => r.View(It.IsAny<string>(), It.IsAny<ViewDataDictionary>()))
                            .Returns(resultMock.Object);
            viewComponent.Initialize(resultHelperMock.Object);

            // Act
            var actualResult = viewComponent.View("CustomViewName");

            // Assert
            resultHelperMock.Verify(r => r.View("CustomViewName", viewComponent.ViewData));
            Assert.Same(resultMock.Object, actualResult);
        }

        [Fact]
        public void ViewComponent_View_WithModelParameter_CallsResultViewWithDefaultViewNameAndModel()
        {
            // Arrange
            var viewComponent = new TestViewComponent()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            };
            var resultHelperMock = new Mock<DefaultViewComponentResultHelper>(It.IsAny<IViewEngine>());
            var resultMock = new Mock<ViewViewComponentResult>(It.IsAny<IViewEngine>(),
                                                               It.IsAny<string>(),
                                                               It.IsAny<ViewDataDictionary>());
            resultHelperMock.Setup(r => r.View(It.IsAny<string>(), It.IsAny<ViewDataDictionary>()))
                            .Returns(resultMock.Object);
            viewComponent.Initialize(resultHelperMock.Object);
            var model = new object();

            // Act
            var actualResult = viewComponent.View(model);

            // Assert
            resultHelperMock.Verify(r => r.View("Default", viewComponent.ViewData));
            Assert.Same(resultMock.Object, actualResult);
        }

        [Fact]
        public void ViewComponent_View_WithViewNameAndModelParameters_CallsResultViewWithCustomViewNameAndModel()
        {
            // Arrange
            var viewComponent = new TestViewComponent()
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
            };
            var resultHelperMock = new Mock<DefaultViewComponentResultHelper>(It.IsAny<IViewEngine>());
            var resultMock = new Mock<ViewViewComponentResult>(It.IsAny<IViewEngine>(),
                                                               It.IsAny<string>(),
                                                               It.IsAny<ViewDataDictionary>());
            resultHelperMock.Setup(r => r.View(It.IsAny<string>(), It.IsAny<ViewDataDictionary>()))
                            .Returns(resultMock.Object);
            viewComponent.Initialize(resultHelperMock.Object);
            var model = new object();

            // Act
            var actualResult = viewComponent.View("CustomViewName", model);

            // Assert
            resultHelperMock.Verify(r => r.View("CustomViewName", viewComponent.ViewData));
            Assert.Same(resultMock.Object, actualResult);
        }

        private class TestViewComponent : ViewComponent
        {
        }
    }
}
