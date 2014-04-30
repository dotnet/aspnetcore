// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

        [Theory]
        [InlineData("~/foo/bar")]
        [InlineData("/foo/bar")]
        [InlineData("~/foo/bar.txt")]
        [InlineData("/foo/bar.txt")]
        public void FindViewFullPathRequiresCshtmlEnding(string viewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                viewEngine.FindView(_controllerTestContext, viewName));

            // Append .cshtml so we can try and no longer throw.
            viewName += ".cshtml";

            // If this throws then our test case fails
            var result = viewEngine.FindPartialView(_controllerTestContext, viewName);

            Assert.False(result.Success);
        }

        [Theory]
        [InlineData("~/foo/bar")]
        [InlineData("/foo/bar")]
        [InlineData("~/foo/bar.txt")]
        [InlineData("/foo/bar.txt")]
        public void FindPartialViewFullPathRequiresCshtmlEnding(string partialViewName)
        {
            // Arrange
            var viewEngine = CreateSearchLocationViewEngineTester();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                viewEngine.FindPartialView(_controllerTestContext, partialViewName));

            // Append .cshtml so we can try and no longer throw.
            partialViewName += ".cshtml";

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
            var virtualPathFactory = new Mock<IVirtualPathViewFactory>();
            virtualPathFactory.Setup(vpf => vpf.CreateInstance(It.IsAny<string>())).Returns<IView>(null);

            var viewEngine = new RazorViewEngine(virtualPathFactory.Object);

            return viewEngine;
        }
    }
}