// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class RazorFileHierarchyTest
    {
        [Fact]
        public void GetViewStartPaths_ForFileAtRoot()
        {
            // Arrange
            var expected = new[] { "/_ViewStart.cshtml", };
            var path = "/Home.cshtml";

            // Act
            var actual = RazorFileHierarchy.GetViewStartPaths(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetViewStartPaths_ForForFileInViewsDirectory()
        {
            // Arrange
            var expected = new[]
            {
                "/Views/Home/_ViewStart.cshtml",
                "/Views/_ViewStart.cshtml",
                "/_ViewStart.cshtml",
            };
            var path = "/Views/Home/Index.cshtml";

            // Act
            var actual = RazorFileHierarchy.GetViewStartPaths(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetViewStartPaths_ForForFileInAreasDirectory()
        {
            // Arrange
            var expected = new[]
            {
                "/Areas/Views/MyArea/Home/_ViewStart.cshtml",
                "/Areas/Views/MyArea/_ViewStart.cshtml",
                "/Areas/Views/_ViewStart.cshtml",
                "/Areas/_ViewStart.cshtml",
                "/_ViewStart.cshtml",
            };
            var path = "/Areas/Views/MyArea/Home/Index.cshtml";

            // Act
            var actual = RazorFileHierarchy.GetViewStartPaths(path);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
