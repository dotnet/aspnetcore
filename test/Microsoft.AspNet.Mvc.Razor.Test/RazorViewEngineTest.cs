// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Razor.OptionDescriptors;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
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
            var pageFactory = new Mock<IRazorPageFactory>();
            var page = Mock.Of<IRazorPage>();
            pageFactory.Setup(p => p.CreateInstance(It.IsAny<string>()))
                       .Returns(Mock.Of<IRazorPage>());
            var viewEngine = CreateViewEngine(pageFactory.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "test-view");

            // Assert
            Assert.True(result.Success);
            Assert.IsAssignableFrom<IRazorView>(result.View);
            Assert.Equal("/Views/bar/test-view.cshtml", result.ViewName);
        }

        [Fact]
        public void FindView_UsesViewLocationFormat_IfRouteDoesNotContainArea()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactory>();
            var page = Mock.Of<IRazorPage>();
            pageFactory.Setup(p => p.CreateInstance("fake-path1/bar/test-view.rzr"))
                       .Returns(Mock.Of<IRazorPage>())
                       .Verifiable();
            var viewEngine = new OverloadedLocationViewEngine(pageFactory.Object,
                                                              GetViewLocationExpanders(),
                                                              GetViewLocationCache());
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "test-view");

            // Assert
            pageFactory.Verify();
        }

        [Fact]
        public void FindView_UsesAreaViewLocationFormat_IfRouteContainsArea()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactory>();
            var page = Mock.Of<IRazorPage>();
            pageFactory.Setup(p => p.CreateInstance("fake-area-path/foo/bar/test-view2.rzr"))
                       .Returns(Mock.Of<IRazorPage>())
                       .Verifiable();
            var viewEngine = new OverloadedLocationViewEngine(pageFactory.Object,
                                                              GetViewLocationExpanders(),
                                                              GetViewLocationCache());
            var context = GetActionContext(_areaTestContext);

            // Act
            var result = viewEngine.FindView(context, "test-view2");

            // Assert
            pageFactory.Verify();
        }

        public static IEnumerable<object[]> FindView_UsesViewLocationExpandersToLocateViewsData
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
        [MemberData(nameof(FindView_UsesViewLocationExpandersToLocateViewsData))]
        public void FindView_UsesViewLocationExpandersToLocateViews(IDictionary<string, object> routeValues,
                                                                    IEnumerable<string> expectedSeeds)
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("test-string/bar.cshtml"))
                       .Returns(Mock.Of<IRazorPage>())
                       .Verifiable();
            var expander1Result = new[] { "some-seed" };
            var expander1 = new Mock<IViewLocationExpander>();
            expander1.Setup(e => e.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                    .Callback((ViewLocationExpanderContext c) =>
                    {
                        Assert.NotNull(c.ActionContext);
                        c.Values["expander-key"] = expander1.ToString();
                    })
                    .Verifiable();
            expander1.Setup(e => e.ExpandViewLocations(It.IsAny<ViewLocationExpanderContext>(),
                                                      It.IsAny<IEnumerable<string>>()))
                    .Callback((ViewLocationExpanderContext c, IEnumerable<string> seeds) =>
                    {
                        Assert.NotNull(c.ActionContext);
                        Assert.Equal(expectedSeeds, seeds);
                    })
                    .Returns(expander1Result)
                    .Verifiable();

            var expander2 = new Mock<IViewLocationExpander>();
            expander2.Setup(e => e.ExpandViewLocations(It.IsAny<ViewLocationExpanderContext>(),
                                                      It.IsAny<IEnumerable<string>>()))
                     .Callback((ViewLocationExpanderContext c, IEnumerable<string> seeds) =>
                     {
                         Assert.Equal(expander1Result, seeds);
                     })
                     .Returns(new[] { "test-string/{1}.cshtml" })
                     .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object,
                                 new[] { expander1.Object, expander2.Object });
            var context = GetActionContext(routeValues);

            // Act
            var result = viewEngine.FindView(context, "test-view");

            // Assert
            Assert.True(result.Success);
            Assert.IsAssignableFrom<IRazorView>(result.View);
            pageFactory.Verify();
            expander1.Verify();
            expander2.Verify();
        }

        [Fact]
        public void FindView_CachesValuesIfViewWasFound()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("/Views/bar/baz.cshtml"))
                       .Verifiable();
            pageFactory.Setup(p => p.CreateInstance("/Views/Shared/baz.cshtml"))
                       .Returns(Mock.Of<IRazorPage>())
                       .Verifiable();
            var cache = GetViewLocationCache();
            var cacheMock = Mock.Get<IViewLocationCache>(cache);

            cacheMock.Setup(c => c.Set(It.IsAny<ViewLocationExpanderContext>(), "/Views/Shared/baz.cshtml"))
                     .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object, cache: cache);
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "baz");

            // Assert
            Assert.True(result.Success);
            pageFactory.Verify();
            cacheMock.Verify();
        }

        [Fact]
        public void FindView_UsesCachedValueIfViewWasFound()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactory>(MockBehavior.Strict);
            pageFactory.Setup(p => p.CreateInstance("some-view-location"))
                       .Returns(Mock.Of<IRazorPage>())
                       .Verifiable();
            var expander = new Mock<IViewLocationExpander>(MockBehavior.Strict);
            expander.Setup(v => v.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                    .Verifiable();
            var cacheMock = new Mock<IViewLocationCache>();
            cacheMock.Setup(c => c.Get(It.IsAny<ViewLocationExpanderContext>()))
                     .Returns("some-view-location")
                     .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object,
                                              new[] { expander.Object },
                                              cacheMock.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "baz");

            // Assert
            Assert.True(result.Success);
            pageFactory.Verify();
            cacheMock.Verify();
            expander.Verify();
        }

        [Fact]
        public void FindView_LooksForViewsIfCachedViewDoesNotExist()
        {
            // Arrange
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(p => p.CreateInstance("expired-location"))
                       .Returns((IRazorPage)null)
                       .Verifiable();
            pageFactory.Setup(p => p.CreateInstance("some-view-location"))
                       .Returns(Mock.Of<IRazorPage>())
                       .Verifiable();
            var cacheMock = new Mock<IViewLocationCache>();
            cacheMock.Setup(c => c.Get(It.IsAny<ViewLocationExpanderContext>()))
                     .Returns("expired-location");

            var expander = new Mock<IViewLocationExpander>();
            expander.Setup(v => v.PopulateValues(It.IsAny<ViewLocationExpanderContext>()))
                    .Verifiable();
            var expanderResult = new[] { "some-view-location" };
            expander.Setup(v => v.ExpandViewLocations(
                        It.IsAny<ViewLocationExpanderContext>(), It.IsAny<IEnumerable<string>>()))
                    .Returns((ViewLocationExpanderContext c, IEnumerable<string> seed) => expanderResult)
                    .Verifiable();

            var viewEngine = CreateViewEngine(pageFactory.Object,
                                              new[] { expander.Object },
                                              cacheMock.Object);
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "baz");

            // Assert
            Assert.True(result.Success);
            pageFactory.Verify();
            cacheMock.Verify();
            expander.Verify();
        }

        private IViewEngine CreateViewEngine(IRazorPageFactory pageFactory = null,
                                             IEnumerable<IViewLocationExpander> expanders = null,
                                             IViewLocationCache cache = null)
        {
            pageFactory = pageFactory ?? Mock.Of<IRazorPageFactory>();
            cache = cache ?? GetViewLocationCache();
            var viewLocationExpanderProvider = GetViewLocationExpanders(expanders);

            var viewEngine = new RazorViewEngine(pageFactory,
                                                 viewLocationExpanderProvider,
                                                 cache);

            return viewEngine;
        }

        private static IViewLocationExpanderProvider GetViewLocationExpanders(
            IEnumerable<IViewLocationExpander> expanders = null)
        {
            expanders = expanders ?? Enumerable.Empty<IViewLocationExpander>();
            var viewLocationExpander = new Mock<IViewLocationExpanderProvider>();
            viewLocationExpander.Setup(v => v.ViewLocationExpanders)
                                .Returns(expanders.ToList());
            return viewLocationExpander.Object;
        }

        private static IViewLocationCache GetViewLocationCache()
        {
            var cacheMock = new Mock<IViewLocationCache>();
            cacheMock.Setup(c => c.Get(It.IsAny<ViewLocationExpanderContext>()))
                     .Returns<string>(null);

            return cacheMock.Object;
        }

        private static ActionContext GetActionContext(IDictionary<string, object> routeValues,
                                                      IRazorView razorView = null)
        {
            var httpContext = new DefaultHttpContext();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(IRazorView)))
                           .Returns(razorView ?? Mock.Of<IRazorView>());

            httpContext.RequestServices = serviceProvider.Object;
            var routeData = new RouteData { Values = routeValues };
            return new ActionContext(httpContext, routeData, new ActionDescriptor());
        }

        private class OverloadedLocationViewEngine : RazorViewEngine
        {
            public OverloadedLocationViewEngine(IRazorPageFactory pageFactory,
                                                IViewLocationExpanderProvider expanderProvider,
                                                IViewLocationCache cache)
                : base(pageFactory, expanderProvider, cache)
            {
            }

            public override IEnumerable<string> ViewLocationFormats
            {
                get
                {
                    return new[] { "fake-path1/{1}/{0}.rzr" };
                }
            }

            public override IEnumerable<string> AreaViewLocationFormats
            {
                get
                {
                    return new[] { "fake-area-path/{2}/{1}/{0}.rzr" };
                }
            }
        }
    }
}
