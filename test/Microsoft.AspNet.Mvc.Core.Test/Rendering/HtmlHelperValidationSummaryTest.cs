// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelperValidationSummaryTest
    {
        // Message, HTML attributes, tag -> expected result.
        public static TheoryData<string, object, string, string> ValidValidationSummaryData
        {
            get
            {
                var attributes = new { @class = "wood smoke", attribute_name = "attribute-value", };
                var dictionary = new Dictionary<string, object>
                {
                    { "class", "wood smoke" },
                    { "attribute-name", "attribute-value" },
                };

                var basicDiv = "<div class=\"validation-summary-valid\" data-valmsg-summary=\"true\">" +
                    "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithAttributes = "<div attribute-name=\"attribute-value\" " +
                    "class=\"validation-summary-valid wood smoke\" data-valmsg-summary=\"true\"><ul>" +
                    "<li style=\"display:none\"></li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithMessage = "<div class=\"validation-summary-valid\" data-valmsg-summary=\"true\">" +
                    "<span>This is my message</span>" + Environment.NewLine +
                    "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithH3Message = "<div class=\"validation-summary-valid\" data-valmsg-summary=\"true\">" +
                    "<h3>This is my message</h3>" + Environment.NewLine +
                    "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithMessageAndAttributes = "<div attribute-name=\"attribute-value\" " +
                    "class=\"validation-summary-valid wood smoke\" data-valmsg-summary=\"true\">" +
                    "<span>This is my message</span>" + Environment.NewLine +
                    "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithH3MessageAndAttributes = "<div attribute-name=\"attribute-value\" " +
                    "class=\"validation-summary-valid wood smoke\" data-valmsg-summary=\"true\">" +
                    "<h3>This is my message</h3>" + Environment.NewLine +
                    "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                    "</ul></div>";

                return new TheoryData<string, object, string, string>
                {
                    { null, null, null, basicDiv },
                    { null, null, "h3", basicDiv },
                    { null, attributes, null, divWithAttributes },
                    { null, attributes, "h3", divWithAttributes },
                    { null, dictionary, null, divWithAttributes },
                    { null, dictionary, "h3", divWithAttributes },
                    { "This is my message", null, null, divWithMessage },
                    { "This is my message", null, "h3", divWithH3Message },
                    { "This is my message", attributes, null, divWithMessageAndAttributes },
                    { "This is my message", attributes, "h3", divWithH3MessageAndAttributes },
                    { "This is my message", dictionary, null, divWithMessageAndAttributes },
                    { "This is my message", dictionary, "h3", divWithH3MessageAndAttributes },
                };
            }
        }

        // Exclude property errors, client validation enabled -> expected result with model error, with property error.
        public static TheoryData<bool, bool, string, string> OneErrorValidationSummaryData
        {
            get
            {
                var basicDiv = "<div class=\"validation-summary-errors\"><ul>" +
                    "<li style=\"display:none\"></li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithError = "<div class=\"validation-summary-errors\"><ul>" +
                    "<li>This is my validation message</li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithErrorAndSummary = "<div class=\"validation-summary-errors\" data-valmsg-summary=\"true\"><ul>" +
                    "<li>This is my validation message</li>" + Environment.NewLine +
                    "</ul></div>";

                return new TheoryData<bool, bool, string, string>
                {
                    { false, false, divWithError, divWithError },
                    { false, true, divWithErrorAndSummary, divWithErrorAndSummary },
                    { true, false, divWithError, basicDiv },
                    { true, true, divWithError, basicDiv },
                };
            }
        }

        // Exclude property errors, prefix -> expected result
        public static TheoryData<bool, string, string> MultipleErrorsValidationSummaryData
        {
            get
            {
                var basicDiv = "<div class=\"validation-summary-errors\"><ul>" +
                    "<li style=\"display:none\"></li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithRootError = "<div class=\"validation-summary-errors\"><ul>" +
                    "<li>This is an error for the model root.</li>" + Environment.NewLine +
                    "<li>This is another error for the model root.</li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithProperty3Error = "<div class=\"validation-summary-errors\"><ul>" +
                    "<li>This is an error for Property3.</li>" + Environment.NewLine +
                    "</ul></div>";
                var divWithAllErrors = "<div class=\"validation-summary-errors\" data-valmsg-summary=\"true\"><ul>" +
                    "<li>This is an error for Property3.Property2.</li>" + Environment.NewLine +
                    "<li>This is an error for Property3.OrderedProperty3.</li>" + Environment.NewLine +
                    "<li>This is an error for Property3.OrderedProperty2.</li>" + Environment.NewLine +
                    "<li>This is an error for Property3.</li>" + Environment.NewLine +
                    "<li>This is an error for Property2.</li>" + Environment.NewLine +
                    "<li>This is another error for Property2.</li>" + Environment.NewLine +
                    "<li>This is an error for the model root.</li>" + Environment.NewLine +
                    "<li>This is another error for the model root.</li>" + Environment.NewLine +
                    "</ul></div>";

                return new TheoryData<bool, string, string>
                {
                    { false, string.Empty, divWithAllErrors },
                    { false, "Property2", divWithAllErrors },
                    { false, "some.unrelated.prefix", divWithAllErrors },
                    { true, string.Empty, divWithRootError },
                    { true, "Property3", divWithProperty3Error },
                    { true, "some.unrelated.prefix", basicDiv },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValidValidationSummaryData))]
        public void ValidationSummary_AllValid_ReturnsExpectedDiv(
            string message,
            object htmlAttributes,
            string tag,
            string expected)
        {
            // Arrange
            var model = new ValidationModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = html.ValidationSummary(
                excludePropertyErrors: false,
                message: message,
                htmlAttributes: htmlAttributes,
                tag: tag);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(ValidValidationSummaryData))]
        public void ValidationSummary_ExcludePropertyErrorsAllValid_ReturnsEmpty(
            string message,
            object htmlAttributes,
            string tag,
            string ignored)
        {
            // Arrange
            var model = new ValidationModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);

            // Act
            var result = html.ValidationSummary(
                excludePropertyErrors: true,
                message: message,
                htmlAttributes: htmlAttributes,
                tag: tag);

            // Assert
            Assert.Equal(HtmlString.Empty, result);
        }

        [Theory]
        [MemberData(nameof(ValidValidationSummaryData))]
        public void ValidationSummary_ClientValidationDisabledAllValid_ReturnsEmpty(
            string message,
            object htmlAttributes,
            string tag,
            string ignored)
        {
            // Arrange
            var model = new ValidationModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            html.ViewContext.ClientValidationEnabled = false;

            // Act
            var result = html.ValidationSummary(
                excludePropertyErrors: false,
                message: message,
                htmlAttributes: htmlAttributes,
                tag: tag);

            // Assert
            Assert.Equal(HtmlString.Empty, result);
        }

        [Theory]
        [MemberData(nameof(OneErrorValidationSummaryData))]
        public void ValidationSummary_InvalidModel_ReturnsExpectedDiv(
            bool excludePropertyErrors,
            bool clientValidationEnabled,
            string expected,
            string ignored)
        {
            // Arrange
            var model = new ValidationModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            html.ViewContext.ClientValidationEnabled = clientValidationEnabled;
            html.ViewData.ModelState.AddModelError(string.Empty, "This is my validation message");

            // Act
            var result = html.ValidationSummary(
                excludePropertyErrors: excludePropertyErrors,
                message: null,
                htmlAttributes: null,
                tag: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(OneErrorValidationSummaryData))]
        public void ValidationSummary_InvalidModelWithPrefix_ReturnsExpectedDiv(
            bool excludePropertyErrors,
            bool clientValidationEnabled,
            string expected,
            string ignored)
        {
            // Arrange
            var model = new ValidationModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            html.ViewContext.ClientValidationEnabled = clientValidationEnabled;
            html.ViewData.TemplateInfo.HtmlFieldPrefix = "this.is.my.prefix";
            html.ViewData.ModelState.AddModelError("this.is.my.prefix", "This is my validation message");

            // Act
            var result = html.ValidationSummary(
                excludePropertyErrors,
                message: null,
                htmlAttributes: null,
                tag: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(OneErrorValidationSummaryData))]
        public void ValidationSummary_OneInvalidProperty_ReturnsExpectedDiv(
            bool excludePropertyErrors,
            bool clientValidationEnabled,
            string ignored,
            string expected)
        {
            // Arrange
            var model = new ValidationModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            html.ViewContext.ClientValidationEnabled = clientValidationEnabled;
            html.ViewData.ModelState.AddModelError("Property1", "This is my validation message");

            // Act
            var result = html.ValidationSummary(
                excludePropertyErrors,
                message: null,
                htmlAttributes: null,
                tag: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [MemberData(nameof(MultipleErrorsValidationSummaryData))]
        public void ValidationSummary_MultipleErrors_ReturnsExpectedDiv(
            bool excludePropertyErrors,
            string prefix,
            string expected)
        {
            // Arrange
            var model = new ValidationModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            html.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;
            AddMultipleErrors(html.ViewData.ModelState);

            // Act
            var result = html.ValidationSummary(
                excludePropertyErrors,
                message: null,
                htmlAttributes: null,
                tag: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void ValidationSummary_ErrorsInModelUsingOrder_SortsErrorsAsExpected()
        {
            // Arrange
            var expected = "<div class=\"validation-summary-errors\" data-valmsg-summary=\"true\"><ul>" +
                "<li>This is an error for OrderedProperty3.</li>" + Environment.NewLine +
                "<li>This is an error for OrderedProperty2.</li>" + Environment.NewLine +
                "<li>This is another error for OrderedProperty2.</li>" + Environment.NewLine +
                "<li>This is yet-another error for OrderedProperty2.</li>" + Environment.NewLine +
                "<li>This is an error for OrderedProperty1.</li>" + Environment.NewLine +
                "<li>This is an error for Property3.</li>" + Environment.NewLine +
                "<li>This is an error for Property2.</li>" + Environment.NewLine +
                "<li>This is another error for Property2.</li>" + Environment.NewLine +
                "<li>This is an error for Property1.</li>" + Environment.NewLine +
                "<li>This is another error for Property1.</li>" + Environment.NewLine +
                "<li>This is an error for LastProperty.</li>" + Environment.NewLine +
                "</ul></div>";

            var model = new OrderedModel();
            var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
            AddOrderedErrors(html.ViewData.ModelState);

            // Act
            var result = html.ValidationSummary(
                excludePropertyErrors: false,
                message: null,
                htmlAttributes: null,
                tag: null);

            // Assert
            Assert.Equal(expected, result.ToString());
        }

        // Adds errors for various parts of the model, including the root.
        private void AddMultipleErrors(ModelStateDictionary modelState)
        {
            modelState.AddModelError("Property3.Property2", "This is an error for Property3.Property2.");
            modelState.AddModelError("Property3.OrderedProperty3", "This is an error for Property3.OrderedProperty3.");
            modelState.AddModelError("Property3.OrderedProperty2", "This is an error for Property3.OrderedProperty2.");

            modelState.AddModelError("Property3", "This is an error for Property3.");
            modelState.AddModelError("Property3", new InvalidCastException("Exception will be ignored."));

            modelState.AddModelError("Property2", "This is an error for Property2.");
            modelState.AddModelError("Property2", "This is another error for Property2.");

            modelState.AddModelError(string.Empty, "This is an error for the model root.");
            modelState.AddModelError(string.Empty, "This is another error for the model root.");
            modelState.AddModelError(string.Empty, new InvalidOperationException("Another ignored Exception."));
        }

        // Adds one or more errors for all properties in OrderedModel. But adds errors out of order.
        private void AddOrderedErrors(ModelStateDictionary modelState)
        {
            modelState.AddModelError("Property3", "This is an error for Property3.");
            modelState.AddModelError("Property3", new InvalidCastException("An ignored Exception."));

            modelState.AddModelError("Property2", "This is an error for Property2.");
            modelState.AddModelError("Property2", "This is another error for Property2.");

            modelState.AddModelError("OrderedProperty3", "This is an error for OrderedProperty3.");

            modelState.AddModelError("OrderedProperty2", "This is an error for OrderedProperty2.");
            modelState.AddModelError("OrderedProperty2", "This is another error for OrderedProperty2.");

            modelState.AddModelError("LastProperty", "This is an error for LastProperty.");

            modelState.AddModelError("Property1", "This is an error for Property1.");
            modelState.AddModelError("Property1", "This is another error for Property1.");

            modelState.AddModelError("OrderedProperty1", "This is an error for OrderedProperty1.");
            modelState.AddModelError("OrderedProperty2", "This is yet-another error for OrderedProperty2.");
        }

        private class ValidationModel
        {
            public string Property1 { get; set; }

            public string Property2 { get; set; }

            public OrderedModel Property3 { get; set; }
        }

        private class OrderedModel
        {
            [Display(Order = 10001)]
            public string LastProperty { get; set; }

            public string Property3 { get; set; }
            public string Property1 { get; set; }
            public string Property2 { get; set; }

            [Display(Order = 23)]
            public string OrderedProperty3 { get; set; }
            [Display(Order = 23)]
            public string OrderedProperty2 { get; set; }
            [Display(Order = 23)]
            public string OrderedProperty1 { get; set; }
        }
    }
}