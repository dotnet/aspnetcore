// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelperPasswordTest
    {
        public static IEnumerable<object[]> PasswordWithViewDataAndAttributesData
        {
            get
            {
                var attributes1 = new Dictionary<string, object>
                {
                    { "test-key", "test-value" },
                    { "value", "attribute-value" }
                };

                var attributes2 = new { test_key = "test-value", value = "attribute-value" };

                var vdd = GetViewDataWithModelStateAndModelAndViewDataValues();
                vdd.Model.Property1 = "does-not-get-used";
                yield return new object[] { vdd, attributes1 };
                yield return new object[] { vdd, attributes2 };

                var nullModelVdd = GetViewDataWithNullModelAndNonEmptyViewData();
                yield return new object[] { nullModelVdd, attributes1 };
                yield return new object[] { nullModelVdd, attributes2 };
            }
        }

        [Theory]
        [MemberData(nameof(PasswordWithViewDataAndAttributesData))]
        public void Password_UsesAttributeValueWhenValueArgumentIsNull(ViewDataDictionary<PasswordModel> vdd,
                                                                      object attributes)
        {
            // Arrange
            var expected = @"<input id=""Property1"" name=""Property1"" test-key=""test-value"" type=""password"" " +
                           @"value=""attribute-value"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(vdd);

            // Act
            var result = helper.Password("Property1", value: null, htmlAttributes: attributes);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(PasswordWithViewDataAndAttributesData))]
        public void Password_UsesExplicitValue_IfSpecified(ViewDataDictionary<PasswordModel> vdd,
                                                           object attributes)
        {
            // Arrange
            var expected = @"<input id=""Property1"" name=""Property1"" test-key=""test-value"" type=""password"" " +
                           @"value=""explicit-value"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(vdd);

            // Act
            var result = helper.Password("Property1", "explicit-value", attributes);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void PasswordWithPrefix_GeneratesExpectedValue()
        {
            // Arrange
            var expected = @"<input id=""MyPrefix_Property1"" name=""MyPrefix.Property1"" type=""password"" " +
                           @"value=""explicit-value"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            var result = helper.Password("Property1", "explicit-value", htmlAttributes: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void PasswordWithPrefix_UsesIdDotReplacementToken()
        {
            // Arrange
            var expected = @"<input id=""MyPrefix$Property1"" name=""MyPrefix.Property1"" type=""password"" " +
                           @"value=""explicit-value"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";
            helper.IdAttributeDotReplacement = "$";

            // Act
            var result = helper.Password("Property1", "explicit-value", htmlAttributes: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void PasswordWithPrefixAndEmptyName_GeneratesExpectedValue()
        {
            // Arrange
            var expected = @"<input id=""MyPrefix"" name=""MyPrefix"" type=""password"" value=""explicit-value"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";
            var name = string.Empty;

            // Act
            var result = helper.Password(name, "explicit-value", htmlAttributes: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void PasswordWithEmptyNameAndPrefixThrows()
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
            var name = string.Empty;
            var value = string.Empty;

            // Act and Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(() => helper.Password(name, value, htmlAttributes: null),
                                                      "expression");
        }

        [Fact]
        public void Password_UsesModelStateErrors_ButDoesNotUseModelOrViewDataOrModelStateForValueAttribute()
        {
            // Arrange
            var expected = @"<input class=""input-validation-error some-class"" id=""Property1""" +
                           @" name=""Property1"" test-key=""test-value"" type=""password"" />";
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
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void PasswordGeneratesUnobtrusiveValidation()
        {
            // Arrange
            var expected = @"<input data-val=""true"" data-val-required=""The Property2 field is required."" " +
                           @"id=""Property2"" name=""Property2"" type=""password"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());

            // Act
            var result = helper.Password("Property2", value: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        public static IEnumerable<object[]> PasswordWithComplexExpressions_UsesIdDotSeparatorData
        {
            get
            {
                yield return new object[]
                {
                    "Property4.Property5",
                    @"<input data-test=""val"" id=""Property4$$Property5"" name=""Property4.Property5"" " +
                    @"type=""password"" />",
                };

                yield return new object[]
               {
                    "Property4.Property6[0]",
                    @"<input data-test=""val"" id=""Property4$$Property6$$0$$"" name=""Property4.Property6[0]"" " +
                    @"type=""password"" />",
               };
            }
        }

        [Theory]
        [MemberData(nameof(PasswordWithComplexExpressions_UsesIdDotSeparatorData))]
        public void PasswordWithComplexExpressions_UsesIdDotSeparator(string expression, string expected)
        {
            // Arrange
            var viewData = GetViewDataWithModelStateAndModelAndViewDataValues();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(viewData);
            helper.IdAttributeDotReplacement = "$$";
            var attributes = new Dictionary<string, object> { { "data-test", "val" } };

            // Act
            var result = helper.Password(expression, value: null, htmlAttributes: attributes);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(PasswordWithViewDataAndAttributesData))]
        public void PasswordForWithAttributes_GeneratesExpectedValue(ViewDataDictionary<PasswordModel> vdd,
                                                                     object htmlAttributes)
        {
            // Arrange
            var expected = @"<input id=""Property1"" name=""Property1"" test-key=""test-value"" type=""password"" " +
                           @"value=""attribute-value"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
            helper.ViewData.Model.Property1 = "test";

            // Act
            var result = helper.PasswordFor(m => m.Property1, htmlAttributes);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void PasswordForWithPrefix_GeneratesExpectedValue()
        {
            // Arrange
            var expected = @"<input id=""MyPrefix_Property1"" name=""MyPrefix.Property1"" type=""password"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithModelStateAndModelAndViewDataValues());
            helper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "MyPrefix";

            // Act
            var result = helper.PasswordFor(m => m.Property1, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void PasswordFor_UsesModelStateErrors_ButDoesNotUseModelOrViewDataOrModelStateForValueAttribute()
        {
            // Arrange
            var expected = @"<input baz=""BazValue"" class=""input-validation-error some-class"" id=""Property1"" " +
                           @"name=""Property1"" type=""password"" />";
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
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void PasswordFor_GeneratesUnobtrusiveValidationAttributes()
        {
            // Arrange
            var expected = @"<input data-val=""true"" data-val-required=""The Property2 field is required."" " +
                           @"id=""Property2"" name=""Property2"" type=""password"" />";
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(GetViewDataWithErrors());

            // Act
            var result = helper.PasswordFor(m => m.Property2, htmlAttributes: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        public static TheoryData PasswordFor_WithComplexExpressionsData
        {
            get
            {
                return new TheoryData<Expression<Func<PasswordModel, string>>, string>
                {
                    {
                        model => model.Property3["key"],
                        @"<input data-val=""true"" id=""pre_Property3_key_"" name=""pre.Property3[key]"" " +
                        @"type=""password"" value=""attr-value"" />"
                    },
                    {
                        model => model.Property4.Property5,
                        @"<input data-val=""true"" id=""pre_Property4_Property5"" name=""pre.Property4.Property5"" " +
                        @"type=""password"" value=""attr-value"" />"
                    },
                    {
                        model => model.Property4.Property6[0],
                        @"<input data-val=""true"" id=""pre_Property4_Property6_0_"" " +
                        @"name=""pre.Property4.Property6[0]"" type=""password"" value=""attr-value"" />"
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
            viewData.ModelState.Add("pre.Property3[key]", GetModelState("Property3Val"));
            viewData.ModelState.Add("pre.Property4.Property5", GetModelState("Property5Val"));
            viewData.ModelState.Add("pre.Property4.Property6[0]", GetModelState("Property6Val"));
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
            Assert.Equal(expected, result.ToString());
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
            viewData.ModelState.Add("Property1", GetModelState("ModelStateValue"));

            return viewData;
        }

        private static ViewDataDictionary<PasswordModel> GetViewDataWithErrors()
        {
            var viewData = GetViewDataWithModelStateAndModelAndViewDataValues();
            viewData.ModelState.AddModelError("Property1", "error 1");
            viewData.ModelState.AddModelError("Property1", "error 2");
            return viewData;
        }

        private static ModelState GetModelState(string value)
        {
            return new ModelState
            {
                Value = new ValueProviderResult(value, value, CultureInfo.InvariantCulture)
            };
        }

        public class PasswordModel
        {
            public string Property1 { get; set; }

            [Required]
            public string Property2 { get; set; }

            public Dictionary<string, string> Property3 { get; } = new Dictionary<string, string>();

            public NestedClass Property4 { get; } = new NestedClass();
        }

        public class NestedClass
        {
            public string Property5 { get; set; }

            public List<string> Property6 { get; } = new List<string>();
        }
    }
}