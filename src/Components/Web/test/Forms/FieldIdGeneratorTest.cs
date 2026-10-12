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
    [InlineData("Field\tName\nWith\rVariousWhitespace", "Field_Name_With_VariousWhitespace")]
    [InlineData("Field\u00A0Name", "Field_Name")] // Non-breaking space
    public void SanitizeHtmlId_ProducesValidId(string? input, string expected)
    {
        var result = FieldIdGenerator.SanitizeHtmlId(input);

        Assert.Equal(expected, result);
    }
}
