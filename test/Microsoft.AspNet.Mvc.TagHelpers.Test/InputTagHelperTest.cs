// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.WebEncoders;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class InputTagHelperTest
    {
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
            var expectedAttributes = new Dictionary<string, string>
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
                allAttributes: new Dictionary<string, object>(),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var originalAttributes = new Dictionary<string, string>
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
            var expectedContent = originalContent + "<input class=\"form-control\" /><hidden />";
            var expectedPostContent = "original post-content";

            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>(),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var originalAttributes = new Dictionary<string, string>
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
            var tagBuilder = new TagBuilder("input", new HtmlEncoder())
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
                .Returns(new TagBuilder("hidden", new HtmlEncoder()))
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
            var contextAttributes = new Dictionary<string, object>
            {
                { "class", "form-control" },
            };
            if (!string.IsNullOrEmpty(inputTypeName))
            {
                contextAttributes["type"] = inputTypeName;  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new Dictionary<string, string>
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
            var originalAttributes = new Dictionary<string, string>
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

            var tagBuilder = new TagBuilder("input", new HtmlEncoder())
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
            var contextAttributes = new Dictionary<string, object>
            {
                { "class", "form-control" },
            };
            if (!string.IsNullOrEmpty(inputTypeName))
            {
                contextAttributes["type"] = inputTypeName;  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new Dictionary<string, string>
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
            var originalAttributes = new Dictionary<string, string>
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

            var tagBuilder = new TagBuilder("input", new HtmlEncoder())
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
            var contextAttributes = new Dictionary<string, object>
            {
                { "class", "form-control" },
                { "value", value },
            };
            if (!string.IsNullOrEmpty(inputTypeName))
            {
                contextAttributes["type"] = inputTypeName;  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new Dictionary<string, string>
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
            var originalAttributes = new Dictionary<string, string>
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

            var tagBuilder = new TagBuilder("input", new HtmlEncoder())
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
            var contextAttributes = new Dictionary<string, object>
            {
                { "class", "form-control" },
            };
            if (!string.IsNullOrEmpty(inputTypeName))
            {
                contextAttributes["type"] = inputTypeName;  // Support restoration of type attribute, if any.
            }

            var expectedAttributes = new Dictionary<string, string>
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
            var originalAttributes = new Dictionary<string, string>
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

            var tagBuilder = new TagBuilder("input", new HtmlEncoder())
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
        }

        private class NestedModel
        {
            public string Text { get; set; }
        }
    }
}
