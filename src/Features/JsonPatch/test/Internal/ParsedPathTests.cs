// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal;

public class ParsedPathTests
{
    [Theory]
    [InlineData("foo/bar~0baz", new string[] { "foo", "bar~baz" })]
    [InlineData("foo/bar~00baz", new string[] { "foo", "bar~0baz" })]
    [InlineData("foo/bar~01baz", new string[] { "foo", "bar~1baz" })]
    [InlineData("foo/bar~10baz", new string[] { "foo", "bar/0baz" })]
    [InlineData("foo/bar~1baz", new string[] { "foo", "bar/baz" })]
    [InlineData("foo/bar~0/~0/~1~1/~0~0/baz", new string[] { "foo", "bar~", "~", "//", "~~", "baz" })]
    [InlineData("~0~1foo", new string[] { "~/foo" })]
    [InlineData("/foo/bar", new string[] { "foo", "bar" })]
    [InlineData("/foo", new string[] { "foo" })]
    [InlineData("", new string[] { })]
    public void ParsingValidPathShouldSucceed(string path, string[] expected)
    {
        // Arrange & Act
        var parsedPath = new ParsedPath(path);

        // Assert
        Assert.Equal(expected, parsedPath.Segments);
    }

    [Theory]
    [InlineData("foo/bar~")]
    [InlineData("~")]
    [InlineData("~2")]
    [InlineData("foo~3bar")]
    public void PathWithInvalidEscapeSequenceShouldFail(string path)
    {
        // Arrange, Act & Assert
        Assert.Throws<JsonPatchException>(() =>
        {
            var parsedPath = new ParsedPath(path);
        });
    }

    [Theory]
    [InlineData("//isSmth", new string[] { "", "isSmth" })]
    [InlineData("//", new string[] { "", "" })]
    [InlineData("/", new string[] { "" })]
    [InlineData("", new string[] { })]
    [InlineData("/foo/", new string[] { "foo", "" })]
    [InlineData("/foo//bar", new string[] { "foo", "", "bar" })]
    [InlineData("foo//bar", new string[] { "foo", "", "bar" })]
    [InlineData("foo/", new string[] { "foo", "" })]
    public void PathWithEmptyReferenceTokenShouldNotFail(string path, string[] expectedSegments)
    {
        var parsedPath = new ParsedPath(path);
        Assert.Equal(expectedSegments, parsedPath.Segments);
    }
}
