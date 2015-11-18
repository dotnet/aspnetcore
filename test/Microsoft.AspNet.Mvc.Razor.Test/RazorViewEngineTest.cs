// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Test
{
    public class RazorViewEngineTest
    {
        private static readonly Dictionary<string, object> _areaTestContext = new Dictionary<string, object>()
        {
            {"area", "foo"},
            {"controller", "bar"},
        };

        private static readonly Dictionary<string, object> _controllerTestContext = new Dictionary<string, object>()
        {
            {"controller", "bar"},
        };

        public static IEnumerable<string[]> AbsoluteViewPathData
        {
            get
            {
                yield return new[] { "~/foo/bar" };
                yield return new[] { "/foo/bar" };
                yield return new[] { "~/foo/bar.txt" };
                yield return new[] { "/foo/bar.txt" };
            }
        }

        public static IEnumerable<object[]> ViewLocationExpanderTestData
        {
            get
            {
                yield return new object[]
                {
                    _controllerTestContext,
                    new[]
                    {
                        "/Views/{1}/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    }
                };

                yield return new object[]
                {
                    _areaTestContext,
                    new[]
                    {
                        "/Areas/{2}/Views/{1}/{0}.cshtml",
                        "/Areas/{2}/Views/Shared/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    }
                };
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FindView_IsMainPage_ThrowsIfViewNameIsNullOrEmpty(string viewName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => viewEngine.FindView(context, viewName, isMainPage: true),
                "viewName");
        }

        [Theory]
        [MemberData(nameof(AbsoluteViewPathData))]
        public void FindView_IsMainPage_WithFullPath_ReturnsNotFound(string viewName)
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, viewName, isMainPage: true);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [MemberData(nameof(AbsoluteViewPathData))]
        public void FindView_IsMainPage_WithFullPathAndCshtmlEnding_ReturnsNotFound(string viewName)
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();
            var context = GetActionContext(_controllerTestContext);
            viewName += ".cshtml";

            // Act
            var result = viewEngine.FindView(context, viewName, isMainPage: true);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FindView_WithRelativePath_ReturnsNotFound(bool isMainPage)
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "View.cshtml", isMainPage);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetView_WithViewName_ReturnsNotFound(bool isMainPage)
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();

            // Act
            var result = viewEngine.GetView("~/Home/View1.cshtml", "View2", isMainPage);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void FindView_ReturnsRazorView_IfLookupWasSuccessful()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            var viewStart1 = Mock.Of<IRazorPage>();
            var viewStart2 = Mock.Of<IRazorPage>();

            pageFactory
                .Setup(p => p.CreateFactory("/Views/bar/test-view.cshtml"))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]));

            pageFactory
                .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => viewStart2, new IChangeToken[0]));

            pageFactory
                .Setup(p => p.CreateFactory("/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => viewStart1, new IChangeToken[0]));

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "test-view", isMainPage: false);

            // Assert
            Assert.True(result.Success);
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Same(page, view.RazorPage);
            Assert.Equal("test-view", result.ViewName);
            Assert.Empty(view.ViewStartPages);
        }

        [Fact]
        public void FindView_DoesNotExpireCachedResults_IfViewStartsExpire()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            var viewStart = Mock.Of<IRazorPage>();
            var cancellationTokenSource = new CancellationTokenSource();
            var changeToken = new CancellationChangeToken(cancellationTokenSource.Token);

            pageFactory
                .Setup(p => p.CreateFactory("/Views/bar/test-view.cshtml"))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]));

            pageFactory
                .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => viewStart, new[] { changeToken }));

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act - 1
            var result1 = viewEngine.FindView(context, "test-view", isMainPage: false);

            // Assert - 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page, view1.RazorPage);
            Assert.Equal("test-view", result1.ViewName);
            Assert.Empty(view1.ViewStartPages);

            // Act - 2
            cancellationTokenSource.Cancel();
            var result2 = viewEngine.FindView(context, "test-view", isMainPage: false);

            // Assert - 2
            Assert.True(result2.Success);
            var view2 = Assert.IsType<RazorView>(result2.View);
            Assert.Same(page, view2.RazorPage);
            pageFactory.Verify(p => p.CreateFactory("/Views/bar/test-view.cshtml"), Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FindView_ThrowsIfViewNameIsNullOrEmpty(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => viewEngine.FindView(context, partialViewName, isMainPage: false),
                "viewName");
        }

        [Theory]
        [MemberData(nameof(AbsoluteViewPathData))]
        public void FindViewWithFullPath_ReturnsNotFound(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, partialViewName, isMainPage: false);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [MemberData(nameof(AbsoluteViewPathData))]
        public void FindViewWithFullPathAndCshtmlEnding_ReturnsNotFound(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();
            var context = GetActionContext(_controllerTestContext);
            partialViewName += ".cshtml";

            // Act
            var result = viewEngine.FindView(context, partialViewName, isMainPage: false);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FindView_FailsButSearchesCorrectLocationsWithAreas(bool isMainPage)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_areaTestContext);

            // Act
            var result = viewEngine.FindView(context, "viewName", isMainPage);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[]
            {
                "/Areas/foo/Views/bar/viewName.cshtml",
                "/Areas/foo/Views/Shared/viewName.cshtml",
                "/Views/Shared/viewName.cshtml",
            }, result.SearchedLocations);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FindView_FailsButSearchesCorrectLocationsWithoutAreas(bool isMainPage)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "viewName", isMainPage);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] {
                "/Views/bar/viewName.cshtml",
                "/Views/Shared/viewName.cshtml",
            }, result.SearchedLocations);
        }

        [Fact]
        public void FindView_IsMainPage_ReturnsRazorView_IfLookupWasSuccessful()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            var viewStart1 = Mock.Of<IRazorPage>();
            var viewStart2 = Mock.Of<IRazorPage>();

            pageFactory
                .Setup(p => p.CreateFactory("/Views/bar/test-view.cshtml"))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]));

            pageFactory
                .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => viewStart2, new IChangeToken[0]));

            pageFactory
                .Setup(p => p.CreateFactory("/_ViewStart.cshtml"))
                .Returns(new RazorPageFactoryResult(() => viewStart1, new IChangeToken[0]));

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "test-view", isMainPage: true);

            // Assert
            Assert.True(result.Success);
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Equal("test-view", result.ViewName);
            Assert.Same(page, view.RazorPage);

            // ViewStartPages is not empty as it is in FindView_ReturnsRazorView_IfLookupWasSuccessful() despite
            // (faked) existence of the view start files in both tests.
            Assert.Equal(new[] { viewStart1, viewStart2 }, view.ViewStartPages);
        }

        [Fact]
        public void FindView_UsesViewLocationFormat_IfRouteDoesNotContainArea()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();

            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory("fake-path1/bar/test-view.rzr"))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());
            viewEngine.SetLocationFormats(
                new[] { "fake-path1/{1}/{0}.rzr" },
                new[] { "fake-area-path/{2}/{1}/{0}.rzr" });
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "test-view", isMainPage: true);

            // Assert
            pageFactory.Verify();
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Same(page, view.RazorPage);
        }

        [Fact]
        public void FindView_UsesAreaViewLocationFormat_IfRouteContainsArea()
        {
            // Arrange
            var viewName = "test-view2";
            var expectedViewName = "fake-area-path/foo/bar/test-view2.rzr";
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(expectedViewName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());
            viewEngine.SetLocationFormats(
                new[] { "fake-path1/{1}/{0}.rzr" },
                new[] { "fake-area-path/{2}/{1}/{0}.rzr" });
            var context = GetActionContext(_areaTestContext);

            // Act
            var result = viewEngine.FindView(context, viewName, isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(viewName, result.ViewName);
            pageFactory.Verify();
        }

        [Theory]
        [InlineData("Test-View.cshtml")]
        [InlineData("/Home/Test-View.cshtml")]
        public void GetView_DoesNotUseViewLocationFormat_WithRelativePath_IfRouteDoesNotContainArea(string viewName)
        {
            // Arrange
            var expectedViewName = "/Home/Test-View.cshtml";
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(expectedViewName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act
            var result = viewEngine.GetView("/Home/Page.cshtml", viewName, isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(viewName, result.ViewName);
            pageFactory.Verify();
        }

        [Theory]
        [InlineData("Test-View.cshtml")]
        [InlineData("/Home/Test-View.cshtml")]
        public void GetView_DoesNotUseViewLocationFormat_WithRelativePath_IfRouteContainArea(string viewName)
        {
            // Arrange
            var expectedViewName = "/Home/Test-View.cshtml";
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(expectedViewName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act
            var result = viewEngine.GetView("/Home/Page.cshtml", viewName, isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(viewName, result.ViewName);
            pageFactory.Verify();
        }

        [Theory]
        [InlineData("/Test-View.cshtml")]
        [InlineData("~/Test-View.CSHTML")]
        [InlineData("/Home/Test-View.CSHTML")]
        [InlineData("~/Home/Test-View.cshtml")]
        [InlineData("~/SHARED/TEST-VIEW.CSHTML")]
        public void GetView_UsesGivenPath_WithAppRelativePath(string viewName)
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(viewName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act
            var result = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(viewName, result.ViewName);
            pageFactory.Verify();
        }

        [Theory]
        [InlineData("Test-View.cshtml")]
        [InlineData("Test-View.CSHTML")]
        [InlineData("PATH/TEST-VIEW.CSHTML")]
        [InlineData("Path1/Path2/Test-View.cshtml")]
        public void GetView_ResolvesRelativeToCurrentPage_WithRelativePath(string viewName)
        {
            // Arrange
            var expectedViewName = $"/Home/{ viewName }";
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(expectedViewName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act
            var result = viewEngine.GetView("/Home/Page.cshtml", viewName, isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(viewName, result.ViewName);
            pageFactory.Verify();
        }

        [Theory]
        [InlineData("Test-View.cshtml")]
        [InlineData("Test-View.CSHTML")]
        [InlineData("PATH/TEST-VIEW.CSHTML")]
        [InlineData("Path1/Path2/Test-View.cshtml")]
        public void GetView_ResolvesRelativeToAppRoot_WithRelativePath_IfNoPageExecuting(string viewName)
        {
            // Arrange
            var expectedViewName = $"/{ viewName }";
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(expectedViewName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act
            var result = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(viewName, result.ViewName);
            pageFactory.Verify();
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Same(page, view.RazorPage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FindView_CreatesDifferentCacheEntries_ForAreaViewsAndNonAreaViews(bool isMainPage)
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var areaPage = Mock.Of<IRazorPage>();
            var nonAreaPage = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory("/Areas/Admin/Views/Home/Index.cshtml"))
                .Returns(new RazorPageFactoryResult(() => areaPage, new IChangeToken[0]));
            pageFactory
                .Setup(p => p.CreateFactory("/Views/Home/Index.cshtml"))
                .Returns(new RazorPageFactoryResult(() => nonAreaPage, new IChangeToken[0]));

            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act 1
            var areaContext = GetActionContext(new Dictionary<string, object>()
            {
                {"area", "Admin"},
                {"controller", "Home"},
            });
            var result1 = viewEngine.FindView(areaContext, "Index", isMainPage);

            // Assert 1
            Assert.NotNull(result1);
            pageFactory.Verify(p => p.CreateFactory("/Areas/Admin/Views/Home/Index.cshtml"), Times.Once());
            pageFactory.Verify(p => p.CreateFactory("/Views/Home/Index.cshtml"), Times.Never());
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(areaPage, view1.RazorPage);

            // Act 2
            var nonAreaContext = GetActionContext(new Dictionary<string, object>()
            {
                {"controller", "Home"},
            });
            var result2 = viewEngine.FindView(nonAreaContext, "Index", isMainPage);

            // Assert 2
            Assert.NotNull(result2);
            pageFactory.Verify(p => p.CreateFactory("/Areas/Admin/Views/Home/Index.cshtml"), Times.Once());
            pageFactory.Verify(p => p.CreateFactory("/Views/Home/Index.cshtml"), Times.Once());
            var view2 = Assert.IsType<RazorView>(result2.View);
            Assert.Same(nonAreaPage, view2.RazorPage);

            // Act 3
            // Ensure we're getting cached results.
            var result3 = viewEngine.FindView(areaContext, "Index", isMainPage);
            var result4 = viewEngine.FindView(nonAreaContext, "Index", isMainPage);

            // Assert 4
            pageFactory.Verify(p => p.CreateFactory("/Areas/Admin/Views/Home/Index.cshtml"), Times.Once());
            pageFactory.Verify(p => p.CreateFactory("/Views/Home/Index.cshtml"), Times.Once());

            var view3 = Assert.IsType<RazorView>(result3.View);
            Assert.Same(areaPage, view3.RazorPage);
            var view4 = Assert.IsType<RazorView>(result4.View);
            Assert.Same(nonAreaPage, view4.RazorPage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FindView_CreatesDifferentCacheEntries_ForDifferentAreas(bool isMainPage)
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var areaPage1 = Mock.Of<IRazorPage>();
            var areaPage2 = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory("/Areas/Marketing/Views/Home/Index.cshtml"))
                .Returns(new RazorPageFactoryResult(() => areaPage1, new IChangeToken[0]));
            pageFactory
                .Setup(p => p.CreateFactory("/Areas/Sales/Views/Home/Index.cshtml"))
                .Returns(new RazorPageFactoryResult(() => areaPage2, new IChangeToken[0]));

            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act 1
            var areaContext1 = GetActionContext(new Dictionary<string, object>()
            {
                {"area", "Marketing"},
                {"controller", "Home"},
            });
            var result1 = viewEngine.FindView(areaContext1, "Index", isMainPage);

            // Assert 1
            Assert.NotNull(result1);
            pageFactory.Verify(p => p.CreateFactory("/Areas/Marketing/Views/Home/Index.cshtml"), Times.Once());
            pageFactory.Verify(p => p.CreateFactory("/Areas/Sales/Views/Home/Index.cshtml"), Times.Never());
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(areaPage1, view1.RazorPage);

            // Act 2
            var areaContext2 = GetActionContext(new Dictionary<string, object>()
            {
                {"controller", "Home"},
                {"area", "Sales"},
            });
            var result2 = viewEngine.FindView(areaContext2, "Index", isMainPage);

            // Assert 2
            Assert.NotNull(result2);
            pageFactory.Verify(p => p.CreateFactory("/Areas/Marketing/Views/Home/Index.cshtml"), Times.Once());
            pageFactory.Verify(p => p.CreateFactory("/Areas/Sales/Views/Home/Index.cshtml"), Times.Once());
            var view2 = Assert.IsType<RazorView>(result2.View);
            Assert.Same(areaPage2, view2.RazorPage);

            // Act 3
            // Ensure we're getting cached results.
            var result3 = viewEngine.FindView(areaContext1, "Index", isMainPage);
            var result4 = viewEngine.FindView(areaContext2, "Index", isMainPage);

            // Assert 4
            pageFactory.Verify(p => p.CreateFactory("/Areas/Marketing/Views/Home/Index.cshtml"), Times.Once());
            pageFactory.Verify(p => p.CreateFactory("/Areas/Sales/Views/Home/Index.cshtml"), Times.Once());

            var view3 = Assert.IsType<RazorView>(result3.View);
            Assert.Same(areaPage1, view3.RazorPage);
            var view4 = Assert.IsType<RazorView>(result4.View);
            Assert.Same(areaPage2, view4.RazorPage);
        }

        [Theory]
        [MemberData(nameof(ViewLocationExpanderTestData))]
        public void FindView_UsesViewLocationExpandersToLocateViews(
            IDictionary<string, object> routeValues,
            IEnumerable<string> expectedSeeds)
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory("test-string/bar.cshtml"))
                .Returns(new RazorPageFactoryResult(() => Mock.Of<IRazorPage>(), new IChangeToken[0]))
                .Verifiable();

            var expander1Result = new[] { "some-seed" };
            var expander1 = new Mock<IViewLocationExpander>();
            expander1
                .Setup(e => e.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                .Callback((ViewLocationExpanderContext c) =>
                {
                    Assert.NotNull(c.ActionContext);
                    c.Values["expander-key"] = expander1.ToString();
                })
                .Verifiable();
            expander1
                .Setup(e => e.ExpandViewLocations(
                    It.IsAny<ViewLocationExpanderContext>(),
                    It.IsAny<IEnumerable<string>>()))
                .Callback((ViewLocationExpanderContext c, IEnumerable<string> seeds) =>
                {
                    Assert.NotNull(c.ActionContext);
                    Assert.Equal(expectedSeeds, seeds);
                })
                .Returns(expander1Result)
                .Verifiable();

            var expander2 = new Mock<IViewLocationExpander>();
            expander2
                .Setup(e => e.ExpandViewLocations(
                    It.IsAny<ViewLocationExpanderContext>(),
                    It.IsAny<IEnumerable<string>>()))
                .Callback((ViewLocationExpanderContext c, IEnumerable<string> seeds) =>
                {
                    Assert.Equal(expander1Result, seeds);
                })
                .Returns(new[] { "test-string/{1}.cshtml" })
                .Verifiable();

            var viewEngine = CreateViewEngine(
                pageFactory.Object,
                new[] { expander1.Object, expander2.Object });
            var context = GetActionContext(routeValues);

            // Act
            var result = viewEngine.FindView(context, "test-view", isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.IsAssignableFrom<IView>(result.View);
            pageFactory.Verify();
            expander1.Verify();
            expander2.Verify();
        }

        [Fact]
        public void FindView_CachesValuesIfViewWasFound()
        {
            // Arrange
            var page = Mock.Of<IRazorPage>();
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory("/Views/bar/baz.cshtml"))
                .Returns(new RazorPageFactoryResult(new IChangeToken[0]))
                .Verifiable();
            pageFactory
               .Setup(p => p.CreateFactory("/Views/Shared/baz.cshtml"))
               .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
               .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act 1
            var result1 = viewEngine.FindView(context, "baz", isMainPage: true);

            // Assert 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page, view1.RazorPage);
            pageFactory.Verify();

            // Act 2
            pageFactory
               .Setup(p => p.CreateFactory(It.IsAny<string>()))
               .Throws(new Exception("Shouldn't be called"));

            var result2 = viewEngine.FindView(context, "baz", isMainPage: true);

            // Assert 2
            Assert.True(result2.Success);
            var view2 = Assert.IsType<RazorView>(result2.View);
            Assert.Same(page, view2.RazorPage);
            pageFactory.Verify();
        }

        [Fact]
        public void FindView_InvokesPageFactoryIfChangeTokenExpired()
        {
            // Arrange
            var page1 = Mock.Of<IRazorPage>();
            var page2 = Mock.Of<IRazorPage>();
            var sequence = new MockSequence();
            var cancellationTokenSource = new CancellationTokenSource();
            var changeToken = new CancellationChangeToken(cancellationTokenSource.Token);

            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .InSequence(sequence)
                .Setup(p => p.CreateFactory("/Views/bar/baz.cshtml"))
                .Returns(new RazorPageFactoryResult(new[] { changeToken }));
            pageFactory
               .InSequence(sequence)
               .Setup(p => p.CreateFactory("/Views/Shared/baz.cshtml"))
               .Returns(new RazorPageFactoryResult(() => page1, new IChangeToken[0]))
               .Verifiable();
            pageFactory
                .InSequence(sequence)
                .Setup(p => p.CreateFactory("/Views/bar/baz.cshtml"))
                .Returns(new RazorPageFactoryResult(() => page2, new IChangeToken[0]));

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act 1
            var result1 = viewEngine.FindView(context, "baz", isMainPage: true);

            // Assert 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page1, view1.RazorPage);

            // Act 2
            cancellationTokenSource.Cancel();
            var result2 = viewEngine.FindView(context, "baz", isMainPage: true);

            // Assert 2
            Assert.True(result2.Success);
            var view2 = Assert.IsType<RazorView>(result2.View);
            Assert.Same(page2, view2.RazorPage);
            pageFactory.Verify();
        }

        [Fact]
        public void FindView_InvokesPageFactoryIfViewStartExpirationTokensHaveExpired()
        {
            // Arrange
            var page1 = Mock.Of<IRazorPage>();
            var page2 = Mock.Of<IRazorPage>();
            var viewStart = Mock.Of<IRazorPage>();
            var sequence = new MockSequence();
            var cancellationTokenSource = new CancellationTokenSource();
            var changeToken = new CancellationChangeToken(cancellationTokenSource.Token);

            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .InSequence(sequence)
                .Setup(p => p.CreateFactory("/Views/bar/baz.cshtml"))
                .Returns(new RazorPageFactoryResult(() => page1, new IChangeToken[0]));
            pageFactory
                .InSequence(sequence)
               .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
               .Returns(new RazorPageFactoryResult(new[] { changeToken }))
               .Verifiable();
            pageFactory
                .InSequence(sequence)
                .Setup(p => p.CreateFactory("/Views/bar/baz.cshtml"))
                .Returns(new RazorPageFactoryResult(() => page2, new IChangeToken[0]));
            pageFactory
                .InSequence(sequence)
               .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
               .Returns(new RazorPageFactoryResult(() => viewStart, new IChangeToken[0]));

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act 1
            var result1 = viewEngine.FindView(context, "baz", isMainPage: true);

            // Assert 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page1, view1.RazorPage);
            Assert.Empty(view1.ViewStartPages);

            // Act 2
            cancellationTokenSource.Cancel();
            var result2 = viewEngine.FindView(context, "baz", isMainPage: true);

            // Assert 2
            Assert.True(result2.Success);
            var view2 = Assert.IsType<RazorView>(result2.View);
            Assert.Same(page2, view2.RazorPage);
            var actualViewStart = Assert.Single(view2.ViewStartPages);
            Assert.Equal(viewStart, actualViewStart);
            pageFactory.Verify();
        }

        // This test validates an important perf scenario of RazorViewEngine not constructing
        // multiple strings for views that do not exist in the file system on a per-request basis.
        [Fact]
        public void FindView_DoesNotInvokeViewLocationExpanders_IfChangeTokenHasNotExpired()
        {
            // Arrange
            var pageFactory = Mock.Of<IRazorPageFactoryProvider>();
            var expander = new Mock<IViewLocationExpander>();
            var expandedLocations = new[]
            {
                "viewlocation1",
                "viewlocation2",
                "viewlocation3",
            };
            expander
                .Setup(v => v.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                .Callback((ViewLocationExpanderContext expanderContext) =>
                {
                    expanderContext.Values["somekey"] = "somevalue";
                })
                .Verifiable();
            expander
                .Setup(v => v.ExpandViewLocations(
                    It.IsAny<ViewLocationExpanderContext>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns(expandedLocations)
                .Verifiable();

            var viewEngine = CreateViewEngine(
                pageFactory,
                expanders: new[] { expander.Object });
            var context = GetActionContext(_controllerTestContext);

            // Act - 1
            var result = viewEngine.FindView(context, "myview", isMainPage: true);

            // Assert - 1
            Assert.False(result.Success);
            Assert.Equal(expandedLocations, result.SearchedLocations);
            expander.Verify();

            // Act - 2
            result = viewEngine.FindView(context, "myview", isMainPage: true);

            // Assert - 2
            Assert.False(result.Success);
            Assert.Equal(expandedLocations, result.SearchedLocations);
            expander.Verify(
                v => v.PopulateValues(It.IsAny<ViewLocationExpanderContext>()),
                Times.Exactly(2));
            expander.Verify(
                v => v.ExpandViewLocations(It.IsAny<ViewLocationExpanderContext>(), It.IsAny<IEnumerable<string>>()),
                Times.Once());
        }

        [Fact]
        public void FindView_InvokesViewLocationExpanders_IfChangeTokenExpires()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var changeToken = new CancellationChangeToken(cancellationTokenSource.Token);
            var page = Mock.Of<IRazorPage>();
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory("viewlocation3"))
                .Returns(new RazorPageFactoryResult(new[] { changeToken }));
            var expander = new Mock<IViewLocationExpander>();
            var expandedLocations = new[]
            {
                "viewlocation1",
                "viewlocation2",
                "viewlocation3",
            };
            expander
                .Setup(v => v.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                .Callback((ViewLocationExpanderContext expanderContext) =>
                {
                    expanderContext.Values["somekey"] = "somevalue";
                })
                .Verifiable();
            expander
                .Setup(v => v.ExpandViewLocations(
                    It.IsAny<ViewLocationExpanderContext>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns(expandedLocations)
                .Verifiable();

            var viewEngine = CreateViewEngine(
                pageFactory.Object,
                expanders: new[] { expander.Object });
            var context = GetActionContext(_controllerTestContext);

            // Act - 1
            var result = viewEngine.FindView(context, "MyView", isMainPage: true);

            // Assert - 1
            Assert.False(result.Success);
            Assert.Equal(expandedLocations, result.SearchedLocations);
            expander.Verify();

            // Act - 2
            pageFactory
                .Setup(p => p.CreateFactory("viewlocation3"))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]));
            cancellationTokenSource.Cancel();
            result = viewEngine.FindView(context, "MyView", isMainPage: true);

            // Assert - 2
            Assert.True(result.Success);
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Same(page, view.RazorPage);
            expander.Verify(
                v => v.PopulateValues(It.IsAny<ViewLocationExpanderContext>()),
                Times.Exactly(2));
            expander.Verify(
                v => v.ExpandViewLocations(It.IsAny<ViewLocationExpanderContext>(), It.IsAny<IEnumerable<string>>()),
                Times.Exactly(2));
        }

        [Theory]
        [MemberData(nameof(AbsoluteViewPathData))]
        public void FindPage_WithFullPath_ReturnsNotFound(string viewName)
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindPage(context, viewName);

            // Assert
            Assert.Null(result.Page);
        }

        [Theory]
        [MemberData(nameof(AbsoluteViewPathData))]
        public void FindPage_WithFullPathAndCshtmlEnding_ReturnsNotFound(string viewName)
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();
            var context = GetActionContext(_controllerTestContext);
            viewName += ".cshtml";

            // Act
            var result = viewEngine.FindPage(context, viewName);

            // Assert
            Assert.Null(result.Page);
        }

        [Fact]
        public void FindPage_WithRelativePath_ReturnsNotFound()
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindPage(context, "View.cshtml");

            // Assert
            Assert.Null(result.Page);
        }

        [Fact]
        public void GetPage_WithViewName_ReturnsNotFound()
        {
            // Arrange
            var viewEngine = CreateSuccessfulViewEngine();

            // Act
            var result = viewEngine.GetPage("~/Home/View1.cshtml", "View2");

            // Assert
            Assert.Null(result.Page);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FindPage_ThrowsIfNameIsNullOrEmpty(string pageName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => viewEngine.FindPage(context, pageName),
                "pageName");
        }

        [Theory]
        [MemberData(nameof(ViewLocationExpanderTestData))]
        public void FindPage_UsesViewLocationExpander_ToExpandPaths(
            IDictionary<string, object> routeValues,
            IEnumerable<string> expectedSeeds)
        {
            // Arrange
            var page = Mock.Of<IRazorPage>();
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory("expanded-path/bar-layout"))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();

            var expander = new Mock<IViewLocationExpander>();
            expander
                .Setup(e => e.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                .Callback((ViewLocationExpanderContext c) =>
                {
                    Assert.NotNull(c.ActionContext);
                    c.Values["expander-key"] = expander.ToString();
                })
                .Verifiable();
            expander
                .Setup(e => e.ExpandViewLocations(
                    It.IsAny<ViewLocationExpanderContext>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns((ViewLocationExpanderContext c, IEnumerable<string> seeds) =>
                {
                    Assert.NotNull(c.ActionContext);
                    Assert.Equal(expectedSeeds, seeds);

                    Assert.Equal(expander.ToString(), c.Values["expander-key"]);

                    return new[] { "expanded-path/bar-{0}" };
                })
                .Verifiable();

            var viewEngine = CreateViewEngine(
                pageFactory.Object,
                new[] { expander.Object });
            var context = GetActionContext(routeValues);

            // Act
            var result = viewEngine.FindPage(context, "layout");

            // Assert
            Assert.Equal("layout", result.Name);
            Assert.Same(page, result.Page);
            Assert.Null(result.SearchedLocations);
            pageFactory.Verify();
            expander.Verify();
        }

        [Fact]
        public void FindPage_ReturnsSearchedLocationsIfPageCannotBeFound()
        {
            // Arrange
            var expected = new[]
            {
                "/Views/bar/layout.cshtml",
                "/Views/Shared/layout.cshtml",
            };

            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindPage(context, "layout");

            // Assert
            Assert.Equal("layout", result.Name);
            Assert.Null(result.Page);
            Assert.Equal(expected, result.SearchedLocations);
        }

        [Theory]
        // Looks in RouteValueDefaults
        [InlineData(true)]
        // Looks in RouteConstraints
        [InlineData(false)]
        public void FindPage_SelectsActionCaseInsensitively(bool isAttributeRouted)
        {
            // The ActionDescriptor contains "Foo" and the RouteData contains "foo"
            // which matches the case of the constructor thus searching in the appropriate location.
            // Arrange
            var routeValues = new Dictionary<string, object>
            {
                { "controller", "foo" }
            };
            var page = new Mock<IRazorPage>(MockBehavior.Strict);
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory("/Views/Foo/details.cshtml"))
                .Returns(new RazorPageFactoryResult(() => page.Object, new IChangeToken[0]))
                .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var routesInActionDescriptor = new Dictionary<string, string>()
            {
                { "controller", "Foo" }
            };

            var context = GetActionContextWithActionDescriptor(
                routeValues,
                routesInActionDescriptor,
                isAttributeRouted);

            // Act
            var result = viewEngine.FindPage(context, "details");

            // Assert
            Assert.Equal("details", result.Name);
            Assert.Same(page.Object, result.Page);
            Assert.Null(result.SearchedLocations);
            pageFactory.Verify();
        }

        [Theory]
        // Looks in RouteValueDefaults
        [InlineData(true)]
        // Looks in RouteConstraints
        [InlineData(false)]
        public void FindPage_LooksForPages_UsingActionDescriptor_Controller(bool isAttributeRouted)
        {
            // Arrange
            var expected = new[]
            {
                "/Views/bar/foo.cshtml",
                "/Views/Shared/foo.cshtml",
            };

            var routeValues = new Dictionary<string, object>
            {
                { "controller", "Bar" }
            };
            var routesInActionDescriptor = new Dictionary<string, string>()
            {
                { "controller", "bar" }
            };

            var viewEngine = CreateViewEngine();
            var context = GetActionContextWithActionDescriptor(
                routeValues,
                routesInActionDescriptor,
                isAttributeRouted);

            // Act
            var result = viewEngine.FindPage(context, "foo");

            // Assert
            Assert.Equal("foo", result.Name);
            Assert.Null(result.Page);
            Assert.Equal(expected, result.SearchedLocations);
        }

        [Theory]
        // Looks in RouteValueDefaults
        [InlineData(true)]
        // Looks in RouteConstraints
        [InlineData(false)]
        public void FindPage_LooksForPages_UsingActionDescriptor_Areas(bool isAttributeRouted)
        {
            // Arrange
            var expected = new[]
            {
                "/Areas/world/Views/bar/foo.cshtml",
                "/Areas/world/Views/Shared/foo.cshtml",
                "/Views/Shared/foo.cshtml"
            };

            var routeValues = new Dictionary<string, object>
            {
                { "controller", "Bar" },
                { "area", "World" }
            };
            var routesInActionDescriptor = new Dictionary<string, string>()
            {
                { "controller", "bar" },
                { "area", "world" }
            };

            var viewEngine = CreateViewEngine();
            var context = GetActionContextWithActionDescriptor(
                routeValues,
                routesInActionDescriptor,
                isAttributeRouted);

            // Act
            var result = viewEngine.FindPage(context, "foo");

            // Assert
            Assert.Equal("foo", result.Name);
            Assert.Null(result.Page);
            Assert.Equal(expected, result.SearchedLocations);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FindPage_LooksForPages_UsesRouteValuesAsFallback(bool isAttributeRouted)
        {
            // Arrange
            var expected = new[]
            {
                "/Views/foo/bar.cshtml",
                "/Views/Shared/bar.cshtml",
            };

            var routeValues = new Dictionary<string, object>()
            {
                { "controller", "foo" }
            };

            var viewEngine = CreateViewEngine();
            var context = GetActionContextWithActionDescriptor(
                routeValues,
                new Dictionary<string, string>(),
                isAttributeRouted);

            // Act
            var result = viewEngine.FindPage(context, "bar");

            // Assert
            Assert.Equal("bar", result.Name);
            Assert.Null(result.Page);
            Assert.Equal(expected, result.SearchedLocations);
        }

        [Theory]
        [InlineData("/Test-View.cshtml")]
        [InlineData("~/Test-View.CSHTML")]
        [InlineData("/Home/Test-View.CSHTML")]
        [InlineData("~/Home/Test-View.cshtml")]
        [InlineData("~/SHARED/TEST-VIEW.CSHTML")]
        public void GetPage_UsesGivenPath_WithAppRelativePath(string pageName)
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(pageName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act
            var result = viewEngine.GetPage("~/Another/Place.cshtml", pagePath: pageName);

            // Assert
            Assert.Same(page, result.Page);
            Assert.Equal(pageName, result.Name);
            pageFactory.Verify();
        }

        [Theory]
        [InlineData("Test-View.cshtml")]
        [InlineData("Test-View.CSHTML")]
        [InlineData("PATH/TEST-VIEW.CSHTML")]
        [InlineData("Path1/Path2/Test-View.cshtml")]
        public void GetPage_ResolvesRelativeToCurrentPage_WithRelativePath(string pageName)
        {
            // Arrange
            var expectedPageName = $"/Home/{ pageName }";
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(expectedPageName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act
            var result = viewEngine.GetPage("/Home/Page.cshtml", pageName);

            // Assert
            Assert.Same(page, result.Page);
            Assert.Equal(pageName, result.Name);
            pageFactory.Verify();
        }

        [Theory]
        [InlineData("Test-View.cshtml")]
        [InlineData("Test-View.CSHTML")]
        [InlineData("PATH/TEST-VIEW.CSHTML")]
        [InlineData("Path1/Path2/Test-View.cshtml")]
        public void GetPage_ResolvesRelativeToAppRoot_WithRelativePath_IfNoPageExecuting(string pageName)
        {
            // Arrange
            var expectedPageName = $"/{ pageName }";
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory(expectedPageName))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor());

            // Act
            var result = viewEngine.GetPage(executingFilePath: null, pagePath: pageName);

            // Assert
            Assert.Same(page, result.Page);
            Assert.Equal(pageName, result.Name);
            pageFactory.Verify();
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "")]
        [InlineData(null, "Page")]
        [InlineData(null, "Folder/Page")]
        [InlineData(null, "Folder1/Folder2/Page")]
        [InlineData("/Home/Index.cshtml", null)]
        [InlineData("/Home/Index.cshtml", "")]
        [InlineData("/Home/Index.cshtml", "Page")]
        [InlineData("/Home/Index.cshtml", "Folder/Page")]
        [InlineData("/Home/Index.cshtml", "Folder1/Folder2/Page")]
        public void GetAbsolutePath_ReturnsPagePathUnchanged_IfNotAPath(string executingFilePath, string pagePath)
        {
            // Arrange
            var viewEngine = CreateViewEngine();

            // Act
            var result = viewEngine.GetAbsolutePath(executingFilePath, pagePath);

            // Assert
            Assert.Same(pagePath, result);
        }

        [Theory]
        [InlineData(null, "/Page")]
        [InlineData(null, "~/Folder/Page.cshtml")]
        [InlineData(null, "/Folder1/Folder2/Page.rzr")]
        [InlineData("/Home/Index.cshtml", "~/Page")]
        [InlineData("/Home/Index.cshtml", "/Folder/Page.cshtml")]
        [InlineData("/Home/Index.cshtml", "~/Folder1/Folder2/Page.rzr")]
        public void GetAbsolutePath_ReturnsPagePathUnchanged_IfAppRelative(string executingFilePath, string pagePath)
        {
            // Arrange
            var viewEngine = CreateViewEngine();

            // Act
            var result = viewEngine.GetAbsolutePath(executingFilePath, pagePath);

            // Assert
            Assert.Same(pagePath, result);
        }

        [Theory]
        [InlineData("Page.cshtml")]
        [InlineData("Folder/Page.cshtml")]
        [InlineData("../../Folder1/Folder2/Page.cshtml")]
        public void GetAbsolutePath_ResolvesRelativeToExecutingPage(string pagePath)
        {
            // Arrange
            var expectedPagePath = "/Home/" + pagePath;
            var viewEngine = CreateViewEngine();

            // Act
            var result = viewEngine.GetAbsolutePath("/Home/Page.cshtml", pagePath);

            // Assert
            Assert.Equal(expectedPagePath, result);
        }

        [Theory]
        [InlineData("Page.cshtml")]
        [InlineData("Folder/Page.cshtml")]
        [InlineData("../../Folder1/Folder2/Page.cshtml")]
        public void GetAbsolutePath_ResolvesRelativeToAppRoot_IfNoPageExecuting(string pagePath)
        {
            // Arrange
            var expectedPagePath = "/" + pagePath;
            var viewEngine = CreateViewEngine();

            // Act
            var result = viewEngine.GetAbsolutePath(executingFilePath: null, pagePath: pagePath);

            // Assert
            Assert.Equal(expectedPagePath, result);
        }

        [Fact]
        public void AreaViewLocationFormats_ContainsExpectedLocations()
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var areaViewLocations = new string[]
            {
                "/Areas/{2}/Views/{1}/{0}.cshtml",
                "/Areas/{2}/Views/Shared/{0}.cshtml",
                "/Views/Shared/{0}.cshtml"
            };

            // Act & Assert
            Assert.Equal(areaViewLocations, viewEngine.AreaViewLocationFormats);
        }

        [Fact]
        public void ViewLocationFormats_ContainsExpectedLocations()
        {
            // Arrange
            var viewEngine = CreateViewEngine();

            var viewLocations = new string[]
            {
                "/Views/{1}/{0}.cshtml",
                "/Views/Shared/{0}.cshtml"
            };

            // Act & Assert
            Assert.Equal(viewLocations, viewEngine.ViewLocationFormats);
        }

        [Fact]
        public void GetNormalizedRouteValue_ReturnsValueFromRouteConstraints_IfKeyHandlingIsRequired()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor
            {
                RouteConstraints = new[]
                {
                    new RouteDataActionConstraint(key, "Route-Value")
                }
            };

            var actionContext = new ActionContext
            {
                ActionDescriptor = actionDescriptor,
                RouteData = new RouteData()
            };

            actionContext.RouteData.Values[key] = "route-value";

            // Act
            var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

            // Assert
            Assert.Equal("Route-Value", result);
        }

        [Fact]
        public void GetNormalizedRouteValue_ReturnsRouteValue_IfValueDoesNotMatchRouteConstraint()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor
            {
                RouteConstraints = new[]
                {
                    new RouteDataActionConstraint(key, "different-value")
                }
            };

            var actionContext = new ActionContext
            {
                ActionDescriptor = actionDescriptor,
                RouteData = new RouteData()
            };

            actionContext.RouteData.Values[key] = "route-value";

            // Act
            var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

            // Assert
            Assert.Equal("route-value", result);
        }

        [Fact]
        public void GetNormalizedRouteValue_ReturnsNull_IfRouteConstraintKeyHandlingIsDeny()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor
            {
                RouteConstraints = new[]
                {
                    new RouteDataActionConstraint(key, routeValue: string.Empty)
                }
            };

            var actionContext = new ActionContext
            {
                ActionDescriptor = actionDescriptor,
                RouteData = new RouteData()
            };

            actionContext.RouteData.Values[key] = "route-value";

            // Act
            var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetNormalizedRouteValue_ReturnsRouteDataValue_IfRouteConstraintKeyHandlingIsCatchAll()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor
            {
                RouteConstraints = new[]
                {
                    RouteDataActionConstraint.CreateCatchAll(key)
                }
            };

            var actionContext = new ActionContext
            {
                ActionDescriptor = actionDescriptor,
                RouteData = new RouteData()
            };

            actionContext.RouteData.Values[key] = "route-value";

            // Act
            var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

            // Assert
            Assert.Equal("route-value", result);
        }

        [Fact]
        public void GetNormalizedRouteValue_UsesRouteValueDefaults_IfAttributeRouted()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor
            {
                AttributeRouteInfo = new AttributeRouteInfo(),
            };
            actionDescriptor.RouteValueDefaults[key] = "Route-Value";

            var actionContext = new ActionContext
            {
                ActionDescriptor = actionDescriptor,
                RouteData = new RouteData()
            };

            actionContext.RouteData.Values[key] = "route-value";

            // Act
            var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

            // Assert
            Assert.Equal("Route-Value", result);
        }

        [Fact]
        public void GetNormalizedRouteValue_UsesRouteValue_IfRouteValueDefaultsDoesNotMatchRouteValue()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor
            {
                AttributeRouteInfo = new AttributeRouteInfo(),
            };
            actionDescriptor.RouteValueDefaults[key] = "different-value";

            var actionContext = new ActionContext
            {
                ActionDescriptor = actionDescriptor,
                RouteData = new RouteData()
            };

            actionContext.RouteData.Values[key] = "route-value";

            // Act
            var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

            // Assert
            Assert.Equal("route-value", result);
        }

        [Fact]
        public void GetNormalizedRouteValue_ConvertsRouteDefaultToStringValue_IfAttributeRouted()
        {
            using (new CultureReplacer())
            {
                // Arrange
                var key = "some-key";
                var actionDescriptor = new ActionDescriptor
                {
                    AttributeRouteInfo = new AttributeRouteInfo(),
                };
                actionDescriptor.RouteValueDefaults[key] = 32;

                var actionContext = new ActionContext
                {
                    ActionDescriptor = actionDescriptor,
                    RouteData = new RouteData()
                };

                actionContext.RouteData.Values[key] = 32;

                // Act
                var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

                // Assert
                Assert.Equal("32", result);
            }
        }

        [Fact]
        public void GetNormalizedRouteValue_UsesRouteDataValue_IfKeyDoesNotExistInRouteDefaultValues()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor
            {
                AttributeRouteInfo = new AttributeRouteInfo(),
            };

            var actionContext = new ActionContext
            {
                ActionDescriptor = actionDescriptor,
                RouteData = new RouteData()
            };

            actionContext.RouteData.Values[key] = "route-value";

            // Act
            var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

            // Assert
            Assert.Equal("route-value", result);
        }

        [Fact]
        public void GetNormalizedRouteValue_ConvertsRouteValueToString()
        {
            using (new CultureReplacer())
            {
                // Arrange
                var key = "some-key";
                var actionDescriptor = new ActionDescriptor
                {
                    AttributeRouteInfo = new AttributeRouteInfo(),
                };

                var actionContext = new ActionContext
                {
                    ActionDescriptor = actionDescriptor,
                    RouteData = new RouteData()
                };

                actionContext.RouteData.Values[key] = 43;

                // Act
                var result = RazorViewEngine.GetNormalizedRouteValue(actionContext, key);

                // Assert
                Assert.Equal("43", result);
            }
        }

        // Return RazorViewEngine with a page factory provider that is always successful.
        private RazorViewEngine CreateSuccessfulViewEngine()
        {
            var pageFactory = new Mock<IRazorPageFactoryProvider>(MockBehavior.Strict);
            pageFactory
                .Setup(f => f.CreateFactory(It.IsAny<string>()))
                .Returns(new RazorPageFactoryResult(() => Mock.Of<IRazorPage>(), new IChangeToken[0]));

            return CreateViewEngine(pageFactory.Object);
        }

        private TestableRazorViewEngine CreateViewEngine(
            IRazorPageFactoryProvider pageFactory = null,
            IEnumerable<IViewLocationExpander> expanders = null)
        {
            pageFactory = pageFactory ?? Mock.Of<IRazorPageFactoryProvider>();
            return new TestableRazorViewEngine(
                pageFactory,
                GetOptionsAccessor(expanders));
        }

        private static IOptions<RazorViewEngineOptions> GetOptionsAccessor(
            IEnumerable<IViewLocationExpander> expanders = null)
        {
            var options = new RazorViewEngineOptions();
            if (expanders != null)
            {
                foreach (var expander in expanders)
                {
                    options.ViewLocationExpanders.Add(expander);
                }
            }

            var optionsAccessor = new Mock<IOptions<RazorViewEngineOptions>>();
            optionsAccessor
                .SetupGet(v => v.Value)
                .Returns(options);
            return optionsAccessor.Object;
        }

        private static ActionContext GetActionContext(IDictionary<string, object> routeValues)
        {
            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            foreach (var kvp in routeValues)
            {
                routeData.Values.Add(kvp.Key, kvp.Value);
            }

            var actionDesciptor = new ActionDescriptor();
            actionDesciptor.RouteConstraints = new List<RouteDataActionConstraint>();
            return new ActionContext(httpContext, routeData, actionDesciptor);
        }

        private static ActionContext GetActionContextWithActionDescriptor(
            IDictionary<string, object> routeValues,
            IDictionary<string, string> routesInActionDescriptor,
            bool isAttributeRouted)
        {
            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            foreach (var kvp in routeValues)
            {
                routeData.Values.Add(kvp.Key, kvp.Value);
            }

            var actionDescriptor = new ActionDescriptor();
            if (isAttributeRouted)
            {
                actionDescriptor.AttributeRouteInfo = new AttributeRouteInfo();
                foreach (var kvp in routesInActionDescriptor)
                {
                    actionDescriptor.RouteValueDefaults.Add(kvp.Key, kvp.Value);
                }
            }
            else
            {
                actionDescriptor.RouteConstraints = new List<RouteDataActionConstraint>();
                foreach (var kvp in routesInActionDescriptor)
                {
                    actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(kvp.Key, kvp.Value));
                }
            }

            return new ActionContext(httpContext, routeData, actionDescriptor);
        }

        private class TestableRazorViewEngine : RazorViewEngine
        {
            private IEnumerable<string> _viewLocationFormats;
            private IEnumerable<string> _areaViewLocationFormats;

            public TestableRazorViewEngine(
                IRazorPageFactoryProvider pageFactory,
                IOptions<RazorViewEngineOptions> optionsAccessor)
                : base(pageFactory, Mock.Of<IRazorPageActivator>(), new HtmlTestEncoder(), optionsAccessor)
            {
            }

            public void SetLocationFormats(
                IEnumerable<string> viewLocationFormats,
                IEnumerable<string> areaViewLocationFormats)
            {
                _viewLocationFormats = viewLocationFormats;
                _areaViewLocationFormats = areaViewLocationFormats;
            }

            public override IEnumerable<string> ViewLocationFormats =>
                _viewLocationFormats != null ? _viewLocationFormats : base.ViewLocationFormats;

            public override IEnumerable<string> AreaViewLocationFormats =>
                _areaViewLocationFormats != null ? _areaViewLocationFormats : base.AreaViewLocationFormats;

            public IMemoryCache ViewLookupCachePublic => ViewLookupCache;
        }
    }
}
