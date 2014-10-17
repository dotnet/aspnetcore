// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class LabelTagHelperTest
    {
        // Model (List<Model> or Model instance), container type (Model or NestModel), model accessor,
        // property path, TagHelperOutput.Content values.
        public static TheoryData<object, Type, Func<object>, string, TagHelperOutputContent> TestDataSet
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

                return new TheoryData<object, Type, Func<object>, string, TagHelperOutputContent>
                {
                    { null, typeof(Model), () => null, "Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "Text") },

                    { modelWithNull, typeof(Model), () => modelWithNull.Text, "Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "Text") },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "Text") },
                    { modelWithText, typeof(Model), () => modelWithNull.Text, "Text",
                        new TagHelperOutputContent("Hello World", "Hello World", "Text") },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        new TagHelperOutputContent("Hello World", "Hello World", "Text") },

                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "NestedModel_Text") },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "NestedModel_Text") },
                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent("Hello World", "Hello World", "NestedModel_Text") },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent("Hello World", "Hello World", "NestedModel_Text") },

                    // Note: Tests cases below here will not work in practice due to current limitations on indexing 
                    // into ModelExpressions. Will be fixed in https://github.com/aspnet/Mvc/issues/1345.
                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "z0__Text") },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "z1__Text") },
                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        new TagHelperOutputContent("Hello World", "Hello World", "z0__Text") },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        new TagHelperOutputContent("Hello World", "Hello World", "z1__Text") },

                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "z0__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, "Text", "z1__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        new TagHelperOutputContent("Hello World", "Hello World", "z0__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        new TagHelperOutputContent("Hello World", "Hello World", "z1__NestedModel_Text") },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestDataSet))]
        public async Task ProcessAsync_GeneratesExpectedOutput(
            object model,
            Type containerType,
            Func<object> modelAccessor,
            string propertyPath,
            TagHelperOutputContent tagHelperOutputContent)
        {
            // Arrange
            var expectedAttributes = new Dictionary<string, string>
            {
                { "class", "form-control" },
                { "for", tagHelperOutputContent.ExpectedId }
            };
            var metadataProvider = new DataAnnotationsModelMetadataProvider();

            // Property name is either nameof(Model.Text) or nameof(NestedModel.Text).
            var metadata = metadataProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName: "Text");
            var modelExpression = new ModelExpression(propertyPath, metadata);
            var tagHelper = new LabelTagHelper
            {
                For = modelExpression,
            };

            var tagHelperContext = new TagHelperContext(allAttributes: new Dictionary<string, object>());
            var htmlAttributes = new Dictionary<string, string>
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput("A random tag name", htmlAttributes, tagHelperOutputContent.OriginalContent);
            var expectedTagName = "label";
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            tagHelper.ViewContext = viewContext;
            tagHelper.Generator = htmlGenerator;

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(tagHelperOutputContent.ExpectedContent, output.Content);
            Assert.False(output.SelfClosing);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Fact]
        public async Task TagHelper_LeavesOutputUnchanged_IfForNotBound2()
        {
            // Arrange
            var expectedAttributes = new Dictionary<string, string>
            {
                { "class", "form-control" },
            };
            var expectedContent = "original content";
            var expectedTagName = "original tag name";

            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForProperty(
                modelAccessor: () => null,
                containerType: typeof(Model),
                propertyName: nameof(Model.Text));
            var modelExpression = new ModelExpression(nameof(Model.Text), metadata);
            var tagHelper = new LabelTagHelper();

            var tagHelperContext = new TagHelperContext(allAttributes: new Dictionary<string, object>());
            var output = new TagHelperOutput(expectedTagName, expectedAttributes, expectedContent);

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            Model model = null;
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            tagHelper.ViewContext = viewContext;
            tagHelper.Generator = htmlGenerator;

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedContent, output.Content);
            Assert.Equal(expectedTagName, output.TagName);
        }

        public class TagHelperOutputContent
        {
            public TagHelperOutputContent(string outputContent, string expectedContent, string expectedId)
            {
                OriginalContent = outputContent;
                ExpectedContent = expectedContent;
                ExpectedId = expectedId;
            }

            public string OriginalContent { get; set; }

            public string ExpectedContent { get; set; }

            public string ExpectedId { get; set; }
        }

        private class Model
        {
            public string Text { get; set; }

            public NestedModel NestedModel { get; set; }
        }

        private class NestedModel
        {
            public string Text { get; set; }
        }
    }
}