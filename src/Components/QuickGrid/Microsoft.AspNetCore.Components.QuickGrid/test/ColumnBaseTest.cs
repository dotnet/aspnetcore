// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable annotations

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class ColumnBaseTest
{
    [Theory]
    [InlineData(null, false, null, "col-options-button")]
    [InlineData("  custom-class  ", false, null, "col-options-button custom-class")]
    [InlineData(null, true, null, "col-options-button col-options-active")]
    [InlineData("custom-class", true, "attribute-class", "attribute-class col-options-button custom-class")]
    [InlineData("custom-class", true, "", "col-options-button custom-class")]
    public void ColumnOptionsButtonCssClass_PreservesExistingClasses(string? customClass, bool active, string? attributeClass, string expectedClass)
    {
        var column = new TemplateColumn<object>
        {
            ColumnOptionsButtonClass = customClass,
            ColumnOptionsActive = active,
            ColumnOptionsButtonAttributes = attributeClass is null
                ? null
                : new Dictionary<string, object> { ["class"] = attributeClass }
        };

        Assert.Equal(expectedClass, column.ColumnOptionsButtonCssClass);
    }

    [Fact]
    public void ColumnOptionsButtonCssClass_LabelOnlyAttributesPreserveActiveIndicator()
    {
        var column = new TemplateColumn<object>
        {
            ColumnOptionsActive = true,
            ColumnOptionsButtonAttributes = new Dictionary<string, object> { ["aria-label"] = "Custom options" }
        };

        Assert.Equal("col-options-button col-options-active", column.ColumnOptionsButtonCssClass);
    }
}
