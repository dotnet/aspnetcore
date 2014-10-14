// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class TextAreaTagHelperTest
    {
        // Model (List<Model> or Model instance), container type (Model or NestModel), model accessor,
        // property path, expected content.
        public static TheoryData<object, Type, Func<object>, string, string> TestDataSet
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

                return new TheoryData<object, Type, Func<object>, string, string>
                {
                    { null, typeof(Model), () => null, "Text",
                        Environment.NewLine },

                    { modelWithNull, typeof(Model), () => modelWithNull.Text, "Text",
                        Environment.NewLine },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        Environment.NewLine + "outer text" },

                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        Environment.NewLine },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        Environment.NewLine + "inner text" },

                        // Top-level indexing does not work end-to-end due to code generation issue #1345.
                        // TODO: Remove above comment when #1345 is fixed.
                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        Environment.NewLine },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        Environment.NewLine + "outer text" },

                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        Environment.NewLine },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        Environment.NewLine + "inner text" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestDataSet))]
        public async Task Process_GeneratesExpectedOutput(
            object model,
            Type containerType,
            Func<object> modelAccessor,
            string propertyPath,
            string expectedContent)
        {
            // Arrange
            var expectedAttributes = new Dictionary<string, string>
            {
                { "class", "form-control" },
                { "id", propertyPath },
                { "name", propertyPath },
                {  "valid", "from validation attributes" },
            };
            var expectedTagName = "textarea";

            var metadataProvider = new DataAnnotationsModelMetadataProvider();

            // Property name is either nameof(Model.Text) or nameof(NestedModel.Text).
            var metadata = metadataProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName: "Text");
            var modelExpression = new ModelExpression(propertyPath, metadata);
            var tagHelper = new TextAreaTagHelper
            {
                For = modelExpression,
            };

            var tagHelperContext = new TagHelperContext(new Dictionary<string, object>());
            var htmlAttributes = new Dictionary<string, string>
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput("original tag name", htmlAttributes, "original content")
            {
                SelfClosing = true,
            };

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider)
            {
                ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
            };
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            var activator = new DefaultTagHelperActivator();
            activator.Activate(tagHelper, viewContext);

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedContent, output.Content);
            Assert.False(output.SelfClosing);
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Fact]
        public async Task TagHelper_LeavesOutputUnchanged_IfForNotBound()
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
            var tagHelper = new TextAreaTagHelper();

            var tagHelperContext = new TagHelperContext(new Dictionary<string, object>());
            var output = new TagHelperOutput(expectedTagName, expectedAttributes, expectedContent)
            {
                SelfClosing = true,
            };

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider)
            {
                ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
            };
            Model model = null;
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            var activator = new DefaultTagHelperActivator();
            activator.Activate(tagHelper, viewContext);

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedContent, output.Content);
            Assert.True(output.SelfClosing);
            Assert.Equal(expectedTagName, output.TagName);
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
