// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageViewLocationExpanderTest
    {
        [Fact]
        public void PopulateValues_DoesNothing()
        {
            // Arrange
            var context = CreateContext();

            var expander = new PageViewLocationExpander();

            // Act
            expander.PopulateValues(context);

            // Assert
            Assert.Empty(context.Values);
        }

        [Fact]
        public void ExpandLocations_NoOp_ForNonPage()
        {
            // Arrange
            var context = CreateContext(pageName: null);
            var locations = new string[]
            {
                "/ignore-me",
            };

            var expander = new PageViewLocationExpander();

            // Act
            var actual = expander.ExpandViewLocations(context, locations);

            // Assert
            Assert.Equal(locations, actual);
        }

        [Fact]
        public void ExpandLocations_NoOp_WhenLocationDoesNotContainPageToken()
        {
            // Arrange
            var context = CreateContext(pageName: null);
            var locations = new string[]
            {
                "/ignore-me",
            };

            var expander = new PageViewLocationExpander();

            // Act
            var actual = expander.ExpandViewLocations(context, locations);

            // Assert
            Assert.Equal(locations, actual);
        }

        [Theory]
        [InlineData("/Index", new string[] { "/{0}.cshtml" })]
        [InlineData("/Edit", new string[] { "/{0}.cshtml" })]
        [InlineData("/Customers/Add", new string[] { "/Customers/{0}.cshtml", "/{0}.cshtml" })]
        public void ExpandLocations_ExpandsDirectories_WhenLocationContainsPage(
            string pageName,
            string[] expected)
        {
            // Arrange
            var context = CreateContext(pageName: pageName);

            var locations = new string[]
            {
                "/{1}/{0}.cshtml",
            };

            var expander = new PageViewLocationExpander();

            // Act
            var actual = expander.ExpandViewLocations(context, locations);

            // Assert
            Assert.Equal(expected, actual.ToArray());
        }

        [Fact]
        public void ExpandLocations_ExpandsDirectories_MultipleLocations()
        {
            // Arrange
            var context = CreateContext(pageName: "/Customers/Edit");

            var locations = new string[]
            {
                "/Pages/{1}/{0}.cshtml",
                "/More/Paths/{1}/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
            };

            var expected = new string[]
            {
                "/Pages/Customers/{0}.cshtml",
                "/Pages/{0}.cshtml",
                "/More/Paths/Customers/{0}.cshtml",
                "/More/Paths/{0}.cshtml",
                "/Views/Shared/{0}.cshtml",
            };

            var expander = new PageViewLocationExpander();

            // Act
            var actual = expander.ExpandViewLocations(context, locations);

            // Assert
            Assert.Equal(expected, actual.ToArray());
        }

        private ViewLocationExpanderContext CreateContext(string viewName = "_LoginPartial.cshtml", string pageName = null)
        {
            return new ViewLocationExpanderContext(
                new ActionContext(),
                viewName,
                controllerName: null,
                areaName: null,
                pageName: pageName,
                isMainPage: true)
            {
                Values = new Dictionary<string, string>(),
            };
        }
    }
}
