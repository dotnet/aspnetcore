// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewEngines
{
    public class CompositeViewEngineTest
    {
        [Fact]
        public void ViewEngines_UsesListOfViewEnginesFromOptions()
        {
            // Arrange
            var viewEngine1 = Mock.Of<IViewEngine>();
            var viewEngine2 = Mock.Of<IViewEngine>();
            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(viewEngine1);
            optionsAccessor.Value.ViewEngines.Add(viewEngine2);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.ViewEngines;

            // Assert
            Assert.Equal(new[] { viewEngine1, viewEngine2 }, result);
        }

        [Fact]
        public void FindView_IsMainPage_Throws_WhenNoViewEnginesAreRegistered()
        {
            // Arrange
            var expected = $"'{typeof(MvcViewOptions).FullName}.{nameof(MvcViewOptions.ViewEngines)}' must not be " +
                $"empty. At least one '{typeof(IViewEngine).FullName}' is required to locate a view for rendering.";
            var viewName = "test-view";
            var actionContext = GetActionContext();
            var optionsAccessor = Options.Create(new MvcViewOptions());
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => compositeViewEngine.FindView(actionContext, viewName, isMainPage: true));
            Assert.Equal(expected, exception.Message);
        }


        [Fact]
        public void FindView_IsMainPage_ReturnsNotFoundResult_WhenExactlyOneViewEngineIsRegisteredWhichReturnsNotFoundResult()
        {
            // Arrange
            var viewName = "test-view";
            var engine = new Mock<IViewEngine>(MockBehavior.Strict);
            engine
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "controller/test-view" }));
            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: true);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "controller/test-view" }, result.SearchedLocations);
        }

        [Fact]
        public void FindView_IsMainPage_ReturnsView_WhenExactlyOneViewEngineIsRegisteredWhichReturnsAFoundResult()
        {
            // Arrange
            var viewName = "test-view";
            var engine = new Mock<IViewEngine>(MockBehavior.Strict);
            var view = Mock.Of<IView>();
            engine
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.Found(viewName, view));
            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view, result.View);
        }

        [Fact]
        public void FindView_IsMainPage_ReturnsViewFromFirstViewEngineWithFoundResult()
        {
            // Arrange
            var viewName = "foo";
            var engine1 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine2 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine3 = new Mock<IViewEngine>(MockBehavior.Strict);
            var view2 = Mock.Of<IView>();
            var view3 = Mock.Of<IView>();
            engine1
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()));
            engine2
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.Found(viewName, view2));
            engine3
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.Found(viewName, view3));

            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine1.Object);
            optionsAccessor.Value.ViewEngines.Add(engine2.Object);
            optionsAccessor.Value.ViewEngines.Add(engine3.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view2, result.View);
            Assert.Equal(viewName, result.ViewName);
        }

        [Fact]
        public void FindView_IsMainPage_ReturnsNotFound_IfAllViewEnginesReturnNotFound()
        {
            // Arrange
            var viewName = "foo";
            var engine1 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine2 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine3 = new Mock<IViewEngine>(MockBehavior.Strict);
            engine1
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "1", "2" }));
            engine2
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "3" }));
            engine3
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "4", "5" }));

            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine1.Object);
            optionsAccessor.Value.ViewEngines.Add(engine2.Object);
            optionsAccessor.Value.ViewEngines.Add(engine3.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: true);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result.SearchedLocations);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetView_ReturnsNotFoundResult_WhenNoViewEnginesAreRegistered(bool isMainPage)
        {
            // Arrange
            var expected = $"'{typeof(MvcViewOptions).FullName}.{nameof(MvcViewOptions.ViewEngines)}' must not be " +
                $"empty. At least one '{typeof(IViewEngine).FullName}' is required to locate a view for rendering.";
            var viewName = "test-view.cshtml";
            var optionsAccessor = Options.Create(new MvcViewOptions());
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => compositeViewEngine.GetView("~/Index.html", viewName, isMainPage));
            Assert.Equal(expected, exception.Message);
        }


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetView_ReturnsNotFoundResult_WhenExactlyOneViewEngineIsRegisteredWhichReturnsNotFoundResult(
            bool isMainPage)
        {
            // Arrange
            var viewName = "test-view.cshtml";
            var expectedViewName = "~/" + viewName;
            var engine = new Mock<IViewEngine>(MockBehavior.Strict);
            engine
                .Setup(e => e.GetView("~/Index.html", viewName, isMainPage))
                .Returns(ViewEngineResult.NotFound(expectedViewName, new[] { expectedViewName }));
            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.GetView("~/Index.html", viewName, isMainPage);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { expectedViewName }, result.SearchedLocations);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetView_ReturnsView_WhenExactlyOneViewEngineIsRegisteredWhichReturnsAFoundResult(bool isMainPage)
        {
            // Arrange
            var viewName = "test-view.cshtml";
            var expectedViewName = "~/" + viewName;
            var engine = new Mock<IViewEngine>(MockBehavior.Strict);
            var view = Mock.Of<IView>();
            engine
                .Setup(e => e.GetView("~/Index.html", viewName, isMainPage))
                .Returns(ViewEngineResult.Found(expectedViewName, view));
            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.GetView("~/Index.html", viewName, isMainPage);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view, result.View);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetView_ReturnsViewFromFirstViewEngineWithFoundResult(bool isMainPage)
        {
            // Arrange
            var viewName = "foo.cshtml";
            var expectedViewName = "~/" + viewName;
            var engine1 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine2 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine3 = new Mock<IViewEngine>(MockBehavior.Strict);
            var view2 = Mock.Of<IView>();
            var view3 = Mock.Of<IView>();
            engine1
                .Setup(e => e.GetView("~/Index.html", viewName, isMainPage))
                .Returns(ViewEngineResult.NotFound(expectedViewName, Enumerable.Empty<string>()));
            engine2
                .Setup(e => e.GetView("~/Index.html", viewName, isMainPage))
                .Returns(ViewEngineResult.Found(expectedViewName, view2));
            engine3
                .Setup(e => e.GetView("~/Index.html", viewName, isMainPage))
                .Returns(ViewEngineResult.Found(expectedViewName, view3));

            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine1.Object);
            optionsAccessor.Value.ViewEngines.Add(engine2.Object);
            optionsAccessor.Value.ViewEngines.Add(engine3.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.GetView("~/Index.html", viewName, isMainPage);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view2, result.View);
            Assert.Equal(expectedViewName, result.ViewName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetView_ReturnsNotFound_IfAllViewEnginesReturnNotFound(bool isMainPage)
        {
            // Arrange
            var viewName = "foo.cshtml";
            var expectedViewName = "~/" + viewName;
            var engine1 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine2 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine3 = new Mock<IViewEngine>(MockBehavior.Strict);
            engine1
                .Setup(e => e.GetView("~/Index.html", viewName, isMainPage))
                .Returns(ViewEngineResult.NotFound(expectedViewName, new[] { "1", "2" }));
            engine2
                .Setup(e => e.GetView("~/Index.html", viewName, isMainPage))
                .Returns(ViewEngineResult.NotFound(expectedViewName, new[] { "3" }));
            engine3
                .Setup(e => e.GetView("~/Index.html", viewName, isMainPage))
                .Returns(ViewEngineResult.NotFound(expectedViewName, new[] { "4", "5" }));

            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine1.Object);
            optionsAccessor.Value.ViewEngines.Add(engine2.Object);
            optionsAccessor.Value.ViewEngines.Add(engine3.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.GetView("~/Index.html", viewName, isMainPage);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result.SearchedLocations);
        }

        [Fact]
        public void FindView_ReturnsNotFoundResult_WhenNoViewEnginesAreRegistered()
        {
            // Arrange
            var expected = $"'{typeof(MvcViewOptions).FullName}.{nameof(MvcViewOptions.ViewEngines)}' must not be " +
                $"empty. At least one '{typeof(IViewEngine).FullName}' is required to locate a view for rendering.";
            var viewName = "my-partial-view";
            var optionsAccessor = Options.Create(new MvcViewOptions());
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act & AssertS
            var exception = Assert.Throws<InvalidOperationException>(
                () => compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: false));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public void FindView_ReturnsNotFoundResult_WhenExactlyOneViewEngineIsRegisteredWhichReturnsNotFoundResult()
        {
            // Arrange
            var viewName = "partial-view";
            var engine = new Mock<IViewEngine>(MockBehavior.Strict);
            engine
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "Shared/partial-view" }));
            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: false);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "Shared/partial-view" }, result.SearchedLocations);
        }

        [Fact]
        public void FindView_ReturnsView_WhenExactlyOneViewEngineIsRegisteredWhichReturnsAFoundResult()
        {
            // Arrange
            var viewName = "test-view";
            var engine = new Mock<IViewEngine>(MockBehavior.Strict);
            var view = Mock.Of<IView>();
            engine
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.Found(viewName, view));
            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: false);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view, result.View);
        }

        [Fact]
        public void FindView_ReturnsViewFromFirstViewEngineWithFoundResult()
        {
            // Arrange
            var viewName = "bar";
            var engine1 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine2 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine3 = new Mock<IViewEngine>(MockBehavior.Strict);
            var view2 = Mock.Of<IView>();
            var view3 = Mock.Of<IView>();
            engine1
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()));
            engine2
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.Found(viewName, view2));
            engine3
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.Found(viewName, view3));

            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine1.Object);
            optionsAccessor.Value.ViewEngines.Add(engine2.Object);
            optionsAccessor.Value.ViewEngines.Add(engine3.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: false);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view2, result.View);
            Assert.Equal(viewName, result.ViewName);
        }

        [Fact]
        public void FindView_ReturnsNotFound_IfAllViewEnginesReturnNotFound()
        {
            // Arrange
            var viewName = "foo";
            var engine1 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine2 = new Mock<IViewEngine>(MockBehavior.Strict);
            var engine3 = new Mock<IViewEngine>(MockBehavior.Strict);
            engine1
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "1", "2" }));
            engine2
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "3" }));
            engine3
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), viewName, /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound(viewName, new[] { "4", "5" }));

            var optionsAccessor = Options.Create(new MvcViewOptions());
            optionsAccessor.Value.ViewEngines.Add(engine1.Object);
            optionsAccessor.Value.ViewEngines.Add(engine2.Object);
            optionsAccessor.Value.ViewEngines.Add(engine3.Object);
            var compositeViewEngine = new CompositeViewEngine(optionsAccessor);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName, isMainPage: false);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result.SearchedLocations);
        }

        private static ActionContext GetActionContext()
        {
            return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        }

        private class TestViewEngine : IViewEngine
        {
            public TestViewEngine(ITestService service)
            {
                Service = service;
            }

            public ITestService Service { get; private set; }

            public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
            {
                throw new NotImplementedException();
            }

            public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage)
            {
                throw new NotImplementedException();
            }
        }

        public interface ITestService
        {
        }
    }
}
