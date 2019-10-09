// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class HtmlHelperCheckBoxTest
    {
        [Fact]
        public void CheckBoxOverridesCalculatedValuesWithValuesFromHtmlAttributes()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property3]]"" " +
                @"name=""HtmlEncode[[Property3]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[false]]"" /><input name=""HtmlEncode[[Property3]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act
            var html = helper.CheckBox("Property3",
                                       isChecked: null,
                                       htmlAttributes: new { @checked = "checked", value = "false" });

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxExplicitParametersOverrideDictionary_ForValueInModel()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property3]]"" " +
                @"name=""HtmlEncode[[Property3]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[false]]"" /><input name=""HtmlEncode[[Property3]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act
            var html = helper.CheckBox("Property3",
                                       isChecked: true,
                                       htmlAttributes: new { @checked = "unchecked", value = "false" });

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxExplicitParametersOverrideDictionary_ForNullModel()
        {
            // Arrange
            var expected = @"<input checked=""HtmlEncode[[checked]]"" id=""HtmlEncode[[foo]]"" name=""HtmlEncode[[foo]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[false]]"" />" +
                           @"<input name=""HtmlEncode[[foo]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var html = helper.CheckBox("foo",
                                       isChecked: true,
                                       htmlAttributes: new { @checked = "unchecked", value = "false" });

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxWithInvalidBooleanThrows()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act & Assert
            var ex = Assert.Throws<FormatException>(
                        () => helper.CheckBox("Property2", isChecked: null, htmlAttributes: null));
            Assert.Contains("Boolean", ex.Message);
        }

        [Fact]
        public void CheckBoxWithNullExpressionThrows()
        {
            // Arrange
            var expected = "The name of an HTML field cannot be null or empty. Instead use methods " +
                "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
                "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model: false);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => helper.CheckBox(null, isChecked: true, htmlAttributes: null),
                "expression",
                expected);
        }

        [Fact]
        public void CheckBoxWithNullExpression_DoesNotThrow_WithNameAttribute()
        {
            // Arrange
            var expected = @"<input class=""HtmlEncode[[some-class]]"" name=""HtmlEncode[[-expression-]]"" " +
                @"type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" /><input " +
                @"name=""HtmlEncode[[-expression-]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model: false);
            helper.ViewContext.ClientValidationEnabled = false;
            var attributes = new Dictionary<string, object>
            {
                { "class", "some-class"},
                { "name", "-expression-" },
            };

            // Act
            var html = helper.CheckBox(null, isChecked: false, htmlAttributes: attributes);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxCheckedWithOnlyName_GeneratesExpectedValue()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[true]]"" /><input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBox_WithCanRenderAtEndOfFormSet_DoesNotGenerateInlineHiddenTag()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[true]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.FormContext.CanRenderAtEndOfForm = true;

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: null);

            // Assert
            Assert.True(helper.ViewContext.FormContext.HasEndOfFormContent);
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
            var writer = new StringWriter();
            var hiddenTag = Assert.Single(helper.ViewContext.FormContext.EndOfFormContent);
            hiddenTag.WriteTo(writer, new HtmlTestEncoder());
            Assert.Equal("<input name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />",
                writer.ToString());
        }

        [Fact]
        public void CheckBox_WithHiddenInputRenderModeNone_DoesNotGenerateHiddenInput()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[true]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.None;

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: null);

            // Assert
            Assert.False(helper.ViewContext.FormContext.HasEndOfFormContent);
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBox_WithHiddenInputRenderModeInline_GeneratesHiddenInput()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[true]]"" /><input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.FormContext.CanRenderAtEndOfForm = true;
            helper.ViewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.Inline;

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBox_WithHiddenInputRenderModeEndOfForm_GeneratesHiddenInput()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[true]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.FormContext.CanRenderAtEndOfForm = true;
            helper.ViewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.EndOfForm;

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: null);

            // Assert
            Assert.True(helper.ViewContext.FormContext.HasEndOfFormContent);
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
            var writer = new StringWriter();
            var hiddenTag = Assert.Single(helper.ViewContext.FormContext.EndOfFormContent);
            hiddenTag.WriteTo(writer, new HtmlTestEncoder());
            Assert.Equal("<input name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />",
                writer.ToString());
        }

        [Fact]
        public void CheckBox_WithHiddenInputRenderModeEndOfForm_WithCanRenderAtEndOfFormNotSet_GeneratesHiddenInput()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[true]]"" /><input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.FormContext.CanRenderAtEndOfForm = false;
            helper.ViewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.EndOfForm;

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: null);

            // Assert
            Assert.False(helper.ViewContext.FormContext.HasEndOfFormContent);
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxUsesAttemptedValueFromModelState()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewData.ModelState.SetModelValue("Property1", new string[] { "false" }, "false");

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxNotInTemplate_GetsValueFromViewDataDictionary()
        {
            // Arrange
            var expected =
                @"<input checked=""HtmlEncode[[checked]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel();

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxInTemplate_GetsValueFromViewDataDictionary()
        {
            // Arrange
            var expected =
                @"<input checked=""HtmlEncode[[checked]]"" id=""HtmlEncode[[Prefix_Property1]]"" " +
                @"name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Remove(nameof(TestModel.Property1));
            helper.ViewData["Prefix.Property1"] = true;
            helper.ViewData.Model = new TestModel();
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxNotInTemplate_GetsValueFromPropertyOfViewDataEntry()
        {
            // Arrange
            var expected =
                @"<input checked=""HtmlEncode[[checked]]"" id=""HtmlEncode[[Prefix_Property1]]"" " +
                @"name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Remove(nameof(TestModel.Property1));
            helper.ViewData["Prefix"] = new TestModel { Property1 = true };
            helper.ViewData.Model = new TestModel();

            // Act
            var html = helper.CheckBox("Prefix.Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxInTemplate_GetsValueFromPropertyOfViewDataEntry()
        {
            // Arrange
            var expected =
                @"<input checked=""HtmlEncode[[checked]]"" id=""HtmlEncode[[Prefix_Property1]]"" " +
                @"name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Remove(nameof(TestModel.Property1));
            helper.ViewData["Prefix"] = new TestModel { Property1 = true };
            helper.ViewData.Model = new TestModel();
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxNotInTemplate_GetsModelValue_IfModelStateAndViewDataEmpty()
        {
            // Arrange
            var expected =
                @"<input checked=""HtmlEncode[[checked]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = true };

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxInTemplate_GetsModelValue_IfModelStateAndViewDataEmpty()
        {
            // Arrange
            var expected =
                @"<input checked=""HtmlEncode[[checked]]"" id=""HtmlEncode[[Prefix_Property1]]"" " +
                @"name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = true };
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxNotInTemplate_NotChecked_IfPropertyIsNotFound()
        {
            // Arrange
            var expected =
                @"<input id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxInTemplate_NotChecked_IfPropertyIsNotFound()
        {
            // Arrange
            var expected =
                @"<input id=""HtmlEncode[[Prefix_Property1]]"" " +
                @"name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Prefix.Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxGeneratesUnobtrusiveValidationAttributes()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Name");
            var expected =
                $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Name]]""" +
                @" name=""HtmlEncode[[Name]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Name]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetModelWithValidationViewData());

            // Act
            var html = helper.CheckBox("Name", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxReplacesUnderscoresInHtmlAttributesWithDashes()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Property1]]"" " +
                @"name=""HtmlEncode[[Property1]]"" Property1-Property3=""HtmlEncode[[Property3ObjValue]]"" " +
                @"type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" /><input " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            var htmlAttributes = new { Property1_Property3 = "Property3ObjValue" };

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: htmlAttributes);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxInTemplate_ReplaceDotsInIdByDefaultWithUnderscores()
        {
            // Arrange
            var expected = @"<input id=""HtmlEncode[[MyPrefix_Property1]]"" name=""HtmlEncode[[MyPrefix.Property1]]"" " +
                           @"Property3=""HtmlEncode[[Property3Value]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" /><input " +
                           @"name=""HtmlEncode[[MyPrefix.Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var dictionary = new RouteValueDictionary(new { Property3 = "Property3Value" });
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            var html = helper.CheckBox("Property1", isChecked: false, htmlAttributes: dictionary);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxInTemplate_ReplacesDotsInIdWithIdDotReplacement()
        {
            // Arrange
            var expected = @"<input id=""HtmlEncode[[MyPrefix!!!Property1]]"" name=""HtmlEncode[[MyPrefix.Property1]]"" " +
                           @"Property3=""HtmlEncode[[Property3Value]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" /><input " +
                           @"name=""HtmlEncode[[MyPrefix.Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var dictionary = new Dictionary<string, object> { { "Property3", "Property3Value" } };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel>(
                model: null,
                idAttributeDotReplacement: "!!!");
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            var html = helper.CheckBox("Property1", isChecked: false, htmlAttributes: dictionary);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxInTemplate_WithEmptyExpression_GeneratesExpectedValue()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected =
                $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[MyPrefix]]"" name=""HtmlEncode[[MyPrefix]]"" " +
                @"Property3=""HtmlEncode[[Property3Value]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[true]]"" /><input name=""HtmlEncode[[MyPrefix]]"" " +
                @"type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model: false);
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";
            var attributes = new Dictionary<string, object>
            {
                { "Property3", "Property3Value" },
                { "name", "-expression-" }, // overridden
            };

            // Act
            var html = helper.CheckBox(string.Empty, isChecked: false, htmlAttributes: attributes);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxWithComplexExpressionsEvaluatesValuesInViewDataDictionary()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Boolean");
            var expected = @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[ComplexProperty_Property1]]"" " +
                @"name=""HtmlEncode[[ComplexProperty." +
                @"Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" /><input name=""HtmlEncode[[ComplexProperty.Property1]]""" +
                @" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetModelWithValidationViewData());

            // Act
            var html = helper.CheckBox("ComplexProperty.Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxForWithNullContainer_TreatsBooleanAsFalse()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property1");
            var expected =
                $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var viewData = GetTestModelViewData();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);
            viewData.ModelState.SetModelValue("Property1", new string[] { "false" }, "false");

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Theory]
        [InlineData(false, "")]
        [InlineData(true, "checked=\"HtmlEncode[[checked]]\" ")]
        public void CheckBoxForWithNonNullContainer_UsesPropertyValue(bool value, string expectedChecked)
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property1");
            // Mono issue - https://github.com/aspnet/External/issues/19
            var expected =
                $@"<input {{0}}data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            expected = string.Format(expected, expectedChecked);

            var viewData = GetTestModelViewData();
            viewData.Model = new TestModel
            {
                Property1 = value,
            };

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxForOverridesCalculatedParametersWithValuesFromHtmlAttributes()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property3");
            var expected =
                @"<input checked=""HtmlEncode[[checked]]"" data-val=""HtmlEncode[[true]]"" " +
                $@"data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[Property3]]"" name=""HtmlEncode[[Property3]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[false]]"" /><input name=""HtmlEncode[[Property3]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act
            var html = helper.CheckBoxFor(m => m.Property3, new { @checked = "checked", value = "false" });

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxForGeneratesUnobtrusiveValidationAttributes()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Name");
            var expected =
                $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" id=""HtmlEncode[[Name]]""" +
                @" name=""HtmlEncode[[Name]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Name]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var viewDataDictionary = new ViewDataDictionary<ModelWithValidation>(metadataProvider)
            {
                Model = new ModelWithValidation()
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewDataDictionary);

            // Act
            var html = helper.CheckBoxFor(m => m.Name, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Theory]
        [InlineData("false", "")]
        [InlineData("true", "checked=\"HtmlEncode[[checked]]\" ")]
        public void CheckBoxFor_UsesModelStateAttemptedValue(string attemptedValue, string expectedChecked)
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property1");
            var expected =
                $@"<input {{0}}data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" />" +
                @"<input name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            expected = string.Format(expected, expectedChecked);

            var viewData = GetTestModelViewData();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);
            viewData.ModelState.SetModelValue("Property1", new string[] { attemptedValue }, attemptedValue);

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxFor_WithObjectAttribute_MapsUnderscoresInNamesToDashes()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property1");
            var expected =
                $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" " +
                @"Property1-Property3=""HtmlEncode[[Property3ObjValue]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" /><input " +
                @"name=""HtmlEncode[[Property1]]"" type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            var htmlAttributes = new { Property1_Property3 = "Property3ObjValue" };

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, htmlAttributes);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxFor_WithAttributeDictionary_GeneratesExpectedAttributes()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property1");
            var expected =
                $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[Property1]]"" name=""HtmlEncode[[Property1]]"" " +
                @"Property3=""HtmlEncode[[Property3Value]]"" type=""HtmlEncode[[checkbox]]"" " +
                @"value=""HtmlEncode[[true]]"" /><input name=""HtmlEncode[[Property1]]"" " +
                @"type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            var attributes = new Dictionary<string, object>
            {
                { "Property3", "Property3Value" },
                { "name", "-expression-" }, // overridden
            };

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, attributes);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxForInTemplate_GeneratesExpectedValue()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property1");
            var expected =
                $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[MyPrefix_Property1]]"" name=""HtmlEncode[[MyPrefix.Property1]]"" Property3=""HtmlEncode[[PropValue]]"" " +
                @"type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" /><input name=""HtmlEncode[[MyPrefix.Property1]]"" type=""HtmlEncode[[hidden]]"" " +
                @"value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";
            var attributes = new Dictionary<string, object> { { "Property3", "PropValue" } };

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, attributes);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void CheckBoxFor_WithComplexExpressions_DoesNotUseValuesFromViewDataDictionary()
        {
            // Arrange
            var requiredMessage = ValidationAttributeUtil.GetRequiredErrorMessage("Property1");
            var expected =
                $@"<input data-val=""HtmlEncode[[true]]"" data-val-required=""HtmlEncode[[{requiredMessage}]]"" " +
                @"id=""HtmlEncode[[ComplexProperty_Property1]]"" name=""HtmlEncode[[ComplexProperty." +
                @"Property1]]"" type=""HtmlEncode[[checkbox]]"" value=""HtmlEncode[[true]]"" /><input name=""HtmlEncode[[ComplexProperty.Property1]]"" " +
                @"type=""HtmlEncode[[hidden]]"" value=""HtmlEncode[[false]]"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetModelWithValidationViewData());

            // Act
            var html = helper.CheckBoxFor(m => m.ComplexProperty.Property1, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(html));
        }

        [Fact]
        public void Checkbox_UsesSpecifiedExpression()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var checkboxResult = helper.CheckBox("Property1");

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[checkbox]]\" value=\"HtmlEncode[[true]]\" />" +
                "<input name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />",
                HtmlContentUtilities.HtmlContentToString(checkboxResult));
        }

        [Fact]
        public void Checkbox_UsesSpecifiedIsChecked()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var checkboxResult = helper.CheckBox("Property1", isChecked: true);

            // Assert
            Assert.Equal(
                "<input checked=\"HtmlEncode[[checked]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[checkbox]]\" value=\"HtmlEncode[[true]]\" />" +
                "<input name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />",
                HtmlContentUtilities.HtmlContentToString(checkboxResult));
        }

        [Fact]
        public void Checkbox_UsesSpecifiedIsCheckedRegardlessOfExpressionValue()
        {
            // Arrange
            var metadataProvider = new EmptyModelMetadataProvider();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
            helper.ViewContext.ClientValidationEnabled = false;
            helper.ViewData.Model = new TestModel { Property1 = true };

            // Act
            var checkboxResult = helper.CheckBox("Property1", isChecked: false);

            // Assert
            Assert.Equal(
                "<input id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[checkbox]]\" value=\"HtmlEncode[[true]]\" />" +
                "<input name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />",
                HtmlContentUtilities.HtmlContentToString(checkboxResult));
        }

        [Fact]
        public void Checkbox_UsesSpecifiedHtmlAttributes()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var checkboxResult = helper.CheckBox("Property1", htmlAttributes: new { attr = "value" });

            // Assert
            Assert.Equal(
                "<input attr=\"HtmlEncode[[value]]\" id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[checkbox]]\" value=\"HtmlEncode[[true]]\" />" +
                "<input name=\"HtmlEncode[[Property1]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />",
                HtmlContentUtilities.HtmlContentToString(checkboxResult));
        }

        private static ViewDataDictionary<TestModel> GetTestModelViewData()
        {
            return new ViewDataDictionary<TestModel>(new EmptyModelMetadataProvider())
            {
                { "Property1", true },
                { "Property2", "NotTrue" },
                { "Property3", false }
            };
        }

        private static ViewDataDictionary<ModelWithValidation> GetModelWithValidationViewData()
        {
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var viewData = new ViewDataDictionary<ModelWithValidation>(provider)
            {
                { "ComplexProperty.Property1", true },
                { "ComplexProperty.Property2", "NotTrue" },
                { "ComplexProperty.Property3", false }
            };
            viewData.Model = new ModelWithValidation();

            return viewData;
        }

        private class TestModel
        {
            public bool Property1 { get; set; }

            public bool Property2 { get; set; }

            public bool Property3 { get; set; }
        }

        private class ModelWithValidation
        {
            [Required]
            public bool Name { get; set; }

            public TestModel ComplexProperty { get; set; }
        }
    }
}
