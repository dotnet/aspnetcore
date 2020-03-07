// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public abstract partial class MatcherConformanceTest
    {
        [Fact]
        public virtual async Task Match_EmptyRoute()
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/");
            var httpContext = CreateContext("/");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public virtual async Task Match_SingleLiteralSegment()
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/simple");
            var httpContext = CreateContext("/simple");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Fact]
        public virtual async Task Match_SingleLiteralSegment_TrailingSlash()
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/simple");
            var httpContext = CreateContext("/simple/");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Theory]
        [InlineData("/simple")]
        [InlineData("/sImpLe")]
        [InlineData("/SIMPLE")]
        public virtual async Task Match_SingleLiteralSegment_CaseInsensitive(string path)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/Simple");
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        // Some matchers will optimize for the ASCII case
        [Theory]
        [InlineData("/SÏmple", "/SÏmple")]
        [InlineData("/ab\uD834\uDD1Ecd", "/ab\uD834\uDD1Ecd")] // surrogate pair
        public virtual async Task Match_SingleLiteralSegment_Unicode(string template, string path)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher(template);
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        // Matchers should operate on the decoded representation - a matcher that calls
        // `httpContext.Request.Path.ToString()` will break this test.
        [Theory]
        [InlineData("/S%mple", "/S%mple")]
        [InlineData("/S\\imple", "/S\\imple")] // surrogate pair
        public virtual async Task Match_SingleLiteralSegment_PercentEncoded(string template, string path)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher(template);
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/imple")]
        [InlineData("/siple")]
        [InlineData("/simple1")]
        [InlineData("/simple/not-simple")]
        [InlineData("/simple/a/b/c")]
        public virtual async Task NotMatch_SingleLiteralSegment(string path)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/simple");
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Theory]
        [InlineData("simple")]
        [InlineData("/simple")]
        [InlineData("~/simple")]
        public virtual async Task Match_Sanitizies_Template(string template)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher(template);
            var httpContext = CreateContext("/simple");

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        // Matchers do their own 'splitting' of the path into segments, so including
        // some extra variation here
        [Theory]
        [InlineData("/a/b", "/a/b")]
        [InlineData("/a/b", "/A/B")]
        [InlineData("/a/b", "/a/b/")]
        [InlineData("/a/b/c", "/a/b/c")]
        [InlineData("/a/b/c", "/a/b/c/")]
        [InlineData("/a/b/c/d", "/a/b/c/d")]
        [InlineData("/a/b/c/d", "/a/b/c/d/")]
        public virtual async Task Match_MultipleLiteralSegments(string template, string path)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher(template);
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint);
        }

        // Matchers do their own 'splitting' of the path into segments, so including
        // some extra variation here
        [Theory]
        [InlineData("/a/b", "/")]
        [InlineData("/a/b", "/a")]
        [InlineData("/a/b", "/a/")]
        [InlineData("/a/b", "/a//")]
        [InlineData("/a/b", "/aa/")]
        [InlineData("/a/b", "/a/bb")]
        [InlineData("/a/b", "/a/bb/")]
        [InlineData("/a/b/c", "/aa/b/c")]
        [InlineData("/a/b/c", "/a/bb/c/")]
        [InlineData("/a/b/c", "/a/b/cab")]
        [InlineData("/a/b/c", "/d/b/c/")]
        [InlineData("/a/b/c", "//b/c")]
        [InlineData("/a/b/c", "/a/b//")]
        [InlineData("/a/b/c", "/a/b/c/d")]
        [InlineData("/a/b/c", "/a/b/c/d/e")]
        public virtual async Task NotMatch_MultipleLiteralSegments(string template, string path)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher(template);
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Fact]
        public virtual async Task Match_SingleParameter()
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/{p}");
            var httpContext = CreateContext("/14");
            var values = new RouteValueDictionary(new { p = "14", });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint, values);
        }

        [Fact]
        public virtual async Task Match_Constraint()
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/{p:int}");
            var httpContext = CreateContext("/14");
            var values = new RouteValueDictionary(new { p = "14", });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint, values);
        }

        [Fact]
        public virtual async Task Match_SingleParameter_TrailingSlash()
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/{p}");
            var httpContext = CreateContext("/14/");
            var values = new RouteValueDictionary(new { p = "14", });

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint, values);
        }

        [Fact]
        public virtual async Task Match_SingleParameter_WeirdNames()
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/foo/{ }/{.!$%}/{dynamic.data}");
            var httpContext = CreateContext("/foo/space/weirdmatch/matcherid");
            var values = new RouteValueDictionary()
            {
                { " ", "space" },
                { ".!$%", "weirdmatch" },
                { "dynamic.data", "matcherid" },
            };

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint, values);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/a/b")]
        [InlineData("/a/b/c")]
        [InlineData("//")]
        public virtual async Task NotMatch_SingleParameter(string path)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher("/{p}");
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }

        [Theory]
        [InlineData("/{a}/b", "/54/b", new string[] { "a", }, new string[] { "54", })]
        [InlineData("/{a}/b", "/54/b/", new string[] { "a", }, new string[] { "54", })]
        [InlineData("/{a}/{b}", "/54/73", new string[] { "a", "b" }, new string[] { "54", "73", })]
        [InlineData("/a/{b}/c", "/a/b/c", new string[] { "b", }, new string[] { "b", })]
        [InlineData("/a/{b}/c/", "/a/b/c", new string[] { "b", }, new string[] { "b", })]
        [InlineData("/{a}/b/{c}", "/54/b/c", new string[] { "a", "c", }, new string[] { "54", "c", })]
        [InlineData("/{a}/{b}/{c}", "/54/b/c", new string[] { "a", "b", "c", }, new string[] { "54", "b", "c", })]
        public virtual async Task Match_MultipleParameters(string template, string path, string[] keys, string[] values)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher(template);
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertMatch(httpContext, endpoint, keys, values);
        }

        [Theory]
        [InlineData("/{a}/b", "/54/bb")]
        [InlineData("/{a}/b", "/54/b/17")]
        [InlineData("/{a}/b", "/54/b//")]
        [InlineData("/{a}/{b}", "//73")]
        [InlineData("/{a}/{b}", "/54//")]
        [InlineData("/{a}/{b}", "/54/73/18")]
        [InlineData("/a/{b}/c", "/aa/b/c")]
        [InlineData("/a/{b}/c", "/a/b/cc")]
        [InlineData("/a/{b}/c", "/a/b/c/d")]
        [InlineData("/{a}/b/{c}", "/54/bb/c")]
        [InlineData("/{a}/{b}/{c}", "/54/b/c/d")]
        [InlineData("/{a}/{b}/{c}", "/54/b/c//")]
        [InlineData("/{a}/{b}/{c}", "//b/c/")]
        [InlineData("/{a}/{b}/{c}", "/54//c/")]
        [InlineData("/{a}/{b}/{c}", "/54/b//")]
        public virtual async Task NotMatch_MultipleParameters(string template, string path)
        {
            // Arrange
            var (matcher, endpoint) = CreateMatcher(template);
            var httpContext = CreateContext(path);

            // Act
            await matcher.MatchAsync(httpContext);

            // Assert
            MatcherAssert.AssertNotMatch(httpContext);
        }
    }
}
