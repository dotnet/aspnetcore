// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class HtmlHelperPasswordTest
{
    public static TheoryData<object> HtmlAttributeData
    {
        get
        {
            return new TheoryData<object>
                {
                    new Dictionary<string, object>
                    {
                        { "name", "-expression-" }, // overridden
                        { "test-key", "test-value" },
                        { "value", "attribute-value" },
                    },
                    new
                    {
                        name = "-expression-", // overridden
                        test_key = "test-value",
                        value = "attribute-value",
                    },
                };
        }
    }

    public static TheoryData<ViewDataDictionary<PasswordModel>, object> PasswordWithViewDataAndAttributesData
    {
        get
        {
            var nullModelViewData = GetViewDataWithNullModelAndNonEmptyViewData();
            var viewData = GetViewDataWithModelStateAndModelAndViewDataValues();
            viewData.Model.Property1 = "does-not-get-used";

            var data = new TheoryData<ViewDataDictionary<PasswordModel>, object>();
            foreach (var items in HtmlAttributeData)
            {
                data.Add(viewData, items[0]);
                data.Add(nullModelViewData, items[0]);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(PasswordWithViewDataAndAttributesData))]
    public void Password_UsesAttributeValueWhenValueArgumentIsNull(
        ViewDataDictionary<PasswordModel> viewData,
        object attributes)
    {
        // Arrange
        var expected = @"<input id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" " +
            @"test-key=""HtmlEncode[[test-value]]"" type=""HtmlEncode[[password]]"" " +
            @"value=""HtmlEncode[[attribute-value]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);

        // Act
        var result = helper.Password("Property1", value: null, htmlAttributes: attributes);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Theory]
    [MemberData(nameof(PasswordWithViewDataAndAttributesData))]
    public void Password_UsesExplicitValue_IfSpecified(
        ViewDataDictionary<PasswordModel> viewData,
        object attributes)
    {
        // Arrange
        var expected = @"<input id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" " +
            @"test-key=""HtmlEncode[[test-value]]"" type=""HtmlEncode[[password]]"" " +
            @"value=""HtmlEncode[[explicit-value]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);

        // Act
        var result = helper.Password("Property1", "explicit-value", attributes);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordWithPrefix_GeneratesExpectedValue()
    {
        // Arrange
        var expected = @"<input id=""HtmlEncode[[MyPrefix_Property1]]"" name=""HtmlEncode[[MyPrefix.Property1]]"" type=""HtmlEncode[[password]]"" " +
                       @"value=""HtmlEncode[[explicit-value]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
        helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

        // Act
        var result = helper.Password("Property1", "explicit-value", htmlAttributes: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordWithPrefix_UsesIdDotReplacementToken()
    {
        // Arrange
        var expected = @"<input id=""HtmlEncode[[MyPrefix$Property1]]"" name=""HtmlEncode[[MyPrefix.Property1]]"" type=""HtmlEncode[[password]]"" " +
                       @"value=""HtmlEncode[[explicit-value]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(
            GetViewDataWithModelStateAndModelAndViewDataValues(),
            idAttributeDotReplacement: "$");
        helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

        // Act
        var result = helper.Password("Property1", "explicit-value", htmlAttributes: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordWithPrefixAndEmptyName_GeneratesExpectedValue()
    {
        // Arrange
        var expected = @"<input id=""HtmlEncode[[MyPrefix]]"" name=""HtmlEncode[[MyPrefix]]"" type=""HtmlEncode[[password]]"" value=""HtmlEncode[[explicit-value]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
        helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";
        var name = string.Empty;

        // Act
        var result = helper.Password(name, "explicit-value", htmlAttributes: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordWithEmptyNameAndPrefixThrows()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper("model-value");
        var expression = string.Empty;
        var expectedMessage = "The name of an HTML field cannot be null or empty. Instead use methods " +
            "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
            "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

        // Act and Assert
        ExceptionAssert.ThrowsArgument(
            () => helper.Password(expression, value: null, htmlAttributes: null),
            "expression",
            expectedMessage);
    }

    [Fact]
    public void PasswordWithEmptyNameAndPrefix_DoesNotThrow_WithNameAttribute()
    {
        // Arrange
        var expected = @"<input name=""HtmlEncode[[-expression-]]"" type=""HtmlEncode[[password]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper("model-value");
        var expression = string.Empty;
        var htmlAttributes = new { name = "-expression-" };

        // Act
        var result = helper.Password(expression, value: null, htmlAttributes: htmlAttributes);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void Password_UsesModelStateErrors_ButDoesNotUseModelOrViewDataOrModelStateForValueAttribute()
    {
        // Arrange
        var expected = @"<input class=""HtmlEncode[[some-class input-validation-error]]"" id=""HtmlEncode[[Property1]]""" +
                       @" name=""HtmlEncode[[Property1]]"" test-key=""HtmlEncode[[test-value]]"" type=""HtmlEncode[[password]]"" />";
        var vdd = GetViewDataWithErrors();
        vdd.Model.Property1 = "property-value";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(vdd);
        var attributes = new Dictionary<string, object>
            {
                { "test-key", "test-value" },
                { "class", "some-class"}
            };

        // Act
        var result = helper.Password("Property1", value: null, htmlAttributes: attributes);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordGeneratesUnobtrusiveValidation()
    {
        // Arrange
        var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property2");
        var expected =
            $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
            @"id=""HtmlEncode[[Property2]]"" name=""HtmlEncode[[Property2]]"" type=""HtmlEncode[[password]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());

        // Act
        var result = helper.Password("Property2", value: null, htmlAttributes: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    public static IEnumerable<object[]> PasswordWithComplexExpressions_UsesIdDotSeparatorData
    {
        get
        {
            yield return new object[]
            {
                    "Property4.Property5",
                    @"<input data-test=""HtmlEncode[[val]]"" id=""HtmlEncode[[Property4$$Property5]]"" name=""HtmlEncode[[Property4.Property5]]"" " +
                    @"type=""HtmlEncode[[password]]"" />",
            };

            yield return new object[]
           {
                    "Property4.Property6[0]",
                    @"<input data-test=""HtmlEncode[[val]]"" id=""HtmlEncode[[Property4$$Property6$$0$$]]"" name=""HtmlEncode[[Property4.Property6[0]]]"" " +
                    @"type=""HtmlEncode[[password]]"" />",
           };
        }
    }

    [Theory]
    [MemberData(nameof(PasswordWithComplexExpressions_UsesIdDotSeparatorData))]
    public void PasswordWithComplexExpressions_UsesIdDotSeparator(string expression, string expected)
    {
        // Arrange
        var viewData = GetViewDataWithModelStateAndModelAndViewDataValues();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData, idAttributeDotReplacement: "$$");
        var attributes = new Dictionary<string, object> { { "data-test", "val" } };

        // Act
        var result = helper.Password(expression, value: null, htmlAttributes: attributes);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Theory]
    [MemberData(nameof(HtmlAttributeData))]
    public void PasswordForWithAttributes_GeneratesExpectedValue(object htmlAttributes)
    {
        // Arrange
        var expected = @"<input id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" " +
            @"test-key=""HtmlEncode[[test-value]]"" type=""HtmlEncode[[password]]"" " +
            @"value=""HtmlEncode[[attribute-value]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
        helper.ViewData.Model.Property1 = "test";

        // Act
        var result = helper.PasswordFor(m => m.Property1, htmlAttributes);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordForWithPrefix_GeneratesExpectedValue()
    {
        // Arrange
        var expected = @"<input id=""HtmlEncode[[MyPrefix_Property1]]"" name=""HtmlEncode[[MyPrefix.Property1]]"" type=""HtmlEncode[[password]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
        helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

        // Act
        var result = helper.PasswordFor(m => m.Property1, htmlAttributes: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordFor_UsesModelStateErrors_ButDoesNotUseModelOrViewDataOrModelStateForValueAttribute()
    {
        // Arrange
        var expected = @"<input baz=""HtmlEncode[[BazValue]]"" class=""HtmlEncode[[some-class input-validation-error]]"" id=""HtmlEncode[[Property1]]"" " +
                       @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[password]]"" />";
        var vdd = GetViewDataWithErrors();
        vdd.Model.Property1 = "prop1-value";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(vdd);
        var attributes = new Dictionary<string, object>
            {
                { "baz", "BazValue" },
                { "class", "some-class"}
            };

        // Act
        var result = helper.PasswordFor(m => m.Property1, attributes);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void PasswordFor_GeneratesUnobtrusiveValidationAttributes()
    {
        // Arrange
        var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property2");
        var expected =
            $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
            @"id=""HtmlEncode[[Property2]]"" name=""HtmlEncode[[Property2]]"" type=""HtmlEncode[[password]]"" />";
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithErrors());

        // Act
        var result = helper.PasswordFor(m => m.Property2, htmlAttributes: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    public static TheoryData PasswordFor_WithComplexExpressionsData
    {
        get
        {
            return new TheoryData<Expression<Func<PasswordModel, string>>, string>
                {
                    {
                        model => model.Property3["key"],
                        @"<input data-val=""HtmlEncode[[true]]"" id=""HtmlEncode[[pre_Property3_key_]]"" name=""HtmlEncode[[pre.Property3[key]]]"" " +
                        @"type=""HtmlEncode[[password]]"" value=""HtmlEncode[[attr-value]]"" />"
                    },
                    {
                        model => model.Property4.Property5,
                        @"<input data-val=""HtmlEncode[[true]]"" id=""HtmlEncode[[pre_Property4_Property5]]"" name=""HtmlEncode[[pre.Property4.Property5]]"" " +
                        @"type=""HtmlEncode[[password]]"" value=""HtmlEncode[[attr-value]]"" />"
                    },
                    {
                        model => model.Property4.Property6[0],
                        @"<input data-val=""HtmlEncode[[true]]"" id=""HtmlEncode[[pre_Property4_Property6_0_]]"" " +
                        @"name=""HtmlEncode[[pre.Property4.Property6[0]]]"" type=""HtmlEncode[[password]]"" value=""HtmlEncode[[attr-value]]"" />"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(PasswordFor_WithComplexExpressionsData))]
    public void PasswordFor_WithComplexExpressionsAndFieldPrefix_UsesAttributeValueIfSpecified(
        Expression<Func<PasswordModel, string>> expression,
        string expected)
    {
        // Arrange
        var viewData = GetViewDataWithModelStateAndModelAndViewDataValues();
        viewData.ModelState.SetModelValue("pre.Property3[key]", "Property3Val", "Property3Val");
        viewData.ModelState.SetModelValue("pre.Property4.Property5", "Property5Val", "Property5Val");
        viewData.ModelState.SetModelValue("pre.Property4.Property6[0]", "Property6Val", "Property6Val");
        viewData["pre.Property3[key]"] = "vdd-value1";
        viewData["pre.Property4.Property5"] = "vdd-value2";
        viewData["pre.Property4.Property6[0]"] = "vdd-value3";
        viewData.Model.Property3["key"] = "prop-value1";
        viewData.Model.Property4.Property5 = "prop-value2";
        viewData.Model.Property4.Property6.Add("prop-value3");

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);
        viewData.TemplateInfo.HtmlFieldPrefix = "pre";
        var attributes = new { data_val = "true", value = "attr-value" };

        // Act
        var result = helper.PasswordFor(expression, attributes);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void Password_UsesSpecifiedExpressionForNames_IgnoresExpressionValue()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var passwordResult = helper.Password("Property1");

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[password]]\" />",
            HtmlContentUtilities.HtmlContentToString(passwordResult));
    }

    [Fact]
    public void PasswordFor_UsesSpecifiedExpressionForNames_IgnoresExpressionValue()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var passwordForResult = helper.PasswordFor(m => m.Property1);

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[password]]\" />",
            HtmlContentUtilities.HtmlContentToString(passwordForResult));
    }

    [Fact]
    public void Password_UsesSpecifiedValue()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property1 = "propValue" };

        // Act
        var passwordResult = helper.Password("Property1", value: "myvalue");

        // Assert
        Assert.Equal(
            "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[password]]\" value=\"HtmlEncode[[myvalue]]\" />",
            HtmlContentUtilities.HtmlContentToString(passwordResult));
    }

    [Fact]
    public void PasswordFor_GeneratesPlaceholderAttribute_WhenDisplayAttributePromptIsSet()
    {
        // Arrange
        var expected = @"<input id=""HtmlEncode[[Property7]]"" name=""HtmlEncode[[Property7]]"" placeholder=""HtmlEncode[[placeholder]]"" type=""HtmlEncode[[password]]"" />";
        var model = new PasswordModel();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);

        // Act
        var result = helper.PasswordFor(m => m.Property7, htmlAttributes: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    private static ViewDataDictionary<PasswordModel> GetViewDataWithNullModelAndNonEmptyViewData()
    {
        return new ViewDataDictionary<PasswordModel>(new EmptyModelMetadataProvider())
        {
            ["Property1"] = "view-data-val",
        };
    }

    private static ViewDataDictionary<PasswordModel> GetViewDataWithModelStateAndModelAndViewDataValues()
    {
        var viewData = GetViewDataWithNullModelAndNonEmptyViewData();
        viewData.Model = new PasswordModel();
        viewData.ModelState.SetModelValue("Property1", "ModelStateValue", "ModelStateValue");

        return viewData;
    }

    private static ViewDataDictionary<PasswordModel> GetViewDataWithErrors()
    {
        var viewData = GetViewDataWithModelStateAndModelAndViewDataValues();
        viewData.ModelState.AddModelError("Property1", "error 1");
        viewData.ModelState.AddModelError("Property1", "error 2");
        return viewData;
    }

    public static TheoryData PasswordFor_IgnoresExpressionValueForComplexExpressionsData
    {
        get
        {
            return new TheoryData<Expression<Func<PasswordModel, string>>, string>
                {
                    {
                        model => model.Property3["key"],
                        @"<input id=""HtmlEncode[[pre_Property3_key_]]"" name=""HtmlEncode[[pre.Property3[key]]]"" " +
                        @"type=""HtmlEncode[[password]]"" />"
                    },
                    {
                        model => model.Property4.Property5,
                        @"<input id=""HtmlEncode[[pre_Property4_Property5]]"" name=""HtmlEncode[[pre.Property4.Property5]]"" " +
                        @"type=""HtmlEncode[[password]]"" />"
                    },
                    {
                        model => model.Property4.Property6[0],
                        @"<input id=""HtmlEncode[[pre_Property4_Property6_0_]]"" " +
                        @"name=""HtmlEncode[[pre.Property4.Property6[0]]]"" type=""HtmlEncode[[password]]"" />"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(PasswordFor_IgnoresExpressionValueForComplexExpressionsData))]
    public void PasswordFor_ComplexExpressions_IgnoresValueFromViewDataModelStateAndModel(
        Expression<Func<PasswordModel, string>> expression,
        string expected)
    {
        // Arrange
        var model = new PasswordModel();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        helper.ViewData.TemplateInfo.HtmlFieldPrefix = "pre";

        helper.ViewData.ModelState.SetModelValue("pre.Property3[key]", "MProp3Val", "MProp3Val");
        helper.ViewData.ModelState.SetModelValue("pre.Property4.Property5", "MProp5Val", "MProp5Val");
        helper.ViewData.ModelState.SetModelValue("pre.Property4.Property6[0]", "MProp6Val", "MProp6Val");

        helper.ViewData["pre.Property3[key]"] = "VDProp3Val";
        helper.ViewData["pre.Property4.Property5"] = "VDProp5Val";
        helper.ViewData["pre.Property4.Property6"] = "VDProp6Val";

        helper.ViewData.Model.Property3["key"] = "Prop3Val";
        helper.ViewData.Model.Property4.Property5 = "Prop5Val";
        helper.ViewData.Model.Property4.Property6.Add("Prop6Val");

        // Act
        var result = helper.PasswordFor(expression);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    public class PasswordModel
    {
        public string Property1 { get; set; }

        [Required]
        public string Property2 { get; set; }

        public Dictionary<string, string> Property3 { get; } = new Dictionary<string, string>();

        public NestedClass Property4 { get; } = new NestedClass();

        [Display(Prompt = "placeholder")]
        public string Property7 { get; set; }
    }

    public class NestedClass
    {
        public string Property5 { get; set; }

        public List<string> Property6 { get; } = new List<string>();
    }

    private class TestModel
    {
        public string Property1 { get; set; }
    }
}
