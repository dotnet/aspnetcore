// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Html;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class ValidationSummaryTagHelperTest
    {
        [Fact]
        public async Task ProcessAsync_GeneratesExpectedOutput()
        {
            // Arrange
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
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                expectedTagName,
                attributes: new TagHelperAttributeList
                {
                    { "class", "form-control" }
                },
                getChildContentAsync: useCachedResult =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent("Custom Content");

            Model model = null;
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            validationSummaryTagHelper.ViewContext = viewContext;

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
            Assert.Equal("Custom Content<ul><li style=\"display:none\"></li>" + Environment.NewLine + "</ul>",
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
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent(expectedPostContent);

            validationSummaryTagHelper.ViewContext = expectedViewContext;

            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
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
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent("Content of validation summary");

            var viewContext = CreateViewContext();
            validationSummaryTagHelper.ViewContext = viewContext;

            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
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
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent(expectedPostContent);

            var viewContext = CreateViewContext();
            validationSummaryTagHelper.ViewContext = viewContext;

            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
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
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent("Content of validation message");

            var viewContext = CreateViewContext();
            validationSummaryTagHelper.ViewContext = viewContext;

            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
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
            var expectedMessage =
                $@"The value of argument 'value' ({validationSummary}) is invalid for Enum type '{validationTypeName}'.
Parameter name: value";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                "value",
                () => { validationSummaryTagHelper.ValidationSummary = validationSummary; });
            Assert.Equal(expectedMessage, ex.Message);
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

        private class Model
        {
            public string Text { get; set; }
        }
    }
}