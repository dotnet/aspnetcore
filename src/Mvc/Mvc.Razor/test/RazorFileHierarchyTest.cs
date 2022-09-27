// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

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
