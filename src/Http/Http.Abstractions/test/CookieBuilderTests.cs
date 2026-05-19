// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Abstractions.Tests;

public class CookieBuilderTests
{
    [Theory]
    [InlineData(CookieSecurePolicy.Always, false, true)]
    [InlineData(CookieSecurePolicy.Always, true, true)]
    [InlineData(CookieSecurePolicy.SameAsRequest, true, true)]
    [InlineData(CookieSecurePolicy.SameAsRequest, false, false)]
    [InlineData(CookieSecurePolicy.None, true, false)]
    [InlineData(CookieSecurePolicy.None, false, false)]
    public void ConfiguresSecurePolicy(CookieSecurePolicy policy, bool requestIsHttps, bool secure)
    {
        var builder = new CookieBuilder
        {
            SecurePolicy = policy
        };
        var context = new DefaultHttpContext();
        context.Request.IsHttps = requestIsHttps;
        var options = builder.Build(context);

        Assert.Equal(secure, options.Secure);
    }

    [Fact]
    public void ComputesExpiration()
    {
        Assert.Null(new CookieBuilder().Build(new DefaultHttpContext()).Expires);

        var now = DateTimeOffset.Now;
        var options = new CookieBuilder { Expiration = TimeSpan.FromHours(1) }.Build(new DefaultHttpContext(), now);
        Assert.Equal(now.AddHours(1), options.Expires);
    }

    [Fact]
    public void ComputesMaxAge()
    {
        Assert.Null(new CookieBuilder().Build(new DefaultHttpContext()).MaxAge);

        var now = TimeSpan.FromHours(1);
        var options = new CookieBuilder { MaxAge = now }.Build(new DefaultHttpContext());
        Assert.Equal(now, options.MaxAge);
    }

    [Fact]
    public void CookieBuilderPreservesDefaultPath()
    {
        Assert.Equal(new CookieOptions().Path, new CookieBuilder().Build(new DefaultHttpContext()).Path);
    }

    [Fact]
    public void CookieBuilder_Extensions_Added()
    {
        var builder = new CookieBuilder();
        builder.Extensions.Add("simple");
        builder.Extensions.Add("key=value");

        var options = builder.Build(new DefaultHttpContext());
        Assert.Equal(2, options.Extensions.Count);
        Assert.Contains("simple", options.Extensions);
        Assert.Contains("key=value", options.Extensions);

        var cookie = options.CreateCookieHeader("name", "value");
        Assert.Equal("name", cookie.Name.AsSpan());
        Assert.Equal("value", cookie.Value.AsSpan());
        Assert.Equal(2, cookie.Extensions.Count);
        Assert.Contains("simple", cookie.Extensions);
        Assert.Contains("key=value", cookie.Extensions);

        Assert.Equal("name=value; path=/; simple; key=value", cookie.ToString());
    }
}
