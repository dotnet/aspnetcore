// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.ApiDescription.Client;

public class CSharpIdentifierTest
{
    [Theory]
    [InlineData('a')]
    [InlineData('Q')]
    [InlineData('\u2164')] // UnicodeCategory.LetterNumber (roman numeral five)
    [InlineData('_')]
    [InlineData('9')]
    [InlineData('\u0303')] // UnicodeCategory.NonSpacingMark (combining tilde)
    [InlineData('\u09CB')] // UnicodeCategory.SpacingCombiningMark (Bengali vowel sign O)
    [InlineData('\uFE4F')] // UnicodeCategory.ConnectorPunctuation (wavy low line)
    [InlineData('\u2062')] // UnicodeCategory.Format (invisible times)
    public void IsIdentifierPart_ReturnsTrue_WhenItShould(char character)
    {
        // Arrange and Act
        var result = CSharpIdentifier.IsIdentifierPart(character);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData('/')]
    [InlineData('-')]
    [InlineData('\u20DF')] // UnicodeCategory.EnclosingMark (combining enclosing diamond)
    [InlineData('\u2005')] // UnicodeCategory.SpaceSeparator (four-per-em space)
    [InlineData('\u0096')] // UnicodeCategory.Control (start of guarded area)
    [InlineData('\uFF1C')] // UnicodeCategory.MathSymbol (fullwidth less-than sign)
    public void IsIdentifierPart_ReturnsFalse_WhenItShould(char character)
    {
        // Arrange and Act
        var result = CSharpIdentifier.IsIdentifierPart(character);

        // Assert
        Assert.False(result);
    }

    // Output length is one longer than input in these cases.
    [Theory]
    [InlineData("9", "_9")]
    [InlineData("\u0303", "_\u0303")] // UnicodeCategory.NonSpacingMark (combining tilde)
    [InlineData("\u09CB", "_\u09CB")] // UnicodeCategory.SpacingCombiningMark (Bengali vowel sign O)
    [InlineData("\uFE4F", "_\uFE4F")] // UnicodeCategory.ConnectorPunctuation (wavy low line)
    [InlineData("\u2062", "_\u2062")] // UnicodeCategory.Format (invisible times)
    public void SanitizeIdentifier_AddsUnderscore_WhenItShould(string input, string expectdOutput)
    {
        // Arrange and Act
        var output = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        Assert.Equal(expectdOutput, output);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("Q", "Q")]
    [InlineData("\u2164", "\u2164")]
    [InlineData("_", "_")]
    public void SanitizeIdentifier_DoesNotAddUnderscore_WhenValidStartCharacter(string input, string expectdOutput)
    {
        // Arrange and Act
        var output = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        Assert.Equal(expectdOutput, output);
    }

    [Theory]
    [InlineData("/", "_")]
    [InlineData("-", "_")]
    [InlineData("\u20DF", "_")] // UnicodeCategory.EnclosingMark (combining enclosing diamond)
    [InlineData("\u2005", "_")] // UnicodeCategory.SpaceSeparator (four-per-em space)
    [InlineData("\u0096", "_")] // UnicodeCategory.Control (start of guarded area)
    [InlineData("\uFF1C", "_")] // UnicodeCategory.MathSymbol (fullwidth less-than sign)
    public void SanitizeIdentifier_DoesNotAddUnderscore_WhenInvalidCharacter(string input, string expectdOutput)
    {
        // Arrange and Act
        var output = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        Assert.Equal(expectdOutput, output);
    }

    [Theory]
    [InlineData("a/", "a_")]
    [InlineData("aa-bb", "aa_bb")]
    [InlineData("aa\u20DF\u20DF", "aa__")] // UnicodeCategory.EnclosingMark (combining enclosing diamond)
    [InlineData("aa\u2005bb\u2005cc", "aa_bb_cc")] // UnicodeCategory.SpaceSeparator (four-per-em space)
    [InlineData("aa\u0096\u0096bb", "aa__bb")] // UnicodeCategory.Control (start of guarded area)
    [InlineData("aa\uFF1C\uFF1C\uFF1Cbb", "aa___bb")] // UnicodeCategory.MathSymbol (fullwidth less-than sign)
    public void SanitizeIdentifier_ReplacesInvalidCharacters_WhenNotFirst(string input, string expectdOutput)
    {
        // Arrange and Act
        var output = CSharpIdentifier.SanitizeIdentifier(input);

        // Assert
        Assert.Equal(expectdOutput, output);
    }
}
