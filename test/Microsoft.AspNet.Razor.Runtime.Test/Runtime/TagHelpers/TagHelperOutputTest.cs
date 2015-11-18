// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.Test.Runtime.TagHelpers
{
    public class TagHelperOutputTest
    {
        public static TheoryData<TagHelperOutput, string> WriteTagHelper_InputData
        {
            get
            {
                // parameters: TagHelperOutput, expectedOutput
                return new TheoryData<TagHelperOutput, string>
                {
                    {
                        // parameters: TagName, Attributes, SelfClosing, PreContent, Content, PostContent
                        GetTagHelperOutput(
                            tagName:     "div",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<div>Hello World!</div>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "Hello World!"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "  ",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "Hello World!"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" } },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\">Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                { "test", "testVal" },
                                { "something", "  spaced  " }
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\" something=\"HtmlEncode[[  spaced  ]]\">Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                ["test"] = new TagHelperAttribute
                                {
                                    Name = "test",
                                    Minimized = true
                                },
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                ["test"] = new TagHelperAttribute
                                {
                                    Name = "test",
                                    Minimized = true
                                },
                                ["test2"] = new TagHelperAttribute
                                {
                                    Name = "test2",
                                    Minimized = true
                                },
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test test2>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                ["first"] = "unminimized",
                                ["test"] = new TagHelperAttribute
                                {
                                    Name = "test",
                                    Minimized = true
                                },
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p first=\"HtmlEncode[[unminimized]]\" test>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                ["test"] = new TagHelperAttribute
                                {
                                    Name = "test",
                                    Minimized = true
                                },
                                ["last"] = "unminimized",
                            },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test last=\"HtmlEncode[[unminimized]]\">Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" } },
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\" />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                { "test", "testVal" },
                                { "something", "  spaced  " }
                            },
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\" something=\"HtmlEncode[[  spaced  ]]\" />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList() { { "test", "testVal" } },
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\">"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList()
                            {
                                { "test", "testVal" },
                                { "something", "  spaced  " }
                            },
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p test=\"HtmlEncode[[testVal]]\" something=\"HtmlEncode[[  spaced  ]]\">"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  "Hello World!",
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "<p>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     "Hello World!",
                            postContent: null,
                            postElement: null),
                        "<p>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: "Hello World!",
                            postElement: null),
                        "<p>Hello World!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<p>HelloTestWorld!</p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<p />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "p",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<p>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<custom>HelloTestWorld!</custom>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "random",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<random />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "random",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: null),
                        "<random>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom></custom>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.SelfClosing,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.SelfClosing,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom test=\"HtmlEncode[[testVal]]\" />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.SelfClosing,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom />"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom test=\"HtmlEncode[[testVal]]\">"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: null),
                        "Before<custom>"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom></custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom test=\"HtmlEncode[[testVal]]\" />After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.SelfClosing,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom />After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom test=\"HtmlEncode[[testVal]]\">After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  null,
                            preContent:  null,
                            content:     null,
                            postContent: null,
                            postElement: "After"),
                        "<custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom>HelloTestWorld!</custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom test=\"HtmlEncode[[testVal]]\">HelloTestWorld!</custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.SelfClosing,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom />After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.SelfClosing,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     "custom",
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "Before<custom>After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagOnly,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList(),
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                    {
                        GetTagHelperOutput(
                            tagName:     string.Empty,
                            attributes:  new TagHelperAttributeList { { "test", "testVal" } },
                            tagMode: TagMode.StartTagAndEndTag,
                            preElement:  "Before",
                            preContent:  "Hello",
                            content:     "Test",
                            postContent: "World!",
                            postElement: "After"),
                        "BeforeHelloTestWorld!After"
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(WriteTagHelper_InputData))]
        public void TagHelperOutput_WritesFormattedTagHelper(TagHelperOutput output, string expected)
        {
            // Arrange
            var writer = new StringWriter();
            var tagHelperExecutionContext = new TagHelperExecutionContext(
                tagName: output.TagName,
                tagMode: output.TagMode,
                items: new Dictionary<object, object>(),
                uniqueId: string.Empty,
                executeChildContentAsync: () => Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());
            tagHelperExecutionContext.Output = output;
            var testEncoder = new HtmlTestEncoder();

            // Act
            output.WriteTo(writer, testEncoder);

            // Assert
            Assert.Equal(expected, writer.ToString(), StringComparer.Ordinal);
        }

        private static TagHelperOutput GetTagHelperOutput(
            string tagName,
            TagHelperAttributeList attributes,
            TagMode tagMode,
            string preElement,
            string preContent,
            string content,
            string postContent,
            string postElement)
        {
            var output = new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()))
            {
                TagMode = tagMode
            };

            if (preElement != null)
            {
                output.PreElement.AppendHtml(preElement);
            }

            if (preContent != null)
            {
                output.PreContent.AppendHtml(preContent);
            }

            if (content != null)
            {
                output.Content.AppendHtml(content);
            }

            if (postContent != null)
            {
                output.PostContent.AppendHtml(postContent);
            }

            if (postElement != null)
            {
                output.PostElement.AppendHtml(postElement);
            }

            return output;
        }
    }
}
