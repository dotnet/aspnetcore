// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class TestArtifactDirectoryTests
{
    [Fact]
    public void NormalName_ReturnsUnchanged()
    {
        var result = GetDirectoryName("MyTestName");

        Assert.Equal("MyTestName", result);
    }

    [Theory]
    [InlineData("test<name>", "test_name_")]
    [InlineData("test:name", "test_name")]
    [InlineData("test|name", "test_name")]
    [InlineData("test\"name", "test_name")]
    public void SpecialChars_ReplacedWithUnderscore(string input, string expected)
    {
        var result = GetDirectoryName(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void PathSeparators_ReplacedWithUnderscore()
    {
        var result = GetDirectoryName("path/to\\test");

        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
    }

    [Fact]
    public void EmptyString_ReturnsArtifactRoot()
    {
        var result = TestArtifactDirectory.GetPath("");
        var artifactRoot = Path.GetDirectoryName(TestArtifactDirectory.GetPath("test"));

        Assert.NotNull(artifactRoot);
        Assert.Equal(
            Path.TrimEndingDirectorySeparator(artifactRoot),
            Path.TrimEndingDirectorySeparator(result));
    }

    [Fact]
    public void DotsAndDashes_Preserved()
    {
        var result = GetDirectoryName("my-test.name_v2");

        Assert.Equal("my-test.name_v2", result);
    }

    [Fact]
    public void Spaces_Preserved()
    {
        var result = GetDirectoryName("my test name");

        Assert.Equal("my test name", result);
    }

    [Fact]
    public void LongName_ReturnsAllCharacters()
    {
        var longName = new string('a', 300);

        var result = GetDirectoryName(longName);

        Assert.Equal(300, result.Length);
        Assert.Equal(longName, result);
    }

    [Fact]
    public void QuestionMarkAndAsterisk_Replaced()
    {
        var result = GetDirectoryName("test?name*here");

        Assert.Equal("test_name_here", result);
    }

    [Fact]
    public void NullChar_Replaced()
    {
        var result = GetDirectoryName("test\0name");

        Assert.Equal("test_name", result);
    }

    [Theory]
    [InlineData("Namespace.Class.Method(arg1, arg2)")]
    [InlineData("TestClass.TestMethod [variant \"special\"]")]
    public void TypicalTestDisplayNames_Sanitized(string displayName)
    {
        var result = GetDirectoryName(displayName);

        char[] invalidChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|', '\0'];
        Assert.DoesNotContain(result, c => invalidChars.Contains(c));
    }

    private static string GetDirectoryName(string testName)
    {
        return Path.GetFileName(TestArtifactDirectory.GetPath(testName));
    }
}
