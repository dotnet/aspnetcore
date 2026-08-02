// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class SelectTagHelperTest
{
    // Model (List<Model> or Model instance), container type (Model or NestModel), model accessor,
    // property path / id, expected content.
    public static TheoryData<object, Type, Func<object>, NameAndId, string> GeneratesExpectedDataSet
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
            var noneSelected = "<option></option>" + Environment.NewLine +
                "<option>HtmlEncode[[outer text]]</option>" + Environment.NewLine +
                "<option>HtmlEncode[[inner text]]</option>" + Environment.NewLine +
                "<option>HtmlEncode[[other text]]</option>" + Environment.NewLine;
            var innerSelected = "<option></option>" + Environment.NewLine +
                "<option>HtmlEncode[[outer text]]</option>" + Environment.NewLine +
                "<option selected=\"HtmlEncode[[selected]]\">HtmlEncode[[inner text]]</option>" + Environment.NewLine +
                "<option>HtmlEncode[[other text]]</option>" + Environment.NewLine;
            var outerSelected = "<option></option>" + Environment.NewLine +
                "<option selected=\"HtmlEncode[[selected]]\">HtmlEncode[[outer text]]</option>" + Environment.NewLine +
                "<option>HtmlEncode[[inner text]]</option>" + Environment.NewLine +
                "<option>HtmlEncode[[other text]]</option>" + Environment.NewLine;

            return new TheoryData<object, Type, Func<object>, NameAndId, string>
                {
                    { null, typeof(Model), () => null, new NameAndId("Text", "Text"), noneSelected },

                    // Imitate a temporary variable set from somewhere else in the view model.
                    { null, typeof(Model), () => modelWithText.NestedModel.Text,
                        new NameAndId("item.Text", "item_Text"), innerSelected },

                    { modelWithNull, typeof(Model), () => modelWithNull.Text,
                        new NameAndId("Text", "Text"), noneSelected },
                    { modelWithText, typeof(Model), () => modelWithText.Text,
                        new NameAndId("Text", "Text"), outerSelected },

                    { modelWithNull, typeof(NestedModel), () => modelWithNull.NestedModel.Text,
                        new NameAndId("NestedModel.Text", "NestedModel_Text"), noneSelected },
                    { modelWithText, typeof(NestedModel), () => modelWithText.NestedModel.Text,
                        new NameAndId("NestedModel.Text", "NestedModel_Text"), innerSelected },

                    { models, typeof(Model), () => models[0].Text,
                        new NameAndId("[0].Text", "z0__Text"), noneSelected },
                    { models, typeof(NestedModel), () => models[0].NestedModel.Text,
                        new NameAndId("[0].NestedModel.Text", "z0__NestedModel_Text"), noneSelected },

                    { models, typeof(Model), () => models[1].Text,
                        new NameAndId("[1].Text", "z1__Text"), outerSelected },
                    { models, typeof(NestedModel), () => models[1].NestedModel.Text,
                        new NameAndId("[1].NestedModel.Text", "z1__NestedModel_Text"), innerSelected },
                };
        }
    }

    // Items property value, attribute name, attribute value, expected items value (passed to generator). Provides
    // cross product of Items and attributes. These values should not interact.
    public static TheoryData<IEnumerable<SelectListItem>, string, string, IEnumerable<SelectListItem>>
        ItemsAndMultipleDataSet
    {
        get
        {
            var arrayItems = new[] { new SelectListItem() };
            var listItems = new List<SelectListItem>();
            var multiItems = new MultiSelectList(Enumerable.Range(0, 5));
            var selectItems = new SelectList(Enumerable.Range(0, 5));
            var itemsData = new[]
            {
                    new[] { (IEnumerable<SelectListItem>)null, Enumerable.Empty<SelectListItem>() },
                    new[] { arrayItems, arrayItems },
                    new[] { listItems, listItems },
                    new[] { multiItems, multiItems },
                    new[] { selectItems, selectItems },
                };
            var attributeData = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    // SelectTagHelper ignores all "multiple" attribute values.
                    { "multiple", null },
                    { "mUltiple", string.Empty },
                    { "muLtiple", "true" },
                    { "Multiple", "false" },
                    { "MUltiple", "multiple" },
                    { "MuLtiple", "Multiple" },
                    { "mUlTiPlE", "mUlTiPlE" },
                    { "mULTiPlE", "MULTIPLE" },
                    { "mUlTIPlE", "Invalid" },
                    { "MULTiPLE", "0" },
                    { "MUlTIPLE", "1" },
                    { "MULTIPLE", "__true" },
                    // SelectTagHelper also ignores non-"multiple" attributes.
                    { "multiple_", "multiple" },
                    { "not-multiple", "multiple" },
                    { "__multiple", "multiple" },
                };

            var theoryData =
                new TheoryData<IEnumerable<SelectListItem>, string, string, IEnumerable<SelectListItem>>();
            foreach (var items in itemsData)
            {
                foreach (var attribute in attributeData)
                {
                    theoryData.Add(items[0], attribute.Key, attribute.Value, items[1]);
                }
            }

            return theoryData;
        }
    }

    // Model type, model, allowMultiple expected value
    public static TheoryData<Type, object, bool> RealModelTypeDataSet
    {
        get
        {
            return new TheoryData<Type, object, bool>
                {
                    { typeof(object), string.Empty, false },
                    { typeof(string), null, false },
                    { typeof(int?), null, false },
                    { typeof(int), 23, false },
                    { typeof(IEnumerable), null, true },
                    { typeof(IEnumerable<string>), null, true },
                    { typeof(List<int>), null, true },
                    { typeof(object), new[] { "", "", "" }, true },
                    { typeof(object), new List<string>(), true },
                };
        }
    }

    [Theory]
    [MemberData(nameof(GeneratesExpectedDataSet))]
    public async Task ProcessAsync_GeneratesExpectedOutput(
        object model,
        Type containerType,
        Func<object> modelAccessor,
        NameAndId nameAndId,
        string ignored)
    {
        // Arrange
        var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
        var originalPostContent = "original content";

        var expectedAttributes = new TagHelperAttributeList(originalAttributes)
            {
                { "id", nameAndId.Id },
                { "name", nameAndId.Name },
                { "valid", "from validation attributes" },
            };
        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var expectedPostContent = originalPostContent;
        var expectedTagName = "not-select";

        var metadataProvider = new TestModelMetadataProvider();
        var containerMetadata = metadataProvider.GetMetadataForType(containerType);
        var containerExplorer = metadataProvider.GetModelExplorerForType(containerType, model);

        var propertyMetadata = metadataProvider.GetMetadataForProperty(containerType, "Text");
        var modelExplorer = containerExplorer.GetExplorerForExpression(propertyMetadata, modelAccessor());

        var modelExpression = new ModelExpression(nameAndId.Name, modelExplorer);

        var tagHelperContext = new TagHelperContext(
            tagName: "select",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
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
            TagMode = TagMode.SelfClosing,
        };
        output.PreContent.SetContent(expectedPreContent);
        output.Content.SetContent(expectedContent);
        output.PostContent.SetContent(originalPostContent);

        var htmlGenerator = new TestableHtmlGenerator(metadataProvider)
        {
            ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
        };
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        var tagHelper = new SelectTagHelper(htmlGenerator)
        {
            For = modelExpression,
            ViewContext = viewContext,
        };

        // Act
        tagHelper.Init(tagHelperContext);
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal(expectedAttributes, output.Attributes);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        Assert.Equal(expectedTagName, output.TagName);

        Assert.Single(
            tagHelperContext.Items,
            entry => (Type)entry.Key == typeof(SelectTagHelper));
    }

    [Theory]
    [MemberData(nameof(GeneratesExpectedDataSet))]
    public async Task ProcessAsync_WithItems_GeneratesExpectedOutput_DoesNotChangeSelectList(
        object model,
        Type containerType,
        Func<object> modelAccessor,
        NameAndId nameAndId,
        string expectedOptions)
    {
        // Arrange
        var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
        var originalPostContent = "original content";

        var expectedAttributes = new TagHelperAttributeList(originalAttributes)
            {
                { "id", nameAndId.Id },
                { "name", nameAndId.Name },
                { "valid", "from validation attributes" },
            };
        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var expectedPostContent = originalPostContent + expectedOptions;
        var expectedTagName = "select";

        var metadataProvider = new TestModelMetadataProvider();

        var containerMetadata = metadataProvider.GetMetadataForType(containerType);
        var containerExplorer = metadataProvider.GetModelExplorerForType(containerType, model);

        var propertyMetadata = metadataProvider.GetMetadataForProperty(containerType, "Text");
        var modelExplorer = containerExplorer.GetExplorerForExpression(propertyMetadata, modelAccessor());

        var modelExpression = new ModelExpression(nameAndId.Name, modelExplorer);

        var tagHelperContext = new TagHelperContext(
            tagName: "select",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            originalAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.AppendHtml("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            })
        {
            TagMode = TagMode.SelfClosing,
        };
        output.PreContent.AppendHtml(expectedPreContent);
        output.Content.AppendHtml(expectedContent);
        output.PostContent.AppendHtml(originalPostContent);

        var htmlGenerator = new TestableHtmlGenerator(metadataProvider)
        {
            ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
        };
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);

        var items = new SelectList(new[] { "", "outer text", "inner text", "other text" });
        var savedDisabled = items.Select(item => item.Disabled).ToList();
        var savedGroup = items.Select(item => item.Group).ToList();
        var savedSelected = items.Select(item => item.Selected).ToList();
        var savedText = items.Select(item => item.Text).ToList();
        var savedValue = items.Select(item => item.Value).ToList();

        var tagHelper = new SelectTagHelper(htmlGenerator)
        {
            For = modelExpression,
            Items = items,
            ViewContext = viewContext,
        };

        // Act
        tagHelper.Init(tagHelperContext);
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal(expectedAttributes, output.Attributes);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(expectedPostContent, HtmlContentUtilities.HtmlContentToString(output.PostContent));
        Assert.Equal(expectedTagName, output.TagName);

        Assert.Single(
            tagHelperContext.Items,
            entry => (Type)entry.Key == typeof(SelectTagHelper));

        Assert.Equal(savedDisabled, items.Select(item => item.Disabled));
        Assert.Equal(savedGroup, items.Select(item => item.Group));
        Assert.Equal(savedSelected, items.Select(item => item.Selected));
        Assert.Equal(savedText, items.Select(item => item.Text));
        Assert.Equal(savedValue, items.Select(item => item.Value));
    }

    [Fact]
    public async Task ProcessAsync_WithItems_AndNoModelExpression_GeneratesExpectedOutput()
    {
        // Arrange
        var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
        var originalPostContent = "original content";

        var expectedAttributes = new TagHelperAttributeList(originalAttributes);
        var selectItems = new SelectList(Enumerable.Range(0, 5));
        var expectedOptions = "<option>HtmlEncode[[0]]</option>" + Environment.NewLine
            + "<option>HtmlEncode[[1]]</option>" + Environment.NewLine
            + "<option>HtmlEncode[[2]]</option>" + Environment.NewLine
            + "<option>HtmlEncode[[3]]</option>" + Environment.NewLine
            + "<option>HtmlEncode[[4]]</option>" + Environment.NewLine;

        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var expectedPostContent = originalPostContent + expectedOptions;
        var expectedTagName = "select";

        var tagHelperContext = new TagHelperContext(
            tagName: "select",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            originalAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.AppendHtml("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            })
        {
            TagMode = TagMode.SelfClosing,
        };
        output.PreContent.AppendHtml(expectedPreContent);
        output.Content.AppendHtml(expectedContent);
        output.PostContent.AppendHtml(originalPostContent);

        var metadataProvider = new TestModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var viewContext = TestableHtmlGenerator.GetViewContext(
            model: null,
            htmlGenerator: htmlGenerator,
            metadataProvider: metadataProvider);

        var tagHelper = new SelectTagHelper(htmlGenerator)
        {
            Items = selectItems,
            ViewContext = viewContext,
        };

        // Act
        tagHelper.Init(tagHelperContext);
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal(expectedAttributes, output.Attributes);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(expectedPostContent, HtmlContentUtilities.HtmlContentToString(output.PostContent));
        Assert.Equal(expectedTagName, output.TagName);

        var kvp = Assert.Single(tagHelperContext.Items);
        Assert.Equal(typeof(SelectTagHelper), kvp.Key);
        Assert.Null(kvp.Value);
    }

    [Theory]
    [MemberData(nameof(GeneratesExpectedDataSet))]
    public async Task ProcessAsyncInTemplate_WithItems_GeneratesExpectedOutput_DoesNotChangeSelectList(
        object model,
        Type containerType,
        Func<object> modelAccessor,
        NameAndId nameAndId,
        string expectedOptions)
    {
        // Arrange
        var originalAttributes = new TagHelperAttributeList
            {
                { "class", "form-control" },
            };
        var originalPostContent = "original content";

        var expectedAttributes = new TagHelperAttributeList(originalAttributes)
            {
                { "id", nameAndId.Id },
                { "name", nameAndId.Name },
                { "valid", "from validation attributes" },
            };
        var expectedPreContent = "original pre-content";
        var expectedContent = "original content";
        var expectedPostContent = originalPostContent + expectedOptions;
        var expectedTagName = "select";

        var metadataProvider = new TestModelMetadataProvider();

        var containerMetadata = metadataProvider.GetMetadataForType(containerType);
        var containerExplorer = metadataProvider.GetModelExplorerForType(containerType, model);

        var propertyMetadata = metadataProvider.GetMetadataForProperty(containerType, "Text");
        var modelExplorer = containerExplorer.GetExplorerForExpression(propertyMetadata, modelAccessor());

        var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);

        var tagHelperContext = new TagHelperContext(
            tagName: "select",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            originalAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.AppendHtml("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            })
        {
            TagMode = TagMode.SelfClosing,
        };
        output.PreContent.AppendHtml(expectedPreContent);
        output.Content.AppendHtml(expectedContent);
        output.PostContent.AppendHtml(originalPostContent);

        var htmlGenerator = new TestableHtmlGenerator(metadataProvider)
        {
            ValidationAttributes =
                {
                    {  "valid", "from validation attributes" },
                }
        };
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        viewContext.ViewData.TemplateInfo.HtmlFieldPrefix = nameAndId.Name;

        var items = new SelectList(new[] { "", "outer text", "inner text", "other text" });
        var savedDisabled = items.Select(item => item.Disabled).ToList();
        var savedGroup = items.Select(item => item.Group).ToList();
        var savedSelected = items.Select(item => item.Selected).ToList();
        var savedText = items.Select(item => item.Text).ToList();
        var savedValue = items.Select(item => item.Value).ToList();
        var tagHelper = new SelectTagHelper(htmlGenerator)
        {
            For = modelExpression,
            Items = items,
            ViewContext = viewContext,
        };

        // Act
        tagHelper.Init(tagHelperContext);
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal(expectedAttributes, output.Attributes);
        Assert.Equal(expectedPreContent, output.PreContent.GetContent());
        Assert.Equal(expectedContent, output.Content.GetContent());
        Assert.Equal(expectedPostContent, HtmlContentUtilities.HtmlContentToString(output.PostContent));
        Assert.Equal(expectedTagName, output.TagName);

        Assert.Single(
            tagHelperContext.Items,
            entry => (Type)entry.Key == typeof(SelectTagHelper));

        Assert.Equal(savedDisabled, items.Select(item => item.Disabled));
        Assert.Equal(savedGroup, items.Select(item => item.Group));
        Assert.Equal(savedSelected, items.Select(item => item.Selected));
        Assert.Equal(savedText, items.Select(item => item.Text));
        Assert.Equal(savedValue, items.Select(item => item.Value));
    }

    [Theory]
    [MemberData(nameof(ItemsAndMultipleDataSet))]
    public async Task ProcessAsync_CallsGeneratorWithExpectedValues_ItemsAndAttribute(
        IEnumerable<SelectListItem> inputItems,
        string attributeName,
        string attributeValue,
        IEnumerable<SelectListItem> expectedItems)
    {
        // Arrange
        var contextAttributes = new TagHelperAttributeList
            {
                // Provided for completeness. Select tag helper does not confirm AllAttributes set is consistent.
                { attributeName, attributeValue },
            };
        var originalAttributes = new TagHelperAttributeList
            {
                { attributeName, attributeValue },
            };
        var propertyName = "Property1";
        var expectedTagName = "select";

        var tagHelperContext = new TagHelperContext(
            tagName: "select",
            allAttributes: contextAttributes,
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            expectedTagName,
            originalAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
        var metadataProvider = new EmptyModelMetadataProvider();
        string model = null;
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model);

        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator.Object, metadataProvider);

        // Simulate a (model => model) scenario. E.g. the calling helper may appear in a low-level template.
        var modelExpression = new ModelExpression(string.Empty, modelExplorer);
        viewContext.ViewData.TemplateInfo.HtmlFieldPrefix = propertyName;

        var currentValues = new string[0];
        htmlGenerator
            .Setup(real => real.GetCurrentValues(
                viewContext,
                modelExplorer,
                string.Empty,   // expression
                false))         // allowMultiple
            .Returns(currentValues)
            .Verifiable();
        htmlGenerator
            .Setup(real => real.GenerateSelect(
                viewContext,
                modelExplorer,
                null,           // optionLabel
                string.Empty,   // expression
                expectedItems,
                currentValues,
                false,          // allowMultiple
                null))          // htmlAttributes
            .Returns((TagBuilder)null)
            .Verifiable();

        var tagHelper = new SelectTagHelper(htmlGenerator.Object)
        {
            For = modelExpression,
            Items = inputItems,
            ViewContext = viewContext,
        };

        // Act
        tagHelper.Init(tagHelperContext);
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        htmlGenerator.Verify();

        var keyValuePair = Assert.Single(
            tagHelperContext.Items,
            entry => (Type)entry.Key == typeof(SelectTagHelper));
        var actualCurrentValues = Assert.IsType<CurrentValues>(keyValuePair.Value);
        Assert.Same(currentValues, actualCurrentValues.Values);
    }

    [Theory]
    [MemberData(nameof(RealModelTypeDataSet))]
    public async Task TagHelper_CallsGeneratorWithExpectedValues_RealModelType(
        Type modelType,
        object model,
        bool allowMultiple)
    {
        // Arrange
        var contextAttributes = new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>());
        var originalAttributes = new TagHelperAttributeList();
        var propertyName = "Property1";
        var tagName = "select";

        var tagHelperContext = new TagHelperContext(
            tagName: "select",
            allAttributes: contextAttributes,
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            tagName,
            originalAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
        var metadataProvider = new EmptyModelMetadataProvider();
        var modelExplorer = metadataProvider.GetModelExplorerForType(modelType, model);
        var modelExpression = new ModelExpression(propertyName, modelExplorer);

        var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator.Object, metadataProvider);
        var currentValues = new string[0];
        htmlGenerator
            .Setup(real => real.GetCurrentValues(
                viewContext,
                modelExplorer,
                propertyName,   // expression
                allowMultiple))
            .Returns(currentValues)
            .Verifiable();
        htmlGenerator
            .Setup(real => real.GenerateSelect(
                viewContext,
                modelExplorer,
                null,           // optionLabel
                propertyName,   // expression
                It.IsAny<IEnumerable<SelectListItem>>(),
                currentValues,
                allowMultiple,
                null))          // htmlAttributes
            .Returns((TagBuilder)null)
            .Verifiable();

        var tagHelper = new SelectTagHelper(htmlGenerator.Object)
        {
            For = modelExpression,
            ViewContext = viewContext,
        };

        // Act
        tagHelper.Init(tagHelperContext);
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        htmlGenerator.Verify();

        var keyValuePair = Assert.Single(
            tagHelperContext.Items,
            entry => (Type)entry.Key == typeof(SelectTagHelper));
        var actualCurrentValues = Assert.IsType<CurrentValues>(keyValuePair.Value);
        Assert.Same(currentValues, actualCurrentValues.Values);
    }

    [Fact]
    public void Process_WithEmptyForName_Throws()
    {
        // Arrange
        var expectedMessage = "The name of an HTML field cannot be null or empty. Instead use methods " +
            "Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNetCore.Mvc.Rendering." +
            "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value.";
        var expectedTagName = "select";

        var metadataProvider = new EmptyModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var model = "model-value";
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model);
        var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        var tagHelper = new SelectTagHelper(htmlGenerator)
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
        var expectedTagName = "select";

        var metadataProvider = new EmptyModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var model = "model-value";
        var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model);
        var modelExpression = new ModelExpression(name: string.Empty, modelExplorer: modelExplorer);
        var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator, metadataProvider);
        var tagHelper = new SelectTagHelper(htmlGenerator)
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
        Assert.False(output.IsContentModified);

        var attribute = Assert.Single(output.Attributes);
        Assert.Equal("name", attribute.Name);
        Assert.Equal(expectedAttributeValue, attribute.Value);
    }

    [Fact]
    public void Process_PassesNameThrough_EvenIfNullFor()
    {
        // Arrange
        var expectedAttributeValue = "-expression-";
        var expectedTagName = "span";

        var selectList = Array.Empty<SelectListItem>();
        var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
        generator
            .Setup(gen => gen.GenerateGroupsAndOptions(/* optionLabel: */ null, selectList))
            .Returns(HtmlString.Empty)
            .Verifiable();

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewContext = TestableHtmlGenerator.GetViewContext(
            model: null,
            htmlGenerator: generator.Object,
            metadataProvider: metadataProvider);

        var tagHelper = new SelectTagHelper(generator.Object)
        {
            Items = selectList,
            Name = expectedAttributeValue,
            ViewContext = viewContext,
        };

        var attributes = new TagHelperAttributeList
            {
                { "name", expectedAttributeValue },
            };

        var tagHelperContext = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput(
            expectedTagName,
            new TagHelperAttributeList(),
            (_, __) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        // Act
        tagHelper.Process(tagHelperContext, output);

        // Assert
        generator.VerifyAll();
        Assert.Equal(expectedTagName, output.TagName);
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
