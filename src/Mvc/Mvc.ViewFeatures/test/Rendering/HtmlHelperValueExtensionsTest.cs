// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class HtmlHelperValueExtensionsTest
{
    [Fact]
    public void Value_ReturnsModelValue()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.Value("SomeProperty");

        // Assert
        Assert.Equal("ModelValue", result);
    }

    [Fact]
    public void ValueFor_ReturnsModelValue()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.ValueFor(m => m.SomeProperty);

        // Assert
        Assert.Equal("ModelValue", result);
    }

    [Fact]
    public void ValueForModel_ReturnsModelValue()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.ValueForModel();

        // Assert
        Assert.Equal("{ SomeProperty = ModelValue }", result);
    }

    [Fact]
    public void ValueForModel_ReturnsModelValueWithSpecificFormat()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.ValueForModel(format: "-{0}-");

        // Assert
        Assert.Equal("-{ SomeProperty = ModelValue }-", result);
    }

    private class SomeModel
    {
        public string SomeProperty { get; set; }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ SomeProperty = {0} }}", SomeProperty ?? "(null)");
        }
    }
}
