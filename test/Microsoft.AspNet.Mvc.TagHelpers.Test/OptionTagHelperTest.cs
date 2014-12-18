// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class OptionTagHelperTest
    {
        // Original content, selected attribute, value attribute, selected values (to place in FormContext.FormData)
        // and expected output (concatenation of TagHelperOutput generations).
        public static TheoryData<string, string, string, ICollection<string>, string> GeneratesExpectedDataSet
        {
            get
            {
                return new TheoryData<string, string, string, ICollection<string>, string>
                {
                    { null, null, null, null,
                        "<not-option label=\"my-label\"></not-option>" },
                    { null, string.Empty, "value", null,
                        "<not-option label=\"my-label\" value=\"value\" selected=\"\"></not-option>" },
                    { null, "selected", "value", null,
                        "<not-option label=\"my-label\" value=\"value\" selected=\"selected\"></not-option>" },
                    { null, null, "value", new string[0],
                        "<not-option label=\"my-label\" value=\"value\"></not-option>" },
                    { null, null, "value", new [] { string.Empty, },
                        "<not-option label=\"my-label\" value=\"value\"></not-option>" },
                    { null, string.Empty, "value", new [] { string.Empty, },
                        "<not-option label=\"my-label\" value=\"value\" selected=\"\"></not-option>" },
                    { null, null, "value", new [] { "value", },
                        "<not-option label=\"my-label\" value=\"value\" selected=\"selected\"></not-option>" },
                    { null, null, "value", new [] { string.Empty, "value", },
                        "<not-option label=\"my-label\" value=\"value\" selected=\"selected\"></not-option>" },

                    { string.Empty, null, null, null,
                        "<not-option label=\"my-label\"></not-option>" },
                    { string.Empty, string.Empty, null, null,
                        "<not-option label=\"my-label\" selected=\"\"></not-option>" },
                    { string.Empty, "selected", null, null,
                        "<not-option label=\"my-label\" selected=\"selected\"></not-option>" },
                    { string.Empty, null, null, new string[0],
                        "<not-option label=\"my-label\"></not-option>" },
                    { string.Empty, null, null, new [] { string.Empty, },
                        "<not-option label=\"my-label\" selected=\"selected\"></not-option>" },
                    { string.Empty, string.Empty, null, new [] { string.Empty, },
                        "<not-option label=\"my-label\" selected=\"\"></not-option>" },
                    { string.Empty, null, null, new [] { "text", },
                        "<not-option label=\"my-label\"></not-option>" },
                    { string.Empty, null, null, new [] { string.Empty, "text", },
                        "<not-option label=\"my-label\" selected=\"selected\"></not-option>" },

                    { "text", null, null, null,
                        "<not-option label=\"my-label\">text</not-option>" },
                    { "text", string.Empty, null, null,
                        "<not-option label=\"my-label\" selected=\"\">text</not-option>" },
                    { "text", "selected", null, null,
                        "<not-option label=\"my-label\" selected=\"selected\">text</not-option>" },
                    { "text", null, null, new string[0],
                        "<not-option label=\"my-label\">text</not-option>" },
                    { "text", null, null, new [] { string.Empty, },
                        "<not-option label=\"my-label\">text</not-option>" },
                    { "text", null, null, new [] { "text", },
                        "<not-option label=\"my-label\" selected=\"selected\">text</not-option>" },
                    { "text", string.Empty, null, new [] { "text", },
                        "<not-option label=\"my-label\" selected=\"\">text</not-option>" },
                    { "text", null, null, new [] { string.Empty, "text", },
                        "<not-option label=\"my-label\" selected=\"selected\">text</not-option>" },

                    { "text", string.Empty, "value", null,
                        "<not-option label=\"my-label\" value=\"value\" selected=\"\">text</not-option>" },
                    { "text", "selected", "value", null,
                        "<not-option label=\"my-label\" value=\"value\" selected=\"selected\">text</not-option>" },
                    { "text", null, "value", new string[0],
                        "<not-option label=\"my-label\" value=\"value\">text</not-option>" },
                    { "text", null, "value", new [] { string.Empty, },
                        "<not-option label=\"my-label\" value=\"value\">text</not-option>" },
                    { "text", string.Empty, "value", new [] { string.Empty, },
                        "<not-option label=\"my-label\" value=\"value\" selected=\"\">text</not-option>" },
                    { "text", null, "value", new [] { "text", },
                        "<not-option label=\"my-label\" value=\"value\">text</not-option>" },
                    { "text", null, "value", new [] { "value", },
                        "<not-option label=\"my-label\" value=\"value\" selected=\"selected\">text</not-option>" },
                    { "text", null, "value", new [] { string.Empty, "value", },
                        "<not-option label=\"my-label\" value=\"value\" selected=\"selected\">text</not-option>" },
                };
            }
        }

        // Original content, selected attribute, value attribute, selected values (to place in FormContext.FormData)
        // and expected output (concatenation of TagHelperOutput generations). Excludes non-null selected attribute,
        // null selected values, and empty selected values cases.
        public static IEnumerable<object[]> DoesNotUseGeneratorDataSet
        {
            get
            {
                return GeneratesExpectedDataSet.Where(
                    entry => (entry[1] != null || entry[3] == null || (entry[3] as ICollection<string>).Count == 0));
            }
        }

        // Original content, selected attribute, value attribute, selected values (to place in FormContext.FormData)
        // and expected output (concatenation of TagHelperOutput generations). Excludes non-null selected attribute
        // cases.
        public static IEnumerable<object[]> DoesNotUseViewContextDataSet
        {
            get
            {
                return GeneratesExpectedDataSet.Where(entry => entry[1] != null);
            }
        }

        [Theory]
        [MemberData(nameof(GeneratesExpectedDataSet))]
        public async Task ProcessAsync_GeneratesExpectedOutput(
            string originalContent,
            string selected,
            string value,
            ICollection<string> selectedValues,
            string expectedOutput)
        {
            // Arrange
            var originalAttributes = new Dictionary<string, string>
            {
                { "label", "my-label" },
            };
            var expectedTagName = "not-option";

            var contextAttributes = new Dictionary<string, object>
            {
                { "label", "my-label" },
                { "selected", selected },
                { "value", value },
            };
            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult(originalContent));
            var output = new TagHelperOutput(expectedTagName, originalAttributes)
            {
                Content = originalContent,
                SelfClosing = false,
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var viewContext = TestableHtmlGenerator.GetViewContext(
                model: null,
                htmlGenerator: htmlGenerator,
                metadataProvider: metadataProvider);
            viewContext.FormContext.FormData[SelectTagHelper.SelectedValuesFormDataKey] = selectedValues;
            var tagHelper = new OptionTagHelper
            {
                Generator = htmlGenerator,
                Selected = selected,
                Value = value,
                ViewContext = viewContext,
            };

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(
                expectedOutput,
                output.GenerateStartTag() + output.GenerateContent() + output.GenerateEndTag());
        }

        [Theory]
        [MemberData(nameof(DoesNotUseGeneratorDataSet))]
        public async Task ProcessAsync_DoesNotUseGenerator_IfSelectedNullOrNoSelectedValues(
            string originalContent,
            string selected,
            string value,
            ICollection<string> selectedValues,
            string ignored)
        {
            // Arrange
            var originalAttributes = new Dictionary<string, string>
            {
                { "label", "my-label" },
            };
            var originalTagName = "not-option";

            var contextAttributes = new Dictionary<string, object>
            {
                { "label", "my-label" },
                { "selected", selected },
                { "value", value },
            };
            var originalPreContent = "original pre-content";
            var originalPostContent = "original post-content";
            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult(originalContent));
            var output = new TagHelperOutput(originalTagName, originalAttributes)
            {
                PreContent = originalPreContent,
                Content = originalContent,
                PostContent = originalPostContent,
                SelfClosing = false,
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var viewContext = TestableHtmlGenerator.GetViewContext(
                model: null,
                htmlGenerator: htmlGenerator,
                metadataProvider: metadataProvider);
            viewContext.FormContext.FormData[SelectTagHelper.SelectedValuesFormDataKey] = selectedValues;
            var tagHelper = new OptionTagHelper
            {
                Selected = selected,
                Value = value,
                ViewContext = viewContext,
            };

            // Act & Assert (does not throw)
            // Tag helper would throw an NRE if it used Generator value.
            await tagHelper.ProcessAsync(tagHelperContext, output);
        }

        [Theory]
        [MemberData(nameof(DoesNotUseViewContextDataSet))]
        public async Task ProcessAsync_DoesNotUseViewContext_IfSelectedNotNull(
            string originalContent,
            string selected,
            string value,
            ICollection<string> ignoredValues,
            string ignoredOutput)
        {
            // Arrange
            var originalAttributes = new Dictionary<string, string>
            {
                { "label", "my-label" },
            };
            var originalTagName = "not-option";

            var contextAttributes = new Dictionary<string, object>
            {
                { "label", "my-label" },
                { "selected", selected },
                { "value", value },
            };
            var originalPreContent = "original pre-content";
            var originalPostContent = "original post-content";
            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult(originalContent));
            var output = new TagHelperOutput(originalTagName, originalAttributes)
            {
                PreContent = originalPreContent,
                Content = originalContent,
                PostContent = originalPostContent,
                SelfClosing = false,
            };

            var tagHelper = new OptionTagHelper
            {
                Selected = selected,
                Value = value,
            };

            // Act & Assert (does not throw)
            // Tag helper would throw an NRE if it used ViewContext or Generator values.
            await tagHelper.ProcessAsync(tagHelperContext, output);
        }
    }
}
