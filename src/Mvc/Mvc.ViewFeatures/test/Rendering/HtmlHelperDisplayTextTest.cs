// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Test the <see cref="IHtmlHelper.DisplayText"/> and
/// <see cref="IHtmlHelper{TModel}.DisplayTextFor{TValue}"/> methods.
/// </summary>
public class HtmlHelperDisplayTextTest
{
    [Fact]
    public void DisplayText_ReturnsEmpty_IfValueNull()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: null);

        // Act
        var result = helper.DisplayText(expression: string.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DisplayTextFor_ReturnsEmpty_IfValueNull()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: null);

        // Act
        var result = helper.DisplayTextFor(m => m);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DisplayText_ReturnsNullDisplayText_IfSetAndValueNull()
    {
        // Arrange
        var provider = new TestModelMetadataProvider();
        provider.ForType<OverriddenToStringModel>().DisplayDetails(dd =>
        {
            dd.NullDisplayText = "Null display Text";
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: null, provider: provider);

        // Act
        var result = helper.DisplayText(expression: string.Empty);

        // Assert
        Assert.Equal("Null display Text", result);
    }

    [Fact]
    public void DisplayTextFor_ReturnsNullDisplayText_IfSetAndValueNull()
    {
        // Arrange
        var provider = new TestModelMetadataProvider();
        provider.ForType<OverriddenToStringModel>().DisplayDetails(dd =>
        {
            dd.NullDisplayText = "Null display Text";
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: null, provider: provider);

        // Act
        var result = helper.DisplayTextFor(m => m);

        // Assert
        Assert.Equal("Null display Text", result);
    }

    [Fact]
    public void DisplayText_ReturnsValue_IfNameEmpty()
    {
        // Arrange
        var model = new OverriddenToStringModel("Model value");
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.DisplayText(expression: string.Empty);
        var nullResult = helper.DisplayText(expression: null);    // null is another alias for current model

        // Assert
        Assert.Equal("Model value", result);
        Assert.Equal("Model value", nullResult);
    }

    [Fact]
    public void DisplayText_ReturnsEmpty_IfNameNotFound()
    {
        // Arrange
        var model = new OverriddenToStringModel("Model value");
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.DisplayText("NonExistentProperty");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DisplayTextFor_ReturnsValue_IfIdentityExpression()
    {
        // Arrange
        var model = new OverriddenToStringModel("Model value");
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.DisplayTextFor(m => m);

        // Assert
        Assert.Equal("Model value", result);
    }

    [Fact]
    public void DisplayText_ReturnsSimpleDisplayText_IfSetAndValueNonNull()
    {
        // Arrange
        var model = new OverriddenToStringModel("Ignored text")
        {
            SimpleDisplay = "Simple display text",
        };

        var provider = new TestModelMetadataProvider();
        provider.ForType<OverriddenToStringModel>().DisplayDetails(dd =>
        {
            dd.SimpleDisplayProperty = nameof(OverriddenToStringModel.SimpleDisplay);
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: model, provider: provider);

        // Act
        var result = helper.DisplayText(expression: string.Empty);

        // Assert
        Assert.Equal("Simple display text", result);
    }

    [Fact]
    public void DisplayTextFor_ReturnsSimpleDisplayText_IfSetAndValueNonNull()
    {
        // Arrange
        var model = new OverriddenToStringModel("Ignored text")
        {
            SimpleDisplay = "Simple display text",
        };

        var provider = new TestModelMetadataProvider();
        provider.ForType<OverriddenToStringModel>().DisplayDetails(dd =>
        {
            dd.SimpleDisplayProperty = nameof(OverriddenToStringModel.SimpleDisplay);
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: model, provider: provider);

        // Act
        var result = helper.DisplayTextFor(m => m);

        // Assert
        Assert.Equal("Simple display text", result);
    }

    [Fact]
    public void DisplayText_ReturnsPropertyValue_IfNameFound()
    {
        // Arrange
        var model = new OverriddenToStringModel("Ignored text")
        {
            Name = "Property value",
            SimpleDisplay = "Simple display text",
        };

        var provider = new TestModelMetadataProvider();
        provider.ForType<OverriddenToStringModel>().DisplayDetails(dd =>
        {
            dd.SimpleDisplayProperty = nameof(OverriddenToStringModel.SimpleDisplay);
        });

        var helper = DefaultTemplatesUtilities.GetHtmlHelper<OverriddenToStringModel>(model: model, provider: provider);

        // Act
        var result = helper.DisplayText("Name");

        // Assert
        Assert.Equal("Property value", result);
    }

    [Fact]
    public void DisplayTextFor_ReturnsPropertyValue_IfPropertyExpression()
    {
        // Arrange
        var model = new OverriddenToStringModel("ignored text")
        {
            Name = "Property value",
        };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.DisplayTextFor(m => m.Name);

        // Assert
        Assert.Equal("Property value", result);
    }

    [Fact]
    public void DisplayText_ReturnsViewDataEntry()
    {
        // Arrange
        var model = new OverriddenToStringModel("Model value")
        {
            Name = "Property value",
        };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        helper.ViewData["Name"] = "View data dictionary value";

        // Act
        var result = helper.DisplayText("Name");

        // Assert
        Assert.Equal("View data dictionary value", result);
    }

    [Fact]
    public void DisplayTextFor_IgnoresViewDataEntry()
    {
        // Arrange
        var model = new OverriddenToStringModel("Model value")
        {
            Name = "Property value",
        };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        helper.ViewData["Name"] = "View data dictionary value";

        // Act
        var result = helper.DisplayTextFor(m => m.Name);

        // Assert
        Assert.Equal("Property value", result);
    }

    [Fact]
    public void DisplayText_IgnoresModelStateEntry_ReturnsViewDataEntry()
    {
        // Arrange
        var model = new OverriddenToStringModel("Model value")
        {
            Name = "Property value",
        };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        var viewData = helper.ViewData;
        viewData["FieldPrefix.Name"] = "View data dictionary value";
        viewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

        viewData.ModelState.SetModelValue(
            "FieldPrefix.Name",
            "Attempted name value",
            "Attempted name value");

        // Act
        var result = helper.DisplayText("Name");

        // Assert
        Assert.Equal("View data dictionary value", result);
    }

    [Fact]
    public void DisplayTextFor_IgnoresModelStateEntry()
    {
        // Arrange
        var model = new OverriddenToStringModel("Model value")
        {
            Name = "Property value",
        };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        var viewData = helper.ViewData;
        viewData["Name"] = "View data dictionary value";
        viewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

        viewData.ModelState.SetModelValue(
            "FieldPrefix.Name",
            "Attempted name value",
            "Attempted name value");

        // Act
        var result = helper.DisplayTextFor(m => m.Name);

        // Assert
        Assert.Equal("Property value", result);
    }

    [Fact]
    public void DisplayTextFor_EnumDisplayAttribute_WhenPresent()
    {
        // Arrange
        var model = EnumWithDisplayAttribute.Value1;
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.DisplayText(expression: string.Empty);
        var forResult = helper.DisplayTextFor(m => m);

        // Assert
        Assert.Equal("Value One", result);
        Assert.Equal("Value One", forResult);
    }

    [Fact]
    public void DisplayTextFor_EnumDisplayAttribute_WhenPresentOnProperty()
    {
        // Arrange
        var model = new EnumWithDisplayAttributeContainer { EnumValue = EnumWithDisplayAttribute.Value1 };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.DisplayText(expression: nameof(EnumWithDisplayAttributeContainer.EnumValue));
        var forResult = helper.DisplayTextFor(m => m.EnumValue);

        // Assert
        Assert.Equal("Value One", result);
        Assert.Equal("Value One", forResult);
    }

    [Fact]
    public void DisplayTextFor_EnumDisplayAttribute_WhenNotPresent()
    {
        // Arrange
        var model = EnumWithoutDisplayAttribute.Value1;
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.DisplayText(expression: null);
        var forResult = helper.DisplayTextFor(m => m);

        // Assert
        Assert.Equal("Value1", result);
        Assert.Equal("Value1", forResult);
    }

    // ModelMetadata.SimpleDisplayText returns ToString() result if that method has been overridden.
    private sealed class OverriddenToStringModel
    {
        private readonly string _simpleDisplayText;

        public OverriddenToStringModel(string simpleDisplayText)
        {
            _simpleDisplayText = simpleDisplayText;
        }

        public string SimpleDisplay { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return _simpleDisplayText;
        }
    }

    private enum EnumWithDisplayAttribute
    {
        [Display(Name = "Value One")]
        Value1
    }

    private enum EnumWithoutDisplayAttribute
    {
        Value1
    }

    private class EnumWithDisplayAttributeContainer
    {
        public EnumWithDisplayAttribute EnumValue { get; set; }
    }
}
