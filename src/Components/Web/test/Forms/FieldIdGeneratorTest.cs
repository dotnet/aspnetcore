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
    [InlineData("Items[0]", "Items_0_")]
    [InlineData("Items[0].Name", "Items_0__Name")]
    [InlineData("Model.Items[0].Name", "Model_Items_0__Name")]
    public void SanitizeHtmlId_ProducesValidId(string? input, string expected)
    {
        // Act
        var result = FieldIdGenerator.SanitizeHtmlId(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeHtmlId_StartsWithNonLetter_PrependsZ()
    {
        // Arrange
        var input = "123Name";

        // Act
        var result = FieldIdGenerator.SanitizeHtmlId(input);

        // Assert
        Assert.StartsWith("z", result);
        Assert.Equal("z123Name", result);
    }

    [Fact]
    public void SanitizeHtmlId_StartsWithInvalidChar_PrependsZAndReplaces()
    {
        // Arrange
        var input = ".Property";

        // Act
        var result = FieldIdGenerator.SanitizeHtmlId(input);

        // Assert
        Assert.StartsWith("z", result);
        Assert.Equal("z_Property", result);
    }

    [Fact]
    public void SanitizeHtmlId_AllowsHyphensUnderscoresColons()
    {
        // Arrange
        var input = "my-field_name:value";

        // Act
        var result = FieldIdGenerator.SanitizeHtmlId(input);

        // Assert
        Assert.Equal("my-field_name:value", result);
    }

    [Fact]
    public void SanitizeHtmlId_ReplacesSpacesWithUnderscores()
    {
        // Arrange
        var input = "Field Name";

        // Act
        var result = FieldIdGenerator.SanitizeHtmlId(input);

        // Assert
        Assert.Equal("Field_Name", result);
    }
}
