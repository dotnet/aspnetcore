// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class AttributeMatcherTest
{
    private static readonly Func<Mode, Mode, int> Compare = (a, b) => a - b;

    [Theory]
    [InlineData(new object[] { new[] { "required-attr" } })]
    [InlineData(new object[] { new[] { "first-attr", "second-attr" } })]
    public void TryDetermineMode_ReturnsFalseIfNoAttributeMatchesAllRequiredAttributes(string[] modeAttributes)
    {
        // Arrange
        var modeInfos = new[]
        {
                new ModeAttributes<Mode>(Mode.A, modeAttributes)
            };
        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("first-attr", "value"),
                new TagHelperAttribute("not-in-any-mode", "value")
            };
        var context = MakeTagHelperContext(attributes);

        // Act
        Mode result;
        var modeMatch = AttributeMatcher.TryDetermineMode(context, modeInfos, Compare, out result);

        // Assert
        Assert.False(modeMatch);
    }

    [Fact]
    public void DetermineMode_SetsModeIfAllAttributesMatch()
    {
        // Arrange
        var modeInfos = new[]
        {
                new ModeAttributes<Mode>(Mode.A, new[] { "a-required-attributes" }),
                new ModeAttributes<Mode>(Mode.B, new[] { "first-attr", "second-attr" }),
                new ModeAttributes<Mode>(Mode.C, new[] { "first-attr", "third-attr" }),
            };
        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("first-attr", "value"),
                new TagHelperAttribute("second-attr", "value"),
                new TagHelperAttribute("not-in-any-mode", "value")
            };
        var context = MakeTagHelperContext(attributes);

        // Act
        Mode result;
        var modeMatch = AttributeMatcher.TryDetermineMode(context, modeInfos, Compare, out result);

        // Assert
        Assert.True(modeMatch);
        Assert.Equal(Mode.B, result);
    }

    [Fact]
    public void DetermineMode_SetsModeWithHighestValue()
    {
        // Arrange
        var modeInfos = new[]
        {
                new ModeAttributes<Mode>(Mode.A, new[] { "first-attr" }),
                new ModeAttributes<Mode>(Mode.B, new[] { "first-attr", "second-attr" }),
                new ModeAttributes<Mode>(Mode.D, new[] { "second-attr", "third-attr" }),
                new ModeAttributes<Mode>(Mode.C, new[] { "first-attr", "second-attr", "third-attr" }),
            };
        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("first-attr", "value"),
                new TagHelperAttribute("second-attr", "value"),
                new TagHelperAttribute("third-attr", "value"),
                new TagHelperAttribute("not-in-any-mode", "value")
            };
        var context = MakeTagHelperContext(attributes);

        // Act
        Mode result;
        var modeMatch = AttributeMatcher.TryDetermineMode(context, modeInfos, Compare, out result);

        // Assert
        Assert.True(modeMatch);
        Assert.Equal(Mode.D, result);
    }

    private static TagHelperContext MakeTagHelperContext(TagHelperAttributeList attributes)
    {
        return new TagHelperContext(
            tagName: "test",
            allAttributes: attributes,
            items: new Dictionary<object, object>(),
            uniqueId: Guid.NewGuid().ToString("N"));
    }

    private enum Mode
    {
        A = 0,
        B = 1,
        C = 3,
        D = 4
    };
}
