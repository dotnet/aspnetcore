// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class TextAreaTagHelperTest
    {
        // Model (List<Model> or Model instance), container type (Model or NestModel), model accessor,
        // property path / id, expected content.
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
                    { null, typeof(Model), null,
                        new NameAndId("Text", "Text"),
                        Environment.NewLine + "HtmlEncode[[]]" },

                    { modelWithNull, typeof(Model), modelWithNull.Text,
                        new NameAndId("Text", "Text"),
                        Environment.NewLine + "HtmlEncode[[]]" },
                    { modelWithText, typeof(Model), modelWithText.Text,
                        new NameAndId("Text", "Text"),
                        Environment.NewLine + "HtmlEncode[[outer text]]" },

                    { modelWithNull, typeof(NestedModel), modelWithNull.NestedModel.Text,
                        new NameAndId("NestedModel.Text", "NestedModel_Text"),
                        Environment.NewLine + "HtmlEncode[[]]" },
                    { modelWithText, typeof(NestedModel), modelWithText.NestedModel.Text,
                        new NameAndId("NestedModel.Text", "NestedModel_Text"),
                        Environment.NewLine + "HtmlEncode[[inner text]]" },

                    { models, typeof(Model), models[0].Text,
                        new NameAndId("[0].Text", "z0__Text"),
                        Environment.NewLine + "HtmlEncode[[]]" },
                    { models, typeof(Model), models[1].Text,
                        new NameAndId("[1].Text", "z1__Text"),
                        Environment.NewLine + "HtmlEncode[[outer text]]" },

                    { models, typeof(NestedModel), models[0].NestedModel.Text,
                        new NameAndId("[0].NestedModel.Text", "z0__NestedModel_Text"),
                        Environment.NewLine + "HtmlEncode[[]]" },
                    { models, typeof(NestedModel), models[1].NestedModel.Text,
                        new NameAndId("[1].NestedModel.Text", "z1__NestedModel_Text"),
                        Environment.NewLine + "HtmlEncode[[inner text]]" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestDataSet))]
        public async Task Process_GeneratesExpectedOutput(
            object container,
            Type containerType,
            object model,
            NameAndId nameAndId,
            string expectedContent)
        {
            // Arrange
            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
                { "id", nameAndId.Id },
                { "name", nameAndId.Name },
                {  "valid", "from validation attributes" },
            };
            var expectedTagName = "not-textarea";

            var metadataProvider = new TestModelMetadataProvider();

            var containerMetadata = metadataProvider.GetMetadataForType(containerType);
            var containerExplorer = metadataProvider.GetModelExplorerForType(containerType, container);

            var propertyMetadata = metadataProvider.GetMetadataForProperty(containerType, "Text");
            var modelExplorer = containerExplorer.GetExplorerForExpression(propertyMetadata, model);

            // Property name is either nameof(Model.Text) or nameof(NestedModel.Text).
            var modelExpression = new ModelExpression(nameAndId.Name, modelExplorer);
            var tagHelper = new TextAreaTagHelper
            {
                For = modelExpression,
            };

            var tagHelperContext = new TagHelperContext(
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
            var htmlAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
            var output = new TagHelperOutput(expectedTagName, htmlAttributes)
            {
                SelfClosing = true,
            };
            output.Content.SetContent("original content");

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider)
            {
                ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
            };
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
            tagHelper.ViewContext = viewContext;
            tagHelper.Generator = htmlGenerator;

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.True(output.SelfClosing);
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedContent, output.Content.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
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
        }

        private class NestedModel
        {
            public string Text { get; set; }
        }
    }
}
