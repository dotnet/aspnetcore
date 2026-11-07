// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Rewrite.Tests;

// The regex rules match against the request path without its leading '/'. These tests lock in that the
// leading '/' is never visible to the pattern or to the replacement.
public class RegexRuleMatchTests
{
    public static TheoryData<string, string, string, string> RegexCases => new()
    {
        // pattern, replacement, request path, expected rewritten value (null when the rule does not match)
        { "(bar)", "x$`y$_z", "/foo/bar", "xfoo/yfoo/barz" },
        { "(foo)", "$_,$`,$'", "/foo/bar", "foo/bar,,/bar" },
        { "^foo/(.*)$", "$1-$'-$&", "/foo/bar", "bar--foo/bar" },
        { "(?<!/)foo", "lb$0", "/foo/bar", "lbfoo" },
        { "(?<=/)bar", "la$0", "/foo/bar", "labar" },
        { @"\bfoo", "b$0", "/foo/bar", "bfoo" },
        { @"\Afoo", "a", "/foo/bar", "a" },
        { @"\Gfoo", "g", "/foo/bar", "g" },
        { "^/foo", "x", "/foo/bar", null },
        { "^$", "empty$_", "/", "empty" },
        { "(foo)", "$$_$1", "/foo/bar", "$_foo" },
        { "(?i)^FOO/(.+)$", "${1}", "/foo/bar", "bar" },
    };

    [Theory]
    [MemberData(nameof(RegexCases))]
    public void RewriteRule_MatchesPathWithoutLeadingSlash(string pattern, string replacement, string path, string expected)
    {
        var context = CreateContext(path);

        new RewriteRule(pattern, replacement, stopProcessing: false).ApplyRule(context);

        var expectedPath = expected is null ? path : "/" + expected.TrimStart('/');
        Assert.Equal(expectedPath, context.HttpContext.Request.Path.Value);
    }

    [Theory]
    [MemberData(nameof(RegexCases))]
    public void RedirectRule_MatchesPathWithoutLeadingSlash(string pattern, string replacement, string path, string expected)
    {
        var context = CreateContext(path);

        new RedirectRule(pattern, replacement, StatusCodes.Status302Found).ApplyRule(context);

        if (expected is null)
        {
            Assert.Equal(RuleResult.ContinueRules, context.Result);
            Assert.False(context.HttpContext.Response.Headers.ContainsKey("Location"));
        }
        else
        {
            Assert.Equal(RuleResult.EndResponse, context.Result);
            var expectedLocation = expected.Length == 0 ? "/" : "/" + expected.TrimStart('/');
            Assert.Equal(expectedLocation, context.HttpContext.Response.Headers.Location.ToString());
        }
    }

    [Theory]
    [InlineData("^foo/(.*)$", "{R:1}-{R:0}", "/foo/bar", "/bar-foo/bar")]
    [InlineData("(?<!/)foo", "lb{R:0}", "/foo/bar", "/lbfoo")]
    [InlineData("^/foo", "x", "/foo/bar", "/foo/bar")]
    [InlineData("^$", "empty", "/", "/empty")]
    [InlineData("^foo/(.*)$", "{R:1}", "/foo/bar", "/foo/bar", true)]
    [InlineData("^/foo", "x", "/foo/bar", "/x", true)]
    public void IISRule_MatchesPathWithoutLeadingSlash(string pattern, string url, string path, string expectedPath, bool negate = false)
    {
        var xml = $"""
            <rewrite>
              <rules>
                <rule name="test">
                  <match url="{System.Security.SecurityElement.Escape(pattern)}"{(negate ? " negate=\"true\"" : "")} />
                  <action type="Rewrite" url="{url}" />
                </rule>
              </rules>
            </rewrite>
            """;
        var rule = new UrlRewriteFileParser().Parse(new StringReader(xml), false).Single();
        var context = CreateContext(path);

        rule.ApplyRule(context);

        Assert.Equal(expectedPath, context.HttpContext.Request.Path.Value);
    }

    private static RewriteContext CreateContext(string path)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.Path = path;
        return new RewriteContext { HttpContext = httpContext, Logger = NullLogger.Instance, Result = RuleResult.ContinueRules };
    }
}
