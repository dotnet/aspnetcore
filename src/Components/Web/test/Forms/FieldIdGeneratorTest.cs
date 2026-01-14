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
    [InlineData("Model.Address.Street", "Model_Address_Street")]
    [InlineData("Model.Items[0].Name", "Model_Items[0]_Name")]
    public void SanitizeHtmlId_ProducesValidId(string? input, string expected)
    {
        var result = FieldIdGenerator.SanitizeHtmlId(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeHtmlId_ReplacesWhitespaceWithUnderscores()
    {
        var input = "Field\tName\nWith\rVariousWhitespace";

        var result = FieldIdGenerator.SanitizeHtmlId(input);

        Assert.Equal("Field_Name_With_VariousWhitespace", result);
    }
}
