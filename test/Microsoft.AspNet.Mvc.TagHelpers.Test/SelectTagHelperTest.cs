// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
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
                    "<option>outer text</option>" + Environment.NewLine +
                    "<option>inner text</option>" + Environment.NewLine +
                    "<option>other text</option>" + Environment.NewLine;
                var innerSelected = "<option></option>" + Environment.NewLine +
                    "<option>outer text</option>" + Environment.NewLine +
                    "<option selected=\"selected\">inner text</option>" + Environment.NewLine +
                    "<option>other text</option>" + Environment.NewLine;
                var outerSelected = "<option></option>" + Environment.NewLine +
                    "<option selected=\"selected\">outer text</option>" + Environment.NewLine +
                    "<option>inner text</option>" + Environment.NewLine +
                    "<option>other text</option>" + Environment.NewLine;

                return new TheoryData<object, Type, Func<object>, NameAndId, string>
                {
                    { null, typeof(Model), () => null, new NameAndId("Text", "Text"), noneSelected },

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

                    // TODO: https://github.com/aspnet/Mvc/issues/1468
                    // Skip last two test cases because DefaultHtmlGenerator evaluates expression name against
                    // ViewData, not using ModelMetadata.Model. ViewData.Eval() handles simple property paths and some
                    // dictionary lookups, but not indexing into an array or list. See #1468...
                    ////{ models, typeof(Model), () => models[1].Text,
                    ////    new NameAndId("[1].Text", "z1__Text"), outerSelected },
                    ////{ models, typeof(NestedModel), () => models[1].NestedModel.Text,
                    ////    new NameAndId("[1].NestedModel.Text", "z1__NestedModel_Text"), innerSelected },
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
            var originalAttributes = new Dictionary<string, string>
            {
                { "class", "form-control" },
            };
            var originalPostContent = "original content";

            var expectedAttributes = new Dictionary<string, string>(originalAttributes)
            {
                { "id", nameAndId.Id },
                { "name", nameAndId.Name },
                { "valid", "from validation attributes" },
            };
            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var expectedPostContent = originalPostContent;
            var expectedTagName = "not-select";

            var metadataProvider = new DataAnnotationsModelMetadataProvider();

            // Property name is either nameof(Model.Text) or nameof(NestedModel.Text).
            var metadata = metadataProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName: "Text");
            var modelExpression = new ModelExpression(nameAndId.Name, metadata);

            var tagHelperContext = new TagHelperContext(
                allAttributes: new Dictionary<string, object>(),
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult("Something"));
            var output = new TagHelperOutput(expectedTagName, originalAttributes)
            {
                PreContent = expectedPreContent,
                Content = expectedContent,
                PostContent = originalPostContent,
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
            var tagHelper = new SelectTagHelper
            {
                For = modelExpression,
                Generator = htmlGenerator,
                ViewContext = viewContext,
            };

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedPreContent, output.PreContent);
            Assert.Equal(expectedContent, output.Content);
            Assert.Equal(expectedPostContent, output.PostContent);
            Assert.False(output.SelfClosing);
            Assert.Equal(expectedTagName, output.TagName);

            Assert.NotNull(viewContext.FormContext?.FormData);
            var keyValuePair = Assert.Single(
                viewContext.FormContext.FormData,
                entry => entry.Key == SelectTagHelper.SelectedValuesFormDataKey);
            Assert.NotNull(keyValuePair.Value);
            var selectedValues = Assert.IsAssignableFrom<ICollection<string>>(keyValuePair.Value);
            Assert.InRange(selectedValues.Count, 0, 1);
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
            var originalAttributes = new Dictionary<string, string>
            {
                { "class", "form-control" },
            };
            var originalPostContent = "original content";

            var expectedAttributes = new Dictionary<string, string>(originalAttributes)
            {
                { "id", nameAndId.Id },
                { "name", nameAndId.Name },
                { "valid", "from validation attributes" },
            };
            var expectedPreContent = "original pre-content";
            var expectedContent = "original content";
            var expectedPostContent = originalPostContent + expectedOptions;
            var expectedTagName = "select";

            var metadataProvider = new DataAnnotationsModelMetadataProvider();

            // Property name is either nameof(Model.Text) or nameof(NestedModel.Text).
            var metadata = metadataProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName: "Text");
            var modelExpression = new ModelExpression(nameAndId.Name, metadata);

            var tagHelperContext = new TagHelperContext(
                allAttributes: new Dictionary<string, object>(),
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult("Something"));
            var output = new TagHelperOutput(expectedTagName, originalAttributes)
            {
                PreContent = expectedPreContent,
                Content = expectedContent,
                PostContent = originalPostContent,
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

            var items = new SelectList(new[] { "", "outer text", "inner text", "other text" });
            var savedDisabled = items.Select(item => item.Disabled).ToList();
            var savedGroup = items.Select(item => item.Group).ToList();
            var savedSelected = items.Select(item => item.Selected).ToList();
            var savedText = items.Select(item => item.Text).ToList();
            var savedValue = items.Select(item => item.Value).ToList();
            var tagHelper = new SelectTagHelper
            {
                For = modelExpression,
                Generator = htmlGenerator,
                Items = items,
                ViewContext = viewContext,
            };

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
            Assert.Equal(expectedPreContent, output.PreContent);
            Assert.Equal(expectedContent, output.Content);
            Assert.Equal(expectedPostContent, output.PostContent);
            Assert.False(output.SelfClosing);
            Assert.Equal(expectedTagName, output.TagName);

            Assert.NotNull(viewContext.FormContext?.FormData);
            var keyValuePair = Assert.Single(
                viewContext.FormContext.FormData,
                entry => entry.Key == SelectTagHelper.SelectedValuesFormDataKey);
            Assert.NotNull(keyValuePair.Value);
            var selectedValues = Assert.IsAssignableFrom<ICollection<string>>(keyValuePair.Value);
            Assert.InRange(selectedValues.Count, 0, 1);

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
            var contextAttributes = new Dictionary<string, object>
            {
                // Provided for completeness. Select tag helper does not confirm AllAttributes set is consistent.
                { attributeName, attributeValue },
            };
            var originalAttributes = new Dictionary<string, string>
            {
                { attributeName, attributeValue },
            };
            var propertyName = "Property1";
            var expectedTagName = "select";

            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult("Something"));
            var output = new TagHelperOutput(expectedTagName, originalAttributes);

            var metadataProvider = new EmptyModelMetadataProvider();
            string model = null;
            var metadata = metadataProvider.GetMetadataForType(() => model, typeof(string));

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator.Object, metadataProvider);

            // Simulate a (model => model) scenario. E.g. the calling helper may appear in a low-level template.
            var modelExpression = new ModelExpression(string.Empty, metadata);
            viewContext.ViewData.TemplateInfo.HtmlFieldPrefix = propertyName;

            ICollection<string> selectedValues = new string[0];
            htmlGenerator
                .Setup(real => real.GenerateSelect(
                    viewContext,
                    metadata,
                    null,         // optionLabel
                    string.Empty, // name
                    expectedItems,
                    false,        // allowMultiple
                    null,         // htmlAttributes
                    out selectedValues))
                .Returns((TagBuilder)null)
                .Verifiable();

            var tagHelper = new SelectTagHelper
            {
                For = modelExpression,
                Items = inputItems,
                Generator = htmlGenerator.Object,
                ViewContext = viewContext,
            };

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            htmlGenerator.Verify();

            Assert.NotNull(viewContext.FormContext?.FormData);
            var keyValuePair = Assert.Single(
                viewContext.FormContext.FormData,
                entry => entry.Key == SelectTagHelper.SelectedValuesFormDataKey);
            Assert.Same(selectedValues, keyValuePair.Value);
        }

        [Theory]
        [MemberData(nameof(RealModelTypeDataSet))]
        public async Task TagHelper_CallsGeneratorWithExpectedValues_RealModelType(
            Type modelType,
            object model,
            bool allowMultiple)
        {
            // Arrange
            var contextAttributes = new Dictionary<string, object>();
            var originalAttributes = new Dictionary<string, string>();
            var propertyName = "Property1";
            var tagName = "select";

            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult("Something"));
            var output = new TagHelperOutput(tagName, originalAttributes);

            var metadataProvider = new EmptyModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(() => model, modelType);
            var modelExpression = new ModelExpression(propertyName, metadata);

            var htmlGenerator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            var viewContext = TestableHtmlGenerator.GetViewContext(model, htmlGenerator.Object, metadataProvider);
            ICollection<string> selectedValues = new string[0];
            htmlGenerator
                .Setup(real => real.GenerateSelect(
                    viewContext,
                    metadata,
                    null,         // optionLabel
                    propertyName, // name
                    It.IsAny<IEnumerable<SelectListItem>>(),
                    allowMultiple,
                    null,         // htmlAttributes
                    out selectedValues))
                .Returns((TagBuilder)null)
                .Verifiable();

            var tagHelper = new SelectTagHelper
            {
                For = modelExpression,
                Generator = htmlGenerator.Object,
                ViewContext = viewContext,
            };

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            htmlGenerator.Verify();

            Assert.NotNull(viewContext.FormContext?.FormData);
            var keyValuePair = Assert.Single(
                viewContext.FormContext.FormData,
                entry => entry.Key == SelectTagHelper.SelectedValuesFormDataKey);
            Assert.Same(selectedValues, keyValuePair.Value);
        }

        [Fact]
        public async Task ProcessAsync_Throws_IfForNotBoundButItemsIs()
        {
            // Arrange
            var contextAttributes = new Dictionary<string, object>();
            var originalAttributes = new Dictionary<string, string>();
            var expectedTagName = "select";
            var expectedMessage = "Cannot determine body for <select>. 'asp-items' must be null if 'asp-for' is null.";

            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult("Something"));
            var output = new TagHelperOutput(expectedTagName, originalAttributes);
            var tagHelper = new SelectTagHelper
            {
                Items = Enumerable.Empty<SelectListItem>(),
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => tagHelper.ProcessAsync(tagHelperContext, output));
            Assert.Equal(expectedMessage, exception.Message);
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
