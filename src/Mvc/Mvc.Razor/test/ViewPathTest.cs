// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

public class ViewPathTest
{
    [Theory]
    [InlineData("/Views/Home/Index.cshtml")]
    [InlineData("\\Views/Home/Index.cshtml")]
    [InlineData("\\Views\\Home/Index.cshtml")]
    [InlineData("\\Views\\Home\\Index.cshtml")]
    public void NormalizePath_NormalizesSlashes(string input)
    {
        // Act
        var normalizedPath = ViewPath.NormalizePath(input);

        // Assert
        Assert.Equal("/Views/Home/Index.cshtml", normalizedPath);
    }

    [Theory]
    [InlineData("Views/Home/Index.cshtml")]
    [InlineData("Views\\Home\\Index.cshtml")]
    public void NormalizePath_AppendsLeadingSlash(string input)
    {
        // Act
        var normalizedPath = ViewPath.NormalizePath(input);

        // Assert
        Assert.Equal("/Views/Home/Index.cshtml", normalizedPath);
    }
}
