// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Test the <see cref="IHtmlHelper.Value" /> and <see cref="IHtmlHelper{TModel}.ValueFor"/> methods.
/// </summary>
public class HtmlHelperValueTest
{
    // Value

    [Fact]
    public void ValueNotInTemplate_GetsValueFromViewData()
    {
        // Arrange
        var helper = GetHtmlHelper();

        // Act
        var html = helper.Value("StringProperty", format: null);

        // Assert
        Assert.Equal("ViewDataValue", html);
    }

    [Fact]
    public void ValueInTemplate_GetsValueFromPrefixedViewDataEntry()
    {
        // Arrange
        var helper = GetHtmlHelper();
        helper.ViewData["Prefix.StringProperty"] = "PrefixedViewDataValue";
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

        // Act
        var html = helper.Value("StringProperty", format: null);

        // Assert
        Assert.Equal("PrefixedViewDataValue", html);
    }

    [Fact]
    public void ValueNotInTemplate_GetsValueFromPropertyOfViewDataEntry()
    {
        // Arrange
        var helper = GetHtmlHelper();
        helper.ViewData["Prefix"] = new { StringProperty = "ContainedViewDataValue" };

        // Act
        var html = helper.Value("Prefix.StringProperty", format: null);

        // Assert
        Assert.Equal("ContainedViewDataValue", html);
    }

    [Fact]
    public void ValueInTemplate_GetsValueFromPropertyOfViewDataEntry()
    {
        // Arrange
        var helper = GetHtmlHelper();
        helper.ViewData["Prefix"] = new { StringProperty = "ContainedViewDataValue" };
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

        // Act
        var html = helper.Value("StringProperty", format: null);

        // Assert
        Assert.Equal("ContainedViewDataValue", html);
    }

    [Fact]
    public void ValueNotInTemplate_GetsValueFromModel_IfNoViewDataEntry()
    {
        // Arrange
        var helper = GetHtmlHelper();
        helper.ViewData.Clear();

        // Act
        var html = helper.Value("StringProperty", format: null);

        // Assert
        Assert.Equal("ModelStringPropertyValue", html);
    }

    [Fact]
    public void ValueInTemplate_GetsValueFromModel_IfNoViewDataEntry()
    {
        // Arrange
        var helper = GetHtmlHelper();
        helper.ViewData.Clear();
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

        // Act
        var html = helper.Value("StringProperty", format: null);

        // Assert
        Assert.Equal("ModelStringPropertyValue", html);
    }

    [Fact]
    public void ValueNotInTemplate_GetsValueFromViewData_EvenIfNull()
    {
        // Arrange
        var helper = GetHtmlHelper();
        helper.ViewData["StringProperty"] = null;

        // Act
        var html = helper.Value("StringProperty", format: null);

        // Assert
        Assert.Empty(html);
    }

    [Fact]
    public void ValueInTemplate_GetsValueFromViewData_EvenIfNull()
    {
        // Arrange
        var helper = GetHtmlHelper();
        helper.ViewData["Prefix.StringProperty"] = null;
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

        // Act
        var html = helper.Value("StringProperty", format: null);

        // Assert
        Assert.Empty(html);
    }

    // ValueFor

    [Fact]
    public void ValueForGetsExpressionValueFromViewDataModel()
    {
        // Arrange
        var helper = GetHtmlHelper();

        // Act
        var html = helper.ValueFor(m => m.StringProperty, format: null);

        // Assert
        Assert.Equal("ModelStringPropertyValue", html);
    }

    // All Value Helpers including ValueForModel

