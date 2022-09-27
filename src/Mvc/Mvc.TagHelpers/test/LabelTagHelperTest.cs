// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class LabelTagHelperTest
{
    // Model (List<Model> or Model instance), container type (Model or NestModel), model accessor,
    // property path, TagHelperOutput values. All accessors should end at a Text property.
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
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "Text") },
                    { null, typeof(Model), () => null, "Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "Text") },

                    { modelWithNull, typeof(Model), () => modelWithNull.Text, "Text",
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "Text") },
                    { modelWithNull, typeof(Model), () => modelWithNull.Text, "Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "Text") },
                    { modelWithNull, typeof(Model), () => modelWithNull.Text, "Text",
                        new TagHelperOutputContent(Environment.NewLine, "Hello World", "Hello World", "Text") },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "Text") },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "Text") },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        new TagHelperOutputContent(Environment.NewLine, "Hello World", "Hello World", "Text") },
                    { modelWithText, typeof(Model), () => modelWithNull.Text, "Text",
                        new TagHelperOutputContent(string.Empty, "Hello World", "Hello World", "Text") },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        new TagHelperOutputContent(string.Empty, "Hello World", "Hello World", "Text") },
                    { modelWithText, typeof(Model), () => modelWithNull.Text, "Text",
                        new TagHelperOutputContent("Hello World", string.Empty, "Hello World", "Text") },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        new TagHelperOutputContent("Hello World", string.Empty, "Hello World", "Text") },
                    { modelWithText, typeof(Model), () => modelWithNull.Text, "Text",
                        new TagHelperOutputContent("Hello World1", "Hello World2", "Hello World2", "Text") },
                    { modelWithText, typeof(Model), () => modelWithText.Text, "Text",
                        new TagHelperOutputContent("Hello World1", "Hello World2", "Hello World2", "Text") },

                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "NestedModel_Text") },
                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, "Hello World", "Hello World", "NestedModel_Text") },
                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "NestedModel_Text") },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "NestedModel_Text") },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, "Hello World", "Hello World", "NestedModel_Text") },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "NestedModel_Text") },
                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(string.Empty, "Hello World", "Hello World", "NestedModel_Text") },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent(string.Empty, "Hello World", "Hello World", "NestedModel_Text") },
                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent("Hello World", string.Empty, "Hello World", "NestedModel_Text") },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent("Hello World", string.Empty, "Hello World", "NestedModel_Text") },
                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent("Hello World1", "Hello World2", "Hello World2", "NestedModel_Text") },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text, "NestedModel.Text",
                        new TagHelperOutputContent("Hello World1", "Hello World2", "Hello World2", "NestedModel_Text") },

                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "z0__Text") },
                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        new TagHelperOutputContent(Environment.NewLine, "Hello World", "Hello World", "z0__Text") },
                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "z0__Text") },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "z1__Text") },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        new TagHelperOutputContent(Environment.NewLine, "Hello World", "Hello World", "z1__Text") },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "z1__Text") },
                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        new TagHelperOutputContent(string.Empty, "Hello World", "Hello World", "z0__Text") },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        new TagHelperOutputContent(string.Empty, "Hello World", "Hello World", "z1__Text") },
                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        new TagHelperOutputContent("Hello World", string.Empty, "Hello World", "z0__Text") },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        new TagHelperOutputContent("Hello World", string.Empty, "Hello World", "z1__Text") },
                    { models, typeof(Model), () => models[0].Text, "[0].Text",
                        new TagHelperOutputContent("Hello World1", "Hello World2", "Hello World2", "z0__Text") },
                    { models, typeof(Model), () => models[1].Text, "[1].Text",
                        new TagHelperOutputContent("Hello World1", "Hello World2", "Hello World2", "z1__Text") },

                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "z0__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, "Hello World", "Hello World", "z0__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "z0__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, string.Empty, "HtmlEncode[[Text]]", "z1__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        new TagHelperOutputContent(Environment.NewLine, "Hello World", "Hello World", "z1__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        new TagHelperOutputContent(string.Empty, Environment.NewLine, Environment.NewLine, "z1__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        new TagHelperOutputContent(string.Empty, "Hello World", "Hello World", "z0__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        new TagHelperOutputContent(string.Empty, "Hello World", "Hello World", "z1__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        new TagHelperOutputContent("Hello World", string.Empty, "Hello World", "z0__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        new TagHelperOutputContent("Hello World", string.Empty, "Hello World", "z1__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[0].NestedModel.Text, "[0].NestedModel.Text",
                        new TagHelperOutputContent("Hello World1", "Hello World2", "Hello World2", "z0__NestedModel_Text") },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text, "[1].NestedModel.Text",
                        new TagHelperOutputContent("Hello World1", "Hello World2", "Hello World2", "z1__NestedModel_Text") },
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
        var expectedTagName = "not-label";
        var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
                { "for", tagHelperOutputContent.ExpectedId }
            };
        var metadataProvider = new TestModelMetadataProvider();

        var containerMetadata = metadataProvider.GetMetadataForType(containerType);
        var containerExplorer = metadataProvider.GetModelExplorerForType(containerType, model);

        var propertyMetadata = metadataProvider.GetMetadataForProperty(containerType, "Text");
        var modelExplorer = containerExplorer.GetExplorerForExpression(propertyMetadata, modelAccessor());
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);

        var modelExpression = new ModelExpression(propertyPath, modelExplorer);
        var tagHelper = new LabelTagHelper(htmlGenerator)
        {
            For = modelExpression,
        };
        var expectedPreContent = "original pre-content";
        var expectedPostContent = "original post-content";

        var tagHelperContext = new TagHelperContext(
            tagName: "not-label",
            allAttributes: new TagHelperAttributeList(),
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
                tagHelperContent.AppendHtml(tagHelperOutputContent.OriginalChildContent);
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
        output.PreContent.AppendHtml(expectedPreContent);
        output.PostContent.AppendHtml(expectedPostContent);

        // LabelTagHelper checks IsContentModified so we don't want to forcibly set it if
        // tagHelperOutputContent.OriginalContent is going to be null or empty.
        if (!string.IsNullOrEmpty(tagHelperOutputContent.OriginalContent))
        {
            output.Content.AppendHtml(tagHelperOutputContent.OriginalContent);
        }

        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        tagHelper.ViewContext = viewContext;

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(expectedAttributes, output.Attributes);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(
            tagHelperOutputContent.ExpectedContent,
            HtmlContentUtilities.HtmlContentToString(output.Content));
        Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
        Assert.Equal(expectedTagName, output.TagName);
    }

    // Display name, original child content, HTML field prefix, expected child content, and expected ID.
    // Uses TagHelperOutputContent.OriginalContent to pass HtmlFieldPrefix values.
    public static TheoryData<string, string, string, string, string> DisplayNameDataSet
    {
        get
        {
            return new TheoryData<string, string, string, string, string>
                {
                    {
                        null, string.Empty, string.Empty, $"HtmlEncode[[{nameof(NestedModel.Text)}]]", nameof(NestedModel.Text)
                    },
                    {
                        string.Empty, string.Empty, string.Empty, string.Empty, nameof(NestedModel.Text)
                    },
                    {
                        "a label", string.Empty, string.Empty, "HtmlEncode[[a label]]", nameof(NestedModel.Text)
                    },
                    {
                        null, "original label", string.Empty, "original label", nameof(NestedModel.Text)
                    },
                    {
                        string.Empty, "original label", string.Empty, "original label", nameof(NestedModel.Text)
                    },
                    {
                        "a label", "original label", string.Empty, "original label", nameof(NestedModel.Text)
                    },
                    {
                        null, string.Empty, "prefix", $"HtmlEncode[[{nameof(NestedModel.Text)}]]", $"prefix_{nameof(NestedModel.Text)}"
                    },
                    {
                        string.Empty, string.Empty, "prefix", string.Empty, $"prefix_{nameof(NestedModel.Text)}"
                    },
                    {
                        "a label", string.Empty, "prefix", "HtmlEncode[[a label]]", $"prefix_{nameof(NestedModel.Text)}"
                    },
                };
        }
    }

    // Prior to aspnet/Mvc#6638 fix, helpers generated nothing in this test when displayName was empty.
    [Theory]
    [MemberData(nameof(DisplayNameDataSet))]
    public async Task ProcessAsync_GeneratesExpectedOutput_WithDisplayName(
        string displayName,
        string originalChildContent,
        string htmlFieldPrefix,
        string expectedContent,
        string expectedId)
    {
        // Arrange
        var expectedAttributes = new TagHelperAttributeList
            {
                { "for", expectedId }
            };

        var name = nameof(NestedModel.Text);
        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty<NestedModel>(name)
            .DisplayDetails(metadata => metadata.DisplayName = () => displayName);

        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var viewContext = TestableHtmlGenerator.GetViewContext(
            model: null,
            htmlGenerator: htmlGenerator,
            metadataProvider: metadataProvider);

        var viewData = new ViewDataDictionary<NestedModel>(metadataProvider, viewContext.ModelState);
        viewData.TemplateInfo.HtmlFieldPrefix = htmlFieldPrefix;
        viewContext.ViewData = viewData;

        var containerExplorer = metadataProvider.GetModelExplorerForType(typeof(NestedModel), model: null);
        var modelExplorer = containerExplorer.GetExplorerForProperty(name);
        var modelExpression = new ModelExpression(name, modelExplorer);
        var tagHelper = new LabelTagHelper(htmlGenerator)
        {
            For = modelExpression,
            ViewContext = viewContext,
        };

        var tagHelperContext = new TagHelperContext(
            tagName: "label",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            "label",
            new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.AppendHtml(originalChildContent);
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(expectedAttributes, output.Attributes);
        Assert.Equal(expectedContent, HtmlContentUtilities.HtmlContentToString(output.Content));
    }

    public class TagHelperOutputContent
    {
        public TagHelperOutputContent(
            string originalChildContent,
            string outputContent,
            string expectedContent,
            string expectedId)
        {
            OriginalChildContent = originalChildContent;
            OriginalContent = outputContent;
            ExpectedContent = expectedContent;
            ExpectedId = expectedId;
        }

        public string OriginalChildContent { get; set; }

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
