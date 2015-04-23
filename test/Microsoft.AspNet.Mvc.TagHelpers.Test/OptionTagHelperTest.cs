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
        // and expected tag helper output.
        public static TheoryData<string, string, string, IEnumerable<string>, TagHelperOutput> GeneratesExpectedDataSet
        {
            get
            {
                return new TheoryData<string, string, string, IEnumerable<string>, TagHelperOutput>
                {
                    // original content, selected, value, selected values,
                    // expected tag helper output - attributes, content
                    {
                        null, null, null, null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "")
                    },
                    {
                        null, string.Empty, "value", null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "" }
                            },
                            "")
                    },
                    {
                        null, "selected", "value", null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "selected" }
                            },
                            "")
                    },
                    {
                        null, null, "value", Enumerable.Empty<string>(),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "")
                    },
                    {
                        null, null, "value", new [] { string.Empty, },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "")
                    },
                    {
                        null, string.Empty, "value", new [] { string.Empty, },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "" }
                            },
                            "")
                    },
                    {
                        null, null, "value", new [] { "value", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "selected" }
                            },
                            "")
                    },
                    {
                        null, null, "value", new [] { string.Empty, "value", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "selected" }
                            },
                            "")
                    },
                    {
                        string.Empty, null, null, null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                        "")
                    },
                    {
                        string.Empty, string.Empty, null, null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "" }
                            },
                            "")
                    },
                    {
                        string.Empty, "selected", null, null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "selected" }
                            },
                            "")
                    },
                    {
                        string.Empty, null, null, Enumerable.Empty<string>(),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "")
                    },
                    {
                        string.Empty, null, null, new [] { string.Empty, },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "selected" }
                            },
                            "")
                    },
                    {
                        string.Empty, string.Empty, null, new [] { string.Empty, },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "" }
                            },
                            "")
                    },
                    {
                        string.Empty, null, null, new [] { "text", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "")
                    },
                    {
                        string.Empty, null, null, new [] { string.Empty, "text", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "selected" }
                            },
                            "")
                    },
                    {
                        "text", null, null, null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "text")
                    },
                    {
                        "text", string.Empty, null, null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "" }
                            },
                            "text")
                    },
                    {
                        "text", "selected", null, null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "selected" }
                            },
                            "text")
                    },
                    {
                        "text", null, null, Enumerable.Empty<string>(),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "text")
                    },
                    {
                        "text", null, null, new [] { string.Empty, },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "text")
                    },
                    {
                        "HtmlEncode[[text]]", null, null, new [] { "text", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "selected" }
                            },
                            "HtmlEncode[[text]]")
                    },
                    {
                        "text", string.Empty, null, new [] { "text", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "" }
                            },
                            "text")
                    },
                    {
                        "HtmlEncode[[text]]", null, null, new [] { string.Empty, "text", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "selected" }
                            },
                            "HtmlEncode[[text]]")
                    },
                    {
                        "text", string.Empty, "value", null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "" }
                            },
                            "text")
                    },
                    {
                        "text", "selected", "value", null,
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "selected" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value", Enumerable.Empty<string>(),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value", new [] { string.Empty, },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "text")
                    },
                    {
                        "text", string.Empty, "value", new [] { string.Empty, },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value", new [] { "text", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value", new [] { "value", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "selected" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value", new [] { string.Empty, "value", },
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "selected" }
                            },
                            "text")
                    }
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
            IEnumerable<string> selectedValues,
            TagHelperOutput expectedTagHelperOutput)
        {
            // Arrange
            var originalAttributes = new TagHelperAttributeList
            {
                { "label", "my-label" },
            };
            if (selected != null)
            {
                originalAttributes.Add("selected", selected);
            }

            var contextAttributes = new TagHelperAttributeList(originalAttributes);
            if (value != null)
            {
                contextAttributes.Add("value", value);
            }

            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    // GetChildContentAsync should not be invoked since we are setting the content below.
                    Assert.True(false);
                    return Task.FromResult<TagHelperContent>(null);
                });

            var output = new TagHelperOutput(expectedTagHelperOutput.TagName, originalAttributes)
            {
                SelfClosing = false,
            };

            output.Content.SetContent(originalContent);

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
                Value = value,
                ViewContext = viewContext,
            };

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(expectedTagHelperOutput.TagName, output.TagName);
            Assert.Equal(expectedTagHelperOutput.Content.GetContent(), output.Content.GetContent());
            Assert.Equal(expectedTagHelperOutput.Attributes.Count, output.Attributes.Count);
            foreach (var attribute in output.Attributes)
            {
                Assert.Contains(attribute, expectedTagHelperOutput.Attributes);
            }
        }

        [Theory]
        [MemberData(nameof(DoesNotUseGeneratorDataSet))]
        public async Task ProcessAsync_DoesNotUseGenerator_IfSelectedNullOrNoSelectedValues(
            string originalContent,
            string selected,
            string value,
            IEnumerable<string> selectedValues,
            TagHelperOutput ignored)
        {
            // Arrange
            var originalAttributes = new TagHelperAttributeList
            {
                { "label", "my-label" },
                { "selected", selected },
            };
            var originalTagName = "not-option";

            var contextAttributes = new TagHelperAttributeList
            {
                { "label", "my-label" },
                { "selected", selected },
                { "value", value },
            };
            var originalPreContent = "original pre-content";
            var originalPostContent = "original post-content";
            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent(originalContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var output = new TagHelperOutput(originalTagName, originalAttributes)
            {
                SelfClosing = false,
            };
            output.PreContent.SetContent(originalPreContent);
            output.Content.SetContent(originalContent);
            output.PostContent.SetContent(originalPostContent);

            var metadataProvider = new EmptyModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
            var viewContext = TestableHtmlGenerator.GetViewContext(
                model: null,
                htmlGenerator: htmlGenerator,
                metadataProvider: metadataProvider);
            viewContext.FormContext.FormData[SelectTagHelper.SelectedValuesFormDataKey] = selectedValues;
            var tagHelper = new OptionTagHelper
            {
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
            IEnumerable<string> ignoredValues,
            TagHelperOutput ignoredOutput)
        {
            // Arrange
            var originalAttributes = new TagHelperAttributeList
            {
                { "label", "my-label" },
                { "selected", selected },
            };
            var originalTagName = "not-option";

            var contextAttributes = new TagHelperAttributeList
            {
                { "label", "my-label" },
                { "selected", selected },
                { "value", value },
            };
            var originalPreContent = "original pre-content";
            var originalPostContent = "original post-content";
            var tagHelperContext = new TagHelperContext(
                contextAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent(originalContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var output = new TagHelperOutput(originalTagName, originalAttributes)
            {
                SelfClosing = false,
            };
            output.PreContent.SetContent(originalPreContent);
            output.Content.SetContent(originalContent);
            output.PostContent.SetContent(originalPostContent);

            var tagHelper = new OptionTagHelper
            {
                Value = value,
            };

            // Act & Assert (does not throw)
            // Tag helper would throw an NRE if it used ViewContext or Generator values.
            await tagHelper.ProcessAsync(tagHelperContext, output);
        }

        private static TagHelperOutput GetTagHelperOutput(
            string tagName, TagHelperAttributeList attributes, string content)
        {
            var tagHelperOutput = new TagHelperOutput(tagName, attributes);
            tagHelperOutput.Content.SetContent(content);

            return tagHelperOutput;
        }
    }
}
