// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Core;

/// <summary>
/// Test the ValidationMessage extensions in <see cref="HtmlHelperValidationExtensions" /> class.
/// </summary>
public class HtmlHelperValidationMessageExtensionsTest
{
    [Fact]
    public void ValidationMessage_UsesSpecifiedExpression()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var validationMessageResult = helper.ValidationMessage("Property1");

        // Assert
        Assert.Equal(
            "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[true]]\"></span>",
            HtmlContentUtilities.HtmlContentToString(validationMessageResult));
    }

    [Fact]
    public void ValidationMessageFor_UsesSpecifiedExpression()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var validationMessageForResult = helper.ValidationMessageFor(m => m.Property1);

        // Assert
        Assert.Equal(
            "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[true]]\"></span>",
            HtmlContentUtilities.HtmlContentToString(validationMessageForResult));
    }

    [Fact]
    public void ValidationMessage_UsesSpecifiedMessage()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var validationMessageResult = helper.ValidationMessage("Property1", message: "Custom Message");

        // Assert
        Assert.Equal(
            "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[false]]\">HtmlEncode[[Custom Message]]</span>",
            HtmlContentUtilities.HtmlContentToString(validationMessageResult));
    }

    [Fact]
    public void ValidationMessageFor_UsesSpecifiedMessage()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var validationMessageForResult = helper.ValidationMessageFor(m => m.Property1, message: "Custom Message");

        // Assert
        Assert.Equal(
            "<span class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[false]]\">HtmlEncode[[Custom Message]]</span>",
            HtmlContentUtilities.HtmlContentToString(validationMessageForResult));
    }

    [Fact]
    public void ValidationMessage_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var validationMessageResult = helper.ValidationMessage("Property1", message: "Custom Message", htmlAttributes: new { attr = "value" });

        // Assert
        Assert.Equal(
            "<span attr=\"HtmlEncode[[value]]\" class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[false]]\">HtmlEncode[[Custom Message]]</span>",
            HtmlContentUtilities.HtmlContentToString(validationMessageResult));
    }

    [Fact]
    public void ValidationMessageFor_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var validationMessageForResult = helper.ValidationMessageFor(m => m.Property1, message: "Custom Message", htmlAttributes: new { attr = "value" });

        // Assert
        Assert.Equal(
            "<span attr=\"HtmlEncode[[value]]\" class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[false]]\">HtmlEncode[[Custom Message]]</span>",
            HtmlContentUtilities.HtmlContentToString(validationMessageForResult));
    }

    [Fact]
    public void ValidationMessage_UsesSpecifiedTag()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var validationMessageResult = helper.ValidationMessage("Property1", message: "Custom Message", tag: "div");

        // Assert
        Assert.Equal(
            "<div class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[false]]\">HtmlEncode[[Custom Message]]</div>",
            HtmlContentUtilities.HtmlContentToString(validationMessageResult));
    }

    [Fact]
    public void ValidationMessageFor_UsesSpecifiedTag()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var validationMessageForResult = helper.ValidationMessageFor(m => m.Property1, message: "Custom Message", tag: "div");

        // Assert
        Assert.Equal(
            "<div class=\"HtmlEncode[[field-validation-valid]]\" data-valmsg-for=\"HtmlEncode[[Property1]]\" data-valmsg-replace=\"HtmlEncode[[false]]\">HtmlEncode[[Custom Message]]</div>",
            HtmlContentUtilities.HtmlContentToString(validationMessageForResult));
    }
}
