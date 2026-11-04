// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.Core;

/// <summary>
/// Test the RadioButton extensions in <see cref="HtmlHelperInputExtensions" /> class.
/// </summary>
public class HtmlHelperRadioButtonExtensionsTest
{
    [Fact]
    public void RadioButton_UsesSpecifiedExpression()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var radioButtonResult = helper.RadioButton("Property1", value: "myvalue");

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonResult));
    }

    [Fact]
    public void RadioButtonFor_UsesSpecifiedExpression()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var radioButtonForResult = helper.RadioButtonFor(m => m.Property1, value: "myvalue");

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonForResult));
    }

    [Theory]
    [InlineData("MyValue")]
    [InlineData("myvalue")]
    public void RadioButton_CheckedWhenValueMatchesSpecifiedExpression(string value)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = value };

        // Act
        var radioButtonResult = helper.RadioButton("Property1", value: "myvalue");

        // Assert
        Assert.Equal(
            "<input checked=\"HtmlEncode[[checked]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonResult));
    }

    [Theory]
    [InlineData("MyValue")]
    [InlineData("myvalue")]
    public void RadioButtonFor_CheckedWhenValueMatchesSpecifiedExpression(string value)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = value };

        // Act
        var radioButtonForResult = helper.RadioButtonFor(m => m.Property1, value: "myvalue");

        // Assert
        Assert.Equal(
            "<input checked=\"HtmlEncode[[checked]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonForResult));
    }

    [Fact]
    public void RadioButtonHelpers_UsesSpecifiedIsChecked()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();

        // Act
        var radioButtonResult = helper.RadioButton("Property1", value: "myvalue", isChecked: true);

        // Assert
        Assert.Equal(
            "<input checked=\"HtmlEncode[[checked]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonResult));
    }

    [Fact]
    public void RadioButtonHelpers_UsesSpecifiedIsCheckedRegardlessOfValue()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property2 = true };

        // Act
        var radioButtonResult = helper.RadioButton("Property2", value: "myvalue", isChecked: false);

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property2]]\" name=\"HtmlEncode[[Property2]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonResult));
    }

    [Fact]
    public void RadioButton_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        var htmlAttributes = new
        {
            attr = "value",
            name = "-expression-", // overridden
        };

        // Act
        var radioButtonResult = helper.RadioButton("Property1", "myvalue", htmlAttributes);

        // Assert
        Assert.Equal(
            "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" " +
            "name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonResult));
    }

    [Fact]
    public void RadioButtonFor_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        var htmlAttributes = new
        {
            attr = "value",
            name = "-expression-", // overridden
        };

        // Act
        var radioButtonForResult = helper.RadioButtonFor(m => m.Property1, "myvalue", htmlAttributes);

        // Assert
        Assert.Equal(
            "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" " +
            "name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonForResult));
    }

    [Fact]
    public void RadioButtonFor_Throws_IfFullNameEmpty()
    {
        // Arrange
        var expectedMessage = "The name of an HTML field cannot be null or empty. Instead use methods " +
            "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
            "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

        var helper = DefaultTemplatesUtilities.GetHtmlHelper("anotherValue");
        var htmlAttributes = new
        {
            attr = "value",
        };

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => helper.RadioButtonFor(m => m, "myvalue", htmlAttributes),
            paramName: "expression",
            exceptionMessage: expectedMessage);
    }

    [Fact]
    public void RadioButtonFor_DoesNotThrow_IfFullNameEmpty_WithNameAttribute()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper("anotherValue");
        var htmlAttributes = new
        {
            attr = "value",
            name = "-expression-",
        };

        // Act
        var radioButtonForResult = helper.RadioButtonFor(m => m, "myvalue", htmlAttributes);

        // Assert
        Assert.Equal(
            "<input attr=\"HtmlEncode[[value]]\" " +
            "name=\"HtmlEncode[[-expression-]]\" type=\"HtmlEncode[[radio]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(radioButtonForResult));
    }

    private class TestModel
    {
        public string Property1 { get; set; }

        public bool Property2 { get; set; }
    }
}
