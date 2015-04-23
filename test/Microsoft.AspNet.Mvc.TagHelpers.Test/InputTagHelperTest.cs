// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.Rendering.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class InputTagHelperTest
    {
        public static TheoryData MultiAttributeCheckBoxData
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
                        "hello=\"HtmlEncode[[world]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" },
                            { "hello", "world3" }
                        },
                        "hello=\"HtmlEncode[[world]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "HelLO", "world" },
                            { "HELLO", "world2" }
                        },
                        "HelLO=\"HtmlEncode[[world]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "Hello", "world" },
                            { "HELLO", "world2" },
                            { "hello", "world3" }
                        },
                        "Hello=\"HtmlEncode[[world]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "HeLlO", "world" },
                            { "hello", "world2" }
                        },
                        "HeLlO=\"HtmlEncode[[world]]\""
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MultiAttributeCheckBoxData))]
        public async Task CheckBoxHandlesMultipleAttributesSameNameCorrectly(
            TagHelperAttributeList outputAttributes,
            string expectedAttributeString)
        {
            // Arrange
            var originalContent = "original content";
            var originalTagName = "not-input";
            var expectedContent = $"{originalContent}<input {expectedAttributeString} id=\"HtmlEncode[[IsACar]]\" " +
                "name=\"HtmlEncode[[IsACar]]\" type=\"HtmlEncode[[checkbox]]\" value=\"HtmlEncode[[true]]\" />" +
                "<input name=\"HtmlEncode[[IsACar]]\" type=\"HtmlEncode[[hidden]]\" value=\"HtmlEncode[[false]]\" />";

            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult<TagHelperContent>(result: null));
            var output = new TagHelperOutput(originalTagName, outputAttributes)
            {
                SelfClosing = true,
            };
            output.Content.SetContent(originalContent);
            var htmlGenerator = new TestableHtmlGenerator(new EmptyModelMetadataProvider());
            var tagHelper = GetTagHelper(htmlGenerator, model: false, propertyName: nameof(Model.IsACar));

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Empty(output.Attributes); // Moved to Content and cleared
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.True(output.SelfClosing);
            Assert.Null(output.TagName); // Cleared
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
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(expectedTagName, originalAttributes)
            {
                SelfClosing = false,
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
            Assert.False(output.SelfClosing);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Fact]
        public async Task ProcessAsync_CallsGenerateCheckBox_WithExpectedParameters()
        {
            // Arrange
            var originalContent = "original content";
            var originalTagName = "not-input";
            var expectedPreContent = "original pre-content";
            var expectedContent = originalContent + "<input class=\"HtmlEncode[[form-control]]\" /><hidden />";
            var expectedPostContent = "original post-content";

            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(originalTagName, originalAttributes)
            {
                SelfClosing = true,
            };
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(originalContent);
            output.PostContent.SetContent(expectedPostContent);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(htmlGenerator.Object, model: false, propertyName: nameof(Model.IsACar));
            var tagBuilder = new TagBuilder("input", new CommonTestEncoder())
            {
                Attributes =
                {
                    { "class", "form-control" },
                },
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
                .Returns(new TagBuilder("hidden", new NullTestEncoder()))
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.Empty(output.Attributes);    // Moved to Content and cleared
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.True(output.SelfClosing);
            Assert.Null(output.TagName);       // Cleared
        }

        [Theory]
        [InlineData(null, "hidden", null)]
        [InlineData(null, "Hidden", "not-null")]
        [InlineData(null, "HIDden", null)]
        [InlineData(null, "HIDDEN", "not-null")]
        [InlineData("hiddeninput", null, null)]
        [InlineData("HiddenInput", null, "not-null")]
        [InlineData("hidDENinPUT", null, null)]
        [InlineData("HIDDENINPUT", null, "not-null")]
        public async Task ProcessAsync_CallsGenerateHidden_WithExpectedParameters(
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
                contextAttributes["type"] = inputTypeName;  // Support restoration of type attribute, if any.
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
                allAttributes: contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(expectedTagName, originalAttributes)
            {
                SelfClosing = false,
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
            tagHelper.InputTypeName = inputTypeName;

            var tagBuilder = new TagBuilder("input", new NullTestEncoder())
            {
                Attributes =
                {
                    { "class", "hidden-control" },
                },
            };
            htmlGenerator
                .Setup(mock => mock.GenerateHidden(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    model,      // value
                    false,      // useViewData
                    null))      // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.False(output.SelfClosing);
            Assert.Equal(expectedAttributes, output.Attributes);
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
                contextAttributes["type"] = inputTypeName;  // Support restoration of type attribute, if any.
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
                allAttributes: contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(expectedTagName, originalAttributes)
            {
                SelfClosing = false,
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
            tagHelper.InputTypeName = inputTypeName;

            var tagBuilder = new TagBuilder("input", new NullTestEncoder())
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

            Assert.False(output.SelfClosing);
            Assert.Equal(expectedAttributes, output.Attributes);
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
                contextAttributes["type"] = inputTypeName;  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "form-control radio-control" },
                { "type", inputTypeName ?? "radio" },       // Generator restores type attribute; adds "radio" if none.
                { "value", value },
            };
            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var expectedPostContent = "original post-content";
            var expectedTagName = "not-input";

            var context = new TagHelperContext(
                allAttributes: contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(expectedTagName, originalAttributes)
            {
                SelfClosing = false,
            };
            output.PreContent.SetContent(expectedPreContent);
            output.Content.SetContent(expectedContent);
            output.PostContent.SetContent(expectedPostContent);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(htmlGenerator.Object, model, nameof(Model.Text));
            tagHelper.InputTypeName = inputTypeName;
            tagHelper.Value = value;

            var tagBuilder = new TagBuilder("input", new NullTestEncoder())
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

            Assert.False(output.SelfClosing);
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedPreContent, output.PreContent.GetContent());
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, null, "not-null")]
        [InlineData(null, "string", null)]
        [InlineData(null, "String", "not-null")]
        [InlineData(null, "STRing", null)]
        [InlineData(null, "STRING", "not-null")]
        [InlineData(null, "text", null)]
        [InlineData(null, "Text", "not-null")]
        [InlineData(null, "TExt", null)]
        [InlineData(null, "TEXT", "not-null")]
        [InlineData("string", null, null)]
        [InlineData("String", null, "not-null")]
        [InlineData("STRing", null, null)]
        [InlineData("STRING", null, "not-null")]
        [InlineData("text", null, null)]
        [InlineData("Text", null, "not-null")]
        [InlineData("TExt", null, null)]
        [InlineData("TEXT", null, "not-null")]
        [InlineData("custom-datatype", null, null)]
        [InlineData(null, "unknown-input-type", "not-null")]
        public async Task ProcessAsync_CallsGenerateTextBox_WithExpectedParameters(
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
                contextAttributes["type"] = inputTypeName;  // Support restoration of type attribute, if any.
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
                allAttributes: contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(expectedTagName, originalAttributes)
            {
                SelfClosing = false,
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
            tagHelper.InputTypeName = inputTypeName;

            var tagBuilder = new TagBuilder("input", new NullTestEncoder())
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
                    model,      // value
                    null,       // format
                    null))      // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.False(output.SelfClosing);
            Assert.Equal(expectedAttributes, output.Attributes);
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
                    { null, null, "text" },
                    { "Byte", null, "number" },
                    { "custom-datatype", null, "text" },
                    { "Custom-Datatype", null, "text" },
                    { "date", null, "date" },                  // No date/time special cases since ModelType is string.
                    { "datetime", null, "datetime" },
                    { "datetime-local", null, "datetime-local" },
                    { "DATETIME-local", null, "datetime-local" },
                    { "Decimal", "{0:0.00}", "text" },
                    { "Double", null, "number" },
                    { "Int16", null, "number" },
                    { "Int32", null, "number" },
                    { "int32", null, "number" },
                    { "Int64", null, "number" },
                    { "SByte", null, "number" },
                    { "Single", null, "number" },
                    { "SINGLE", null, "number" },
                    { "string", null, "text" },
                    { "STRING", null, "text" },
                    { "text", null, "text" },
                    { "TEXT", null, "text" },
                    { "time", null, "time" },
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
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            var output = new TagHelperOutput(expectedTagName, attributes: new TagHelperAttributeList())
            {
                SelfClosing = true,
            };

            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForProperty<Model>("Text").DisplayDetails(dd => dd.DataTypeName = dataTypeName);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(
                htmlGenerator.Object,
                model: null,
                propertyName: nameof(Model.Text),
                metadataProvider: metadataProvider);

            var tagBuilder = new TagBuilder("input", new NullTestEncoder());

            Dictionary<string, object> htmlAttributes = null;
            if (string.Equals(dataTypeName, TemplateRenderer.IEnumerableOfIFormFileName))
            {
                htmlAttributes = new Dictionary<string, object>
                {
                    { "multiple", "multiple" }
                };
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

            Assert.True(output.SelfClosing);
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Empty(output.PreContent);
            Assert.Equal(new[] { string.Empty }, output.Content);
            Assert.Empty(output.PostContent);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData("Date", Html5DateRenderingMode.CurrentCulture, "{0:d}", "date")]    // Format from [DataType].
        [InlineData("Date", Html5DateRenderingMode.Rfc3339, "{0:yyyy-MM-dd}", "date")]
        [InlineData("DateTime", Html5DateRenderingMode.CurrentCulture, null, "datetime")]
        [InlineData("DateTime", Html5DateRenderingMode.Rfc3339, "{0:yyyy-MM-ddTHH:mm:ss.fffK}", "datetime")]
        [InlineData("DateTimeOffset", Html5DateRenderingMode.CurrentCulture, null, "datetime")]
        [InlineData("DateTimeOffset", Html5DateRenderingMode.Rfc3339, "{0:yyyy-MM-ddTHH:mm:ss.fffK}", "datetime")]
        [InlineData("DateTimeLocal", Html5DateRenderingMode.CurrentCulture, null, "datetime-local")]
        [InlineData("DateTimeLocal", Html5DateRenderingMode.Rfc3339, "{0:yyyy-MM-ddTHH:mm:ss.fff}", "datetime-local")]
        [InlineData("Time", Html5DateRenderingMode.CurrentCulture, "{0:t}", "time")]    // Format from [DataType].
        [InlineData("Time", Html5DateRenderingMode.Rfc3339, "{0:HH:mm:ss.fff}", "time")]
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
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            var output = new TagHelperOutput(expectedTagName, attributes: new TagHelperAttributeList())
            {
                SelfClosing = true,
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var tagHelper = GetTagHelper(
                htmlGenerator.Object,
                model: null,
                propertyName: propertyName,
                metadataProvider: metadataProvider);
            tagHelper.ViewContext.Html5DateRenderingMode = dateRenderingMode;

            var tagBuilder = new TagBuilder("input", new NullTestEncoder());
            htmlGenerator
                .Setup(mock => mock.GenerateTextBox(
                    tagHelper.ViewContext,
                    tagHelper.For.ModelExplorer,
                    tagHelper.For.Name,
                    null,                                   // value
                    expectedFormat,
                    null))                                  // htmlAttributes
                .Returns(tagBuilder)
                .Verifiable();

            // Act
            await tagHelper.ProcessAsync(context, output);

            // Assert
            htmlGenerator.Verify();

            Assert.True(output.SelfClosing);
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Empty(output.PreContent);
            Assert.Equal(new[] { string.Empty }, output.Content);
            Assert.Empty(output.PostContent);
            Assert.Equal(expectedTagName, output.TagName);
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
            var inputTagHelper = new InputTagHelper
            {
                For = modelExpression,
                Generator = htmlGenerator,
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

            [DataType("datetime-local")]
            public DateTime DateTimeLocal { get; set; }

            [DataType(DataType.Time)]
            public DateTimeOffset Time { get; set; }
        }

        private class NestedModel
        {
            public string Text { get; set; }
        }
    }
}
