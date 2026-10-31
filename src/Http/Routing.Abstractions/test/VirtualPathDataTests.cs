// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Routing;

public class VirtualPathDataTests
{
    [Fact]
    public void Constructor_CreatesEmptyDataTokensIfNull()
    {
        // Arrange
        var router = Mock.Of<IRouter>();
        var path = "/virtual path";

        // Act
        var pathData = new VirtualPathData(router, path, null);

        // Assert
        Assert.Same(router, pathData.Router);
        Assert.Equal(path, pathData.VirtualPath);
        Assert.NotNull(pathData.DataTokens);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void Constructor_CopiesDataTokens()
    {
        // Arrange
        var router = Mock.Of<IRouter>();
        var path = "/virtual path";
        var dataTokens = new RouteValueDictionary();
        dataTokens["TestKey"] = "TestValue";

        // Act
        var pathData = new VirtualPathData(router, path, dataTokens);

        // Assert
        Assert.Same(router, pathData.Router);
        Assert.Equal(path, pathData.VirtualPath);
        Assert.NotNull(pathData.DataTokens);
        Assert.Equal("TestValue", pathData.DataTokens["TestKey"]);
        Assert.Single(pathData.DataTokens);
        Assert.NotSame(dataTokens, pathData.DataTokens);
    }

    [Fact]
    public void VirtualPath_ReturnsEmptyStringIfNull()
    {
        // Arrange
        var router = Mock.Of<IRouter>();

        // Act
        var pathData = new VirtualPathData(router, virtualPath: null);

        // Assert
        Assert.Same(router, pathData.Router);
        Assert.Empty(pathData.VirtualPath);
        Assert.NotNull(pathData.DataTokens);
        Assert.Empty(pathData.DataTokens);
    }
}
