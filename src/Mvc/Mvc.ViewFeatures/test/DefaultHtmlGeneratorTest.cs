// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class DefaultHtmlGeneratorTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetCurrentValues_WithEmptyViewData_ReturnsNull(bool allowMultiple)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: nameof(Model.Name),
            allowMultiple: allowMultiple);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetCurrentValues_WithNullExpressionResult_ReturnsNull(bool allowMultiple)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer,
            expression: nameof(Model.Name),
            allowMultiple: allowMultiple);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentValues_WithNullExpression_DoesNotThrow()
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

        // Act and Assert (does not throw).
        htmlGenerator.GetCurrentValues(viewContext, modelExplorer, expression: null, allowMultiple: true);
    }

    [Fact]
    public void GenerateSelect_WithNullExpression_Throws()
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

        var expected = "The name of an HTML field cannot be null or empty. Instead use methods " +
            "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
            "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

        // Act and Assert
        ExceptionAssert.ThrowsArgument(
            () => htmlGenerator.GenerateSelect(
                viewContext,
                modelExplorer,
                "label",
                expression: null,
                selectList: new List<SelectListItem>(),
                allowMultiple: true,
                htmlAttributes: null),
            "expression",
            expected);
    }

    [Fact]
    public void GenerateSelect_WithNullExpression_WithNameAttribute_DoesNotThrow()
    {
        // Arrange
        var expected = "-expression-";
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", expected },
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateSelect(
            viewContext,
            modelExplorer,
            "label",
            expression: null,
            selectList: new List<SelectListItem>(),
            allowMultiple: true,
            htmlAttributes: htmlAttributes);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes, a => a.Key == "name");
        Assert.Equal(expected, attribute.Value);
    }

    [Fact]
    public void GenerateTextArea_WithNullExpression_Throws()
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

        var expected = "The name of an HTML field cannot be null or empty. Instead use methods " +
            "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
            "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

        // Act and Assert
        ExceptionAssert.ThrowsArgument(
            () => htmlGenerator.GenerateTextArea(
                viewContext,
                modelExplorer,
                expression: null,
                rows: 1,
                columns: 1,
                htmlAttributes: null),
            "expression",
            expected);
    }

    [Fact]
    public void GenerateTextArea_WithNullExpression_WithNameAttribute_DoesNotThrow()
    {
        // Arrange
        var expected = "-expression-";
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", expected },
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateTextArea(
            viewContext,
            modelExplorer,
            expression: null,
            rows: 1,
            columns: 1,
            htmlAttributes: htmlAttributes);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes, a => a.Key == "name");
        Assert.Equal(expected, attribute.Value);
    }

    [Theory]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithMaxLength), ModelWithMaxLengthMetadata.MaxLengthAttributeValue)]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithStringLength), ModelWithMaxLengthMetadata.StringLengthAttributeValue)]
    public void GenerateTextArea_RendersMaxLength(string expression, int expectedValue)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<ModelWithMaxLengthMetadata>(model: null, metadataProvider: metadataProvider);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(ModelWithMaxLengthMetadata), expression);
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateTextArea(viewContext, modelExplorer, expression, rows: 1, columns: 1, htmlAttributes);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes, a => a.Key == "maxlength");
        Assert.Equal(expectedValue, int.Parse(attribute.Value, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithMaxLength), ModelWithMaxLengthMetadata.MaxLengthAttributeValue)]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithStringLength), ModelWithMaxLengthMetadata.StringLengthAttributeValue)]
    public void GeneratePassword_RendersMaxLength(string expression, int expectedValue)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<ModelWithMaxLengthMetadata>(model: null, metadataProvider: metadataProvider);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(ModelWithMaxLengthMetadata), expression);
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
            };

        // Act
        var tagBuilder = htmlGenerator.GeneratePassword(viewContext, modelExplorer, expression, null, htmlAttributes);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes, a => a.Key == "maxlength");
        Assert.Equal(expectedValue, int.Parse(attribute.Value, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithMaxLength), ModelWithMaxLengthMetadata.MaxLengthAttributeValue)]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithStringLength), ModelWithMaxLengthMetadata.StringLengthAttributeValue)]
    public void GenerateTextBox_RendersMaxLength(string expression, int expectedValue)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<ModelWithMaxLengthMetadata>(model: null, metadataProvider: metadataProvider);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(ModelWithMaxLengthMetadata), expression);
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateTextBox(viewContext, modelExplorer, expression, null, null, htmlAttributes);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes, a => a.Key == "maxlength");
        Assert.Equal(expectedValue, int.Parse(attribute.Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GenerateTextBox_RendersMaxLength_WithMinimumValueFromBothAttributes()
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<ModelWithMaxLengthMetadata>(model: null, metadataProvider: metadataProvider);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(ModelWithMaxLengthMetadata), nameof(ModelWithMaxLengthMetadata.FieldWithBothAttributes));
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateTextBox(viewContext, modelExplorer, nameof(ModelWithMaxLengthMetadata.FieldWithBothAttributes), null, null, htmlAttributes);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes, a => a.Key == "maxlength");
        Assert.Equal(Math.Min(ModelWithMaxLengthMetadata.MaxLengthAttributeValue, ModelWithMaxLengthMetadata.StringLengthAttributeValue), int.Parse(attribute.Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GenerateTextBox_DoesNotRenderMaxLength_WhenNoAttributesPresent()
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<ModelWithMaxLengthMetadata>(model: null, metadataProvider: metadataProvider);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(ModelWithMaxLengthMetadata), nameof(ModelWithMaxLengthMetadata.FieldWithoutAttributes));
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateTextBox(viewContext, modelExplorer, nameof(ModelWithMaxLengthMetadata.FieldWithoutAttributes), null, null, htmlAttributes);

        // Assert
        Assert.DoesNotContain(tagBuilder.Attributes, a => a.Key == "maxlength");
    }

    [Theory]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithMaxLength), ModelWithMaxLengthMetadata.MaxLengthAttributeValue)]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithStringLength), ModelWithMaxLengthMetadata.StringLengthAttributeValue)]
    public void GenerateTextBox_SearchType_RendersMaxLength(string expression, int expectedValue)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<ModelWithMaxLengthMetadata>(model: null, metadataProvider: metadataProvider);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(ModelWithMaxLengthMetadata), expression);
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
                { "type", "search"}
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateTextBox(viewContext, modelExplorer, expression, null, null, htmlAttributes);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes, a => a.Key == "maxlength");
        Assert.Equal(expectedValue, int.Parse(attribute.Value, CultureInfo.InvariantCulture));
    }

    // type, shouldUseInvariantFormatting, dateRenderingMode
    public static TheoryData<string, Html5DateRenderingMode, bool> GenerateTextBox_InvariantFormattingData
    {
        get
        {
            return new TheoryData<string, Html5DateRenderingMode, bool>
                {
                    {"text", Html5DateRenderingMode.Rfc3339, false },
                    {"number", Html5DateRenderingMode.Rfc3339, true },
                    {"range", Html5DateRenderingMode.Rfc3339, true },
                    {"date", Html5DateRenderingMode.Rfc3339, true },
                    {"datetime-local", Html5DateRenderingMode.Rfc3339, true },
                    {"month", Html5DateRenderingMode.Rfc3339, true },
                    {"time", Html5DateRenderingMode.Rfc3339, true },
                    {"week", Html5DateRenderingMode.Rfc3339 , true },
                    {"date", Html5DateRenderingMode.CurrentCulture, false },
                    {"datetime-local", Html5DateRenderingMode.CurrentCulture, false },
                    {"month", Html5DateRenderingMode.CurrentCulture, false },
                    {"time", Html5DateRenderingMode.CurrentCulture, false },
                    {"week", Html5DateRenderingMode.CurrentCulture, false },
                };
        }
    }

    [Theory]
    [MemberData(nameof(GenerateTextBox_InvariantFormattingData))]
    public void GenerateTextBox_UsesCultureInvariantFormatting_ForAppropriateTypes(string type, Html5DateRenderingMode dateRenderingMode, bool shouldUseInvariantFormatting)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlHelperOptions = new HtmlHelperOptions()
        {
            Html5DateRenderingMode = dateRenderingMode,
        };
        var htmlGenerator = GetGenerator(metadataProvider, new() { HtmlHelperOptions = htmlHelperOptions });
        var viewContext = GetViewContext<Model>(model: null, metadataProvider, htmlHelperOptions);
        var expression = nameof(Model.Name);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(Model), expression);
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
                { "type", type },
            };

        // Act
        _ = htmlGenerator.GenerateTextBox(viewContext, modelExplorer, expression, null, null, htmlAttributes);

        // Assert
        var didForceInvariantFormatting = viewContext.FormContext.InvariantField(expression);
        Assert.Equal(shouldUseInvariantFormatting, didForceInvariantFormatting);
    }

    [Theory]
    [MemberData(nameof(GenerateTextBox_InvariantFormattingData))]
    public void GenerateTextBox_AlwaysUsesCultureSpecificFormatting_WhenOptionIsSet(string type, Html5DateRenderingMode dateRenderingMode, bool shouldUseInvariantFormatting)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlHelperOptions = new HtmlHelperOptions()
        {
            Html5DateRenderingMode = dateRenderingMode,
            FormInputRenderMode = FormInputRenderMode.AlwaysUseCurrentCulture,
        };
        var htmlGenerator = GetGenerator(metadataProvider, new() { HtmlHelperOptions = htmlHelperOptions });
        var viewContext = GetViewContext<Model>(model: null, metadataProvider, htmlHelperOptions);
        var expression = nameof(Model.Name);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(Model), expression);
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
                { "type", type },
            };

        // Act
        _ = htmlGenerator.GenerateTextBox(viewContext, modelExplorer, expression, null, null, htmlAttributes);

        // Assert
        var didForceInvariantFormatting = viewContext.FormContext.InvariantField(expression);
        Assert.False(didForceInvariantFormatting);
    }

    [Theory]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithMaxLength))]
    [InlineData(nameof(ModelWithMaxLengthMetadata.FieldWithStringLength))]
    public void GenerateHidden_DoesNotRenderMaxLength(string expression)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<ModelWithMaxLengthMetadata>(model: null, metadataProvider: metadataProvider);
        var modelMetadata = metadataProvider.GetMetadataForProperty(typeof(ModelWithMaxLengthMetadata), expression);
        var modelExplorer = new ModelExplorer(metadataProvider, modelMetadata, null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "name", "testElement" },
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateHidden(viewContext, modelExplorer, expression, null, false, htmlAttributes);

        // Assert
        Assert.DoesNotContain(tagBuilder.Attributes, a => a.Key == "maxlength");
    }

    [Fact]
    public void GenerateValidationMessage_WithNullExpression_Throws()
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

        var expected = "The name of an HTML field cannot be null or empty. Instead use methods " +
            "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
            "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

        // Act and Assert
        ExceptionAssert.ThrowsArgument(
            () => htmlGenerator.GenerateValidationMessage(
                viewContext,
                modelExplorer: null,
                expression: null,
                message: "Message",
                tag: "tag",
                htmlAttributes: null),
            "expression",
            expected);
    }

    [Fact]
    public void GenerateValidationMessage_WithNullExpression_WithValidationForAttribute_DoesNotThrow()
    {
        // Arrange
        var expected = "-expression-";
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);
        var htmlAttributes = new Dictionary<string, object>
            {
                { "data-valmsg-for", expected },
            };

        // Act
        var tagBuilder = htmlGenerator.GenerateValidationMessage(
            viewContext,
            modelExplorer: null,
            expression: null,
            message: "Message",
            tag: "tag",
            htmlAttributes: htmlAttributes);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes, a => a.Key == "data-valmsg-for");
        Assert.Equal(expected, attribute.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetCurrentValues_WithSelectListInViewData_ReturnsNull(bool allowMultiple)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.ViewData[nameof(Model.Name)] = Enumerable.Empty<SelectListItem>();

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: nameof(Model.Name),
            allowMultiple: allowMultiple);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("some string")] // treated as if it were not IEnumerable
    [InlineData(23)]
    [InlineData(RegularEnum.Three)]
    public void GetCurrentValues_AllowMultipleWithNonEnumerableInViewData_Throws(object value)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.ViewData[nameof(Model.Name)] = value;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: nameof(Model.Name),
            allowMultiple: true));
        Assert.Equal(
            "The parameter 'expression' must evaluate to an IEnumerable when multiple selection is allowed.",
            exception.Message);
    }

    // rawValue, allowMultiple -> expected current values
    public static TheoryData<string[], bool, IReadOnlyCollection<string>> GetCurrentValues_CollectionData
    {
        get
        {
            return new TheoryData<string[], bool, IReadOnlyCollection<string>>
                {
                    // ModelStateDictionary converts arrays to single values if needed.
                    { new [] { "some string" }, false, new [] { "some string" } },
                    { new [] { "some string" }, true, new [] { "some string" } },
                    { new [] { "some string", "some other string" }, false, new [] { "some string" } },
                    {
                        new [] { "some string", "some other string" },
                        true,
                        new [] { "some string", "some other string" }
                    },
                    // { new string[] { null }, false, null } would fall back to other sources.
                    { new string[] { null }, true, new [] { string.Empty } },
                    { new [] { string.Empty }, false, new [] { string.Empty } },
                    { new [] { string.Empty }, true, new [] { string.Empty } },
                    {
                        new [] { null, "some string", "some other string" },
                        true,
                        new [] { string.Empty, "some string", "some other string" }
                    },
                    // ignores duplicates
                    {
                        new [] { null, "some string", null, "some other string", null, "some string", null },
                        true,
                        new [] { string.Empty, "some string", "some other string" }
                    },
                    // ignores case of duplicates
                    {
                        new [] { "some string", "SoMe StriNg", "Some String", "soME STRing", "SOME STRING" },
                        true,
                        new [] { "some string" }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_CollectionData))]
    public void GetCurrentValues_WithModelStateEntryAndViewData_ReturnsModelStateEntry(
        string[] rawValue,
        bool allowMultiple,
        IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var model = new Model { Name = "ignored property value" };

        var viewContext = GetViewContext<Model>(model, metadataProvider);
        viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";
        viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: nameof(Model.Name),
            allowMultiple: allowMultiple);

        // Assert
        Assert.NotNull(result);
        Assert.Equal<string>(expected, result);
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_CollectionData))]
    public void GetCurrentValues_WithModelStateEntryModelExplorerAndViewData_ReturnsModelStateEntry(
        string[] rawValue,
        bool allowMultiple,
        IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var model = new Model { Name = "ignored property value" };

        var viewContext = GetViewContext<Model>(model, metadataProvider);
        viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";
        viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), "ignored model value");

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer,
            expression: nameof(Model.Name),
            allowMultiple: allowMultiple);

        // Assert
        Assert.NotNull(result);
        Assert.Equal<string>(expected, result);
    }

    // rawValue -> expected current values
    public static TheoryData<string[], string[]> GetCurrentValues_StringData
    {
        get
        {
            return new TheoryData<string[], string[]>
                {
                    // 1. If given a ModelExplorer, GetCurrentValues does not use ViewData even if expression result is
                    // null.
                    // 2. Otherwise if ViewData entry exists, GetCurrentValue does not fall back to ViewData.Model even
                    // if entry is null.
                    // 3. Otherwise, GetCurrentValue does not fall back anywhere else even if ViewData.Model is null.
                    { null, null },
                    { new string[] { string.Empty }, new [] { string.Empty } },
                    { new string[] { "some string" }, new [] { "some string" } },
                };
        }
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_StringData))]
    public void GetCurrentValues_WithModelExplorerAndViewData_ReturnsExpressionResult(
        string[] rawValue,
        IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var model = new Model { Name = "ignored property value" };

        var viewContext = GetViewContext<Model>(model, metadataProvider);
        viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";
        viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), rawValue);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer,
            expression: nameof(Model.Name),
            allowMultiple: false);

        // Assert
        Assert.Equal<string>(expected, result);
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_StringData))]
    public void GetCurrentValues_WithViewData_ReturnsViewDataEntry(
        string[] rawValue,
        IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var model = new Model { Name = "ignored property value" };

        var viewContext = GetViewContext<Model>(model, metadataProvider);
        viewContext.ViewData[nameof(Model.Name)] = rawValue;
        viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: nameof(Model.Name),
            allowMultiple: false);

        // Assert
        Assert.Equal<string>(expected, result);
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_StringData))]
    public void GetCurrentValues_WithModel_ReturnsModel(string[] rawValue, IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var model = new Model { Name = rawValue?[0] };

        var viewContext = GetViewContext<Model>(model, metadataProvider);
        viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: nameof(Model.Name),
            allowMultiple: false);

        // Assert
        Assert.Equal<string>(expected, result);
    }

    // rawValue -> expected current values
    public static TheoryData<string[], string[]> GetCurrentValues_StringCollectionData
    {
        get
        {
            return new TheoryData<string[], string[]>
                {
                    { new string[] { null }, new [] { string.Empty } },
                    { new [] { string.Empty }, new [] { string.Empty } },
                    { new [] { "some string" }, new [] { "some string" } },
                    { new [] { "some string", "some other string" }, new [] { "some string", "some other string" } },
                    {
                        new [] { null, "some string", "some other string" },
                        new [] { string.Empty, "some string", "some other string" }
                    },
                    // ignores duplicates
                    {
                        new [] { null, "some string", null, "some other string", null, "some string", null },
                        new [] { string.Empty, "some string", "some other string" }
                    },
                    // ignores case of duplicates
                    {
                        new [] { "some string", "SoMe StriNg", "Some String", "soME STRing", "SOME STRING" },
                        new [] { "some string" }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_StringCollectionData))]
    public void GetCurrentValues_CollectionWithModelExplorerAndViewData_ReturnsExpressionResult(
        string[] rawValue,
        IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var model = new Model { Collection = { "ignored property value" } };

        var viewContext = GetViewContext<Model>(model, metadataProvider);
        viewContext.ViewData[nameof(Model.Collection)] = new[] { "ignored ViewData value" };
        viewContext.ModelState.SetModelValue(nameof(Model.Collection), rawValue, attemptedValue: null);

        var modelExplorer =
            metadataProvider.GetModelExplorerForType(typeof(List<string>), new List<string>(rawValue));

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer,
            expression: nameof(Model.Collection),
            allowMultiple: true);

        // Assert
        Assert.Equal<string>(expected, result);
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_StringCollectionData))]
    public void GetCurrentValues_CollectionWithViewData_ReturnsViewDataEntry(
        string[] rawValue,
        IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var model = new Model { Collection = { "ignored property value" } };

        var viewContext = GetViewContext<Model>(model, metadataProvider);
        viewContext.ViewData[nameof(Model.Collection)] = rawValue;
        viewContext.ModelState.SetModelValue(nameof(Model.Collection), rawValue, attemptedValue: null);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: nameof(Model.Collection),
            allowMultiple: true);

        // Assert
        Assert.Equal<string>(expected, result);
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_StringCollectionData))]
    public void GetCurrentValues_CollectionWithModel_ReturnsModel(
        string[] rawValue,
        IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var model = new Model();
        model.Collection.AddRange(rawValue);

        var viewContext = GetViewContext<Model>(model, metadataProvider);
        viewContext.ModelState.SetModelValue(
            nameof(Model.Collection),
            rawValue,
            attemptedValue: null);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: nameof(Model.Collection),
            allowMultiple: true);

        // Assert
        Assert.Equal<string>(expected, result);
    }

    // property name, rawValue -> expected current values
    public static TheoryData<string, object, string[]> GetCurrentValues_ValueToConvertData
    {
        get
        {
            return new TheoryData<string, object, string[]>
                {
                    { nameof(Model.FlagsEnum), FlagsEnum.All, new [] { "-1", "All" } },
                    { nameof(Model.FlagsEnum), FlagsEnum.FortyTwo, new [] { "42", "FortyTwo" } },
                    { nameof(Model.FlagsEnum), FlagsEnum.None, new [] { "0", "None" } },
                    { nameof(Model.FlagsEnum), FlagsEnum.Two, new [] { "2", "Two" } },
                    { nameof(Model.FlagsEnum), string.Empty, new [] { string.Empty } },
                    { nameof(Model.FlagsEnum), "All", new [] { "-1", "All" } },
                    { nameof(Model.FlagsEnum), "FortyTwo", new [] { "42", "FortyTwo" } },
                    { nameof(Model.FlagsEnum), "None", new [] { "0", "None" } },
                    { nameof(Model.FlagsEnum), "Two", new [] { "2", "Two" } },
                    { nameof(Model.FlagsEnum), "Two, Four", new [] { "Two, Four", "6" } },
                    { nameof(Model.FlagsEnum), "garbage", new [] { "garbage" } },
                    { nameof(Model.FlagsEnum), "0", new [] { "0", "None" } },
                    { nameof(Model.FlagsEnum), "   43", new [] { "   43", "43" } },
                    { nameof(Model.FlagsEnum), "-5   ", new [] { "-5   ", "-5" } },
                    { nameof(Model.FlagsEnum), 0, new [] { "0", "None" } },
                    { nameof(Model.FlagsEnum), 1, new [] { "1", "One" } },
                    { nameof(Model.FlagsEnum), 43, new [] { "43" } },
                    { nameof(Model.FlagsEnum), -5, new [] { "-5" } },
                    { nameof(Model.FlagsEnum), int.MaxValue, new [] { "2147483647" } },
                    { nameof(Model.FlagsEnum), (uint)int.MaxValue + 1, new [] { "2147483648" } },
                    { nameof(Model.FlagsEnum), uint.MaxValue, new [] { "4294967295" } },  // converted to string & used

                    { nameof(Model.Id), string.Empty, new [] { string.Empty } },
                    { nameof(Model.Id), "garbage", new [] { "garbage" } },                  // no compatibility checks
                    { nameof(Model.Id), "0", new [] { "0" } },
                    { nameof(Model.Id), "  43", new [] { "  43" } },
                    { nameof(Model.Id), "-5  ", new [] { "-5  " } },
                    { nameof(Model.Id), 0, new [] { "0" } },
                    { nameof(Model.Id), 1, new [] { "1" } },
                    { nameof(Model.Id), 43, new [] { "43" } },
                    { nameof(Model.Id), -5, new [] { "-5" } },
                    { nameof(Model.Id), int.MaxValue, new [] { "2147483647" } },
                    { nameof(Model.Id), (uint)int.MaxValue + 1, new [] { "2147483648" } },  // no limit checks
                    { nameof(Model.Id), uint.MaxValue, new [] { "4294967295" } },           // no limit checks

                    { nameof(Model.NullableEnum), RegularEnum.Zero, new [] { "0", "Zero" } },
                    { nameof(Model.NullableEnum), RegularEnum.One, new [] { "1", "One" } },
                    { nameof(Model.NullableEnum), RegularEnum.Two, new [] { "2", "Two" } },
                    { nameof(Model.NullableEnum), RegularEnum.Three, new [] { "3", "Three" } },
                    { nameof(Model.NullableEnum), string.Empty, new [] { string.Empty } },
                    { nameof(Model.NullableEnum), "Zero", new [] { "0", "Zero" } },
                    { nameof(Model.NullableEnum), "Two", new [] { "2", "Two" } },
                    { nameof(Model.NullableEnum), "One, Two", new [] { "One, Two", "3", "Three" } },
                    { nameof(Model.NullableEnum), "garbage", new [] { "garbage" } },
                    { nameof(Model.NullableEnum), "0", new [] { "0", "Zero" } },
                    { nameof(Model.NullableEnum), "   43", new [] { "   43", "43" } },
                    { nameof(Model.NullableEnum), "-5   ", new [] { "-5   ", "-5" } },
                    { nameof(Model.NullableEnum), 0, new [] { "0", "Zero" } },
                    { nameof(Model.NullableEnum), 1, new [] { "1", "One" } },
                    { nameof(Model.NullableEnum), 43, new [] { "43" } },
                    { nameof(Model.NullableEnum), -5, new [] { "-5" } },
                    { nameof(Model.NullableEnum), int.MaxValue, new [] { "2147483647" } },
                    { nameof(Model.NullableEnum), (uint)int.MaxValue + 1, new [] { "2147483648" } },
                    { nameof(Model.NullableEnum), uint.MaxValue, new [] { "4294967295" } },

                    { nameof(Model.RegularEnum), RegularEnum.Zero, new [] { "0", "Zero" } },
                    { nameof(Model.RegularEnum), RegularEnum.One, new [] { "1", "One" } },
                    { nameof(Model.RegularEnum), RegularEnum.Two, new [] { "2", "Two" } },
                    { nameof(Model.RegularEnum), RegularEnum.Three, new [] { "3", "Three" } },
                    { nameof(Model.RegularEnum), string.Empty, new [] { string.Empty } },
                    { nameof(Model.RegularEnum), "Zero", new [] { "0", "Zero" } },
                    { nameof(Model.RegularEnum), "Two", new [] { "2", "Two" } },
                    { nameof(Model.RegularEnum), "One, Two", new [] { "One, Two", "3", "Three" } },
                    { nameof(Model.RegularEnum), "garbage", new [] { "garbage" } },
                    { nameof(Model.RegularEnum), "0", new [] { "0", "Zero" } },
                    { nameof(Model.RegularEnum), "   43", new [] { "   43", "43" } },
                    { nameof(Model.RegularEnum), "-5   ", new [] { "-5   ", "-5" } },
                    { nameof(Model.RegularEnum), 0, new [] { "0", "Zero" } },
                    { nameof(Model.RegularEnum), 1, new [] { "1", "One" } },
                    { nameof(Model.RegularEnum), 43, new [] { "43" } },
                    { nameof(Model.RegularEnum), -5, new [] { "-5" } },
                    { nameof(Model.RegularEnum), int.MaxValue, new [] { "2147483647" } },
                    { nameof(Model.RegularEnum), (uint)int.MaxValue + 1, new [] { "2147483648" } },
                    { nameof(Model.RegularEnum), uint.MaxValue, new [] { "4294967295" } },
                };
        }
    }

    [Theory]
    [MemberData(nameof(GetCurrentValues_ValueToConvertData))]
    public void GetCurrentValues_ValueConvertedAsExpected(
        string propertyName,
        object rawValue,
        IReadOnlyCollection<string> expected)
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.ModelState.SetModelValue(
            propertyName,
            new string[] { rawValue.ToString() },
            attemptedValue: null);

        // Act
        var result = htmlGenerator.GetCurrentValues(
            viewContext,
            modelExplorer: null,
            expression: propertyName,
            allowMultiple: false);

        // Assert
        Assert.Equal<string>(expected, result);
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(false, "<input name=\"formFieldName\" type=\"hidden\" value=\"requestToken\" />")]
    public void GenerateAntiforgery_GeneratesAntiforgeryFieldsOnlyIfRequired(
        bool hasAntiforgeryToken,
        string expectedAntiforgeryHtmlField)
    {
        // Arrange
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.FormContext.CanRenderAtEndOfForm = true;
        viewContext.FormContext.HasAntiforgeryToken = hasAntiforgeryToken;

        // Act
        var result = htmlGenerator.GenerateAntiforgery(viewContext);

        // Assert
        var antiforgeryField = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
        Assert.Equal(expectedAntiforgeryHtmlField, antiforgeryField);
    }

    // This test covers use of the helper within literal <form> tags when tag helpers are not enabled e.g.
    // <form action="/Home/Create">
    //     @Html.AntiForgeryToken()
    // </form>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateAntiforgery_AlwaysGeneratesAntiforgeryToken_IfCannotRenderAtEnd(bool hasAntiforgeryToken)
    {
        // Arrange
        var expected = "<input name=\"formFieldName\" type=\"hidden\" value=\"requestToken\" />";
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var htmlGenerator = GetGenerator(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.FormContext.HasAntiforgeryToken = hasAntiforgeryToken;

        // Act
        var result = htmlGenerator.GenerateAntiforgery(viewContext);

        // Assert
        var antiforgeryField = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
        Assert.Equal(expected, antiforgeryField);
    }

    // GetCurrentValues uses only the IModelMetadataProvider passed to the DefaultHtmlGenerator constructor.
    private static IHtmlGenerator GetGenerator(IModelMetadataProvider metadataProvider, MvcViewOptions options = default)
    {
        var mvcViewOptionsAccessor = new Mock<IOptions<MvcViewOptions>>();
        mvcViewOptionsAccessor.SetupGet(accessor => accessor.Value).Returns(options ?? new MvcViewOptions());

        var htmlEncoder = Mock.Of<HtmlEncoder>();
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(mock => mock.GetAndStoreTokens(It.IsAny<DefaultHttpContext>()))
            .Returns(() =>
            {
                return new AntiforgeryTokenSet("requestToken", "cookieToken", "formFieldName", "headerName");
            });

        var attributeProvider = new DefaultValidationHtmlAttributeProvider(
            mvcViewOptionsAccessor.Object,
            metadataProvider,
            new ClientValidatorCache());

        return new DefaultHtmlGenerator(
            antiforgery.Object,
            mvcViewOptionsAccessor.Object,
            metadataProvider,
            new UrlHelperFactory(),
            htmlEncoder,
            attributeProvider);
    }

    // GetCurrentValues uses only the ModelStateDictionary and ViewDataDictionary from the passed ViewContext.
    private static ViewContext GetViewContext<TModel>(TModel model, IModelMetadataProvider metadataProvider, HtmlHelperOptions options = default)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary<TModel>(metadataProvider, actionContext.ModelState)
        {
            Model = model,
        };

        return new ViewContext(
            actionContext,
            Mock.Of<IView>(),
            viewData,
            Mock.Of<ITempDataDictionary>(),
            TextWriter.Null,
            options ?? new HtmlHelperOptions());
    }

    public enum RegularEnum
    {
        Zero,
        One,
        Two,
        Three,
    }

    public enum FlagsEnum
    {
        None = 0,
        One = 1,
        Two = 2,
        Four = 4,
        FortyTwo = 42,
        All = -1,
    }

    private class ModelWithMaxLengthMetadata
    {
        internal const int MaxLengthAttributeValue = 77;
        internal const int StringLengthAttributeValue = 7;

        [MaxLength(MaxLengthAttributeValue)]
        public string FieldWithMaxLength { get; set; }

        [StringLength(StringLengthAttributeValue)]
        public string FieldWithStringLength { get; set; }

        public string FieldWithoutAttributes { get; set; }

        [MaxLength(MaxLengthAttributeValue)]
        [StringLength(StringLengthAttributeValue)]
        public string FieldWithBothAttributes { get; set; }
    }

    private class Model
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public RegularEnum RegularEnum { get; set; }

        public FlagsEnum FlagsEnum { get; set; }

        public RegularEnum? NullableEnum { get; set; }

        public List<string> Collection { get; } = new List<string>();
    }
}
