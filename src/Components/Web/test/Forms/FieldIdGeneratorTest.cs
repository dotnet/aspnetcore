// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Components.Forms;

public class FieldIdGeneratorTest
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Name", "Name")]
    [InlineData("name", "name")]
    [InlineData("Model.Property", "Model_Property")]
    [InlineData("Model.Address.Street", "Model_Address_Street")]
    [InlineData("Items[0]", "Items[0]")]
    [InlineData("Items[0].Name", "Items[0]_Name")]
    [InlineData("Model.Items[0].Name", "Model_Items[0]_Name")]
    [InlineData("123Name", "123Name")]
    [InlineData(":Property", ":Property")]
    public void SanitizeHtmlId_ProducesValidId(string? input, string expected)
    {
        var result = FieldIdGenerator.SanitizeHtmlId(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeHtmlId_AllowsStartingWithDigit()
    {
        var input = "123Name";

        var result = FieldIdGenerator.SanitizeHtmlId(input);

        Assert.Equal("123Name", result);
    }

    [Fact]
    public void SanitizeHtmlId_ReplacesPeriodAtStart()
    {
        var input = ".Property";

        var result = FieldIdGenerator.SanitizeHtmlId(input);

        Assert.Equal("_Property", result);
    }

    [Fact]
    public void SanitizeHtmlId_AllowsSpecialCharacters()
    {
        var input = "my-field_name:value";

        var result = FieldIdGenerator.SanitizeHtmlId(input);

        Assert.Equal("my-field_name:value", result);
    }

    [Fact]
    public void SanitizeHtmlId_ReplacesWhitespaceWithUnderscores()
    {
        var input = "Field Name";

        var result = FieldIdGenerator.SanitizeHtmlId(input);

        Assert.Equal("Field_Name", result);
    }
}
