// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
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

        private static readonly Dictionary<string, object> _pageTestContext = new Dictionary<string, object>()
        {
            {"page", "/Accounts/Index"},
        };

        public static IEnumerable<object[]> AbsoluteViewPathData
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
                .Returns(GetPageFactoryResult(() => page));

            pageFactory
                .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
                .Returns(GetPageFactoryResult(() => viewStart2));

            pageFactory
                .Setup(p => p.CreateFactory("/_ViewStart.cshtml"))
                .Returns(GetPageFactoryResult(() => viewStart1));

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
                .Returns(GetPageFactoryResult(() => page));

            pageFactory
                .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
                .Returns(GetPageFactoryResult(() => viewStart, new[] { changeToken }));

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
                .Returns(GetPageFactoryResult(() => page));

            pageFactory
                .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
                .Returns(GetPageFactoryResult(() => viewStart2));

            pageFactory
                .Setup(p => p.CreateFactory("/_ViewStart.cshtml"))
                .Returns(GetPageFactoryResult(() => viewStart1));

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
                .Returns(GetPageFactoryResult(() => page))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor(
                    viewLocationFormats: new[] { "fake-path1/{1}/{0}.rzr" },
                    areaViewLocationFormats: new[] { "fake-area-path/{2}/{1}/{0}.rzr" }));
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
                .Returns(GetPageFactoryResult(() => page))
                .Verifiable();
            var viewEngine = new TestableRazorViewEngine(
                pageFactory.Object,
                GetOptionsAccessor(
                    viewLocationFormats: new[] { "fake-path1/{1}/{0}.rzr" },
                    areaViewLocationFormats: new[] { "fake-area-path/{2}/{1}/{0}.rzr" }));
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
                .Returns(GetPageFactoryResult(() => page))
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
                .Returns(GetPageFactoryResult(() => page))
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
                .Returns(GetPageFactoryResult(() => page))
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
                .Returns(GetPageFactoryResult(() => page))
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
                .Returns(GetPageFactoryResult(() => page))
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
                .Returns(GetPageFactoryResult(() => areaPage));
            pageFactory
                .Setup(p => p.CreateFactory("/Views/Home/Index.cshtml"))
                .Returns(GetPageFactoryResult(() => nonAreaPage));

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
                .Returns(GetPageFactoryResult(() => areaPage1));
            pageFactory
                .Setup(p => p.CreateFactory("/Areas/Sales/Views/Home/Index.cshtml"))
                .Returns(GetPageFactoryResult(() => areaPage2));

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
                .Returns(GetPageFactoryResult(() => Mock.Of<IRazorPage>()))
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
        public void FindView_NoramlizesPaths_ReturnedByViewLocationExpanders()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory(@"Views\Home\Index.cshtml"))
                .Returns(GetPageFactoryResult(() => Mock.Of<IRazorPage>()))
                .Verifiable();

            var expander = new Mock<IViewLocationExpander>();
            expander
                .Setup(e => e.ExpandViewLocations(
                    It.IsAny<ViewLocationExpanderContext>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns(new[] { @"Views\Home\Index.cshtml" });

            var viewEngine = CreateViewEngine(
                pageFactory.Object,
                new[] { expander.Object });
            var context = GetActionContext(new Dictionary<string, object>());

            // Act
            var result = viewEngine.FindView(context, "test-view", isMainPage: true);

            // Assert
            Assert.True(result.Success);
            Assert.IsAssignableFrom<IView>(result.View);
            pageFactory.Verify();
        }

        [Fact]
        public void FindView_CachesValuesIfViewWasFound()
        {
            // Arrange
            var page = Mock.Of<IRazorPage>();
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory("/Views/bar/baz.cshtml"))
                .Returns(GetPageFactoryResult(factory: null))
                .Verifiable();
            pageFactory
               .Setup(p => p.CreateFactory("/Views/Shared/baz.cshtml"))
               .Returns(GetPageFactoryResult(() => page))
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
        public void FindView_CachesValuesIfViewWasFound_ForPages()
        {
            // Arrange
            var page = Mock.Of<IRazorPage>();
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
               .Setup(p => p.CreateFactory("/Views/Shared/baz.cshtml"))
               .Returns(GetPageFactoryResult(() => page))
               .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(_pageTestContext);

            // Act 1
            var result1 = viewEngine.FindView(context, "baz", isMainPage: false);

            // Assert 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page, view1.RazorPage);
            pageFactory.Verify();

            // Act 2
            pageFactory
               .Setup(p => p.CreateFactory(It.IsAny<string>()))
               .Throws(new Exception("Shouldn't be called"));

            var result2 = viewEngine.FindView(context, "baz", isMainPage: false);

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
                .Returns(GetPageFactoryResult(factory: null, changeTokens: new[] { changeToken }));
            pageFactory
               .InSequence(sequence)
               .Setup(p => p.CreateFactory("/Views/Shared/baz.cshtml"))
               .Returns(GetPageFactoryResult(() => page1))
               .Verifiable();
            pageFactory
                .InSequence(sequence)
                .Setup(p => p.CreateFactory("/Views/bar/baz.cshtml"))
                .Returns(GetPageFactoryResult(() => page2));

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
                .Returns(GetPageFactoryResult(() => page1));
            pageFactory
                .InSequence(sequence)
               .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
               .Returns(GetPageFactoryResult(factory: null, changeTokens: new[] { changeToken }))
               .Verifiable();
            pageFactory
                .InSequence(sequence)
                .Setup(p => p.CreateFactory("/Views/bar/baz.cshtml"))
                .Returns(GetPageFactoryResult(() => page2));
            pageFactory
                .InSequence(sequence)
               .Setup(p => p.CreateFactory("/Views/_ViewStart.cshtml"))
               .Returns(GetPageFactoryResult(() => viewStart));

            var fileProvider = new TestFileProvider();
            var accessor = Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == fileProvider);
            var razorProject = new FileProviderRazorProject(accessor);
            var viewEngine = CreateViewEngine(pageFactory.Object, razorProject: razorProject);
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
                .Returns(GetPageFactoryResult(factory: null, changeTokens: new[] { changeToken }));
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
                .Returns(GetPageFactoryResult(() => page));
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
                .Returns(GetPageFactoryResult(() => page))
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

        [Fact]
        public void FindPage_SelectsActionCaseInsensitively()
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
                .Returns(GetPageFactoryResult(() => page.Object))
                .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var routesInActionDescriptor = new Dictionary<string, string>()
            {
                { "controller", "Foo" }
            };

            var context = GetActionContextWithActionDescriptor(
                routeValues,
                routesInActionDescriptor);

            // Act
            var result = viewEngine.FindPage(context, "details");

            // Assert
            Assert.Equal("details", result.Name);
            Assert.Same(page.Object, result.Page);
            Assert.Null(result.SearchedLocations);
            pageFactory.Verify();
        }

        [Fact]
        public void FindPage_LooksForPages_UsingActionDescriptor_Controller()
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
                routesInActionDescriptor);

            // Act
            var result = viewEngine.FindPage(context, "foo");

            // Assert
            Assert.Equal("foo", result.Name);
            Assert.Null(result.Page);
            Assert.Equal(expected, result.SearchedLocations);
        }

        [Fact]
        public void FindPage_LooksForPages_UsingActionDescriptor_Areas()
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
                routesInActionDescriptor);

            // Act
            var result = viewEngine.FindPage(context, "foo");

            // Assert
            Assert.Equal("foo", result.Name);
            Assert.Null(result.Page);
            Assert.Equal(expected, result.SearchedLocations);
        }

        [Fact]
        public void FindPage_LooksForPages_UsesRouteValuesAsFallback()
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
                new Dictionary<string, string>());

            // Act
            var result = viewEngine.FindPage(context, "bar");

            // Assert
            Assert.Equal("bar", result.Name);
            Assert.Null(result.Page);
            Assert.Equal(expected, result.SearchedLocations);
        }

        [Fact]
        public void CreateCacheResult_LogsPrecompiledViewFound()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var relativePath = "/Views/Foo/details.cshtml";
            var factoryResult = GetPageFactoryResult(() => Mock.Of<IRazorPage>());
            factoryResult.ViewDescriptor.IsPrecompiled = true;
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory(relativePath))
                .Returns(factoryResult)
                .Verifiable();

            var viewEngine = new RazorViewEngine(
                pageFactory.Object,
                Mock.Of<IRazorPageActivator>(),
                new HtmlTestEncoder(),
                GetOptionsAccessor(expanders: null),
                new FileProviderRazorProject(
                    Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == new TestFileProvider())),
                loggerFactory,
                new DiagnosticListener("Microsoft.AspNetCore.Mvc.Razor"));

            // Act
            var result = viewEngine.CreateCacheResult(null, relativePath, false);

            // Assert
            var logMessage = Assert.Single(sink.Writes);
            Assert.Equal($"Using precompiled view for '{relativePath}'.", logMessage.State.ToString());
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
                .Returns(GetPageFactoryResult(() => page))
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
                .Returns(GetPageFactoryResult(() => page))
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
                .Returns(GetPageFactoryResult(() => page))
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
        [InlineData("/Views/Home/Index.cshtml", "../Shared/_Partial.cshtml")]
        [InlineData("/Views/Home/Index.cshtml", "..\\Shared\\_Partial.cshtml")]
        [InlineData("/Areas/MyArea/Views/Home/Index.cshtml", "../../../../Views/Shared/_Partial.cshtml")]
        [InlineData("/Views/Accounts/Users.cshtml", "../Test/../Shared/_Partial.cshtml")]
        [InlineData("Views/Accounts/Users.cshtml", "./../Shared/./_Partial.cshtml")]
        public void GetAbsolutePath_ResolvesPathTraversals(string executingFilePath, string pagePath)
        {
            // Arrange
            var viewEngine = CreateViewEngine();

            // Act
            var result = viewEngine.GetAbsolutePath(executingFilePath, pagePath);

            // Assert
            Assert.Equal("/Views/Shared/_Partial.cshtml", result);
        }

        [Theory]
        [InlineData("../Shared/_Layout.cshtml")]
        [InlineData("Folder1/../Folder2/../../File.cshtml")]
        public void GetAbsolutePath_DoesNotResolvePathIfTraversalsEscapeTheRoot(string pagePath)
        {
            // Arrange
            var expected = '/' + pagePath;
            var viewEngine = CreateViewEngine();

            // Act
            var result = viewEngine.GetAbsolutePath("/Index.cshtml", pagePath);

            // Assert
            Assert.Equal(expected, result);
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
        public void GetNormalizedRouteValue_ReturnsValueFromRouteValues()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.RouteValues.Add(key, "Route-Value");

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
        public void GetNormalizedRouteValue_ReturnsRouteValue_IfValueDoesNotMatch()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.RouteValues.Add(key, "different-value");

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
        public void GetNormalizedRouteValue_ReturnsNonNormalizedValue_IfActionRouteValueIsNull()
        {
            // Arrange
            var key = "some-key";
            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.RouteValues.Add(key, null);

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

        [Fact]
        public void GetViewLocationFormats_ForControllerWithoutArea_ReturnsDefaultSet()
        {
            // Arrange
            var expected = new string[] { "expected", };

            var viewEngine = new TestableRazorViewEngine(
                Mock.Of<IRazorPageFactoryProvider>(),
                GetOptionsAccessor(viewLocationFormats: expected));

            var context = new ViewLocationExpanderContext(
                new ActionContext(),
                "Index.cshtml",
                controllerName: "Home",
                areaName: null,
                pageName: "ignored",
                isMainPage: true);

            // Act
            var actual = viewEngine.GetViewLocationFormats(context);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetViewLocationFormats_ForControllerWithArea_ReturnsAreaSet()
        {
            // Arrange
            var expected = new string[] { "expected", };

            var viewEngine = new TestableRazorViewEngine(
                Mock.Of<IRazorPageFactoryProvider>(),
                GetOptionsAccessor(areaViewLocationFormats: expected));

            var context = new ViewLocationExpanderContext(
                new ActionContext(),
                "Index.cshtml",
                controllerName: "Home",
                areaName: "Admin",
                pageName: "ignored",
                isMainPage: true);

            // Act
            var actual = viewEngine.GetViewLocationFormats(context);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetViewLocationFormats_ForPage_ReturnsPageSet()
        {
            // Arrange
            var expected = new string[] { "expected", };

            var viewEngine = new TestableRazorViewEngine(
                Mock.Of<IRazorPageFactoryProvider>(),
                GetOptionsAccessor(pageViewLocationFormats: expected));

            var context = new ViewLocationExpanderContext(
                new ActionContext(),
                "Index.cshtml",
                controllerName: null,
                areaName: null,
                pageName: "/Some/Page",
                isMainPage: true);

            // Act
            var actual = viewEngine.GetViewLocationFormats(context);

            // Assert
            Assert.Equal(expected, actual);
        }

        // This isn't a real case we expect to hit in an app, just making sure we have a reasonable default
        // for a weird configuration. In this case we preserve what we did in 1.0.0.
        [Fact]
        public void GetViewLocationFormats_NoRouteValues_ReturnsDefaultSet()
        {
            // Arrange
            var expected = new string[] { "expected", };

            var viewEngine = new TestableRazorViewEngine(
                Mock.Of<IRazorPageFactoryProvider>(),
                GetOptionsAccessor(viewLocationFormats: expected));

            var context = new ViewLocationExpanderContext(
                new ActionContext(),
                "Index.cshtml",
                controllerName: null,
                areaName: null,
                pageName: null,
                isMainPage: true);

            // Act
            var actual = viewEngine.GetViewLocationFormats(context);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ViewEngine_DoesNotSetPageValue_IfItIsNotSpecifiedInRouteValues()
        {
            // Arrange
            var routeValues = new Dictionary<string, object>
            {
                { "controller", "MyController" },
                { "action", "MyAction" }
            };

            var expected = new[] { "some-seed" };
            var expander = new Mock<IViewLocationExpander>();
            expander
                .Setup(e => e.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                .Callback((ViewLocationExpanderContext c) =>
                {
                    Assert.Equal("MyController", c.ControllerName);
                    Assert.Null(c.PageName);
                })
                .Verifiable();

            expander
                .Setup(e => e.ExpandViewLocations(
                    It.IsAny<ViewLocationExpanderContext>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns(expected);

            var viewEngine = CreateViewEngine(expanders: new[] { expander.Object });
            var context = GetActionContext(routeValues);

            // Act
            viewEngine.FindView(context, viewName: "Test-view", isMainPage: true);

            // Assert
            expander.Verify();
        }

        [Fact]
        public void ViewEngine_SetsPageValue_IfItIsSpecifiedInRouteValues()
        {
            // Arrange
            var routeValues = new Dictionary<string, object>
            {
                { "page", "MyPage" },
            };

            var expected = new[] { "some-seed" };
            var expander = new Mock<IViewLocationExpander>();
            expander
                .Setup(e => e.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                .Callback((ViewLocationExpanderContext c) =>
                {
                    Assert.Equal("MyPage", c.PageName);
                })
                .Verifiable();

            expander
                .Setup(e => e.ExpandViewLocations(
                    It.IsAny<ViewLocationExpanderContext>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns(expected);

            var viewEngine = CreateViewEngine(expanders: new[] { expander.Object });
            var context = GetActionContext(routeValues);
            context.ActionDescriptor.RouteValues["page"] = "MyPage";

            // Act
            viewEngine.FindView(context, viewName: "MyView", isMainPage: true);

            // Assert
            expander.Verify();
        }

        [Fact]
        public void FindView_ResolvesDirectoryTraversalsPriorToInvokingPageFactory()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory.Setup(p => p.CreateFactory("/Views/Shared/_Partial.cshtml"))
                .Returns(GetPageFactoryResult(() => Mock.Of<IRazorPage>()))
                .Verifiable();
            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(new Dictionary<string, object>());

            // Act
            var result = viewEngine.FindView(context, "../Shared/_Partial", isMainPage: false);

            // Assert
            pageFactory.Verify();
        }

        [Fact]
        public void FindPage_ResolvesDirectoryTraversalsPriorToInvokingPageFactory()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory.Setup(p => p.CreateFactory("/Views/Shared/_Partial.cshtml"))
                .Returns(GetPageFactoryResult(() => Mock.Of<IRazorPage>()))
                .Verifiable();
            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(new Dictionary<string, object>());

            // Act
            var result = viewEngine.FindPage(context, "../Shared/_Partial");

            // Assert
            pageFactory.Verify();
        }

        // Tests to verify fix for https://github.com/aspnet/Mvc/issues/6672
        // Without normalizing the path, the view engine would have attempted to lookup "/Views//MyView.cshtml"
        // which works for PhysicalFileProvider but fails for exact lookups performed during precompilation.
        // We normalize it to "/Views/MyView.cshtml" to avoid this discrepancy.
        [Fact]
        public void FindView_ResolvesNormalizesSlashesPriorToInvokingPageFactory()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory.Setup(p => p.CreateFactory("/Views/MyView.cshtml"))
                .Returns(GetPageFactoryResult(() => Mock.Of<IRazorPage>()))
                .Verifiable();
            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(new Dictionary<string, object>());

            // Act
            var result = viewEngine.FindView(context, "MyView", isMainPage: true);

            // Assert
            pageFactory.Verify();
        }

        [Fact]
        public void FindPage_ResolvesNormalizesSlashesPriorToInvokingPageFactory()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory.Setup(p => p.CreateFactory("/Views/MyPage.cshtml"))
                .Returns(GetPageFactoryResult(() => Mock.Of<IRazorPage>()))
                .Verifiable();
            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(new Dictionary<string, object>());

            // Act
            var result = viewEngine.FindPage(context, "MyPage");

            // Assert
            pageFactory.Verify();
        }

        // Return RazorViewEngine with a page factory provider that is always successful.
        private RazorViewEngine CreateSuccessfulViewEngine()
        {
            var pageFactory = new Mock<IRazorPageFactoryProvider>(MockBehavior.Strict);
            pageFactory
                .Setup(f => f.CreateFactory(It.IsAny<string>()))
                .Returns(GetPageFactoryResult(() => Mock.Of<IRazorPage>()));

            return CreateViewEngine(pageFactory.Object);
        }

        private static RazorPageFactoryResult GetPageFactoryResult(
            Func<IRazorPage> factory,
            IList<IChangeToken> changeTokens = null,
            string path = "/Views/Home/Index.cshtml")
        {
            var descriptor = new CompiledViewDescriptor
            {
                ExpirationTokens = changeTokens ?? Array.Empty<IChangeToken>(),
                RelativePath = path,
            };

            return new RazorPageFactoryResult(descriptor, factory);
        }

        private TestableRazorViewEngine CreateViewEngine(
            IRazorPageFactoryProvider pageFactory = null,
            IEnumerable<IViewLocationExpander> expanders = null,
            RazorProject razorProject = null)
        {
            pageFactory = pageFactory ?? Mock.Of<IRazorPageFactoryProvider>();
            if (razorProject == null)
            {
                var accessor = Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == new TestFileProvider());
                razorProject = new FileProviderRazorProject(accessor);
            }
            return new TestableRazorViewEngine(pageFactory, GetOptionsAccessor(expanders), razorProject);
        }

        private static IOptions<RazorViewEngineOptions> GetOptionsAccessor(
            IEnumerable<IViewLocationExpander> expanders = null,
            IEnumerable<string> viewLocationFormats = null,
            IEnumerable<string> areaViewLocationFormats = null,
            IEnumerable<string> pageViewLocationFormats = null)
        {
            var optionsSetup = new RazorViewEngineOptionsSetup(Mock.Of<IHostingEnvironment>());

            var options = new RazorViewEngineOptions();
            optionsSetup.Configure(options);
            options.PageViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
            options.PageViewLocationFormats.Add("/Pages/Shared/{0}.cshtml");

            if (expanders != null)
            {
                foreach (var expander in expanders)
                {
                    options.ViewLocationExpanders.Add(expander);
                }
            }

            if (viewLocationFormats != null)
            {
                options.ViewLocationFormats.Clear();

                foreach (var location in viewLocationFormats)
                {
                    options.ViewLocationFormats.Add(location);
                }
            }

            if (areaViewLocationFormats != null)
            {
                options.AreaViewLocationFormats.Clear();

                foreach (var location in areaViewLocationFormats)
                {
                    options.AreaViewLocationFormats.Add(location);
                }
            }

            if (pageViewLocationFormats != null)
            {
                options.PageViewLocationFormats.Clear();

                foreach (var location in pageViewLocationFormats)
                {
                    options.PageViewLocationFormats.Add(location);
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
            return new ActionContext(httpContext, routeData, actionDesciptor);
        }

        private static ActionContext GetActionContextWithActionDescriptor(
            IDictionary<string, object> routeValues,
            IDictionary<string, string> actionRouteValues)
        {
            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            foreach (var kvp in routeValues)
            {
                routeData.Values.Add(kvp.Key, kvp.Value);
            }

            var actionDescriptor = new ActionDescriptor();

            foreach (var kvp in actionRouteValues)
            {
                actionDescriptor.RouteValues.Add(kvp.Key, kvp.Value);
            }

            return new ActionContext(httpContext, routeData, actionDescriptor);
        }

        private class TestableRazorViewEngine : RazorViewEngine
        {
            public TestableRazorViewEngine(
                IRazorPageFactoryProvider pageFactory,
                IOptions<RazorViewEngineOptions> optionsAccessor)
                : this(
                      pageFactory,
                      optionsAccessor,
                      new FileProviderRazorProject(
                          Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == new TestFileProvider())))
            {
            }

            public TestableRazorViewEngine(
                IRazorPageFactoryProvider pageFactory,
                IOptions<RazorViewEngineOptions> optionsAccessor,
                RazorProject razorProject)
                : base(pageFactory, Mock.Of<IRazorPageActivator>(), new HtmlTestEncoder(), optionsAccessor, razorProject, NullLoggerFactory.Instance, new DiagnosticListener("Microsoft.AspNetCore.Mvc.Razor"))
            {
            }

            public IMemoryCache ViewLookupCachePublic => ViewLookupCache;
        }
    }
}
