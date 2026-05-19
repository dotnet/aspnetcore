// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.Core;

/// <summary>
/// Test the TextBox extensions in <see cref="HtmlHelperInputExtensions" /> class.
/// </summary>
public class HtmlHelperTextBoxExtensionsTest
{
    [Fact]
    public void TextBox_UsesSpecifiedExpression()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textBoxResult = helper.TextBox("Property1");

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[propValue]]\" />",
            HtmlContentUtilities.HtmlContentToString(textBoxResult));
    }

    [Fact]
    public void TextBoxFor_UsesSpecifiedExpression()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textBoxForResult = helper.TextBoxFor(m => m.Property1);

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[propValue]]\" />",
            HtmlContentUtilities.HtmlContentToString(textBoxForResult));
    }

    [Fact]
    public void TextBox_UsesSpecifiedValue()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textBoxResult = helper.TextBox("Property1", value: "myvalue");

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(textBoxResult));
    }

    [Fact]
    public void TextBox_UsesSpecifiedFormat()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textBoxResult = helper.TextBox("Property1", value: null, format: "prefix: {0}");

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[prefix: propValue]]\" />",
            HtmlContentUtilities.HtmlContentToString(textBoxResult));
    }

    [Fact]
    public void TextBox_UsesSpecifiedFormatOverridesPropertyValue()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textBoxResult = helper.TextBox("Property1", value: "myvalue", format: "prefix: {0}");

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[prefix: myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(textBoxResult));
    }

    [Fact]
    public void TextBox_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var htmlAttributes = new
        {
            attr = "value",
            name = "-expression-", // overridden
        };

        var model = new TestModel
        {
            Property1 = "propValue"
        };

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        helper.ViewContext.ClientValidationEnabled = false;

        // Act
        var textBoxResult = helper.TextBox("Property1", "myvalue", htmlAttributes);

        // Assert
        Assert.Equal(
            "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" " +
            "name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(textBoxResult));
    }

    [Fact]
    public void TextBoxFor_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var htmlAttributes = new
        {
            attr = "value",
            name = "-expression-", // overridden
        };

        var model = new TestModel
        {
            Property1 = "propValue"
        };

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        helper.ViewContext.ClientValidationEnabled = false;

        // Act
        var textBoxForResult = helper.TextBoxFor(m => m.Property1, htmlAttributes);

        // Assert
        Assert.Equal(
            "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" " +
            "name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[propValue]]\" />",
            HtmlContentUtilities.HtmlContentToString(textBoxForResult));
    }

    [Fact]
    public void TextBoxFor_Throws_IfFullNameEmpty()
    {
        // Arrange
        var expectedMessage = "The name of an HTML field cannot be null or empty. Instead use methods " +
            "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
            "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

        var htmlAttributes = new
        {
            attr = "value",
        };

        var helper = DefaultTemplatesUtilities.GetHtmlHelper("propValue");
        helper.ViewContext.ClientValidationEnabled = false;

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => helper.TextBoxFor(m => m, htmlAttributes),
            paramName: "expression",
            exceptionMessage: expectedMessage);
    }

    [Fact]
    public void TextBoxFor_DoesNotThrow_IfFullNameEmpty_WithNameAttribute()
    {
        // Arrange
        var htmlAttributes = new
        {
            attr = "value",
            name = "-expression-",
        };

        var helper = DefaultTemplatesUtilities.GetHtmlHelper("propValue");
        helper.ViewContext.ClientValidationEnabled = false;

        // Act
        var textBoxForResult = helper.TextBoxFor(m => m, htmlAttributes);

        // Assert
        Assert.Equal(
            "<input attr=\"HtmlEncode[[value]]\" " +
            "name=\"HtmlEncode[[-expression-]]\" type=\"HtmlEncode[[text]]\" value=\"HtmlEncode[[propValue]]\" />",
            HtmlContentUtilities.HtmlContentToString(textBoxForResult));
    }

    private class TestModel
    {
        public string Property1 { get; set; }
    }
}
