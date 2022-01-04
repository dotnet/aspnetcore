// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Rendering;

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

            var basicDiv = "<div class=\"HtmlEncode[[validation-summary-valid]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
                "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                "</ul></div>";
            var divWithAttributes = "<div attribute-name=\"HtmlEncode[[attribute-value]]\" " +
                "class=\"HtmlEncode[[wood smoke validation-summary-valid]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
                "<li style=\"display:none\"></li>" + Environment.NewLine +
                "</ul></div>";
            var divWithMessage = "<div class=\"HtmlEncode[[validation-summary-valid]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
                "<span>HtmlEncode[[This is my message]]</span>" + Environment.NewLine +
                "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                "</ul></div>";
            var divWithH3Message = "<div class=\"HtmlEncode[[validation-summary-valid]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
                "<h3>HtmlEncode[[This is my message]]</h3>" + Environment.NewLine +
                "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                "</ul></div>";
            var divWithMessageAndAttributes = "<div attribute-name=\"HtmlEncode[[attribute-value]]\" " +
                "class=\"HtmlEncode[[wood smoke validation-summary-valid]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
                "<span>HtmlEncode[[This is my message]]</span>" + Environment.NewLine +
                "<ul><li style=\"display:none\"></li>" + Environment.NewLine +
                "</ul></div>";
            var divWithH3MessageAndAttributes = "<div attribute-name=\"HtmlEncode[[attribute-value]]\" " +
                "class=\"HtmlEncode[[wood smoke validation-summary-valid]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
                "<h3>HtmlEncode[[This is my message]]</h3>" + Environment.NewLine +
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
            var divWithError = "<div class=\"HtmlEncode[[validation-summary-errors]]\"><ul>" +
                "<li>HtmlEncode[[This is my validation message]]</li>" + Environment.NewLine +
                "</ul></div>";
            var divWithErrorAndSummary = "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
                "<li>HtmlEncode[[This is my validation message]]</li>" + Environment.NewLine +
                "</ul></div>";

            return new TheoryData<bool, bool, string, string>
                {
                    { false, false, divWithError, divWithError },
                    { false, true, divWithErrorAndSummary, divWithErrorAndSummary },
                    { true, false, divWithError, string.Empty },
                    { true, true, divWithError, string.Empty },
                };
        }
    }

    // Exclude property errors, prefix -> expected result
    public static TheoryData<bool, string, string> MultipleErrorsValidationSummaryData
    {
        get
        {
            var divWithRootError = "<div class=\"HtmlEncode[[validation-summary-errors]]\"><ul>" +
                "<li>HtmlEncode[[This is an error for the model root.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[This is another error for the model root.]]</li>" + Environment.NewLine +
                "</ul></div>";
            var divWithProperty3Error = "<div class=\"HtmlEncode[[validation-summary-errors]]\"><ul>" +
                "<li>HtmlEncode[[This is an error for Property3.]]</li>" + Environment.NewLine +
                "</ul></div>";
            var divWithAllErrors = "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
                "<li>HtmlEncode[[This is an error for Property2.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[This is another error for Property2.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[The value '' is not valid for Property2.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[This is an error for Property3.OrderedProperty3.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[This is an error for Property3.OrderedProperty2.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[This is an error for Property3.Property2.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[This is an error for Property3.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[This is an error for the model root.]]</li>" + Environment.NewLine +
                "<li>HtmlEncode[[This is another error for the model root.]]</li>" + Environment.NewLine +
                "</ul></div>";

            return new TheoryData<bool, string, string>
                {
                    { false, string.Empty, divWithAllErrors },
                    { false, "Property2", divWithAllErrors },
                    { false, "some.unrelated.prefix", divWithAllErrors },
                    { true, string.Empty, divWithRootError },
                    { true, "Property3", divWithProperty3Error },
                    { true, "some.unrelated.prefix", string.Empty },
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
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
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
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
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
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
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
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
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
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ValidationSummary_OrdersCorrectlyWhenElementsAreRemovedFromDictionary()
    {
        // Arrange
        var expected = "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
            "<li>HtmlEncode[[New error for Property2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property3.OrderedProperty3.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property3.Property2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for the model root.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is another error for the model root.]]</li>" + Environment.NewLine +
            "</ul></div>";
        var model = new ValidationModel();
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
        AddMultipleErrors(html.ViewData.ModelState);
        html.ViewData.ModelState.RemoveAll<ValidationModel>(m => m.Property2);
        html.ViewData.ModelState.Remove<ValidationModel>(m => m.Property3);
        html.ViewData.ModelState.Remove<ValidationModel>(m => m.Property3.OrderedProperty2);
        html.ViewData.ModelState.AddModelError("Property2", "New error for Property2.");

        // Act
        var result = html.ValidationSummary(
            excludePropertyErrors: false,
            message: null,
            htmlAttributes: null,
            tag: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ValidationSummary_IncludesErrorsThatAreNotPartOfMetadata()
    {
        // Arrange
        var expected = "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
            "<li>HtmlEncode[[This is an error for Property2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is another error for Property2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[The value '' is not valid for Property2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property3.OrderedProperty3.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property3.OrderedProperty2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property3.Property2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property3.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for the model root.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is another error for the model root.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[non-existent-error1]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[non-existent-error2]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[non-existent-error3]]</li>" + Environment.NewLine +
            "</ul></div>";
        var model = new ValidationModel();
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
        AddMultipleErrors(html.ViewData.ModelState);
        html.ViewData.ModelState.AddModelError("non-existent-property1", "non-existent-error1");
        html.ViewData.ModelState.AddModelError("non.existent.property2", "non-existent-error2");
        html.ViewData.ModelState.AddModelError("non.existent[0].property3", "non-existent-error3");

        // Act
        var result = html.ValidationSummary(
            excludePropertyErrors: false,
            message: null,
            htmlAttributes: null,
            tag: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ValidationSummary_IncludesErrorsForCollectionProperties()
    {
        // Arrange
        var expected = "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
           "<li>HtmlEncode[[Property1 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[Property2[0].OrderedProperty1 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[Property2[0].Property1 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[Property2[2].Property3 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[Property2[10].Property2 error]]</li>" + Environment.NewLine +
           "</ul></div>";
        var model = new ModelWithCollection();
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
        html.ViewData.ModelState.AddModelError("Property1", "Property1 error");
        html.ViewData.ModelState.AddModelError("Property2[0].OrderedProperty1", "Property2[0].OrderedProperty1 error");
        html.ViewData.ModelState.AddModelError("Property2[10].Property2", "Property2[10].Property2 error");
        html.ViewData.ModelState.AddModelError("Property2[2].Property3", "Property2[2].Property3 error");
        html.ViewData.ModelState.AddModelError("Property2[0].Property1", "Property2[0].Property1 error");

        // Act
        var result = html.ValidationSummary(
            excludePropertyErrors: false,
            message: null,
            htmlAttributes: null,
            tag: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ValidationSummary_IncludesErrorsForTopLevelCollectionProperties()
    {
        // Arrange
        var expected = "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
           "<li>HtmlEncode[[[0].OrderedProperty2 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[[0].OrderedProperty1 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[[0].Property1 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[[2].OrderedProperty3 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[[2].Property3 error]]</li>" + Environment.NewLine +
           "</ul></div>";
        var model = new OrderedModel[5];
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
        html.ViewData.ModelState.AddModelError("[0].OrderedProperty2", "[0].OrderedProperty2 error");
        html.ViewData.ModelState.AddModelError("[0].Property1", "[0].Property1 error");
        html.ViewData.ModelState.AddModelError("[0].OrderedProperty1", "[0].OrderedProperty1 error");
        html.ViewData.ModelState.AddModelError("[2].Property3", "[2].Property3 error");
        html.ViewData.ModelState.AddModelError("[2].OrderedProperty3", "[2].OrderedProperty3 error");

        // Act
        var result = html.ValidationSummary(
            excludePropertyErrors: false,
            message: null,
            htmlAttributes: null,
            tag: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ValidationSummary_IncludesErrorsForPropertiesOnCollectionTypes()
    {
        // Arrange
        var expected = "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
           "<li>HtmlEncode[[[0].OrderedProperty2 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[[0].OrderedProperty1 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[[0].Property1 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[[2].OrderedProperty3 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[[2].Property3 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[OrderedProperty1 error]]</li>" + Environment.NewLine +
           "<li>HtmlEncode[[OrderedProperty2 error]]</li>" + Environment.NewLine +
           "</ul></div>";
        var model = new OrderedModel[5];
        var html = DefaultTemplatesUtilities.GetHtmlHelper(model);
        html.ViewData.ModelState.AddModelError("[0].OrderedProperty2", "[0].OrderedProperty2 error");
        html.ViewData.ModelState.AddModelError("[0].Property1", "[0].Property1 error");
        html.ViewData.ModelState.AddModelError("[0].OrderedProperty1", "[0].OrderedProperty1 error");
        html.ViewData.ModelState.AddModelError("[2].Property3", "[2].Property3 error");
        html.ViewData.ModelState.AddModelError("[2].OrderedProperty3", "[2].OrderedProperty3 error");
        html.ViewData.ModelState.AddModelError("OrderedProperty1", "OrderedProperty1 error");
        html.ViewData.ModelState.AddModelError("OrderedProperty2", "OrderedProperty2 error");

        // Act
        var result = html.ValidationSummary(
            excludePropertyErrors: false,
            message: null,
            htmlAttributes: null,
            tag: null);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ValidationSummary_ErrorsInModelUsingOrder_SortsErrorsAsExpected()
    {
        // Arrange
        var expected = "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\"><ul>" +
            "<li>HtmlEncode[[This is an error for OrderedProperty3.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for OrderedProperty2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is another error for OrderedProperty2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is yet-another error for OrderedProperty2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for OrderedProperty1.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property3.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property1.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is another error for Property1.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for Property2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is another error for Property2.]]</li>" + Environment.NewLine +
            "<li>HtmlEncode[[This is an error for LastProperty.]]</li>" + Environment.NewLine +
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
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(result));
    }

    [Fact]
    public void ValidationSummary_UsesValuesFromModelState()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary();

        // Assert
        Assert.Equal(
            "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
            "<ul><li>HtmlEncode[[Error for Property1]]</li>" + Environment.NewLine +
            "</ul></div>",
            HtmlContentUtilities.HtmlContentToString(validationSummaryResult));
    }

    [Fact]
    public void ValidationSummary_ExcludesPropertyErrors()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary(excludePropertyErrors: true);

        // Assert
        Assert.Equal(HtmlString.Empty, validationSummaryResult);
    }

    [Fact]
    public void ValidationSummary_UsesSpecifiedMessage()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary(message: "Custom Message");

        // Assert
        Assert.Equal(
            "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
            "<span>HtmlEncode[[Custom Message]]</span>" + Environment.NewLine +
            "<ul><li>HtmlEncode[[Error for Property1]]</li>" + Environment.NewLine +
            "</ul></div>",
            HtmlContentUtilities.HtmlContentToString(validationSummaryResult));
    }

    [Fact]
    public void ValidationSummary_UsesSpecifiedTag()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary(message: "Custom Message", tag: "div");

        // Assert
        Assert.Equal(
            "<div class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
            "<div>HtmlEncode[[Custom Message]]</div>" + Environment.NewLine +
            "<ul><li>HtmlEncode[[Error for Property1]]</li>" + Environment.NewLine +
            "</ul></div>",
            HtmlContentUtilities.HtmlContentToString(validationSummaryResult));
    }

    [Fact]
    public void ValidationSummary_UsesSpecifiedMessageAndExcludesPropertyErrors()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError(string.Empty, "Error for root");
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary(excludePropertyErrors: true, message: "Custom Message");

        // Assert
        Assert.Equal(
            "<div class=\"HtmlEncode[[validation-summary-errors]]\"><span>HtmlEncode[[Custom Message]]</span>" +
            Environment.NewLine +
            "<ul><li>HtmlEncode[[Error for root]]</li>" + Environment.NewLine +
            "</ul></div>",
            HtmlContentUtilities.HtmlContentToString(validationSummaryResult));
    }

    [Fact]
    public void ValidationSummary_UsesSpecifiedHtmlAttributes()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary(message: "Custom Message", htmlAttributes: new { attr = "value" });

        // Assert
        Assert.Equal(
            "<div attr=\"HtmlEncode[[value]]\" class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
            "<span>HtmlEncode[[Custom Message]]</span>" + Environment.NewLine +
            "<ul><li>HtmlEncode[[Error for Property1]]</li>" + Environment.NewLine +
            "</ul></div>",
            HtmlContentUtilities.HtmlContentToString(validationSummaryResult));
    }

    [Fact]
    public void ValidationSummary_UsesSpecifiedHtmlAttributesAndTag()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary(message: "Custom Message", htmlAttributes: new { attr = "value" }, tag: "div");

        // Assert
        Assert.Equal(
            "<div attr=\"HtmlEncode[[value]]\" class=\"HtmlEncode[[validation-summary-errors]]\" data-valmsg-summary=\"HtmlEncode[[true]]\">" +
            "<div>HtmlEncode[[Custom Message]]</div>" + Environment.NewLine +
            "<ul><li>HtmlEncode[[Error for Property1]]</li>" + Environment.NewLine +
            "</ul></div>",
            HtmlContentUtilities.HtmlContentToString(validationSummaryResult));
    }

    [Fact]
    public void ValidationSummary_UsesSpecifiedUsesSpecifiedTagAndExcludesPropertyErrors()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError(string.Empty, "Error for root");
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary(excludePropertyErrors: true, message: "Custom Message", tag: "div");

        // Assert
        Assert.Equal(
            "<div class=\"HtmlEncode[[validation-summary-errors]]\"><div>HtmlEncode[[Custom Message]]</div>" +
            Environment.NewLine +
            "<ul><li>HtmlEncode[[Error for root]]</li>" + Environment.NewLine +
            "</ul></div>",
            HtmlContentUtilities.HtmlContentToString(validationSummaryResult));
    }

    [Fact]
    public void ValidationSummary_UsesSpecifiedUsesSpecifiedHtmlAttributesAndExcludesPropertyErrors()
    {
        // Arrange
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        helper.ViewData.ModelState.AddModelError(string.Empty, "Error for root");
        helper.ViewData.ModelState.AddModelError("Property1", "Error for Property1");

        // Act
        var validationSummaryResult = helper.ValidationSummary(
            excludePropertyErrors: true,
            message: "Custom Message",
            htmlAttributes: new { attr = "value" });

        // Assert
        Assert.Equal(
            "<div attr=\"HtmlEncode[[value]]\" class=\"HtmlEncode[[validation-summary-errors]]\"><span>HtmlEncode[[Custom Message]]</span>" +
            Environment.NewLine +
            "<ul><li>HtmlEncode[[Error for root]]</li>" + Environment.NewLine +
            "</ul></div>",
            HtmlContentUtilities.HtmlContentToString(validationSummaryResult));
    }

    // Adds errors for various parts of the model, including the root.
    private void AddMultipleErrors(ModelStateDictionary modelState)
    {
        modelState.AddModelError("Property3.Property2", "This is an error for Property3.Property2.");
        modelState.AddModelError("Property3.OrderedProperty3", "This is an error for Property3.OrderedProperty3.");
        modelState.AddModelError("Property3.OrderedProperty2", "This is an error for Property3.OrderedProperty2.");
        modelState.SetModelValue("Property3.Empty", rawValue: null, attemptedValue: null);
        modelState.MarkFieldValid("Property3.Empty");

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForProperty(typeof(ValidationModel), nameof(ValidationModel.Property3));
        modelState.AddModelError("Property3", "This is an error for Property3.");
        modelState.AddModelError("Property3", new InvalidCastException("Exception will be ignored."), metadata);

        metadata = provider.GetMetadataForProperty(typeof(ValidationModel), nameof(ValidationModel.Property2));
        modelState.AddModelError("Property2", "This is an error for Property2.");
        modelState.AddModelError("Property2", "This is another error for Property2.");
        modelState.AddModelError("Property2", new OverflowException("Produces invalid value message"), metadata);

        metadata = provider.GetMetadataForType(typeof(ValidationModel));
        modelState.AddModelError(string.Empty, "This is an error for the model root.");
        modelState.AddModelError(string.Empty, "This is another error for the model root.");
        modelState.AddModelError(string.Empty, new InvalidOperationException("Another ignored Exception."), metadata);
    }

    // Adds one or more errors for all properties in OrderedModel. But adds errors out of order.
    private void AddOrderedErrors(ModelStateDictionary modelState)
    {
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForProperty(typeof(OrderedModel), nameof(OrderedModel.Property3));
        modelState.AddModelError("Property3", "This is an error for Property3.");
        modelState.AddModelError("Property3", new InvalidCastException("An ignored Exception."), metadata);

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

        modelState.SetModelValue("Empty", rawValue: null, attemptedValue: null);
        modelState.MarkFieldValid("Empty");
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

        // Exists to ensure #4989 does not regress. Issue specific to case where collection has a ModelStateEntry
        // but no element does.
        public byte[] Empty { get; set; }
    }

    private class ModelWithCollection
    {
        public string Property1 { get; set; }

        public List<OrderedModel> Property2 { get; set; }
    }

    private class CollectionType : Collection<OrderedModel>
    {
        [Display(Order = 1)]
        public string OrderedProperty2 { get; set; }

        [Display(Order = 2)]
        public string OrderedProperty1 { get; set; }
    }
}
