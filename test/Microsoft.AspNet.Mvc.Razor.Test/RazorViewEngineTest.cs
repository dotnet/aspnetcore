// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Rendering;
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
        [MemberData("InvalidViewNameValues")]
        public void FindViewFullPathFailsWithNoCshtmlEnding(string viewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                viewEngine.FindView(context, viewName));
        }

        [Theory]
        [MemberData("InvalidViewNameValues")]
        public void FindViewFullPathSucceedsWithCshtmlEnding(string viewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();
            // Append .cshtml so the viewname is no longer invalid
            viewName += ".cshtml";
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            // If this throws then our test case fails
            var result = viewEngine.FindPartialView(context, viewName);

            Assert.False(result.Success);
        }

        [Theory]
        [MemberData("InvalidViewNameValues")]
        public void FindPartialViewFullPathFailsWithNoCshtmlEnding(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();
            var context = GetActionContext(_controllerTestContext);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                viewEngine.FindPartialView(context, partialViewName));
        }

        [Theory]
        [MemberData("InvalidViewNameValues")]
        public void FindPartialViewFullPathSucceedsWithCshtmlEnding(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();
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
            var viewEngine = CreateSearchLocationViewEngineTester();
            var context = GetActionContext(_areaTestContext);

            // Act
            var result = viewEngine.FindPartialView(context, "partial");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] {
                "/Areas/foo/Views/bar/partial.cshtml",
                "/Areas/foo/Views/Shared/partial.cshtml",
                "/Views/Shared/partial.cshtml",
            }, result.SearchedLocations);
        }

        [Fact]
        public void FindPartialViewFailureSearchesCorrectLocationsWithoutAreas()
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();
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
            var viewEngine = CreateSearchLocationViewEngineTester();
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
            var viewEngine = CreateSearchLocationViewEngineTester();
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
            pageFactory.Setup(p => p.CreateInstance(It.IsAny<string>()))
                       .Returns(Mock.Of<IRazorPage>());
            var viewEngine = new RazorViewEngine(pageFactory.Object,
                                                 Mock.Of<IRazorPageActivator>(),
                                                 Mock.Of<IViewStartProvider>());
            var context = GetActionContext(_controllerTestContext);

            // Act
            var result = viewEngine.FindView(context, "test-view");

            // Assert
            Assert.True(result.Success);
            Assert.IsType<RazorView>(result.View);
            Assert.Equal("/Views/bar/test-view.cshtml", result.ViewName);
        }

        private IViewEngine CreateSearchLocationViewEngineTester()
        {
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(vpf => vpf.CreateInstance(It.IsAny<string>()))
                       .Returns<RazorPage>(null);

            var viewEngine = new RazorViewEngine(pageFactory.Object,
                                                 Mock.Of<IRazorPageActivator>(),
                                                 Mock.Of<IViewStartProvider>());

            return viewEngine;
        }

        private static ActionContext GetActionContext(IDictionary<string, object> routeValues)
        {
            var httpContext = Mock.Of<HttpContext>();
            var routeData = new RouteData { Values = routeValues };
            return new ActionContext(httpContext, routeData, new ActionDescriptor());
        }
    }
}
