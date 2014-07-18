// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Rendering;
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

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                viewEngine.FindView(_controllerTestContext, viewName));
        }

        [Theory]
        [MemberData("InvalidViewNameValues")]
        public void FindViewFullPathSucceedsWithCshtmlEnding(string viewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();
            // Append .cshtml so the viewname is no longer invalid
            viewName += ".cshtml";

            // Act & Assert
            // If this throws then our test case fails
            var result = viewEngine.FindPartialView(_controllerTestContext, viewName);

            Assert.False(result.Success);
        }

        [Theory]
        [MemberData("InvalidViewNameValues")]
        public void FindPartialViewFullPathFailsWithNoCshtmlEnding(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                viewEngine.FindPartialView(_controllerTestContext, partialViewName));
        }

        [Theory]
        [MemberData("InvalidViewNameValues")]
        public void FindPartialViewFullPathSucceedsWithCshtmlEnding(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();
            // Append .cshtml so the viewname is no longer invalid
            partialViewName += ".cshtml";

            // Act & Assert
            // If this throws then our test case fails
            var result = viewEngine.FindPartialView(_controllerTestContext, partialViewName);

            Assert.False(result.Success);
        }

        [Fact]
        public void FindPartialViewFailureSearchesCorrectLocationsWithAreas()
        {
            // Arrange
            var searchedLocations = new List<string>();
            var viewEngine = CreateSearchLocationViewEngineTester();

            // Act
            var result = viewEngine.FindPartialView(_areaTestContext, "partial");

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

            // Act
            var result = viewEngine.FindPartialView(_controllerTestContext, "partialNoArea");

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

            // Act
            var result = viewEngine.FindView(_areaTestContext, "full");

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

            // Act
            var result = viewEngine.FindView(_controllerTestContext, "fullNoArea");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(new[] { 
                "/Views/bar/fullNoArea.cshtml", 
                "/Views/Shared/fullNoArea.cshtml",
            }, result.SearchedLocations);
        }

        private IViewEngine CreateSearchLocationViewEngineTester()
        {
            var pageFactory = new Mock<IRazorPageFactory>();
            pageFactory.Setup(vpf => vpf.CreateInstance(It.IsAny<string>()))
                       .Returns<RazorPage>(null);

            var pageActivator = Mock.Of<IRazorPageActivator>();

            var viewEngine = new RazorViewEngine(pageFactory.Object, pageActivator);

            return viewEngine;
        }
    }
}