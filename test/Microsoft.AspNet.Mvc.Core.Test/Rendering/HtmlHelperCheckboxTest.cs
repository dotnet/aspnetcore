// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelperCheckBoxTest
    {
        [Fact]
        public void CheckBoxOverridesCalculatedValuesWithValuesFromHtmlAttributes()
        {
            // Arrange
            var expected = @"<input checked=""checked"" id=""Property3"" name=""Property3"" type=""checkbox"" " +
                           @"value=""false"" /><input name=""Property3"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act
            var html = helper.CheckBox("Property3",
                                       isChecked: null,
                                       htmlAttributes: new { @checked = "checked", value = "false" });

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxExplicitParametersOverrideDictionary_ForValueInModel()
        {
            // Arrange
            var expected = @"<input checked=""checked"" id=""Property3"" name=""Property3"" type=""checkbox"" " +
                           @"value=""false"" /><input name=""Property3"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act
            var html = helper.CheckBox("Property3",
                                       isChecked: true,
                                       htmlAttributes: new { @checked = "unchecked", value = "false" });

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxExplicitParametersOverrideDictionary_ForNullModel()
        {
            // Arrange
            var expected = @"<input checked=""checked"" id=""foo"" name=""foo"" type=""checkbox"" value=""false"" />" +
                           @"<input name=""foo"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var html = helper.CheckBox("foo",
                                       isChecked: true,
                                       htmlAttributes: new { @checked = "unchecked", value = "false" });

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxWithInvalidBooleanThrows()
        {
            // Arrange
            var expected = "String was not recognized as a valid Boolean.";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act & Assert
            var ex = Assert.Throws<FormatException>(
                        () => helper.CheckBox("Property2", isChecked: null, htmlAttributes: null));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void CheckBoxCheckedWithOnlyName()
        {
            // Arrange
            var expected = @"<input checked=""checked"" id=""Property1"" name=""Property1"" type=""checkbox"" " +
                           @"value=""true"" /><input name=""Property1"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxUsesAttemptedValueFromModelState()
        {
            // Arrange
            var expected = @"<input id=""Property1"" name=""Property1"" type=""checkbox"" value=""true"" />" +
                           @"<input name=""Property1"" type=""hidden"" value=""false"" />";
            var valueProviderResult = new ValueProviderResult("false", "false", CultureInfo.InvariantCulture);
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewData.ModelState.SetModelValue("Property1", valueProviderResult);

            // Act
            var html = helper.CheckBox("Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxGeneratesUnobtrusiveValidationAttributes()
        {
            // Arrange
            var expected = @"<input data-val=""true"" data-val-required=""The Name field is required."" id=""Name""" +
                           @" name=""Name"" type=""checkbox"" value=""true"" />" +
                           @"<input name=""Name"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetModelWithValidationViewData());

            // Act
            var html = helper.CheckBox("Name", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxReplacesUnderscoresInHtmlAttributesWithDashes()
        {
            // Arrange
            var expected = @"<input checked=""checked"" id=""Property1"" name=""Property1"" " +
                           @"Property1-Property3=""Property3ObjValue"" type=""checkbox"" value=""true"" /><input " +
                           @"name=""Property1"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            var htmlAttributes = new { Property1_Property3 = "Property3ObjValue" };

            // Act
            var html = helper.CheckBox("Property1", isChecked: true, htmlAttributes: htmlAttributes);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxWithPrefix_ReplaceDotsInIdByDefaultWithUnderscores()
        {
            // Arrange
            var expected = @"<input id=""MyPrefix_Property1"" name=""MyPrefix.Property1"" " +
                           @"Property3=""Property3Value"" type=""checkbox"" value=""true"" /><input " +
                           @"name=""MyPrefix.Property1"" type=""hidden"" value=""false"" />";
            var dictionary = new RouteValueDictionary(new { Property3 = "Property3Value" });
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            var html = helper.CheckBox("Property1", isChecked: false, htmlAttributes: dictionary);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxWithPrefix_ReplacesDotsInIdWithIdDotReplacement()
        {
            // Arrange
            var expected = @"<input id=""MyPrefix!!!Property1"" name=""MyPrefix.Property1"" " +
                           @"Property3=""Property3Value"" type=""checkbox"" value=""true"" /><input " +
                           @"name=""MyPrefix.Property1"" type=""hidden"" value=""false"" />";
            var dictionary = new Dictionary<string, object> { { "Property3", "Property3Value" } };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            helper.IdAttributeDotReplacement = "!!!";
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            var html = helper.CheckBox("Property1", isChecked: false, htmlAttributes: dictionary);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxWithPrefixAndEmptyName()
        {
            // Arrange
            var expected = @"<input id=""MyPrefix"" name=""MyPrefix"" Property3=""Property3Value"" " +
                           @"type=""checkbox"" value=""true"" /><input name=""MyPrefix"" type=""hidden"" " +
                           @"value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model: false);
            var attributes = new Dictionary<string, object> { { "Property3", "Property3Value" } };
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            var html = helper.CheckBox(string.Empty, isChecked: false, htmlAttributes: attributes);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxWithComplexExpressionsEvaluatesValuesInViewDataDictionary()
        {
            // Arrange
            var expected = @"<input checked=""checked"" id=""ComplexProperty_Property1"" name=""ComplexProperty." +
                           @"Property1"" type=""checkbox"" value=""true"" /><input name=""ComplexProperty.Property1""" +
                           @" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetModelWithValidationViewData());

            // Act
            var html = helper.CheckBox("ComplexProperty.Property1", isChecked: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxForWithNullContainer_TreatsBooleanAsFalse()
        {
            // Arrange
            var expected = @"<input id=""Property1"" name=""Property1"" type=""checkbox"" value=""true"" />" +
                           @"<input name=""Property1"" type=""hidden"" value=""false"" />";
            var viewData = GetTestModelViewData();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);
            var valueProviderResult = new ValueProviderResult("false", "false", CultureInfo.InvariantCulture);
            viewData.ModelState.SetModelValue("Property1", valueProviderResult);

            // Act
            var html = helper.CheckBoxFor(m => m.Property1);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Theory]
        [InlineData(false, "")]
        [InlineData(true, "checked=\"checked\" ")]
        public void CheckBoxForWithNonNullContainer_UsesPropertyValue(bool value, string expectedChecked)
        {
            // Arrange
            var expected = @"<input {0}id=""Property1"" name=""Property1"" type=""checkbox"" value=""true"" />" +
                           @"<input name=""Property1"" type=""hidden"" value=""false"" />";
            expected = string.Format(expected, expectedChecked);

            var viewData = GetTestModelViewData();
            viewData.Model = new TestModel
            {
                Property1 = value,
            };

            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);

            // Act
            var html = helper.CheckBoxFor(m => m.Property1);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxForOverridesCalculatedParametersWithValuesFromHtmlAttributes()
        {
            // Arrange
            var expected = @"<input checked=""checked"" id=""Property3"" name=""Property3"" type=""checkbox"" " +
                           @"value=""false"" /><input name=""Property3"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());

            // Act
            var html = helper.CheckBoxFor(m => m.Property3, new { @checked = "checked", value = "false" });

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxForGeneratesUnobtrusiveValidationAttributes()
        {
            // Arrange
            var expected = @"<input data-val=""true"" data-val-required=""The Name field is required."" id=""Name""" +
                           @" name=""Name"" type=""checkbox"" value=""true"" />" +
                           @"<input name=""Name"" type=""hidden"" value=""false"" />";
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var viewDataDictionary = new ViewDataDictionary<ModelWithValidation>(metadataProvider)
            {
                Model = new ModelWithValidation()
            };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewDataDictionary);

            // Act
            var html = helper.CheckBoxFor(m => m.Name, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Theory]
        [InlineData("false", "")]
        [InlineData("true", "checked=\"checked\" ")]
        public void CheckBoxFor_UsesModelStateAttemptedValue(string attemptedValue, string expectedChecked)
        {
            // Arrange
            var expected = @"<input {0}id=""Property1"" name=""Property1"" type=""checkbox"" value=""true"" />" +
                           @"<input name=""Property1"" type=""hidden"" value=""false"" />";
            expected = string.Format(expected, expectedChecked);

            var viewData = GetTestModelViewData();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);
            var valueProviderResult =
                new ValueProviderResult(attemptedValue, attemptedValue, CultureInfo.InvariantCulture);
            viewData.ModelState.SetModelValue("Property1", valueProviderResult);

            // Act
            var html = helper.CheckBoxFor(m => m.Property1);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxFor_WithObjectAttribute_MapsUnderscoresInNamesToDashes()
        {
            // Arrange
            var expected = @"<input id=""Property1"" name=""Property1"" " +
                           @"Property1-Property3=""Property3ObjValue"" type=""checkbox"" value=""true"" /><input " +
                           @"name=""Property1"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            var htmlAttributes = new { Property1_Property3 = "Property3ObjValue" };

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, htmlAttributes);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxForWith_AttributeDictionary_GeneratesExpectedAttributes()
        {
            // Arrange
            var expected = @"<input id=""Property1"" name=""Property1"" " +
                           @"Property3=""Property3Value"" type=""checkbox"" value=""true"" /><input " +
                           @"name=""Property1"" type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            var attributes = new Dictionary<string, object> { { "Property3", "Property3Value" } };

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, attributes);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxForWithPrefix()
        {
            // Arrange
            var expected = @"<input id=""MyPrefix_Property1"" name=""MyPrefix.Property1"" Property3=""PropValue"" " +
                           @"type=""checkbox"" value=""true"" /><input name=""MyPrefix.Property1"" type=""hidden"" " +
                           @"value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetTestModelViewData());
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";
            var attributes = new Dictionary<string, object> { { "Property3", "PropValue" } };

            // Act
            var html = helper.CheckBoxFor(m => m.Property1, attributes);

            // Assert
            Assert.Equal(expected, html.ToString());
        }

        [Fact]
        public void CheckBoxFor_WithComplexExpressions_DoesNotUseValuesFromViewDataDictionary()
        {
            // Arrange
            var expected = @"<input id=""ComplexProperty_Property1"" name=""ComplexProperty." +
                        @"Property1"" type=""checkbox"" value=""true"" /><input name=""ComplexProperty.Property1"" " +
                        @"type=""hidden"" value=""false"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetModelWithValidationViewData());

            // Act
            var html = helper.CheckBoxFor(m => m.ComplexProperty.Property1, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, html.ToString());
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