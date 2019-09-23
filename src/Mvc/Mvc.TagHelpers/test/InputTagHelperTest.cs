// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers.Testing;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class InputTagHelperTest
    {
        public static TheoryData<TagHelperAttributeList, string> MultiAttributeCheckBoxData
        {
            get
            {
                // outputAttributes, expectedAttributeString
                return new TheoryData<TagHelperAttributeList, string>
                {
                    {
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" }
                        },
                        "hello=\"HtmlEncode[[world]]\" hello=\"HtmlEncode[[world2]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" },
                            { "hello", "world3" }
                        },
                        "hello=\"HtmlEncode[[world]]\" hello=\"HtmlEncode[[world2]]\" hello=\"HtmlEncode[[world3]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "HelLO", "world" },
                            { "HELLO", "world2" }
                        },
                        "HelLO=\"HtmlEncode[[world]]\" HELLO=\"HtmlEncode[[world2]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "Hello", "world" },
                            { "HELLO", "world2" },
                            { "hello", "world3" }
                        },
                        "Hello=\"HtmlEncode[[world]]\" HELLO=\"HtmlEncode[[world2]]\" hello=\"HtmlEncode[[world3]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "HeLlO", "world" },
                            { "hello", "world2" }
                        },
                        "HeLlO=\"HtmlEncode[[world]]\" hello=\"HtmlEncode[[world2]]\""
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MultiAttributeCheckBoxData))]
        public async Task CheckBoxHandlesMultipleAttributesSameNameArePreserved(
            TagHelperAttributeList outputAttributes,
            string expectedAttributeString)
        {
            // Arrange
            var originalContent = "original content";
            var expectedContent = $"<input {expectedAttributeString} type=\"HtmlEncode[[checkbox]]\" id=\"HtmlEncode[[IsACar]]\" " +
                $"name=\"HtmlEncode[[IsACar]]\" value=\"HtmlEncode[[true]]\" />" +
                "<input name=\"HtmlEncode[[IsACar]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "input",
                outputAttributes,
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            output.Content.AppendHtml(originalContent);
            var htmlGenerator = new TestableHtmlGenerator(new EmptyModelMetadataProvider());
            var tagHelper = GetTagHelper(htmlGenerator, model: false, propertyName: nameof(Model.IsACar));

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.NotNull(output.PostElement);
            Assert.Equal(originalContent, HtmlContentUtilities.HtmlContentToString(output.Content));
            Assert.Equal(expectedContent, HtmlContentUtilities.HtmlContentToString(output));
        }

        [Theory]
        [InlineData("bad")]
        [InlineData("notbool")]
        public void CheckBoxHandlesNonParsableStringsAsBoolsCorrectly(
            string possibleBool)
        {
            // Arrange
            const string content = "original content";
            const string tagName = "input";
            const string forAttributeName = "asp-for";

            var expected = Resources.FormatInputTagHelper_InvalidStringResult(
                forAttributeName,
                possibleBool,
                typeof(bool).FullName);

            var attributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };
            output.Content.AppendHtml(content);
            var htmlGenerator = new TestableHtmlGenerator(new EmptyModelMetadataProvider());
            var tagHelper = GetTagHelper(htmlGenerator, model: possibleBool, propertyName: nameof(Model.IsACar));

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => tagHelper.Process(context, output));
            Assert.Equal(expected, ex.Message);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(1337)]
        public void CheckBoxHandlesInvalidDataTypesCorrectly(
            int possibleBool)
        {
            // Arrange
            const string content = "original content";
            const string tagName = "input";
            const string forAttributeName = "asp-for";

            var expected = Resources.FormatInputTagHelper_InvalidExpressionResult(
                "<input>",
                forAttributeName,
                possibleBool.GetType().FullName,
                typeof(bool).FullName,
                typeof(string).FullName,
                "type",
                "checkbox");

            var attributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };
            output.Content.AppendHtml(content);
            var htmlGenerator = new TestableHtmlGenerator(new EmptyModelMetadataProvider());
            var tagHelper = GetTagHelper(htmlGenerator, model: possibleBool, propertyName: nameof(Model.IsACar));

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => tagHelper.Process(context, output));
            Assert.Equal(expected, ex.Message);
        }

        [Theory]
        [InlineData("trUE")]
        [InlineData("FAlse")]
        public void CheckBoxHandlesParsableStringsAsBoolsCorrectly(
            string possibleBool)
        {
            // Arrange
            const string content = "original content";
            const string tagName = "input";
            const string isCheckedAttr = "checked=\"HtmlEncode[[checked]]\" ";
            var isChecked = (bool.Parse(possibleBool) ? isCheckedAttr : string.Empty);
            var expectedContent = $"<input class=\"HtmlEncode[[form-control]]\" type=\"HtmlEncode[[checkbox]]\" " +
                $"{isChecked}id=\"HtmlEncode[[IsACar]]\" name=\"HtmlEncode[[IsACar]]\" " +
                "value=\"HtmlEncode[[true]]\" /><input name=\"HtmlEncode[[IsACar]]\" type=\"HtmlEncode[[hidden]]\" " +
                "value=\"HtmlEncode[[false]]\" />";
            var expectedPostElement = "<input name=\"IsACar\" type=\"hidden\" value=\"false\" />";

            var attributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };
            output.Content.AppendHtml(content);
            var htmlGenerator = new TestableHtmlGenerator(new EmptyModelMetadataProvider());
            var tagHelper = GetTagHelper(htmlGenerator, model: possibleBool, propertyName: nameof(Model.IsACar));

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal(content, HtmlContentUtilities.HtmlContentToString(output.Content));
            Assert.Equal(expectedContent, HtmlContentUtilities.HtmlContentToString(output));
            Assert.Equal(expectedPostElement, output.PostElement.GetContent());
        }

        [Theory]
        [InlineData("checkbox")]
        [InlineData("hidden")]
        [InlineData("number")]
        [InlineData("password")]
        [InlineData("text")]
        public void Process_WithEmptyForName_Throws(string inputTypeName)
        {
            // Arrange
            var expectedMessage = "The name of an HTML field cannot be null or empty. Instead use methods " +
                "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
                "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = false;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(bool), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "type", inputTypeName },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                "input",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => tagHelper.Process(context, output),
                paramName: "expression",
                exceptionMessage: expectedMessage);
        }

        [Fact]
        public void Process_Radio_WithEmptyForName_Throws()
        {
            // Arrange
            var expectedMessage = "The name of an HTML field cannot be null or empty. Instead use methods " +
                "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
                "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";

            var inputTypeName = "radio";
            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = 23;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(int), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Value = "24",
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "type", inputTypeName },
                { "value", "24" },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                "input",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => tagHelper.Process(context, output),
                paramName: "expression",
                exceptionMessage: expectedMessage);
        }

        [Theory]
        [InlineData("hidden")]
        [InlineData("number")]
        [InlineData("text")]
        public void Process_WithEmptyForName_DoesNotThrow_WithName(string inputTypeName)
        {
            // Arrange
            var expectedAttributeValue = "-expression-";
            var expectedTagName = "input";
            var expectedAttributes = new TagHelperAttributeList
            {
                { "name",  expectedAttributeValue },
                { "type", inputTypeName },
                { "value", "False" },
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = false;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(bool), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            viewContext.ClientValidationEnabled = false;

            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Name = expectedAttributeValue,
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "name", expectedAttributeValue },
                { "type", inputTypeName },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.False(output.IsContentModified);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Fact]
        public void Process_Checkbox_WithEmptyForName_DoesNotThrow_WithName()
        {
            // Arrange
            var expectedAttributeValue = "-expression-";
            var expectedPostElementContent = $"<input name=\"HtmlEncode[[{expectedAttributeValue}]]\" " +
                "type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />";
            var expectedTagName = "input";
            var inputTypeName = "checkbox";
            var expectedAttributes = new TagHelperAttributeList
            {
                { "name",  expectedAttributeValue },
                { "type", inputTypeName },
                { "value", "true" },
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = false;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(bool), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            viewContext.ClientValidationEnabled = false;

            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Name = expectedAttributeValue,
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "name", expectedAttributeValue },
                { "type", inputTypeName },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.False(output.IsContentModified);
            Assert.Equal(expectedTagName, output.TagName);

            Assert.False(viewContext.FormContext.HasEndOfFormContent);
            Assert.Equal(expectedPostElementContent, HtmlContentUtilities.HtmlContentToString(output.PostElement));
        }

        [Fact]
        public void Process_Password_WithEmptyForName_DoesNotThrow_WithName()
        {
            // Arrange
            var expectedAttributeValue = "-expression-";
            var expectedTagName = "input";
            var inputTypeName = "password";
            var expectedAttributes = new TagHelperAttributeList
            {
                { "name",  expectedAttributeValue },
                { "type", inputTypeName },
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = "password";
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            viewContext.ClientValidationEnabled = false;

            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Name = expectedAttributeValue,
                ViewContext = viewContext,
            };

            // Expect attributes to just pass through. Tag helper binds all input attributes and doesn't add any.
            var context = new TagHelperContext(expectedAttributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.False(output.IsContentModified);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Fact]
        public void Process_Radio_WithEmptyForName_DoesNotThrow_WithName()
        {
            // Arrange
            var expectedAttributeValue = "-expression-";
            var expectedTagName = "input";
            var inputTypeName = "radio";
            var expectedAttributes = new TagHelperAttributeList
            {
                { "name",  expectedAttributeValue },
                { "type", inputTypeName },
                { "value", "24" },
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = 23;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(int), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            viewContext.ClientValidationEnabled = false;

            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Name = expectedAttributeValue,
                Value = "24",
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "name", expectedAttributeValue },
                { "type", inputTypeName },
                { "value", "24" },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.False(output.IsContentModified);
            Assert.Equal(expectedTagName, output.TagName);
        }

        // Top-level container (List<Model> or Model instance), immediate container type (Model or NestModel),
        // model accessor, expression path / id, expected value.
        public static TheoryData<object, Type, object, NameAndId, string> TestDataSet
        {
            get
            {
                var modelWithNull = new Model
                {
                    NestedModel = new NestedModel
                    {
                        Text = null,
                    },
                    Text = null,
                };
                var modelWithText = new Model
                {
                    NestedModel = new NestedModel
                    {
                        Text = "inner text",
                    },
                    Text = "outer text",
                };
                var models = new List<Model>
                {
                    modelWithNull,
                    modelWithText,
                };

                return new TheoryData<object, Type, object, NameAndId, string>
                {
                    { null, typeof(Model), null, new NameAndId("Text", "Text"),
                        string.Empty },

                    { modelWithNull, typeof(Model), modelWithNull.Text, new NameAndId("Text", "Text"),
                        string.Empty },
                    { modelWithText, typeof(Model), modelWithText.Text, new NameAndId("Text", "Text"),
                        "outer text" },

                    { modelWithNull, typeof(NestedModel), modelWithNull.NestedModel.Text,
                        new NameAndId("NestedModel.Text", "NestedModel_Text"), string.Empty },
                    { modelWithText, typeof(NestedModel), modelWithText.NestedModel.Text,
                        new NameAndId("NestedModel.Text", "NestedModel_Text"), "inner text" },

                    { models, typeof(Model), models[0].Text,
                        new NameAndId("[0].Text", "z0__Text"), string.Empty },
                    { models, typeof(Model), models[1].Text,
                        new NameAndId("[1].Text", "z1__Text"), "outer text" },

                    { models, typeof(NestedModel), models[0].NestedModel.Text,
                        new NameAndId("[0].NestedModel.Text", "z0__NestedModel_Text"), string.Empty },
                    { models, typeof(NestedModel), models[1].NestedModel.Text,
                        new NameAndId("[1].NestedModel.Text", "z1__NestedModel_Text"), "inner text" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestDataSet))]
        public async Task ProcessAsync_GeneratesExpectedOutput(
            object container,
            Type containerType,
            object model,
            NameAndId nameAndId,
            string expectedValue)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
                { "type", "text" },
                { "id", nameAndId.Id },
                { "name", nameAndId.Name },
                { "valid", "from validation attributes" },
                { "value", expectedValue },
            };
            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var expectedPostContent = "original post-content";
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(
                expectedTagName,
                originalAttributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                })
            {
                TagMode = TagMode.StartTagOnly,
            };
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent(expectedPostContent);

            var htmlGenerator = new TestableHtmlGenerator(new EmptyModelMetadataProvider())
            {
                ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
            };

            // Property name is either nameof(Model.Text) or nameof(NestedModel.Text).
            var tagHelper = GetTagHelper(
                htmlGenerator,
                container,
                containerType,
                model,
                propertyName: nameof(Model.Text),
                expressionName: nameAndId.Name);

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(TagMode.StartTagOnly, output.TagMode);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData("datetime", "datetime")]
        [InlineData(null, "text")]
        [InlineData("hidden", "hidden")]
        public void Process_GeneratesFormattedOutput(string specifiedType, string expectedType)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "type", expectedType },
                { "id", "DateTimeOffset" },
                { "name", "DateTimeOffset" },
                { "valid", "from validation attributes" },
                { "value", "datetime: 2011-08-31T05:30:45.0000000+03:00" },
            };
            var expectedTagName = "not-input";
            var container = new Model
            {
                DateTimeOffset = new DateTimeOffset(2011, 8, 31, hour: 5, minute: 30, second: 45, offset: TimeSpan.FromHours(3))
            };

            var allAttributes = new TagHelperAttributeList
            {
                { "type", specifiedType },
            };
            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: allAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    throw new Exception("getChildContentAsync should not be called.");
                })
            {
                TagMode = TagMode.StartTagOnly,
            };

            var htmlGenerator = new TestableHtmlGenerator(new EmptyModelMetadataProvider())
            {
                ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
            };

            var tagHelper = GetTagHelper(
                htmlGenerator,
                container,
                typeof(Model),
                model: container.DateTimeOffset,
                propertyName: nameof(Model.DateTimeOffset),
                expressionName: nameof(Model.DateTimeOffset));
            tagHelper.Format = "datetime: {0:o}";
            tagHelper.InputTypeName = specifiedType;

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Empty(output.PreContent.GetContent());
            Assert.Empty(output.Content.GetContent());
            Assert.Empty(output.PostContent.GetContent());
            Assert.Equal(TagMode.StartTagOnly, output.TagMode);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData("datetime", "datetime")]
        [InlineData(null, "datetime-local")]
        [InlineData("hidden", "hidden")]
        public void Process_GeneratesFormattedOutput_ForDateTime(string specifiedType, string expectedType)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "type", expectedType },
                { "id", nameof(Model.DateTime) },
                { "name", nameof(Model.DateTime) },
                { "valid", "from validation attributes" },
                { "value", "datetime: 2011-08-31T05:30:45.0000000Z" },
            };
            var expectedTagName = "not-input";
            var container = new Model
            {
                DateTime = new DateTime(2011, 8, 31, hour: 5, minute: 30, second: 45, kind: DateTimeKind.Utc),
            };

            var allAttributes = new TagHelperAttributeList
            {
                { "type", specifiedType },
            };
            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: allAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    throw new Exception("getChildContentAsync should not be called.");
                })
            {
                TagMode = TagMode.StartTagOnly,
            };

            var htmlGenerator = new TestableHtmlGenerator(new EmptyModelMetadataProvider())
            {
                ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
            };

            var tagHelper = GetTagHelper(
                htmlGenerator,
                container,
                typeof(Model),
                model: container.DateTime,
                propertyName: nameof(Model.DateTime),
                expressionName: nameof(Model.DateTime));
            tagHelper.Format = "datetime: {0:o}";
            tagHelper.InputTypeName = specifiedType;

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Empty(output.PreContent.GetContent());
            Assert.Empty(output.Content.GetContent());
            Assert.Empty(output.PostContent.GetContent());
            Assert.Equal(TagMode.StartTagOnly, output.TagMode);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Fact]
        public async Task ProcessAsync_GenerateCheckBox_WithHiddenInputRenderModeNone()
        {
            var propertyName = "-expression-";
            var expectedTagName = "input";
            var inputTypeName = "checkbox";
            var expectedAttributes = new TagHelperAttributeList
            {
                { "name",  propertyName },
                { "type", inputTypeName },
                { "value", "true" },
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = false;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(bool), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);

            viewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.None;

            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Name = propertyName,
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "name", propertyName },
                { "type", inputTypeName },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.False(output.IsContentModified);
            Assert.Equal(expectedTagName, output.TagName);

            Assert.False(viewContext.FormContext.HasEndOfFormContent);
            Assert.True(string.IsNullOrEmpty(HtmlContentUtilities.HtmlContentToString(output.PostElement)));
        }

        [Fact]
        public async Task ProcessAsync_GenerateCheckBox_WithHiddenInputRenderModeInline()
        {
            var propertyName = "-expression-";
            var expectedTagName = "input";
            var expectedPostElementContent = $"<input name=\"HtmlEncode[[{propertyName}]]\" " +
                "type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />";
            var inputTypeName = "checkbox";
            var expectedAttributes = new TagHelperAttributeList
            {
                { "name",  propertyName },
                { "type", inputTypeName },
                { "value", "true" },
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = false;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(bool), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);

            viewContext.FormContext.CanRenderAtEndOfForm = true;
            viewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.Inline;

            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Name = propertyName,
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "name", propertyName },
                { "type", inputTypeName },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.False(output.IsContentModified);
            Assert.Equal(expectedTagName, output.TagName);

            Assert.False(viewContext.FormContext.HasEndOfFormContent);
            Assert.Equal(expectedPostElementContent, HtmlContentUtilities.HtmlContentToString(output.PostElement));
        }

        [Fact]
        public async Task ProcessAsync_GenerateCheckBox_WithHiddenInputRenderModeEndOfForm()
        {
            var propertyName = "-expression-";
            var expectedTagName = "input";
            var expectedEndOfFormContent = $"<input name=\"HtmlEncode[[{propertyName}]]\" " +
                "type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />";
            var inputTypeName = "checkbox";
            var expectedAttributes = new TagHelperAttributeList
            {
                { "name",  propertyName },
                { "type", inputTypeName },
                { "value", "true" },
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = false;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(bool), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);

            viewContext.FormContext.CanRenderAtEndOfForm = true;
            viewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.EndOfForm;

            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Name = propertyName,
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "name", propertyName },
                { "type", inputTypeName },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.False(output.IsContentModified);
            Assert.Equal(expectedTagName, output.TagName);

            Assert.Equal(expectedEndOfFormContent, string.Join("", viewContext.FormContext.EndOfFormContent.Select(html => HtmlContentUtilities.HtmlContentToString(html))));
            Assert.True(string.IsNullOrEmpty(HtmlContentUtilities.HtmlContentToString(output.PostElement)));
        }

        [Fact]
        public async Task ProcessAsync_GenerateCheckBox_WithHiddenInputRenderModeEndOfForm_AndCanRenderAtEndOfFormNotSet()
        {
            var propertyName = "-expression-";
            var expectedTagName = "input";
            var expectedPostElementContent = $"<input name=\"HtmlEncode[[{propertyName}]]\" " +
                "type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />";
            var inputTypeName = "checkbox";
            var expectedAttributes = new TagHelperAttributeList
            {
                { "name",  propertyName },
                { "type", inputTypeName },
                { "value", "true" },
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var model = false;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(bool), model);
            var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);

            viewContext.FormContext.CanRenderAtEndOfForm = false;
            viewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.EndOfForm;

            var tagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                InputTypeName = inputTypeName,
                Name = propertyName,
                ViewContext = viewContext,
            };

            var attributes = new TagHelperAttributeList
            {
                { "name", propertyName },
                { "type", inputTypeName },
            };

            var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            var output = new TagHelperOutput(
                expectedTagName,
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(result: null))
            {
                TagMode = TagMode.SelfClosing,
            };

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.False(output.IsContentModified);
            Assert.Equal(expectedTagName, output.TagName);

            Assert.False(viewContext.FormContext.HasEndOfFormContent);
            Assert.Equal(expectedPostElementContent, HtmlContentUtilities.HtmlContentToString(output.PostElement));
        }

        [Fact]
        public async Task ProcessAsync_CallsGenerateCheckBox_WithExpectedParameters()
        {
            // Arrange
            var originalContent = "original content";
            var expectedPreContent = "original pre-content";
            var expectedContent = "<input class=\"HtmlEncode[[form-control]]\" type=\"HtmlEncode[[checkbox]]\" /><hidden />";
            var expectedPostContent = "original post-content";
            var expectedPostElement = "<hidden />";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(
                "input",
                originalAttributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                })
            {
                TagMode = TagMode.SelfClosing,
            };
            output.PreContent.AppendHtml(expectedPreContent);
            output.Content.AppendHtml(originalContent);
            output.PostContent.AppendHtml(expectedPostContent);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(htmlGenerator.Object, model: false, propertyName: nameof(Model.IsACar));
            tagHelper.Format = "somewhat-less-null"; // ignored

            var tagBuilder = new TagBuilder("input")
            {
                TagRenderMode = TagRenderMode.SelfClosing
            };
            htmlGenerator
                .Setup(mock => mock.GenerateCheckBox(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    null,                   // isChecked
                    It.IsAny<object>()))    // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();
            htmlGenerator
                .Setup(mock => mock.GenerateHiddenForCheckbox(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name))
                .Returns(new TagBuilder("hidden") { TagRenderMode = TagRenderMode.SelfClosing })
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.NotEmpty(output.Attributes);
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(originalContent, HtmlContentUtilities.HtmlContentToString(output.Content));
            Assert.Equal(expectedContent, HtmlContentUtilities.HtmlContentToString(output));
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedPostElement, output.PostElement.GetContent());
            Assert.Equal(TagMode.SelfClosing, output.TagMode);
        }

        [Theory]
        [InlineData(null, "hidden", null, null)]
        [InlineData(null, "Hidden", "not-null", "somewhat-less-null")]
        [InlineData(null, "HIDden", null, "somewhat-less-null")]
        [InlineData(null, "HIDDEN", "not-null", null)]
        [InlineData("hiddeninput", null, null, null)]
        [InlineData("HiddenInput", null, "not-null", null)]
        [InlineData("hidDENinPUT", null, null, "somewhat-less-null")]
        [InlineData("HIDDENINPUT", null, "not-null", "somewhat-less-null")]
        public async Task ProcessAsync_CallsGenerateTextBox_WithExpectedParametersForHidden(
            string dataTypeName,
            string inputTypeName,
            string model,
            string format)
        {
            // Arrange
            var contextAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            if (!string.IsNullOrEmpty(inputTypeName))
            {
                contextAttributes.SetAttribute("type", inputTypeName);  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "form-control hidden-control" },
                { "type", inputTypeName ?? "hidden" },      // Generator restores type attribute; adds "hidden" if none.
            };
            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var expectedPostContent = "original post-content";
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(
                expectedTagName,
                originalAttributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                })
            {
                TagMode = TagMode.StartTagOnly,
            };
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent(expectedPostContent);

            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForProperty<Model>("Text").DisplayDetails(dd => dd.DataTypeName = dataTypeName);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(
                htmlGenerator.Object,
                model,
                nameof(Model.Text),
                metadataProvider: metadataProvider);
            tagHelper.Format = format;
            tagHelper.InputTypeName = inputTypeName;

            var tagBuilder = new TagBuilder("input")
            {
                Attributes =
                {
                    { "class", "hidden-control" },
                },
            };
            htmlGenerator
                .Setup(mock => mock.GenerateTextBox(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    model,  // value
                    format,
                    new Dictionary<string, object> { { "type", "hidden" } }))   // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.Equal(TagMode.StartTagOnly, output.TagMode);
            Assert.Equal(expectedAttributes, output.Attributes, CaseSensitiveTagHelperAttributeComparer.Default);
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData(null, "password", null)]
        [InlineData(null, "Password", "not-null")]
        [InlineData(null, "PASSword", null)]
        [InlineData(null, "PASSWORD", "not-null")]
        [InlineData("password", null, null)]
        [InlineData("Password", null, "not-null")]
        [InlineData("PASSword", null, null)]
        [InlineData("PASSWORD", null, "not-null")]
        public async Task ProcessAsync_CallsGeneratePassword_WithExpectedParameters(
            string dataTypeName,
            string inputTypeName,
            string model)
        {
            // Arrange
            var contextAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            if (!string.IsNullOrEmpty(inputTypeName))
            {
                contextAttributes.SetAttribute("type", inputTypeName);  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "form-control password-control" },
                { "type", inputTypeName ?? "password" },    // Generator restores type attribute; adds "password" if none.
            };
            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var expectedPostContent = "original post-content";
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(
                expectedTagName,
                originalAttributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                })
            {
                TagMode = TagMode.StartTagOnly,
            };
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent(expectedPostContent);

            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForProperty<Model>("Text").DisplayDetails(dd => dd.DataTypeName = dataTypeName);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(
                htmlGenerator.Object,
                model,
                nameof(Model.Text),
                metadataProvider: metadataProvider);
            tagHelper.Format = "somewhat-less-null"; // ignored
            tagHelper.InputTypeName = inputTypeName;

            var tagBuilder = new TagBuilder("input")
            {
                Attributes =
                {
                    { "class", "password-control" },
                },
            };
            htmlGenerator
                .Setup(mock => mock.GeneratePassword(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    null,       // value
                    null))      // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.Equal(TagMode.StartTagOnly, output.TagMode);
            Assert.Equal(expectedAttributes, output.Attributes, CaseSensitiveTagHelperAttributeComparer.Default);
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData("radio", null)]
        [InlineData("Radio", "not-null")]
        [InlineData("RADio", null)]
        [InlineData("RADIO", "not-null")]
        public async Task ProcessAsync_CallsGenerateRadioButton_WithExpectedParameters(
            string inputTypeName,
            string model)
        {
            // Arrange
            var value = "match";            // Real generator would use this for comparison with For.Metadata.Model.
            var contextAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
                { "value", value },
            };
            if (!string.IsNullOrEmpty(inputTypeName))
            {
                contextAttributes.SetAttribute("type", inputTypeName);  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "form-control radio-control" },
                { "value", value },
                { "type", inputTypeName ?? "radio" },       // Generator restores type attribute; adds "radio" if none.
            };
            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var expectedPostContent = "original post-content";
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(
                expectedTagName,
                originalAttributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                })
            {
                TagMode = TagMode.StartTagOnly,
            };
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent(expectedPostContent);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(htmlGenerator.Object, model, nameof(Model.Text));
            tagHelper.Format = "somewhat-less-null"; // ignored
            tagHelper.InputTypeName = inputTypeName;
            tagHelper.Value = value;

            var tagBuilder = new TagBuilder("input")
            {
                Attributes =
                {
                    { "class", "radio-control" },
                },
            };
            htmlGenerator
                .Setup(mock => mock.GenerateRadioButton(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    value,
                    null,       // isChecked
                    null))      // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.Equal(TagMode.StartTagOnly, output.TagMode);
            Assert.Equal(expectedAttributes, output.Attributes, CaseSensitiveTagHelperAttributeComparer.Default);
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData(null, null, null, "somewhat-less-null")]
        [InlineData(null, null, "not-null", null)]
        [InlineData(null, "string", null, null)]
        [InlineData(null, "String", "not-null", null)]
        [InlineData(null, "STRing", null, "somewhat-less-null")]
        [InlineData(null, "STRING", "not-null", null)]
        [InlineData(null, "text", null, null)]
        [InlineData(null, "Text", "not-null", "somewhat-less-null")]
        [InlineData(null, "TExt", null, null)]
        [InlineData(null, "TEXT", "not-null", null)]
        [InlineData("string", null, null, null)]
        [InlineData("String", null, "not-null", null)]
        [InlineData("STRing", null, null, null)]
        [InlineData("STRING", null, "not-null", null)]
        [InlineData("text", null, null, null)]
        [InlineData("Text", null, "not-null", null)]
        [InlineData("TExt", null, null, null)]
        [InlineData("TEXT", null, "not-null", null)]
        [InlineData("custom-datatype", null, null, null)]
        [InlineData(null, "unknown-input-type", "not-null", null)]
        [InlineData("Image", null, "not-null", "somewhat-less-null")]
        [InlineData(null, "image", "not-null", null)]
        public async Task ProcessAsync_CallsGenerateTextBox_WithExpectedParameters(
            string dataTypeName,
            string inputTypeName,
            string model,
            string format)
        {
            // Arrange
            var contextAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            if (!string.IsNullOrEmpty(inputTypeName))
            {
                contextAttributes.SetAttribute("type", inputTypeName);  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "form-control text-control" },
                { "type", inputTypeName ?? "text" },        // Generator restores type attribute; adds "text" if none.
            };
            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var expectedPostContent = "original post-content";
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(
                expectedTagName,
                originalAttributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                })
            {
                TagMode = TagMode.StartTagOnly,
            };
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent(expectedPostContent);

            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForProperty<Model>("Text").DisplayDetails(dd => dd.DataTypeName = dataTypeName);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(
                htmlGenerator.Object,
                model,
                nameof(Model.Text),
                metadataProvider: metadataProvider);
            tagHelper.Format = format;
            tagHelper.InputTypeName = inputTypeName;

            var tagBuilder = new TagBuilder("input")
            {
                Attributes =
                {
                    { "class", "text-control" },
                },
            };
            htmlGenerator
                .Setup(mock => mock.GenerateTextBox(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    model,  // value
                    format,
                    It.Is<Dictionary<string, object>>(m => m.ContainsKey("type"))))     // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.Equal(TagMode.StartTagOnly, output.TagMode);
            Assert.Equal(expectedAttributes, output.Attributes, CaseSensitiveTagHelperAttributeComparer.Default);
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        public static TheoryData<string, string, string> InputTypeData
        {
            get
            {
                return new TheoryData<string, string, string>
                {
                    { null, null, "text" },
                    { "Byte", null, "number" },
                    { "custom-datatype", null, "text" },
                    { "Custom-Datatype", null, "text" },
                    { "date", null, "date" },                  // No date/time special cases since ModelType is string.
                    { "datetime", null, "datetime-local" },
                    { "datetime-local", null, "datetime-local" },
                    { "DATETIME-local", null, "datetime-local" },
                    { "datetimeOffset", null, "text" },
                    { "Decimal", "{0:0.00}", "text" },
                    { "Double", null, "text" },
                    { "Int16", null, "number" },
                    { "Int32", null, "number" },
                    { "int32", null, "number" },
                    { "Int64", null, "number" },
                    { "SByte", null, "number" },
                    { "Single", null, "text" },
                    { "SINGLE", null, "text" },
                    { "string", null, "text" },
                    { "STRING", null, "text" },
                    { "text", null, "text" },
                    { "TEXT", null, "text" },
                    { "time", null, "time" },
                    { "month", "{0:yyyy-MM}", "month" },
                    { "week", null, "week" },
                    { "UInt16", null, "number" },
                    { "uint16", null, "number" },
                    { "UInt32", null, "number" },
                    { "UInt64", null, "number" },
                    { nameof(IFormFile), null, "file" },
                    { TemplateRenderer.IEnumerableOfIFormFileName, null, "file" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(InputTypeData))]
        public async Task ProcessAsync_CallsGenerateTextBox_AddsExpectedAttributes(
            string dataTypeName,
            string expectedFormat,
            string expectedType)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "type", expectedType },                   // Calculated; not passed to HtmlGenerator.
            };
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            var output = new TagHelperOutput(
                expectedTagName,
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()))
            {
                TagMode = TagMode.SelfClosing,
            };

            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForProperty<Model>("Text").DisplayDetails(dd => dd.DataTypeName = dataTypeName);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(
                htmlGenerator.Object,
                model: null,
                propertyName: nameof(Model.Text),
                metadataProvider: metadataProvider);

            var tagBuilder = new TagBuilder("input");

            var htmlAttributes = new Dictionary<string, object>
            {
                { "type", expectedType }
            };
            if (string.Equals(dataTypeName, TemplateRenderer.IEnumerableOfIFormFileName))
            {
                htmlAttributes["multiple"] = "multiple";
            }
            htmlGenerator
                .Setup(mock => mock.GenerateTextBox(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    null,                                   // value
                    expectedFormat,
                    htmlAttributes))                // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.Equal(TagMode.SelfClosing, output.TagMode);
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Empty(output.PreContent.GetContent());
            Assert.Equal(string.Empty, output.Content.GetContent());
            Assert.Empty(output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Fact]
        public async Task ProcessAsync_CallsGenerateTextBox_InputTypeDateTime_RendersAsDateTime()
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "type", "datetime" },                   // Calculated; not passed to HtmlGenerator.
            };
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList()
                {
                    {"type", "datetime" }
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            var output = new TagHelperOutput(
                expectedTagName,
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()))
            {
                TagMode = TagMode.SelfClosing,
            };

            var htmlAttributes = new Dictionary<string, object>
            {
                { "type", "datetime" }
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(
                htmlGenerator.Object,
                model: null,
                propertyName: "DateTime",
                metadataProvider: metadataProvider);
            tagHelper.ViewContext.Html5DateRenderingMode = Html5DateRenderingMode.Rfc3339;
            tagHelper.InputTypeName = "datetime";
            var tagBuilder = new TagBuilder("input");
            htmlGenerator
                .Setup(mock => mock.GenerateTextBox(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    null,                                   // value
                    @"{0:yyyy-MM-ddTHH\:mm\:ss.fffK}",
                    htmlAttributes))                    // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.Equal(TagMode.SelfClosing, output.TagMode);
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Empty(output.PreContent.GetContent());
            Assert.Equal(string.Empty, output.Content.GetContent());
            Assert.Empty(output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData("Date", Html5DateRenderingMode.CurrentCulture, "{0:d}", "date")]    // Format from [DataType].
        [InlineData("Date", Html5DateRenderingMode.Rfc3339, "{0:yyyy-MM-dd}", "date")]
        [InlineData("DateTime", Html5DateRenderingMode.CurrentCulture, null, "datetime-local")]
        [InlineData("DateTime", Html5DateRenderingMode.Rfc3339, @"{0:yyyy-MM-ddTHH\:mm\:ss.fff}", "datetime-local")]
        [InlineData("DateTimeOffset", Html5DateRenderingMode.CurrentCulture, null, "text")]
        [InlineData("DateTimeOffset", Html5DateRenderingMode.Rfc3339, @"{0:yyyy-MM-ddTHH\:mm\:ss.fffK}", "text")]
        [InlineData("DateTimeLocal", Html5DateRenderingMode.CurrentCulture, null, "datetime-local")]
        [InlineData("DateTimeLocal", Html5DateRenderingMode.Rfc3339, @"{0:yyyy-MM-ddTHH\:mm\:ss.fff}", "datetime-local")]
        [InlineData("Time", Html5DateRenderingMode.CurrentCulture, "{0:t}", "time")]    // Format from [DataType].
        [InlineData("Time", Html5DateRenderingMode.Rfc3339, @"{0:HH\:mm\:ss.fff}", "time")]
        [InlineData("Month", Html5DateRenderingMode.CurrentCulture, "{0:yyyy-MM}", "month")]
        [InlineData("Month", Html5DateRenderingMode.Rfc3339, "{0:yyyy-MM}", "month")]
        [InlineData("Week", Html5DateRenderingMode.CurrentCulture, null, "week")]
        [InlineData("Week", Html5DateRenderingMode.Rfc3339, null, "week")]
        [InlineData("NullableDate", Html5DateRenderingMode.Rfc3339, "{0:yyyy-MM-dd}", "date")]
        [InlineData("NullableDateTime", Html5DateRenderingMode.Rfc3339, @"{0:yyyy-MM-ddTHH\:mm\:ss.fff}", "datetime-local")]
        [InlineData("NullableDateTimeOffset", Html5DateRenderingMode.Rfc3339, @"{0:yyyy-MM-ddTHH\:mm\:ss.fffK}", "text")]
        public async Task ProcessAsync_CallsGenerateTextBox_AddsExpectedAttributesForRfc3339(
            string propertyName,
            Html5DateRenderingMode dateRenderingMode,
            string expectedFormat,
            string expectedType)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "type", expectedType },                   // Calculated; not passed to HtmlGenerator.
            };
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            var output = new TagHelperOutput(
                expectedTagName,
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()))
            {
                TagMode = TagMode.SelfClosing,
            };

            var htmlAttributes = new Dictionary<string, object>
            {
                { "type", expectedType }
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(
                htmlGenerator.Object,
                model: null,
                propertyName: propertyName,
                metadataProvider: metadataProvider);
            tagHelper.ViewContext.Html5DateRenderingMode = dateRenderingMode;

            var tagBuilder = new TagBuilder("input");
            htmlGenerator
                .Setup(mock => mock.GenerateTextBox(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    null,                                   // value
                    expectedFormat,
                    htmlAttributes))                    // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.Equal(TagMode.SelfClosing, output.TagMode);
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Empty(output.PreContent.GetContent());
            Assert.Equal(string.Empty, output.Content.GetContent());
            Assert.Empty(output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        // Html5DateRenderingMode.Rfc3339 is enabled by default.
        [Theory]
        [InlineData("Date", "2000-01-02", "date")]
        [InlineData("DateTime", "2000-01-02T03:04:05.060", "datetime-local")]
        [InlineData("DateTimeLocal", "2000-01-02T03:04:05.060", "datetime-local")]
        [InlineData("DateTimeOffset", "2000-01-02T03:04:05.060Z", "text")]
        [InlineData("Time", "03:04:05.060", "time")]
        [InlineData("Month", "2000-01", "month")]
        [InlineData("Week", "1999-W52", "week")]
        public async Task ProcessAsync_CallsGenerateTextBox_ProducesExpectedValue_ForDateTime(
            string propertyName,
            string expectedValue,
            string expectedType)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "type", expectedType },
                { "id", propertyName },
                { "name", propertyName },
                { "value", expectedValue },
            };

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            var output = new TagHelperOutput(
                expectedType,
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()))
            {
                TagMode = TagMode.SelfClosing,
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var model = new DateTime(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                kind: DateTimeKind.Utc);

            var htmlGenerator = HtmlGeneratorUtilities.GetHtmlGenerator(metadataProvider);
            var tagHelper = GetTagHelper(
                htmlGenerator,
                model: model,
                propertyName: propertyName,
                metadataProvider: metadataProvider);
            tagHelper.ViewContext.Html5DateRenderingMode = Html5DateRenderingMode.Rfc3339;

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedType, output.TagName);
        }

        // Html5DateRenderingMode.Rfc3339 can be disabled.
        [Theory]
        [InlineData("Date", null, "02/01/2000", "date")]
        [InlineData("Date", "{0:d}", "02/01/2000", "date")]
        [InlineData("DateTime", null, "02/01/2000 03:04:05", "datetime-local")]
        [InlineData("DateTimeLocal", null, "02/01/2000 03:04:05", "datetime-local")]
        [InlineData("DateTimeOffset", null, "02/01/2000 03:04:05", "text")]
        [InlineData("DateTimeOffset", "{0:o}", "2000-01-02T03:04:05.0600000Z", "text")]
        [InlineData("Time", null, "03:04", "time")]
        [InlineData("Time", "{0:t}", "03:04", "time")]
        [InlineData("Month", null, "2000-01", "month")]
        [InlineData("Month", "{0:yyyy/MM}", "2000/01", "month")]
        [InlineData("Week", null, "1999-W52", "week")]
        [InlineData("Week", "{0:yyyy/'W1'}", "2000/W1", "week")]
        [ReplaceCulture]
        public async Task ProcessAsync_CallsGenerateTextBox_ProducesExpectedValue_ForDateTimeNotRfc3339(
            string propertyName,
            string editFormatString,
            string expectedValue,
            string expectedType)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "type", expectedType },
                { "id", propertyName },
                { "name", propertyName },
                { "value", expectedValue },
            };

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            var output = new TagHelperOutput(
                expectedType,
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()))
            {
                TagMode = TagMode.SelfClosing,
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            var model = new DateTime(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                kind: DateTimeKind.Utc);

            var htmlGenerator = HtmlGeneratorUtilities.GetHtmlGenerator(metadataProvider);
            var tagHelper = GetTagHelper(
                htmlGenerator,
                model: model,
                propertyName: propertyName,
                metadataProvider: metadataProvider);
            tagHelper.ViewContext.Html5DateRenderingMode = Html5DateRenderingMode.CurrentCulture;
            tagHelper.Format = editFormatString;

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedType, output.TagName);
        }

        // Html5DateRenderingMode.Rfc3339 can be disabled.
        [Theory]
        [InlineData("Month", "month")]
        [InlineData("Week", "week")]
        [ReplaceCulture]
        public async Task ProcessAsync_CallsGenerateTextBox_ProducesExpectedValue_OverridesDefaultFormat(
            string propertyName,
            string expectedType)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "type", expectedType },
                { "id", propertyName },
                { "name", propertyName },
                { "value", "non-default format string" },
            };

            var context = new TagHelperContext(
                tagName: "input",
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            var output = new TagHelperOutput(
                expectedType,
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()))
            {
                TagMode = TagMode.SelfClosing,
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            var model = new DateTime(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                kind: DateTimeKind.Utc);

            var htmlGenerator = HtmlGeneratorUtilities.GetHtmlGenerator(metadataProvider);
            var tagHelper = GetTagHelper(
                htmlGenerator,
                model: model,
                propertyName: propertyName,
                metadataProvider: metadataProvider);
            tagHelper.ViewContext.Html5DateRenderingMode = Html5DateRenderingMode.CurrentCulture;
            tagHelper.Format = "non-default format string";

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedType, output.TagName);
        }

        private static InputTagHelper GetTagHelper(
            IHtmlGenerator htmlGenerator,
            object model,
            string propertyName,
            IModelMetadataProvider metadataProvider = null)
        {
            return GetTagHelper(
                htmlGenerator,
                container: new Model(),
                containerType: typeof(Model),
                model: model,
                propertyName: propertyName,
                expressionName: propertyName,
                metadataProvider: metadataProvider);
        }

        private static InputTagHelper GetTagHelper(
            IHtmlGenerator htmlGenerator,
            object container,
            Type containerType,
            object model,
            string propertyName,
            string expressionName,
            IModelMetadataProvider metadataProvider = null)
        {
            if (metadataProvider == null)
            {
                metadataProvider = new TestModelMetadataProvider();
            }

            var containerMetadata = metadataProvider.GetMetadataForType(containerType);
            var containerExplorer = metadataProvider.GetModelExplorerForType(containerType, container);

            var propertyMetadata = metadataProvider.GetMetadataForProperty(containerType, propertyName);
            var modelExplorer = containerExplorer.GetExplorerForExpression(propertyMetadata, model);

            var modelExpression = new ModelExpression(expressionName, modelExplorer);
            var viewContext = TestableHtmlGenerator.GetViewContext(container, htmlGenerator, metadataProvider);
            var inputTagHelper = new InputTagHelper(htmlGenerator)
            {
                For = modelExpression,
                ViewContext = viewContext,
            };

            return inputTagHelper;
        }

        public class NameAndId
        {
            public NameAndId(string name, string id)
            {
                Name = name;
                Id = id;
            }

            public string Name { get; private set; }

            public string Id { get; private set; }
        }

        private class Model
        {
            public string Text { get; set; }

            public NestedModel NestedModel { get; set; }

            public bool IsACar { get; set; }

            [DataType(DataType.Date)]
            public DateTime Date { get; set; }

            public DateTime DateTime { get; set; }

            public DateTimeOffset DateTimeOffset { get; set; }

            [DataType(DataType.Date)]
            public DateTime? NullableDate { get; set; }

            public DateTime? NullableDateTime { get; set; }

            public DateTimeOffset? NullableDateTimeOffset { get; set; }

            [DataType("datetime-local")]
            public DateTime DateTimeLocal { get; set; }

            [DataType(DataType.Time)]
            public DateTimeOffset Time { get; set; }

            [DataType("month")]
            public DateTimeOffset Month { get; set; }

            [DataType("week")]
            public DateTimeOffset Week { get; set; }
        }

        private class NestedModel
        {
            public string Text { get; set; }
        }
    }
}
