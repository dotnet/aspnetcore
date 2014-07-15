// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class CompositeViewEngineTest
    {
        [Fact]
        public void FindView_ReturnsNotFoundResult_WhenNoViewEnginesAreRegistered()
        {
            // Arrange
            var viewName = "test-view";
            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new IViewEngine[0]);
            var actionContext = GetActionContext();
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindView(actionContext, viewName);

            // Assert
            Assert.False(result.Success);
            Assert.Empty(result.SearchedLocations);
        }


        [Fact]
        public void FindView_ReturnsNotFoundResult_WhenExactlyOneViewEngineIsRegisteredWhichReturnsNotFoundResult()
        {
            // Arrange
            var viewName = "test-view";
            var provider = new Mock<IViewEngineProvider>();
            var engine = new Mock<IViewEngine>();
            engine.Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "controller/test-view" }));
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "controller/test-view" }, result.SearchedLocations);
        }

        [Fact]
        public void FindView_ReturnsView_WhenExactlyOneViewEngineIsRegisteredWhichReturnsAFoundResult()
        {
            // Arrange
            var viewName = "test-view";
            var provider = new Mock<IViewEngineProvider>();
            var engine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();
            engine.Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view));
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view, result.View);
        }

        [Fact]
        public void FindView_ReturnsViewFromFirstViewEngineWithFoundResult()
        {
            // Arrange
            var viewName = "foo";
            var engine1 = new Mock<IViewEngine>();
            var engine2 = new Mock<IViewEngine>();
            var engine3 = new Mock<IViewEngine>();
            var view2 = Mock.Of<IView>();
            var view3 = Mock.Of<IView>();
            engine1.Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()));
            engine2.Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view2));
            engine3.Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view3));

            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine1.Object, engine2.Object, engine3.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName);

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
            var engine1 = new Mock<IViewEngine>();
            var engine2 = new Mock<IViewEngine>();
            var engine3 = new Mock<IViewEngine>();
            engine1.Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "1", "2" }));
            engine2.Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "3" }));
            engine3.Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "4", "5" }));

            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine1.Object, engine2.Object, engine3.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindView(GetActionContext(), viewName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result.SearchedLocations);
        }

        [Fact]
        public void FindPartialView_ReturnsNotFoundResult_WhenNoViewEnginesAreRegistered()
        {
            // Arrange
            var viewName = "my-partial-view";
            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new IViewEngine[0]);
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindPartialView(GetActionContext(), viewName);

            // Assert
            Assert.False(result.Success);
            Assert.Empty(result.SearchedLocations);
        }

        [Fact]
        public void FindPartialView_ReturnsNotFoundResult_WhenExactlyOneViewEngineIsRegisteredWhichReturnsNotFoundResult()
        {
            // Arrange
            var viewName = "partial-view";
            var provider = new Mock<IViewEngineProvider>();
            var engine = new Mock<IViewEngine>();
            engine.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "shared/partial-view" }));
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindPartialView(GetActionContext(), viewName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "shared/partial-view" }, result.SearchedLocations);
        }

        [Fact]
        public void FindPartialView_ReturnsView_WhenExactlyOneViewEngineIsRegisteredWhichReturnsAFoundResult()
        {
            // Arrange
            var viewName = "test-view";
            var provider = new Mock<IViewEngineProvider>();
            var engine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();
            engine.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view));
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindPartialView(GetActionContext(), viewName);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view, result.View);
        }

        [Fact]
        public void FindPartialView_ReturnsViewFromFirstViewEngineWithFoundResult()
        {
            // Arrange
            var viewName = "bar";
            var engine1 = new Mock<IViewEngine>();
            var engine2 = new Mock<IViewEngine>();
            var engine3 = new Mock<IViewEngine>();
            var view2 = Mock.Of<IView>();
            var view3 = Mock.Of<IView>();
            engine1.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()));
            engine2.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view2));
            engine3.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view3));

            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine1.Object, engine2.Object, engine3.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindPartialView(GetActionContext(), viewName);

            // Assert
            Assert.True(result.Success);
            Assert.Same(view2, result.View);
            Assert.Equal(viewName, result.ViewName);
        }

        [Fact]
        public void FindPartialView_ReturnsNotFound_IfAllViewEnginesReturnNotFound()
        {
            // Arrange
            var viewName = "foo";
            var engine1 = new Mock<IViewEngine>();
            var engine2 = new Mock<IViewEngine>();
            var engine3 = new Mock<IViewEngine>();
            engine1.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "1", "2" }));
            engine2.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "3" }));
            engine3.Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "4", "5" }));

            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine1.Object, engine2.Object, engine3.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindPartialView(GetActionContext(), viewName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result.SearchedLocations);
        }

        private static ActionContext GetActionContext()
        {
            var httpContext = Mock.Of<HttpContext>();
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }
    }
}