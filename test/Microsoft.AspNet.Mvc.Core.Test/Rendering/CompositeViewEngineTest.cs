// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class CompositeViewEngineTest
    {
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
            engine1.Setup(e => e.FindView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()));
            engine2.Setup(e => e.FindView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view2));
            engine3.Setup(e => e.FindView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view3));

            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine1.Object, engine2.Object, engine3.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindView(new Dictionary<string, object>(), viewName);

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
            engine1.Setup(e => e.FindView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "1", "2" }));
            engine2.Setup(e => e.FindView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "3" }));
            engine3.Setup(e => e.FindView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "4", "5" }));

            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine1.Object, engine2.Object, engine3.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindView(new Dictionary<string, object>(), viewName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result.SearchedLocations);
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
            engine1.Setup(e => e.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()));
            engine2.Setup(e => e.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view2));
            engine3.Setup(e => e.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.Found(viewName, view3));

            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine1.Object, engine2.Object, engine3.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindPartialView(new Dictionary<string, object>(), viewName);

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
            engine1.Setup(e => e.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "1", "2" }));
            engine2.Setup(e => e.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "3" }));
            engine3.Setup(e => e.FindPartialView(It.IsAny<IDictionary<string, object>>(), It.IsAny<string>()))
                   .Returns(ViewEngineResult.NotFound(viewName, new[] { "4", "5" }));

            var provider = new Mock<IViewEngineProvider>();
            provider.SetupGet(p => p.ViewEngines)
                    .Returns(new[] { engine1.Object, engine2.Object, engine3.Object });
            var compositeViewEngine = new CompositeViewEngine(provider.Object);

            // Act
            var result = compositeViewEngine.FindPartialView(new Dictionary<string, object>(), viewName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result.SearchedLocations);
        }
    }
}