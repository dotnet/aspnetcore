// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

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
                        Environment.NewLine },

                    { modelWithNull, typeof(Model), modelWithNull.Text,
                        new NameAndId("Text", "Text"),
                        Environment.NewLine },
                    { modelWithText, typeof(Model), modelWithText.Text,
                        new NameAndId("Text", "Text"),
                        Environment.NewLine + "HtmlEncode[[outer text]]" },

                    { modelWithNull, typeof(NestedModel), modelWithNull.NestedModel.Text,
                        new NameAndId("NestedModel.Text", "NestedModel_Text"),
                        Environment.NewLine },
                    { modelWithText, typeof(NestedModel), modelWithText.NestedModel.Text,
                        new NameAndId("NestedModel.Text", "NestedModel_Text"),
                        Environment.NewLine + "HtmlEncode[[inner text]]" },

                    { models, typeof(Model), models[0].Text,
                        new NameAndId("[0].Text", "z0__Text"),
                        Environment.NewLine },
                    { models, typeof(Model), models[1].Text,
                        new NameAndId("[1].Text", "z1__Text"),
                        Environment.NewLine + "HtmlEncode[[outer text]]" },

                    { models, typeof(NestedModel), models[0].NestedModel.Text,
                        new NameAndId("[0].NestedModel.Text", "z0__NestedModel_Text"),
                        Environment.NewLine },
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

        var htmlGenerator = new TestableHtmlGenerator(metadataProvider)
        {
            ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
        };

        // Property name is either nameof(Model.Text) or nameof(NestedModel.Text).
        var modelExpression = new ModelExpression(nameAndId.Name, modelExplorer);
        var tagHelper = new TextAreaTagHelper(htmlGenerator)
        {
            For = modelExpression,
        };

        var tagHelperContext = new TagHelperContext(
            tagName: "text-area",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var htmlAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
        var output = new TagHelperOutput(
            expectedTagName,
            htmlAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            })
        {
            TagMode = TagMode.SelfClosing,
        };
        output.Content.SetContent("original content");

        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        tagHelper.ViewContext = viewContext;

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal(expectedAttributes, output.Attributes);
        Assert.Equal(expectedContent, HtmlContentUtilities.HtmlContentToString(output.Content));
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Fact]
    public void Process_WithEmptyForName_Throws()
    {
        // Arrange
        var expectedMessage = "The name of an HTML field cannot be null or empty. Instead use methods " +
            "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
            "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";
        var expectedTagName = "textarea";

        var metadataProvider = new EmptyModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var model = "model-value";
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model);
        var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        var tagHelper = new TextAreaTagHelper(htmlGenerator)
        {
            For = modelExpression,
            ViewContext = viewContext,
        };

        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput(
            expectedTagName,
            new TagHelperAttributeList(),
            (_, __) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => tagHelper.Process(context, output),
            paramName: "expression",
            exceptionMessage: expectedMessage);
    }

    [Fact]
    public void Process_WithEmptyForName_DoesNotThrow_WithName()
    {
        // Arrange
        var expectedAttributeValue = "-expression-";
        var expectedContent = Environment.NewLine + "HtmlEncode[[model-value]]";
        var expectedTagName = "textarea";

        var metadataProvider = new EmptyModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var model = "model-value";
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model);
        var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        var tagHelper = new TextAreaTagHelper(htmlGenerator)
        {
            For = modelExpression,
            Name = expectedAttributeValue,
            ViewContext = viewContext,
        };

        var attributes = new TagHelperAttributeList
            {
                { "name", expectedAttributeValue },
            };

        var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput(
            expectedTagName,
            new TagHelperAttributeList(),
            (_, __) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        // Act
        tagHelper.Process(context, output);

        // Assert
        Assert.Equal(expectedTagName, output.TagName);
        Assert.Equal(expectedContent, HtmlContentUtilities.HtmlContentToString(output.Content));

        var attribute = Assert.Single(output.Attributes);
        Assert.Equal("name", attribute.Name);
        Assert.Equal(expectedAttributeValue, attribute.Value);
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
