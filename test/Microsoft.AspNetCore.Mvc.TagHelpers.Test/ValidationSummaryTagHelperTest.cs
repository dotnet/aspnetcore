// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
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
        public async Task ProcessAsync_GeneratesExpectedOutput_WithNoErrors(
            ModelStateDictionary modelState)
        {
            // Arrange
            var expectedTagName = "not-div";
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);

            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
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
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider, modelState);
            var validationSummaryTagHelper = new ValidationSummaryTagHelper(htmlGenerator)
            {
                ValidationSummary = ValidationSummary.All,
                ViewContext = viewContext,
            };

            // Act
            await validationSummaryTagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(2, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("class"));
            Assert.Equal("form-control validation-summary-valid", attribute.Value);
            attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("data-valmsg-summary"));
            Assert.Equal("true", attribute.Value);
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(
                $"Custom Content<ul><li style=\"display:none\"></li>{Environment.NewLine}</ul>",
                output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
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
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
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
            Assert.Equal("form-control validation-summary-errors", attribute.Value);
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
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);

            var validationSummaryTagHelper = new ValidationSummaryTagHelper(htmlGenerator)
            {
                ValidationSummary = ValidationSummary.All,
            };

            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
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
            Assert.Equal(2, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("class"));
            Assert.Equal("form-control validation-summary-errors", attribute.Value);
            attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("data-valmsg-summary"));
            Assert.Equal("true", attribute.Value);
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
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
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
            tagBuilder.Attributes.Add("data-foo", "bar");
            tagBuilder.Attributes.Add("data-hello", "world");
            tagBuilder.Attributes.Add("anything", "something");

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
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            // Act
            await validationSummaryTagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("div", output.TagName);
            Assert.Equal(3, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("data-foo"));
            Assert.Equal("bar", attribute.Value);
            attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("data-hello"));
            Assert.Equal("world", attribute.Value);
            attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("anything"));
            Assert.Equal("something", attribute.Value);
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
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
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
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
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
                    new EmptyModelMetadataProvider()),
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
    }
}