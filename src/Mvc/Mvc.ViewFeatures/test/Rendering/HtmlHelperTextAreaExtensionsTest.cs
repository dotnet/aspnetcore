// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Core;

/// <summary>
/// Test the TextArea extensions in <see cref="HtmlHelperInputExtensions" /> class.
/// </summary>
public class HtmlHelperTextAreaExtensionsTest
{
    [Fact]
    public void TextArea_UsesSpecifiedExpression()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textAreaResult = helper.TextArea("Property1");

        // Assert
        Assert.Equal(
            "<textarea id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\">" + Environment.NewLine +
            "HtmlEncode[[propValue]]</textarea>",
            HtmlContentUtilities.HtmlContentToString(textAreaResult));
    }

    [Fact]
    public void TextAreaFor_UsesSpecifiedExpression()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textAreaForResult = helper.TextAreaFor(m => m.Property1);

        // Assert
        Assert.Equal(
            "<textarea id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\">" + Environment.NewLine +
            "HtmlEncode[[propValue]]</textarea>",
            HtmlContentUtilities.HtmlContentToString(textAreaForResult));
    }

    [Fact]
    public void TextAreaHelpers_UsesSpecifiedValue()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textAreaResult = helper.TextArea("Property1", value: "myvalue");

        // Assert
        Assert.Equal(
            "<textarea id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\">" + Environment.NewLine +
            "HtmlEncode[[myvalue]]</textarea>",
            HtmlContentUtilities.HtmlContentToString(textAreaResult));
    }

    [Fact]
    public void TextArea_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textAreaResult = helper.TextArea("Property1", htmlAttributes: new { attr = "value" });

        // Assert
        Assert.Equal(
            "<textarea attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\">" +
            Environment.NewLine +
            "HtmlEncode[[propValue]]</textarea>",
            HtmlContentUtilities.HtmlContentToString(textAreaResult));
    }

    [Fact]
    public void TextAreaFor_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var textAreaForResult = helper.TextAreaFor(m => m.Property1, htmlAttributes: new { attr = "value" });

        // Assert
        Assert.Equal(
            "<textarea attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\">" +
            Environment.NewLine +
            "HtmlEncode[[propValue]]</textarea>",
            HtmlContentUtilities.HtmlContentToString(textAreaForResult));
    }

    [Fact]
    public void TextArea_UsesSpecifiedRowsAndColumns()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var textAreaResult = helper.TextArea("Property1", value: "myvalue", rows: 1, columns: 2, htmlAttributes: new { attr = "value" });

        // Assert
        Assert.Equal(
            "<textarea attr=\"HtmlEncode[[value]]\" cols=\"HtmlEncode[[2]]\" id=\"HtmlEncode[[Property1]]\" " +
            "name=\"HtmlEncode[[Property1]]\" rows=\"HtmlEncode[[1]]\">" + Environment.NewLine +
            "HtmlEncode[[myvalue]]</textarea>",
            HtmlContentUtilities.HtmlContentToString(textAreaResult));
    }

    [Fact]
    public void TextAreaFor_UsesSpecifiedRowsAndColumns()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var textAreaForResult = helper.TextAreaFor(m => m.Property1, rows: 1, columns: 2, htmlAttributes: new { attr = "value" });

        // Assert
        Assert.Equal(
            "<textarea attr=\"HtmlEncode[[value]]\" cols=\"HtmlEncode[[2]]\" id=\"HtmlEncode[[Property1]]\" " +
            "name=\"HtmlEncode[[Property1]]\" rows=\"HtmlEncode[[1]]\">" + Environment.NewLine +
            "</textarea>",
            HtmlContentUtilities.HtmlContentToString(textAreaForResult));
    }

    private class TestModel
    {
        public string Property1 { get; set; }
    }
}
