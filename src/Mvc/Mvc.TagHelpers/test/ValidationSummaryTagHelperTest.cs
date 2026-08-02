// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class ValidationSummaryTagHelperTest
{
    public static TheoryData<ModelStateDictionary> ProcessAsync_GeneratesExpectedOutput_WithNoErrorsData
    {
        get
        {
            var emptyModelState = new ModelStateDictionary();

            var modelState = new ModelStateDictionary();
            SetValidModelState(modelState);

            return new TheoryData<ModelStateDictionary>
                {
                    emptyModelState,
                    modelState,
                };
        }
    }

    [Theory]
    [MemberData(nameof(ProcessAsync_GeneratesExpectedOutput_WithNoErrorsData))]
    public async Task ProcessAsync_GeneratesExpectedOutput_WithNoErrors(ModelStateDictionary modelState)
    {
        // Arrange
        var expectedTagName = "not-div";
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var expectedAttributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("class", "form-control validation-summary-valid"),
                new TagHelperAttribute("data-valmsg-summary", "true"),
            };

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var tagHelperContext = new TagHelperContext(
            tagName: "not-div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList
            {
                    { "class", "form-control" }
            },
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                throw new InvalidOperationException("getChildContentAsync called unexpectedly");
            });
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent("Custom Content");

        var model = new Model();
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider, modelState);
        var validationSummaryTagHelper = new ValidationSummaryTagHelper(htmlGenerator)
        {
            ValidationSummary = ValidationSummary.All,
            ViewContext = viewContext,
        };

        // Act
        await validationSummaryTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(expectedAttributes, output.Attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(
            $"Custom Content<ul><li style=\"display:none\"></li>{Environment.NewLine}</ul>",
            output.PostContent.GetContent());
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Theory]
    [MemberData(nameof(ProcessAsync_GeneratesExpectedOutput_WithNoErrorsData))]
    public async Task ProcessAsync_SuppressesOutput_IfClientSideValidationDisabled_WithNoErrorsData(
        ModelStateDictionary modelStateDictionary)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var viewContext = CreateViewContext();
        viewContext.ClientValidationEnabled = false;

        var validationSummaryTagHelper = new ValidationSummaryTagHelper(htmlGenerator)
        {
            ValidationSummary = ValidationSummary.All,
            ViewContext = viewContext,
        };

        var output = new TagHelperOutput(
            "div",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                throw new InvalidOperationException("getChildContentAsync called unexpectedly.");
            });

        var context = new TagHelperContext(
            tagName: "div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        await validationSummaryTagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.Empty(output.Attributes);
        Assert.Empty(HtmlContentUtilities.HtmlContentToString(output));
    }

    public static TheoryData<string, ModelStateDictionary> ProcessAsync_SuppressesOutput_IfModelOnlyWithNoModelErrorData
    {
        get
        {
            var emptyModelState = new ModelStateDictionary();

            var modelState = new ModelStateDictionary();
            SetValidModelState(modelState);

            var invalidModelState = new ModelStateDictionary();
            SetValidModelState(invalidModelState);
            invalidModelState.AddModelError($"{nameof(Model.Strings)}[1]", "This value is invalid.");

            return new TheoryData<string, ModelStateDictionary>
                {
                    { string.Empty, emptyModelState },
                    { string.Empty, modelState },
                    { nameof(Model.Text), modelState },
                    { "not-a-key", modelState },
                    { string.Empty, invalidModelState },
                    { $"{nameof(Model.Strings)}[2]", invalidModelState },
                    { nameof(Model.Text), invalidModelState },
                    { "not-a-key", invalidModelState },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ProcessAsync_SuppressesOutput_IfModelOnlyWithNoModelErrorData))]
    public async Task ProcessAsync_SuppressesOutput_IfModelOnly_WithNoModelError(
        string prefix,
        ModelStateDictionary modelStateDictionary)
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var viewContext = CreateViewContext();
        viewContext.ViewData.TemplateInfo.HtmlFieldPrefix = prefix;

        var validationSummaryTagHelper = new ValidationSummaryTagHelper(htmlGenerator)
        {
            ValidationSummary = ValidationSummary.ModelOnly,
            ViewContext = viewContext,
        };

        var output = new TagHelperOutput(
            "div",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                throw new InvalidOperationException("getChildContentAsync called unexpectedly.");
            });

        var context = new TagHelperContext(
            tagName: "div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        await validationSummaryTagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.Empty(output.Attributes);
        Assert.Empty(HtmlContentUtilities.HtmlContentToString(output));
    }

    [Theory]
    [InlineData(ValidationSummary.All)]
    [InlineData(ValidationSummary.ModelOnly)]
    public async Task ProcessAsync_GeneratesExpectedOutput_WithModelError(ValidationSummary validationSummary)
    {
        // Arrange
        var expectedError = "I am an error.";
        var expectedTagName = "not-div";
        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);

        var validationSummaryTagHelper = new ValidationSummaryTagHelper(htmlGenerator)
        {
            ValidationSummary = validationSummary,
        };

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var tagHelperContext = new TagHelperContext(
            tagName: "not-div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList
            {
                    { "class", "form-control" }
            },
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent("Custom Content");

        var model = new Model();
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        validationSummaryTagHelper.ViewContext = viewContext;

        var modelState = viewContext.ModelState;
        SetValidModelState(modelState);
        modelState.AddModelError(string.Empty, expectedError);

        // Act
        await validationSummaryTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.InRange(output.Attributes.Count, low: 1, high: 2);
        var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("class"));
        Assert.Equal(
            new TagHelperAttribute("class", "form-control validation-summary-errors"),
            attribute,
            CaseSensitiveTagHelperAttributeComparer.Default);

        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(
            $"Custom Content<ul><li>{expectedError}</li>{Environment.NewLine}</ul>",
            output.PostContent.GetContent());
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_GeneratesExpectedOutput_WithPropertyErrors()
    {
        // Arrange
        var expectedError0 = "I am an error.";
        var expectedError2 = "I am also an error.";
        var expectedTagName = "not-div";
        var expectedAttributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("class", "form-control validation-summary-errors"),
                new TagHelperAttribute("data-valmsg-summary", "true"),
            };

        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var validationSummaryTagHelper = new ValidationSummaryTagHelper(htmlGenerator)
        {
            ValidationSummary = ValidationSummary.All,
        };

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var tagHelperContext = new TagHelperContext(
            tagName: "not-div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList
            {
                    { "class", "form-control" }
            },
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent("Custom Content");

        var model = new Model();
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        validationSummaryTagHelper.ViewContext = viewContext;

        var modelState = viewContext.ModelState;
        SetValidModelState(modelState);
        modelState.AddModelError(key: $"{nameof(Model.Strings)}[0]", errorMessage: expectedError0);
        modelState.AddModelError(key: $"{nameof(Model.Strings)}[2]", errorMessage: expectedError2);

        // Act
        await validationSummaryTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(expectedAttributes, output.Attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(
            $"Custom Content<ul><li>{expectedError0}</li>{Environment.NewLine}" +
            $"<li>{expectedError2}</li>{Environment.NewLine}</ul>",
            output.PostContent.GetContent());
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Theory]
    [InlineData(ValidationSummary.All, false)]
    [InlineData(ValidationSummary.ModelOnly, true)]
    public async Task ProcessAsync_CallsIntoGenerateValidationSummaryWithExpectedParameters(
        ValidationSummary validationSummary,
        bool expectedExcludePropertyErrors)
    {
        // Arrange
        var expectedViewContext = CreateViewContext();

        var generator = new Mock<IHtmlGenerator>();
        generator
            .Setup(mock => mock.GenerateValidationSummary(
                expectedViewContext,
                expectedExcludePropertyErrors,
                null,   // message
                null,   // headerTag
                null))  // htmlAttributes
            .Returns(new TagBuilder("div"))
            .Verifiable();

        var validationSummaryTagHelper = new ValidationSummaryTagHelper(generator.Object)
        {
            ValidationSummary = validationSummary,
        };

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var expectedPostContent = "original post-content";
        var output = new TagHelperOutput(
            tagName: "div",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent(expectedPostContent);

        validationSummaryTagHelper.ViewContext = expectedViewContext;

        var context = new TagHelperContext(
            tagName: "div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        await validationSummaryTagHelper.ProcessAsync(context, output);

        generator.Verify();
        Assert.Equal("div", output.TagName);
        Assert.Empty(output.Attributes);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(expectedPostContent, output.PostContent.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_MergesTagBuilderFromGenerateValidationSummary()
    {
        // Arrange
        var tagBuilder = new TagBuilder("span2");
        tagBuilder.InnerHtml.SetHtmlContent("New HTML");
        tagBuilder.Attributes.Add("anything", "something");
        tagBuilder.Attributes.Add("data-foo", "bar");
        tagBuilder.Attributes.Add("data-hello", "world");

        var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        generator
            .Setup(mock => mock.GenerateValidationSummary(
                It.IsAny<ViewContext>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(tagBuilder);

        var validationSummaryTagHelper = new ValidationSummaryTagHelper(generator.Object)
        {
            ValidationSummary = ValidationSummary.ModelOnly,
        };

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var output = new TagHelperOutput(
            tagName: "div",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent("Content of validation summary");

        var viewContext = CreateViewContext();
        validationSummaryTagHelper.ViewContext = viewContext;

        var context = new TagHelperContext(
            tagName: "div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        await validationSummaryTagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("div", output.TagName);
        Assert.Collection(
            output.Attributes,
            attribute =>
            {
                Assert.Equal("anything", attribute.Name);
                Assert.Equal("something", attribute.Value);
            },
            attribute =>
            {
                Assert.Equal("data-foo", attribute.Name);
                Assert.Equal("bar", attribute.Value);
            },
            attribute =>
            {
                Assert.Equal("data-hello", attribute.Name);
                Assert.Equal("world", attribute.Value);
            });
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal("Content of validation summaryNew HTML", output.PostContent.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_DoesNothingIfValidationSummaryNone()
    {
        // Arrange
        var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);

        var validationSummaryTagHelper = new ValidationSummaryTagHelper(generator.Object)
        {
            ValidationSummary = ValidationSummary.None,
        };

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var expectedPostContent = "original post-content";
        var output = new TagHelperOutput(
            tagName: "div",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent(expectedPostContent);

        var viewContext = CreateViewContext();
        validationSummaryTagHelper.ViewContext = viewContext;

        var context = new TagHelperContext(
            tagName: "div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        await validationSummaryTagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("div", output.TagName);
        Assert.Empty(output.Attributes);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(expectedPostContent, output.PostContent.GetContent());
    }

    [Theory]
    [InlineData(ValidationSummary.All)]
    [InlineData(ValidationSummary.ModelOnly)]
    public async Task ProcessAsync_GeneratesValidationSummaryWhenNotNone(ValidationSummary validationSummary)
    {
        // Arrange
        var tagBuilder = new TagBuilder("span2");
        tagBuilder.InnerHtml.SetHtmlContent("New HTML");

        var generator = new Mock<IHtmlGenerator>();
        generator
            .Setup(mock => mock.GenerateValidationSummary(
                It.IsAny<ViewContext>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(tagBuilder)
            .Verifiable();

        var validationSummaryTagHelper = new ValidationSummaryTagHelper(generator.Object)
        {
            ValidationSummary = validationSummary,
        };

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var output = new TagHelperOutput(
            tagName: "div",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent("Content of validation message");

        var viewContext = CreateViewContext();
        validationSummaryTagHelper.ViewContext = viewContext;

        var context = new TagHelperContext(
            tagName: "div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        await validationSummaryTagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("div", output.TagName);
        Assert.Empty(output.Attributes);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal("Content of validation messageNew HTML", output.PostContent.GetContent());
        generator.Verify();
    }

    [Theory]
    [InlineData((ValidationSummary)(-1))]
    [InlineData((ValidationSummary)23)]
    [InlineData(ValidationSummary.All | ValidationSummary.ModelOnly)]
    [ReplaceCulture]
    public void ValidationSummaryProperty_ThrowsWhenSetToInvalidValidationSummaryValue(
        ValidationSummary validationSummary)
    {
        // Arrange
        var generator = new TestableHtmlGenerator(new EmptyModelMetadataProvider());

        var validationSummaryTagHelper = new ValidationSummaryTagHelper(generator);
        var validationTypeName = typeof(ValidationSummary).FullName;
        var expectedMessage = $"The value of argument 'value' ({validationSummary}) is invalid for Enum type '{validationTypeName}'.";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => validationSummaryTagHelper.ValidationSummary = validationSummary,
            "value",
            expectedMessage);
    }

    [Fact]
    public async Task ProcessAsync_GeneratesExpectedOutput_WithModelErrorForIEnumerable()
    {
        // Arrange
        var expectedError = "Something went wrong.";
        var expectedTagName = "not-div";
        var expectedAttributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("class", "form-control validation-summary-errors"),
                new TagHelperAttribute("data-valmsg-summary", "true"),
            };

        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var validationSummaryTagHelper = new ValidationSummaryTagHelper(htmlGenerator)
        {
            ValidationSummary = ValidationSummary.All,
        };

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var tagHelperContext = new TagHelperContext(
            tagName: "not-div",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList
            {
                    { "class", "form-control" }
            },
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent("Custom Content");

        var model = new FormMetadata();
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        validationSummaryTagHelper.ViewContext = viewContext;

        viewContext.ModelState.AddModelError(key: nameof(FormMetadata.ID), errorMessage: expectedError);

        // Act
        await validationSummaryTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(expectedAttributes, output.Attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(
            $"Custom Content<ul><li>{expectedError}</li>{Environment.NewLine}</ul>",
            output.PostContent.GetContent());
        Assert.Equal(expectedTagName, output.TagName);
    }

    private static ViewContext CreateViewContext()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ViewContext(
            actionContext,
            Mock.Of<IView>(),
            new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary()),
            Mock.Of<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());
    }

    private static void SetValidModelState(ModelStateDictionary modelState)
    {
        modelState.SetModelValue(key: nameof(Model.Empty), rawValue: null, attemptedValue: null);
        modelState.SetModelValue(key: $"{nameof(Model.Strings)}[0]", rawValue: null, attemptedValue: null);
        modelState.SetModelValue(key: $"{nameof(Model.Strings)}[1]", rawValue: null, attemptedValue: null);
        modelState.SetModelValue(key: $"{nameof(Model.Strings)}[2]", rawValue: null, attemptedValue: null);
        modelState.SetModelValue(key: nameof(Model.Text), rawValue: null, attemptedValue: null);

        foreach (var key in modelState.Keys)
        {
            modelState.MarkFieldValid(key);
        }
    }

    private class Model
    {
        public string Text { get; set; }

        public string[] Strings { get; set; }

        // Exists to ensure #4989 does not regress. Issue specific to case where collection has a ModelStateEntry
        // but no element does.
        public byte[] Empty { get; set; }
    }

    private class FormMetadata : IEnumerable<Model>
    {
        private readonly List<Model> _fields = new List<Model>();

        public int ID { get; set; }

        public IEnumerator<Model> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fields.GetEnumerator();
        }
    }
}
