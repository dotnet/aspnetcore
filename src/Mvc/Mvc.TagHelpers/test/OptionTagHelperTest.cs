// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class OptionTagHelperTest
{
    // Original content, selected attribute, value attribute, selected values (to place in TagHelperContext.Items)
    // and expected tag helper output.
    public static TheoryData<string, string, string, ICollection<string>, TagHelperOutput> GeneratesExpectedDataSet
    {
        get
        {
            return new TheoryData<string, string, string, ICollection<string>, TagHelperOutput>
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
                        null, null, "value", new HashSet<string>(),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "")
                    },
                    {
                        null, null, "value", new HashSet<string>(new [] { string.Empty, }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "")
                    },
                    {
                        null, string.Empty, "value", new HashSet<string>(new [] { string.Empty, }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "" }
                            },
                            "")
                    },
                    {
                        null, null, "value", new HashSet<string>(new [] { "value", }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "selected" }
                            },
                            "")
                    },
                    {
                        null, null, "value", new HashSet<string>(new [] { string.Empty, "value", }),
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
                        string.Empty, null, null, new HashSet<string>(),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "")
                    },
                    {
                        string.Empty, null, null, new HashSet<string>(new [] { string.Empty, }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "selected" }
                            },
                            "")
                    },
                    {
                        string.Empty, string.Empty, null,
                        new HashSet<string>(new [] { string.Empty, }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "" }
                            },
                            "")
                    },
                    {
                        string.Empty, null, null, new HashSet<string>(new [] { "text", }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "")
                    },
                    {
                        string.Empty, null, null,
                        new HashSet<string>(new [] { string.Empty, "text", }),
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
                        "text", null, null, new HashSet<string>(),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "text")
                    },
                    {
                        "text", null, null, new HashSet<string>(new [] { string.Empty, }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }
                            },
                            "text")
                    },
                    {
                        "HtmlEncode[[text]]", null, null, new HashSet<string>(new [] { "text", }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "selected" }
                            },
                            "HtmlEncode[[text]]")
                    },
                    {
                        "text", string.Empty, null, new HashSet<string>(new [] { "text", }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "selected", "" }
                            },
                            "text")
                    },
                    {
                        "HtmlEncode[[text]]", null, null,
                        new HashSet<string>(new [] { string.Empty, "text", }),
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
                        "text", null, "value", new HashSet<string>(),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value", new HashSet<string>(new [] { string.Empty, }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "text")
                    },
                    {
                        "text", string.Empty, "value",
                        new HashSet<string>(new [] { string.Empty, }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value", new HashSet<string>(new [] { "text", }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value", new HashSet<string>(new [] { "value", }),
                        GetTagHelperOutput(
                            "not-option",
                            new TagHelperAttributeList
                            {
                                { "label", "my-label" }, { "value", "value" }, { "selected", "selected" }
                            },
                            "text")
                    },
                    {
                        "text", null, "value",
                        new HashSet<string>(new [] { string.Empty, "value", }),
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

    // Original content, selected attribute, value attribute, selected values (to place in TagHelperContext.Items)
    // and expected output (concatenation of TagHelperOutput generations). Excludes non-null selected attribute,
    // null selected values, and empty selected values cases.
    public static IEnumerable<object[]> DoesNotUseGeneratorDataSet
    {
        get
        {
            return GeneratesExpectedDataSet.Where(
                entry => (entry[1] != null || entry[3] == null || ((ICollection<string>)(entry[3])).Count == 0));
        }
    }

    // Original content, selected attribute, value attribute, selected values (to place in TagHelperContext.Items)
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
        ICollection<string> currentValues,
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
            tagName: "option",
            allAttributes: contextAttributes,
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            expectedTagHelperOutput.TagName,
            originalAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                // GetChildContentAsync should not be invoked since we are setting the content below.
                Assert.True(false);
                return Task.FromResult<TagHelperContent>(null);
            })
        {
            TagMode = TagMode.StartTagAndEndTag
        };

        output.Content.SetContent(originalContent);

        var metadataProvider = new EmptyModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);
        var viewContext = TestableHtmlGenerator.GetViewContext(
            model: null,
            htmlGenerator: htmlGenerator,
            metadataProvider: metadataProvider);
        tagHelperContext.Items[typeof(SelectTagHelper)] = currentValues == null ? null : new CurrentValues(currentValues);
        var tagHelper = new OptionTagHelper(htmlGenerator)
        {
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
        ICollection<string> currentValues,
        TagHelperOutput _)
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
            tagName: "option",
            allAttributes: contextAttributes,
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            originalTagName,
            originalAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent(originalContent);
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            })
        {
            TagMode = TagMode.StartTagAndEndTag,
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
        tagHelperContext.Items[typeof(SelectTagHelper)] = currentValues == null ? null : new CurrentValues(currentValues);
        var tagHelper = new OptionTagHelper(htmlGenerator)
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
        ICollection<string> _,
        TagHelperOutput __)
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
            tagName: "option",
            allAttributes: contextAttributes,
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            originalTagName,
            originalAttributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent(originalContent);
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            })
        {
            TagMode = TagMode.StartTagAndEndTag,
        };
        output.PreContent.SetContent(originalPreContent);
        output.Content.SetContent(originalContent);
        output.PostContent.SetContent(originalPostContent);

        var metadataProvider = new EmptyModelMetadataProvider();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider);

        var tagHelper = new OptionTagHelper(htmlGenerator)
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
        var tagHelperOutput = new TagHelperOutput(
            tagName,
            attributes,
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));
        tagHelperOutput.Content.SetContent(content);

        return tagHelperOutput;
    }
}
