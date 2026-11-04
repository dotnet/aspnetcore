// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class HttpRoutePatternParserTests
{
    [Fact]
    public void ParseMultipleVariables()
    {
        var pattern = HttpRoutePattern.Parse("/shelves/{shelf}/books/{book}");
        Assert.Null(pattern.Verb);
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("shelves", s),
            s => Assert.Equal("*", s),
            s => Assert.Equal("books", s),
            s => Assert.Equal("*", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(2, v.EndSegment);
                Assert.Equal("shelf", string.Join(".", v.FieldPath));
                Assert.False(v.HasCatchAllPath);
            },
            v =>
            {
                Assert.Equal(3, v.StartSegment);
                Assert.Equal(4, v.EndSegment);
                Assert.Equal("book", string.Join(".", v.FieldPath));
                Assert.False(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseComplexVariable()
    {
        var pattern = HttpRoutePattern.Parse("/v1/{book.name=shelves/*/books/*}");
        Assert.Null(pattern.Verb);
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("v1", s),
            s => Assert.Equal("shelves", s),
            s => Assert.Equal("*", s),
            s => Assert.Equal("books", s),
            s => Assert.Equal("*", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(5, v.EndSegment);
                Assert.Equal("book.name", string.Join(".", v.FieldPath));
                Assert.False(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseCatchAllSegment()
    {
        var pattern = HttpRoutePattern.Parse("/shelves/**");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("shelves", s),
            s => Assert.Equal("**", s));
        Assert.Empty(pattern.Variables);
    }

    [Fact]
    public void ParseCatchAllSegment2()
    {
        var pattern = HttpRoutePattern.Parse("/**");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("**", s));
        Assert.Empty(pattern.Variables);
    }

    [Fact]
    public void ParseAnySegment()
    {
        var pattern = HttpRoutePattern.Parse("/*");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("*", s));
        Assert.Empty(pattern.Variables);
    }

    [Fact]
    public void ParseSlash()
    {
        var pattern = HttpRoutePattern.Parse("/");
        Assert.Empty(pattern.Segments);
        Assert.Empty(pattern.Variables);
    }

    [Fact]
    public void ParseVerb()
    {
        var pattern = HttpRoutePattern.Parse("/a:foo");
        Assert.Equal("foo", pattern.Verb);
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s));
        Assert.Empty(pattern.Variables);
    }

    [Fact]
    public void ParseAnyAndCatchAllSegment()
    {
        var pattern = HttpRoutePattern.Parse("/*/**");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("*", s),
            s => Assert.Equal("**", s));
        Assert.Empty(pattern.Variables);
    }

    [Fact]
    public void ParseAnyAndCatchAllSegment2()
    {
        var pattern = HttpRoutePattern.Parse("/*/a/**");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("*", s),
            s => Assert.Equal("a", s),
            s => Assert.Equal("**", s));
        Assert.Empty(pattern.Variables);
    }

    [Fact]
    public void ParseNestedFieldPath()
    {
        var pattern = HttpRoutePattern.Parse("/a/{a.b.c}");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s),
            s => Assert.Equal("*", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(2, v.EndSegment);
                Assert.Equal("a.b.c", string.Join(".", v.FieldPath));
                Assert.False(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseComplexNestedFieldPath()
    {
        var pattern = HttpRoutePattern.Parse("/a/{a.b.c=*}");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s),
            s => Assert.Equal("*", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(2, v.EndSegment);
                Assert.Equal("a.b.c", string.Join(".", v.FieldPath));
                Assert.False(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseComplexCatchAll()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=**}");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s),
            s => Assert.Equal("**", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(2, v.EndSegment);
                Assert.Equal("b", string.Join(".", v.FieldPath));
                Assert.True(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseComplexPrefixSegment()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=c/*}");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s),
            s => Assert.Equal("c", s),
            s => Assert.Equal("*", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(3, v.EndSegment);
                Assert.Equal("b", string.Join(".", v.FieldPath));
                Assert.False(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseComplexPrefixSuffixSegment()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=c/*/d}");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s),
            s => Assert.Equal("c", s),
            s => Assert.Equal("*", s),
            s => Assert.Equal("d", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(4, v.EndSegment);
                Assert.Equal("b", string.Join(".", v.FieldPath));
                Assert.False(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseComplexPathCatchAll()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=c/**}");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s),
            s => Assert.Equal("c", s),
            s => Assert.Equal("**", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(3, v.EndSegment);
                Assert.Equal("b", string.Join(".", v.FieldPath));
                Assert.True(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseComplexPrefixSuffixCatchAll()
    {
        var pattern = HttpRoutePattern.Parse("/{x.y.z=a/**/b}/c/d");
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s),
            s => Assert.Equal("**", s),
            s => Assert.Equal("b", s),
            s => Assert.Equal("c", s),
            s => Assert.Equal("d", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(0, v.StartSegment);
                Assert.Equal(3, v.EndSegment);
                Assert.Equal("x.y.z", string.Join(".", v.FieldPath));
                Assert.True(v.HasCatchAllPath);
            });
    }

    [Fact]
    public void ParseCatchAllVerb()
    {
        var pattern = HttpRoutePattern.Parse("/a/{b=*}/**:verb");
        Assert.Equal("verb", pattern.Verb);
        Assert.Collection(
            pattern.Segments,
            s => Assert.Equal("a", s),
            s => Assert.Equal("*", s),
            s => Assert.Equal("**", s));
        Assert.Collection(
            pattern.Variables,
            v =>
            {
                Assert.Equal(1, v.StartSegment);
                Assert.Equal(2, v.EndSegment);
                Assert.Equal("b", string.Join(".", v.FieldPath));
                Assert.False(v.HasCatchAllPath);
            });
    }

    [Theory]
    [InlineData("", "Path template must start with a '/'.")]
    [InlineData("//", "Path template has an empty segment.")]
    [InlineData("/{}", "Incomplete or empty field path.")]
    [InlineData("/a/", "Route template shouldn't end with a '/'.")]
    [InlineData(":verb", "Path template must start with a '/'.")]
    [InlineData(":", "Path template must start with a '/'.")]
    [InlineData("/:", "Path template has an empty segment.")]
    [InlineData("/{var}:", "Empty verb.")]
    [InlineData("/{", "Incomplete or empty field path.")]
    [InlineData("/a{x}", "Path segment must end with a '/'.")]
    [InlineData("/{x}a", "Path segment must end with a '/'.")]
    [InlineData("/{x}{y}", "Path segment must end with a '/'.")]
    [InlineData("/{var=a/{nested=b}}", "Variable can't be nested.")]
    [InlineData("/{x=**}/*", "Only literal segments can follow a catch-all segment.")]
    [InlineData("/{x=}", "Path template has an empty segment.")]
    [InlineData("/**/*", "Only literal segments can follow a catch-all segment.")]
    [InlineData("/{x", "Expected '}' when parsing path template.")]
    public void Error(string pattern, string errorMessage)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => HttpRoutePattern.Parse(pattern));
        Assert.Equal(errorMessage, ex.InnerException!.Message);
    }
}