    [Fact]
    public void ValueHelpersWithErrorsGetValueFromModelState()
    {
        // Arrange
        var model = new TestModel()
        {
            StringProperty = "ModelStringPropertyValue",
            ObjectProperty = "ModelObjectPropertyValue",
        };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper<TestModel>(model);
        var viewData = helper.ViewData;
        viewData["StringProperty"] = "ViewDataValue";
        viewData.TemplateInfo.HtmlFieldPrefix = "FieldPrefix";

        viewData.ModelState.SetModelValue(
            "FieldPrefix.StringProperty",
            "StringPropertyRawValue",
            "StringPropertyAttemptedValue");

        viewData.ModelState.SetModelValue(
            "FieldPrefix",
            "ModelRawValue",
            "ModelAttemptedValue");

        // Act & Assert
        Assert.Equal("StringPropertyRawValue", helper.Value("StringProperty", format: null));
        Assert.Equal("StringPropertyRawValue", helper.ValueFor(m => m.StringProperty, format: null));
        Assert.Equal("ModelRawValue", helper.ValueForModel(format: null));
    }

    [Fact]
    [ReplaceCulture]
    public void ValueHelpersWithEmptyNameConvertModelValueUsingCurrentCulture()
    {
        // Arrange
        var helper = GetHtmlHelper();
        var expectedModelValue =
            "{ StringProperty = ModelStringPropertyValue, ObjectProperty = 01/01/1900 00:00:00 }";

        // Act & Assert
        Assert.Equal(expectedModelValue, helper.Value(expression: string.Empty, format: null));
        Assert.Equal(expectedModelValue, helper.Value(expression: null, format: null)); // null is another alias for current model
        Assert.Equal(expectedModelValue, helper.ValueFor(m => m, format: null));
        Assert.Equal(expectedModelValue, helper.ValueForModel(format: null));
    }

    [Fact]
    [ReplaceCulture]
    public void ValueHelpersFormatValue()
    {
        // Arrange
        var helper = GetHtmlHelper();
        var expectedModelValue =
            "-{ StringProperty = ModelStringPropertyValue, ObjectProperty = 01/01/1900 00:00:00 }-";
        var expectedObjectPropertyValue = "-01/01/1900 00:00:00-";

        // Act & Assert
        Assert.Equal(expectedModelValue, helper.ValueForModel("-{0}-"));
        Assert.Equal(expectedObjectPropertyValue, helper.Value("ObjectProperty", "-{0}-"));
        Assert.Equal(expectedObjectPropertyValue, helper.ValueFor(m => m.ObjectProperty, "-{0}-"));
    }

    [Fact]
    public void ValueHelpersDoNotEncodeValue()
    {
        // Arrange
        var model = new TestModel { StringProperty = "ModelStringPropertyValue <\"\">" };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper<TestModel>(model);
        var viewData = helper.ViewData;
        viewData["StringProperty"] = "ViewDataValue <\"\">";

        viewData.ModelState.SetModelValue(
            "ObjectProperty",
            "ObjectPropertyRawValue <\"\">",
            "ObjectPropertyAttemptedValue <\"\">");

        // Act & Assert
        Assert.Equal(
            "<{ StringProperty = ModelStringPropertyValue <\"\">, ObjectProperty = (null) }>",
            helper.ValueForModel("<{0}>"));
        Assert.Equal("<ViewDataValue <\"\">>", helper.Value("StringProperty", "<{0}>"));
        Assert.Equal("<ModelStringPropertyValue <\"\">>", helper.ValueFor(m => m.StringProperty, "<{0}>"));
        Assert.Equal("ObjectPropertyRawValue <\"\">", helper.ValueFor(m => m.ObjectProperty, format: null));
    }

    private sealed class TestModel
    {
        public string StringProperty { get; set; }
        public object ObjectProperty { get; set; }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ StringProperty = {0}, ObjectProperty = {1} }}",
                StringProperty ?? "(null)",
                ObjectProperty ?? "(null)");
        }
    }

    private static HtmlHelper<TestModel> GetHtmlHelper()
    {
        var model = new TestModel
        {
            StringProperty = "ModelStringPropertyValue",
            ObjectProperty = new DateTime(1900, 1, 1, 0, 0, 0),
        };
        var helper = DefaultTemplatesUtilities.GetHtmlHelper<TestModel>(model);
        helper.ViewData["StringProperty"] = "ViewDataValue";

        return helper;
    }
}
