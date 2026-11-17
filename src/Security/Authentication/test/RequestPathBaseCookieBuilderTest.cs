// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

public class RequestPathBaseCookieBuilderTest
{
    [Fact]
    public void Build_EscapesSemicolonInPathBase()
    {
        var context = new DefaultHttpContext();
        context.Request.PathBase = "/a;b";
        var builder = new RequestPathBaseCookieBuilder();

        var options = builder.Build(context);

        Assert.Equal("/a%3Bb", options.Path);
    }

    [Fact]
    public void Build_DoesNotEscapeCommaInPathBase()
    {
        var context = new DefaultHttpContext();
        context.Request.PathBase = "/a,b";
        var builder = new RequestPathBaseCookieBuilder();

        var options = builder.Build(context);

        Assert.Equal("/a,b", options.Path);
    }

    [Fact]
    public void Build_DoesNotEscapeExplicitPath()
    {
        var context = new DefaultHttpContext();
        context.Request.PathBase = "/a;b";
        var builder = new RequestPathBaseCookieBuilder { Path = "/x;y" };

        var options = builder.Build(context);

        Assert.Equal("/x;y", options.Path);
    }
}
