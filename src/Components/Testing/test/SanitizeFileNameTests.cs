// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class SanitizeFileNameTests
{
    [Fact]
    public void NormalName_ReturnsUnchanged()
    {
        // Arrange & Act
        var result = PlaywrightExtensions.SanitizeFileName("MyTestName");

        // Assert
        Assert.Equal("MyTestName", result);
    }

    [Theory]
    [InlineData("test<name>", "test_name_")]
    [InlineData("test:name", "test_name")]
    [InlineData("test|name", "test_name")]
    [InlineData("test\"name", "test_name")]
    public void SpecialChars_ReplacedWithUnderscore(string input, string expected)
    {
        // Act
        var result = PlaywrightExtensions.SanitizeFileName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PathSeparators_ReplacedWithUnderscore()
    {
        // Arrange & Act
        var result = PlaywrightExtensions.SanitizeFileName("path/to\\test");

        // Assert
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
    }

    [Fact]
    public void EmptyString_ReturnsEmpty()
    {
        // Act
        var result = PlaywrightExtensions.SanitizeFileName("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void DotsAndDashes_Preserved()
    {
        // Act
        var result = PlaywrightExtensions.SanitizeFileName("my-test.name_v2");

        // Assert
        Assert.Equal("my-test.name_v2", result);
    }

    [Fact]
    public void Spaces_Preserved()
    {
        // Act
        var result = PlaywrightExtensions.SanitizeFileName("my test name");

        // Assert
        Assert.Equal("my test name", result);
    }

    [Fact]
    public void LongName_ReturnsAllCharacters()
    {
        // Arrange
        var longName = new string('a', 300);

        // Act
        var result = PlaywrightExtensions.SanitizeFileName(longName);

        // Assert
        Assert.Equal(300, result.Length);
        Assert.Equal(longName, result);
    }

    [Fact]
    public void QuestionMarkAndAsterisk_Replaced()
    {
        // Act
        var result = PlaywrightExtensions.SanitizeFileName("test?name*here");

        // Assert
        Assert.Equal("test_name_here", result);
    }

    [Fact]
    public void NullChar_Replaced()
    {
        // Act
        var result = PlaywrightExtensions.SanitizeFileName("test\0name");

        // Assert
        Assert.Equal("test_name", result);
    }

    [Theory]
    [InlineData("Namespace.Class.Method(arg1, arg2)")]
    [InlineData("TestClass.TestMethod [variant \"special\"]")]
    public void TypicalTestDisplayNames_Sanitized(string displayName)
    {
        // Act
        var result = PlaywrightExtensions.SanitizeFileName(displayName);

        // Assert — must not contain any cross-platform invalid file name chars
        char[] invalidChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|', '\0'];
        Assert.DoesNotContain(result, c => invalidChars.Contains(c));
    }
}
