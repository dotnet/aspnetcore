// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Moq;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

public class AbsoluteUrlFactoryTests
{
    [Fact]
    public void GetAbsoluteUrl_ReturnsNull_ForInvalidData()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        var factory = new AbsoluteUrlFactory(accessor.Object);
        var path = "something|invalid";

        // Act
        var result = factory.GetAbsoluteUrl(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAbsoluteUrl_ReturnsUnmodifiedUrl_ForAbsoluteUrls()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        var factory = new AbsoluteUrlFactory(accessor.Object);
        var path = "https://localhost:5001/authenticate";

        // Act
        var result = factory.GetAbsoluteUrl(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void GetAbsoluteUrl_ReturnsContextBasedAbsoluteUrl_ForRelativeUrls()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("localhost:5001");
        ctx.Request.PathBase = "/virtual";

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(c => c.HttpContext).Returns(ctx);
        var factory = new AbsoluteUrlFactory(accessor.Object);
        var path = "/authenticate";

        // Act
        var result = factory.GetAbsoluteUrl(path);

        // Assert
        Assert.Equal("https://localhost:5001/virtual/authenticate", result);
    }
}
