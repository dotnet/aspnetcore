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

        public static IEnumerable<string[]> InvalidViewNameValues
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
        public void FindView_ThrowsIfViewNameIsNullOrEmpty(string viewName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(() => viewEngine.FindView(context, viewName), "viewName");
        }

        [Theory]
        [MemberData(nameof(InvalidViewNameValues))]
        public void FindView_WithFullPathReturnsNotFound_WhenPathDoesNotMatchExtension(string viewName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, viewName);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [MemberData(nameof(InvalidViewNameValues))]
        public void FindViewFullPathSucceedsWithCshtmlEnding(string viewName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            // Append .cshtml so the viewname is no longer invalid
            viewName += ".cshtml";
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            // If this throws then our test case fails
            var result = viewEngine.FindPartialView(context, viewName);

            Assert.False(result.Success);
        }

        [Fact]
        public void FindPartialView_ReturnsRazorView_IfLookupWasSuccessful()
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
            var result = viewEngine.FindPartialView(context, "test-view");

            // Assert
            Assert.True(result.Success);
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Same(page, view.RazorPage);
            Assert.Equal("test-view", result.ViewName);
            Assert.Empty(view.ViewStartPages);
        }

        [Fact]
        public void FindPartialView_DoesNotExpireCachedResults_IfViewStartsExpire()
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
            var result1 = viewEngine.FindPartialView(context, "test-view");

            // Assert - 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page, view1.RazorPage);
            Assert.Equal("test-view", result1.ViewName);
            Assert.Empty(view1.ViewStartPages);

            // Act - 2
            cancellationTokenSource.Cancel();
            var result2 = viewEngine.FindPartialView(context, "test-view");

            // Assert - 2
            Assert.True(result2.Success);
            var view2 = Assert.IsType<RazorView>(result2.View);
            Assert.Same(page, view2.RazorPage);
            pageFactory.Verify(p => p.CreateFactory("/Views/bar/test-view.cshtml"), Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FindPartialView_ThrowsIfViewNameIsNullOrEmpty(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => viewEngine.FindPartialView(context, partialViewName),
                "partialViewName");
        }

        [Theory]
        [MemberData(nameof(InvalidViewNameValues))]
        public void FindPartialView_WithFullPathReturnsNotFound_WhenPathDoesNotMatchExtension(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindPartialView(context, partialViewName);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [MemberData(nameof(InvalidViewNameValues))]
        public void FindPartialViewFullPathSucceedsWithCshtmlEnding(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            // Append .cshtml so the viewname is no longer invalid
            partialViewName += ".cshtml";
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            // If this throws then our test case fails
            var result = viewEngine.FindPartialView(context, partialViewName);

            Assert.False(result.Success);
        }

        [Fact]
        public void FindPartialViewFailureSearchesCorrectLocationsWithAreas()
        {
            // Arrange
            var searchedLocations = new List<string>();
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_areaTestContext);

            // Act
            var result = viewEngine.FindPartialView(context, "partial");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[]
            {
                "/Areas/foo/Views/bar/partial.cshtml",
                "/Areas/foo/Views/Shared/partial.cshtml",
                "/Views/Shared/partial.cshtml",
            }, result.SearchedLocations);
        }

        [Fact]
        public void FindPartialViewFailureSearchesCorrectLocationsWithoutAreas()
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindPartialView(context, "partialNoArea");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] {
                "/Views/bar/partialNoArea.cshtml",
                "/Views/Shared/partialNoArea.cshtml",
            }, result.SearchedLocations);
        }

        [Fact]
        public void FindViewFailureSearchesCorrectLocationsWithAreas()
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_areaTestContext);

            // Act
            var result = viewEngine.FindView(context, "full");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] {
                "/Areas/foo/Views/bar/full.cshtml",
                "/Areas/foo/Views/Shared/full.cshtml",
                "/Views/Shared/full.cshtml",
            }, result.SearchedLocations);
        }

        [Fact]
        public void FindViewFailureSearchesCorrectLocationsWithoutAreas()
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "fullNoArea");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] {
                "/Views/bar/fullNoArea.cshtml",
                "/Views/Shared/fullNoArea.cshtml",
            }, result.SearchedLocations);
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
            var result = viewEngine.FindView(context, "test-view");

            // Assert
            Assert.True(result.Success);
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Equal("test-view", result.ViewName);
            Assert.Same(page, view.RazorPage);
            Assert.False(view.IsPartial);
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
            var result = viewEngine.FindView(context, "test-view");

            // Assert
            pageFactory.Verify();
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Same(page, view.RazorPage);
        }

        [Fact]
        public void FindView_UsesAreaViewLocationFormat_IfRouteContainsArea()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            var page = Mock.Of<IRazorPage>();
            pageFactory
                .Setup(p => p.CreateFactory("fake-area-path/foo/bar/test-view2.rzr"))
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
            var result = viewEngine.FindView(context, "test-view2");

            // Assert
            pageFactory.Verify();
            var view = Assert.IsType<RazorView>(result.View);
            Assert.Same(page, view.RazorPage);
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
            var result = viewEngine.FindView(context, "test-view");

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
            var result1 = viewEngine.FindView(context, "baz");

            // Assert 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page, view1.RazorPage);
            pageFactory.Verify();

            // Act 2
            pageFactory
               .Setup(p => p.CreateFactory(It.IsAny<string>()))
               .Throws(new Exception("Shouldn't be called"));

            var result2 = viewEngine.FindView(context, "baz");

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
            var result1 = viewEngine.FindView(context, "baz");

            // Assert 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page1, view1.RazorPage);

            // Act 2
            cancellationTokenSource.Cancel();
            var result2 = viewEngine.FindView(context, "baz");

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
            var result1 = viewEngine.FindView(context, "baz");

            // Assert 1
            Assert.True(result1.Success);
            var view1 = Assert.IsType<RazorView>(result1.View);
            Assert.Same(page1, view1.RazorPage);
            Assert.Empty(view1.ViewStartPages);

            // Act 2
            cancellationTokenSource.Cancel();
            var result2 = viewEngine.FindView(context, "baz");

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
            var result = viewEngine.FindView(context, "myview");

            // Assert - 1
            Assert.False(result.Success);
            Assert.Equal(expandedLocations, result.SearchedLocations);
            expander.Verify();

            // Act - 2
            result = viewEngine.FindView(context, "myview");

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
            var result = viewEngine.FindView(context, "MyView");

            // Assert - 1
            Assert.False(result.Success);
            Assert.Equal(expandedLocations, result.SearchedLocations);
            expander.Verify();

            // Act - 2
            pageFactory
                .Setup(p => p.CreateFactory("viewlocation3"))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]));
            cancellationTokenSource.Cancel();
            result = viewEngine.FindView(context, "MyView");

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
        [InlineData(null)]
        [InlineData("")]
        public void FindPage_ThrowsIfNameIsNullOrEmpty(string pageName)
        {
            // Arrange
            var viewEngine = CreateViewEngine();
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(() => viewEngine.FindPage(context, pageName),
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
            var page = Mock.Of<IRazorPage>();

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

            var page = new Mock<IRazorPage>(MockBehavior.Strict).Object;
            var pageFactory = new Mock<IRazorPageFactoryProvider>();
            pageFactory
                .Setup(p => p.CreateFactory("/Views/Foo/details.cshtml"))
                .Returns(new RazorPageFactoryResult(() => page, new IChangeToken[0]))
                .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object);
            var routesInActionDescriptor = new Dictionary<string, string>()
            {
                { "controller", "Foo" }
            };

            var context = GetActionContextWithActionDescriptor(routeValues, routesInActionDescriptor, isAttributeRouted);

            // Act
            var result = viewEngine.FindPage(context, "details");

            // Assert
            Assert.Equal("details", result.Name);
            Assert.Same(page, result.Page);
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
            var page = Mock.Of<IRazorPage>();

            var viewEngine = CreateViewEngine();
            var context = GetActionContextWithActionDescriptor(routeValues, routesInActionDescriptor, isAttributeRouted);

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
            var page = Mock.Of<IRazorPage>();

            var viewEngine = CreateViewEngine();
            var context = GetActionContextWithActionDescriptor(routeValues, routesInActionDescriptor, isAttributeRouted);

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
            var page = Mock.Of<IRazorPage>();

            var viewEngine = CreateViewEngine();
            var context = GetActionContextWithActionDescriptor(routeValues, new Dictionary<string, string>(), isAttributeRouted);

            // Act
            var result = viewEngine.FindPage(context, "bar");

            // Assert
            Assert.Equal("bar", result.Name);
            Assert.Null(result.Page);
            Assert.Equal(expected, result.SearchedLocations);
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
            optionsAccessor.SetupGet(v => v.Value)
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
