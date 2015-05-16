// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.TagHelpers
{
    public class TagHelperParseTreeRewriterTest : TagHelperRewritingTestBase
    {
        public static TheoryData RequiredAttributeData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNow = new MarkupBlock(
                    new MarkupBlock(
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("DateTime.Now")
                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharacters.NonWhiteSpace))));

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<p />",
                        new MarkupBlock(blockFactory.MarkupTagBlock("<p />"))
                    },
                    {
                        "<p></p>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<p>"),
                            blockFactory.MarkupTagBlock("</p>"))
                    },
                    {
                        "<div />",
                        new MarkupBlock(blockFactory.MarkupTagBlock("<div />"))
                    },
                    {
                        "<div></div>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<div>"),
                            blockFactory.MarkupTagBlock("</div>"))
                    },
                    {
                        "<p class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<p class=\"@DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow)
                                }))
                    },
                    {
                        "<p class=\"btn\">words and spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<p class=\"@DateTime.Now\">words and spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow)
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<p class=\"btn\">words<strong>and</strong>spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    factory.Markup("words"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    factory.Markup("and"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    factory.Markup("spaces")
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                }))
                    },
                    {
                        "<strong catchAll=\"@DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", dateTimeNow)
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\">words and spaces</strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<strong catchAll=\"@DateTime.Now\">words and spaces</strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", dateTimeNow)
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 4, 0, 4),
                                        suffix: new LocationTagged<string>("\"", 15, 0, 15)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                            value: new LocationTagged<string>("btn", 12, 0, 12))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        "<div class=\"btn\"></div>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 4, 0, 4),
                                        suffix: new LocationTagged<string>("\"", 15, 0, 15)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                            value: new LocationTagged<string>("btn", 12, 0, 12))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(">")),
                            blockFactory.MarkupTagBlock("</div>"))
                    },
                    {
                        "<p notRequired=\"a\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("notRequired", factory.Markup("a")),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<p notRequired=\"@DateTime.Now\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("notRequired", dateTimeNow),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<p notRequired=\"a\" class=\"btn\">words and spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("notRequired", factory.Markup("a")),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<div style=\"@DateTime.Now\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", dateTimeNow),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<div style=\"\" class=\"btn\">words and spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"@DateTime.Now\" class=\"@DateTime.Now\">words and spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", dateTimeNow),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow)
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\">words<strong>and</strong>spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    factory.Markup("words"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    factory.Markup("and"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    factory.Markup("spaces")
                                }))
                    },
                    {
                        "<p class=\"btn\" catchAll=\"hi\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                }))
                    },
                    {
                        "<p class=\"btn\" catchAll=\"hi\">words and spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\" catchAll=\"hi\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                }))
                    },
                    {
                        "<div style=\"\" class=\"btn\" catchAll=\"hi\" >words and spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\" catchAll=\"@@hi\" >words and spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                factory.Markup("@").Accepts(AcceptedCharacters.None),
                                                factory.Markup("@").With(SpanCodeGenerator.Null).Accepts(AcceptedCharacters.None)),
                                            factory.Markup("hi"))),
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"@DateTime.Now\" class=\"@DateTime.Now\" catchAll=\"@DateTime.Now\" >words and " +
                        "spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", dateTimeNow),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow),
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", dateTimeNow)
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\" catchAll=\"hi\" >words<strong>and</strong>spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    factory.Markup("words"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    factory.Markup("and"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    factory.Markup("spaces")
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RequiredAttributeData))]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor(
                        tagName: "p",
                        typeName: "pTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[0],
                        requiredAttributes: new[] { "class" }),
                    new TagHelperDescriptor(
                        tagName: "div",
                        typeName: "divTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[0],
                        requiredAttributes: new[] { "class", "style" }),
                    new TagHelperDescriptor(
                        tagName: "*",
                        typeName: "catchAllTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[0],
                        requiredAttributes: new[] { "catchAll" })
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new RazorError[0]);
        }

        public static TheoryData NestedRequiredAttributeData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNow = new MarkupBlock(
                    new MarkupBlock(
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("DateTime.Now")
                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharacters.NonWhiteSpace))));

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<p class=\"btn\"><p></p></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: new[]
                                {
                                    blockFactory.MarkupTagBlock("<p>"),
                                    blockFactory.MarkupTagBlock("</p>")
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\"><strong></strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                }))
                    },
                    {
                        "<p class=\"btn\"><strong><p></p></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: new[]
                                {
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("<p>"),
                                    blockFactory.MarkupTagBlock("</p>"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\"><p><strong></strong></p></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    blockFactory.MarkupTagBlock("<p>"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    blockFactory.MarkupTagBlock("</p>"),
                                }))
                    },
                    {
                        "<p class=\"btn\"><strong catchAll=\"hi\"><p></p></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: new MarkupTagHelperBlock(
                                    "strong",
                                    attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<p>"),
                                        blockFactory.MarkupTagBlock("</p>")
                                    })))
                    },
                    {
                        "<strong catchAll=\"hi\"><p class=\"btn\"><strong></strong></p></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: new MarkupTagHelperBlock(
                                    "p",
                                    attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<strong>"),
                                        blockFactory.MarkupTagBlock("</strong>"),
                                    })))
                    },
                    {
                        "<p class=\"btn\"><p class=\"btn\"><p></p></p></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: new MarkupTagHelperBlock(
                                    "p",
                                    attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<p>"),
                                        blockFactory.MarkupTagBlock("</p>")
                                    })))
                    },
                    {
                        "<strong catchAll=\"hi\"><strong catchAll=\"hi\"><strong></strong></strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: new MarkupTagHelperBlock(
                                    "strong",
                                    attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<strong>"),
                                        blockFactory.MarkupTagBlock("</strong>"),
                                    })))
                    },
                    {
                        "<p class=\"btn\"><p><p><p class=\"btn\"><p></p></p></p></p></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: new[]
                                {
                                    blockFactory.MarkupTagBlock("<p>"),
                                    blockFactory.MarkupTagBlock("<p>"),
                                    new MarkupTagHelperBlock(
                                        "p",
                                        attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                        {
                                            new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                        },
                                        children: new[]
                                        {
                                            blockFactory.MarkupTagBlock("<p>"),
                                            blockFactory.MarkupTagBlock("</p>")
                                        }),
                                    blockFactory.MarkupTagBlock("</p>"),
                                    blockFactory.MarkupTagBlock("</p>"),
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\"><strong><strong><strong catchAll=\"hi\"><strong></strong></strong>" +
                        "</strong></strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                },
                                children: new[]
                                {
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    new MarkupTagHelperBlock(
                                    "strong",
                                    attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>("catchAll", factory.Markup("hi"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<strong>"),
                                        blockFactory.MarkupTagBlock("</strong>"),
                                    }),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NestedRequiredAttributeData))]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor(
                        tagName: "p",
                        typeName: "pTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[0],
                        requiredAttributes: new[] { "class" }),
                    new TagHelperDescriptor(
                        tagName: "*",
                        typeName: "catchAllTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[0],
                        requiredAttributes: new[] { "catchAll" })
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new RazorError[0]);
        }

        public static TheoryData<string, MarkupBlock, RazorError[]> MalformedRequiredAttributeData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorFormatUnclosed = "Found a malformed '{0}' tag helper. Tag helpers must have a start and " +
                                          "end tag or be self closing.";
                var errorFormatNoCloseAngle = "Missing close angle for tag helper '{0}'.";

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<p",
                        new MarkupBlock(blockFactory.MarkupTagBlock("<p")),
                        new RazorError[0]
                    },
                    {
                        "<p class=\"btn\"",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p notRequired=\"hi\" class=\"btn\"",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("notRequired", factory.Markup("hi")),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p></p",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<p>"),
                            blockFactory.MarkupTagBlock("</p")),
                        new RazorError[0]
                    },
                    {
                        "<p class=\"btn\"></p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                absoluteIndex: 15, lineIndex: 0, columnIndex: 15)
                        }
                    },
                    {
                        "<p notRequired=\"hi\" class=\"btn\"></p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("notRequired", factory.Markup("hi")),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                absoluteIndex: 32, lineIndex: 0, columnIndex: 32)
                        }
                    },
                    {
                        "<p class=\"btn\" <p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: blockFactory.MarkupTagBlock("<p>"))),
                        new[]
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                        }
                    },
                    {
                        "<p notRequired=\"hi\" class=\"btn\" <p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("notRequired", factory.Markup("hi")),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: blockFactory.MarkupTagBlock("<p>"))),
                        new[]
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                        }
                    },
                    {
                        "<p class=\"btn\" </p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                absoluteIndex: 15, lineIndex: 0, columnIndex: 15)
                        }
                    },
                    {
                        "<p notRequired=\"hi\" class=\"btn\" </p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("notRequired", factory.Markup("hi")),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                absoluteIndex: 32, lineIndex: 0, columnIndex: 32)
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MalformedRequiredAttributeData))]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor(
                        tagName: "p",
                        typeName: "pTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new TagHelperAttributeDescriptor[0],
                        requiredAttributes: new[] { "class" })
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors);
        }

        public static TheoryData PrefixedTagHelperBoundData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var availableDescriptorsColon = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor(
                        prefix: "th:",
                        tagName: "myth",
                        typeName: "mythTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        requiredAttributes: Enumerable.Empty<string>()),
                    new TagHelperDescriptor(
                        prefix: "th:",
                        tagName: "myth2",
                        typeName: "mythTagHelper2",
                        assemblyName: "SomeAssembly",
                        attributes: new []
                        {
                            new TagHelperAttributeDescriptor(
                                name: "bound",
                                propertyName: "Bound",
                                typeName: typeof(bool).FullName),
                        },
                        requiredAttributes: Enumerable.Empty<string>())
                };
                var availableDescriptorsText = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor(
                        prefix: "PREFIX",
                        tagName: "myth",
                        typeName: "mythTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        requiredAttributes: Enumerable.Empty<string>()),
                    new TagHelperDescriptor(
                        prefix: "PREFIX",
                        tagName: "myth2",
                        typeName: "mythTagHelper2",
                        assemblyName: "SomeAssembly",
                        attributes: new []
                        {
                            new TagHelperAttributeDescriptor(
                                name: "bound",
                                propertyName: "Bound",
                                typeName: typeof(bool).FullName),
                        },
                        requiredAttributes: Enumerable.Empty<string>())
                };
                var availableDescriptorsCatchAll = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor(
                        prefix: "myth",
                        tagName: "*",
                        typeName: "mythTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                        requiredAttributes: Enumerable.Empty<string>()),
                };

                // documentContent, expectedOutput, availableDescriptors
                return new TheoryData<string, MarkupBlock, IEnumerable<TagHelperDescriptor>>
                {
                    {
                        "<myth />",
                        new MarkupBlock(blockFactory.MarkupTagBlock("<myth />")),
                        availableDescriptorsCatchAll
                    },
                    {
                        "<myth>words and spaces</myth>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<myth>"),
                            factory.Markup("words and spaces"),
                            blockFactory.MarkupTagBlock("</myth>")),
                        availableDescriptorsCatchAll
                    },
                    {
                        "<th:myth />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("th:myth", selfClosing: true)),
                        availableDescriptorsColon
                    },
                    {
                        "<PREFIXmyth />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("PREFIXmyth", selfClosing: true)),
                        availableDescriptorsText
                    },
                    {
                        "<th:myth></th:myth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("th:myth")),
                        availableDescriptorsColon
                    },
                    {
                        "<PREFIXmyth></PREFIXmyth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("PREFIXmyth")),
                        availableDescriptorsText
                    },
                    {
                        "<th:myth><th:my2th></th:my2th></th:myth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth",
                                blockFactory.MarkupTagBlock("<th:my2th>"),
                                blockFactory.MarkupTagBlock("</th:my2th>"))),
                        availableDescriptorsColon
                    },
                    {
                        "<PREFIXmyth><PREFIXmy2th></PREFIXmy2th></PREFIXmyth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "PREFIXmyth",
                                blockFactory.MarkupTagBlock("<PREFIXmy2th>"),
                                blockFactory.MarkupTagBlock("</PREFIXmy2th>"))),
                        availableDescriptorsText
                    },
                    {
                        "<!th:myth />",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "th:myth />")),
                        availableDescriptorsColon
                    },
                    {
                        "<!PREFIXmyth />",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "PREFIXmyth />")),
                        availableDescriptorsText
                    },
                    {
                        "<!th:myth></!th:myth>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "th:myth>"),
                            blockFactory.EscapedMarkupTagBlock("</", "th:myth>")),
                        availableDescriptorsColon
                    },
                    {
                        "<!PREFIXmyth></!PREFIXmyth>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "PREFIXmyth>"),
                            blockFactory.EscapedMarkupTagBlock("</", "PREFIXmyth>")),
                        availableDescriptorsText
                    },
                    {
                        "<th:myth class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        availableDescriptorsColon
                    },
                    {
                        "<PREFIXmyth class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "PREFIXmyth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        availableDescriptorsText
                    },
                    {
                        "<th:myth2 class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth2",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        availableDescriptorsColon
                    },
                    {
                        "<PREFIXmyth2 class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "PREFIXmyth2",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        availableDescriptorsText
                    },
                    {
                        "<th:myth class=\"btn\">words and spaces</th:myth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces"))),
                        availableDescriptorsColon
                    },
                    {
                        "<PREFIXmyth class=\"btn\">words and spaces</PREFIXmyth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "PREFIXmyth",
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces"))),
                        availableDescriptorsText
                    },
                    {
                        "<th:myth2 bound=\"@DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth2",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                    new ExpressionBlock(
                                                        factory.CodeTransition(),
                                                        factory.Code("DateTime.Now")
                                                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                            .Accepts(AcceptedCharacters.NonWhiteSpace)))))
                                    }
                                })),
                        availableDescriptorsColon
                    },
                    {
                        "<PREFIXmyth2 bound=\"@DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "PREFIXmyth2",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                    new ExpressionBlock(
                                                        factory.CodeTransition(),
                                                        factory.Code("DateTime.Now")
                                                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                            .Accepts(AcceptedCharacters.NonWhiteSpace)))))
                                    }
                                })),
                        availableDescriptorsText
                    },
                    {
                        "<PREFIXmyth2 bound=\"@@@DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "PREFIXmyth2",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                    factory.Markup("@").Accepts(AcceptedCharacters.None),
                                                    factory.Markup("@").With(SpanCodeGenerator.Null).Accepts(AcceptedCharacters.None)),
                                                new MarkupBlock(
                                                    factory.EmptyHtml(),
                                                    new ExpressionBlock(
                                                        factory.CodeTransition(),
                                                        factory.Code("DateTime.Now")
                                                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                            .Accepts(AcceptedCharacters.NonWhiteSpace)))))
                                    }
                                })),
                        availableDescriptorsText
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(PrefixedTagHelperBoundData))]
        public void Rewrite_AllowsPrefixedTagHelpers(
            string documentContent,
            MarkupBlock expectedOutput,
            IEnumerable<TagHelperDescriptor> availableDescriptors)
        {
            // Arrange
            var descriptorProvider = new TagHelperDescriptorProvider(availableDescriptors);

            // Act & Assert
            EvaluateData(
                descriptorProvider,
                documentContent,
                expectedOutput,
                expectedErrors: Enumerable.Empty<RazorError>());
        }

        public static TheoryData EmptyTagHelperBoundAttributeData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var emptyAttributeError =
                    "Attribute '{0}' on tag helper element '{1}' requires a value. Tag helper bound attributes of " +
                    "type '{2}' cannot be empty or contain only whitespace.";
                var boolTypeName = typeof(bool).FullName;

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<myth bound='' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", new MarkupBlock())
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5)
                        }
                    },
                    {
                        "<myth bound='    true' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup("    true"))
                                })),
                        new RazorError[0]
                    },
                    {
                        "<myth bound='    ' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup("    "))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5)
                        }
                    },
                    {
                        "<myth bound=''  bound=\"\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", new MarkupBlock())
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5),
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 16, lineIndex: 0, columnIndex: 16, length: 5)
                        }
                    },
                    {
                        "<myth bound=' '  bound=\"  \" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup(" ")),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup("  "))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5),
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 17, lineIndex: 0, columnIndex: 17, length: 5)
                        }
                    },
                    {
                        "<myth bound='true' bound=  />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup("true")),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup(string.Empty).With(SpanCodeGenerator.Null))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 19, lineIndex: 0, columnIndex: 19, length: 5)
                        }
                    },
                    {
                        "<myth bound= name='' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup(string.Empty).With(SpanCodeGenerator.Null)),
                                    new KeyValuePair<string, SyntaxTreeNode>("name", new MarkupBlock())
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5),
                        }
                    },
                    {
                        "<myth bound= name='  ' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup(string.Empty).With(SpanCodeGenerator.Null)),
                                    new KeyValuePair<string, SyntaxTreeNode>("name", factory.Markup("  "))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5),
                        }
                    },
                    {
                        "<myth bound='true' name='john' bound= name= />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup("true")),
                                    new KeyValuePair<string, SyntaxTreeNode>("name", factory.Markup("john")),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound", factory.CodeMarkup(string.Empty).With(SpanCodeGenerator.Null)),
                                    new KeyValuePair<string, SyntaxTreeNode>("name", factory.Markup(string.Empty).With(SpanCodeGenerator.Null))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "bound", "myth", boolTypeName),
                                absoluteIndex: 31, lineIndex: 0, columnIndex: 31, length: 5),
                        }
                    },
                    {
                        "<myth BouND='' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("BouND", new MarkupBlock())
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "BouND", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5),
                        }
                    },
                    {
                        "<myth BOUND=''    bOUnd=\"\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("BOUND", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("bOUnd", new MarkupBlock())
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "BOUND", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5),
                            new RazorError(
                                string.Format(emptyAttributeError, "bOUnd", "myth", boolTypeName),
                                absoluteIndex: 18, lineIndex: 0, columnIndex: 18, length: 5)
                        }
                    },
                    {
                        "<myth BOUND= nAMe='john'></myth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("BOUND", factory.CodeMarkup(string.Empty).With(SpanCodeGenerator.Null)),
                                    new KeyValuePair<string, SyntaxTreeNode>("nAMe", factory.Markup("john"))
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(emptyAttributeError, "BOUND", "myth", boolTypeName),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 5)
                        }
                    },
                    {
                        "<myth bound='    @true  ' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                factory.Markup("    "),
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory.Code("true")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                                factory.Markup("  ")))
                                    }
                                })),
                        new RazorError[0]
                    },
                    {
                        "<myth bound='    @(true)  ' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                factory.Markup("    "),
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                                                    factory.Code("true").AsExpression(),
                                                    factory.MetaCode(")").Accepts(AcceptedCharacters.None))),
                                                factory.Markup("  ")))
                                    }
                                })),
                        new RazorError[0]
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EmptyTagHelperBoundAttributeData))]
        public void Rewrite_CreatesErrorForEmptyTagHelperBoundAttributes(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor(
                        tagName: "myth",
                        typeName: "mythTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                name: "bound",
                                propertyName: "Bound",
                                typeName: typeof(bool).FullName),
                            new TagHelperAttributeDescriptor(
                                name: "name",
                                propertyName: "Name",
                                typeName: typeof(string).FullName)
                        })
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors);
        }

        public static TheoryData OptOut_WithAttributeTextTagData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorFormatNormalUnclosed =
                    "The \"{0}\" element was not closed.  All elements must be either self-closing or have a " +
                    "matching end tag.";
                var errorMatchingBrace =
                    "The code block is missing a closing \"}\" character.  Make sure you have a matching \"}\" " +
                    "character for all the \"{\" characters within this block, and that none of the \"}\" " +
                    "characters are being interpreted as markup.";

                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml());
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "@{<!text class=\"btn\">}",
                        new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    new MarkupTagBlock(
                                        factory.Markup("<"),
                                        factory.BangEscape(),
                                        factory.Markup("text"),
                                        new MarkupBlock(
                                            new AttributeBlockCodeGenerator(
                                                name: "class",
                                                prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                                suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                            factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeCodeGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                    value: new LocationTagged<string>("btn", 16, 0, 16))),
                                            factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                        factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                    factory.Markup("}")))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "!text"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!text class=\"btn\"></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!text class=\"btn\">words with spaces</!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                factory.Markup("words with spaces"),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!text class='btn1 btn2' class2=btn></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 8, 0, 8),
                                            suffix: new LocationTagged<string>("'", 25, 0, 25)),
                                        factory.Markup(" class='").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn1", 16, 0, 16))),
                                        factory.Markup(" btn2").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(" ", 20, 0, 20),
                                                value: new LocationTagged<string>("btn2", 21, 0, 21))),
                                        factory.Markup("'").With(SpanCodeGenerator.Null)),
                                        new MarkupBlock(
                                            new AttributeBlockCodeGenerator(
                                                name: "class2",
                                                prefix: new LocationTagged<string>(" class2=", 26, 0, 26),
                                                suffix: new LocationTagged<string>(string.Empty, 37, 0, 37)),
                                            factory.Markup(" class2=").With(SpanCodeGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeCodeGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 34, 0, 34),
                                                    value: new LocationTagged<string>("btn", 34, 0, 34)))),
                                    factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!text class='btn1 @DateTime.Now btn2'></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 8, 0, 8),
                                            suffix: new LocationTagged<string>("'", 39, 0, 39)),
                                        factory.Markup(" class='").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn1", 16, 0, 16))),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockCodeGenerator(
                                                new LocationTagged<string>(" ", 20, 0, 20), 21, 0, 21),
                                            factory.Markup(" ").With(SpanCodeGenerator.Null),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.Code("DateTime.Now")
                                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                    .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    factory.Markup(" btn2").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(" ", 34, 0, 34),
                                                value: new LocationTagged<string>("btn2", 35, 0, 35))),
                                        factory.Markup("'").With(SpanCodeGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                };
            }
        }

        public static TheoryData OptOut_WithBlockTextTagData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorFormatMalformed =
                    "Found a malformed '{0}' tag helper. Tag helpers must have a start and end tag or be self " +
                    "closing.";
                var errorFormatNormalUnclosed =
                    "The \"{0}\" element was not closed.  All elements must be either self-closing or have a " +
                    "matching end tag.";
                var errorFormatNormalNotStarted =
                    "Encountered end tag \"{0}\" with no matching start tag.  Are your start/end tags properly " +
                    "balanced?";
                var errorMatchingBrace =
                    "The code block is missing a closing \"}\" character.  Make sure you have a matching \"}\" " +
                    "character for all the \"{\" characters within this block, and that none of the \"}\" " +
                    "characters are being interpreted as markup.";

                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml());
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "@{<!text>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharacters.None),
                                    factory.Markup("}")))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "!text", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                        }
                    },
                    {
                        "@{</!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None))),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalNotStarted, "!text", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                        }
                    },
                    {
                        "@{<!text></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharacters.None),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!text>words and spaces</!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharacters.None),
                                factory.Markup("words and spaces"),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!text></text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharacters.None),
                                blockFactory.MarkupTagBlock("</text>", AcceptedCharacters.None))),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "!text", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(errorFormatMalformed, "text", CultureInfo.InvariantCulture),
                                absoluteIndex: 9, lineIndex: 0, columnIndex: 9)
                        }
                    },
                    {
                        "@{<text></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(factory.MarkupTransition("<text>")),
                                new MarkupTagBlock(
                                    factory.Markup("</").Accepts(AcceptedCharacters.None),
                                    factory.BangEscape(),
                                    factory.Markup("text>").Accepts(AcceptedCharacters.None)))),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "text", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!text><text></text></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharacters.None),
                                new MarkupTagHelperBlock("text"),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<text><!text></!text>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    new MarkupTagBlock(factory.MarkupTransition("<text>")),
                                    new MarkupTagBlock(
                                        factory.Markup("<").Accepts(AcceptedCharacters.None),
                                        factory.BangEscape(),
                                        factory.Markup("text>").Accepts(AcceptedCharacters.None)),
                                    blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None),
                                    factory.Markup("}")))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "text", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!text></!text></text>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharacters.None),
                                    blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharacters.None)),
                                new MarkupBlock(
                                    blockFactory.MarkupTagBlock("</text>", AcceptedCharacters.None)),
                                factory.EmptyCSharp().AsStatement(),
                                factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                            factory.EmptyHtml()),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalNotStarted, "text", CultureInfo.InvariantCulture),
                                absoluteIndex: 17, lineIndex: 0, columnIndex: 17),
                            new RazorError(
                                string.Format(errorFormatMalformed, "text", CultureInfo.InvariantCulture),
                                absoluteIndex: 17, lineIndex: 0, columnIndex: 17)
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithAttributeTextTagData))]
        [MemberData(nameof(OptOut_WithBlockTextTagData))]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors, "p", "text");
        }

        public static TheoryData OptOut_WithPartialTextTagData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorMatchingBrace =
                    "The code block is missing a closing \"}\" character.  Make sure you have a matching \"}\" " +
                    "character for all the \"{\" characters within this block, and that none of the \"}\" " +
                    "characters are being interpreted as markup.";
                var errorEOFMatchingBrace =
                    "End of file or an unexpected character was reached before the \"{0}\" tag could be parsed.  " +
                    "Elements inside markup blocks must be complete. They must either be self-closing " +
                    "(\"<br />\") or have matching end tags (\"<p>Hello</p>\").  If you intended " +
                    "to display a \"<\" character, use the \"&lt;\" HTML entity.";

                Func<Func<MarkupBlock>, MarkupBlock> buildPartialStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            insideBuilder()));
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "@{<!text}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "text}"))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorEOFMatchingBrace, "!text}"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!text /}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock(
                                    "<", 
                                    "text /", 
                                    new MarkupBlock(factory.Markup("}"))))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorEOFMatchingBrace, "!text"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!text class=}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=", 8, 0, 8),
                                            suffix: new LocationTagged<string>(string.Empty, 16, 0, 16)),
                                        factory.Markup(" class=").With(SpanCodeGenerator.Null),
                                        factory.Markup("}").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 15, 0, 15),
                                                value: new LocationTagged<string>("}", 15, 0, 15))))))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorEOFMatchingBrace, "!text"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!text class=\"btn}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>(string.Empty, 20, 0, 20)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn}").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn}", 16, 0, 16))))))),
                            new []
                            {
                                new RazorError(
                                    errorMatchingBrace,
                                    absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                                new RazorError(
                                    string.Format(errorEOFMatchingBrace, "!text"),
                                    absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                            }
                    },
                    {
                        "@{<!text class=\"btn\"}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),

                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                        new MarkupBlock(factory.Markup("}"))))),
                                new []
                                {
                                    new RazorError(
                                        errorMatchingBrace,
                                        absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                                    new RazorError(
                                        string.Format(errorEOFMatchingBrace, "!text"),
                                        absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                                }
                    },
                    {
                        "@{<!text class=\"btn\" /}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),

                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                        factory.Markup(" /"),
                                        new MarkupBlock(factory.Markup("}"))))),
                                new []
                                {
                                    new RazorError(
                                        errorMatchingBrace,
                                        absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                                    new RazorError(
                                        string.Format(errorEOFMatchingBrace, "!text"),
                                        absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                                }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithPartialTextTagData))]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors, "text");
        }

        public static TheoryData OptOut_WithPartialData_CSharp
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorMatchingBrace =
                    "The code block is missing a closing \"}\" character.  Make sure you have a matching \"}\" " +
                    "character for all the \"{\" characters within this block, and that none of the \"}\" " +
                    "characters are being interpreted as markup.";
                var errorEOFMatchingBrace =
                    "End of file or an unexpected character was reached before the \"{0}\" tag could be parsed.  " +
                    "Elements inside markup blocks must be complete. They must either be self-closing " +
                    "(\"<br />\") or have matching end tags (\"<p>Hello</p>\").  If you intended " +
                    "to display a \"<\" character, use the \"&lt;\" HTML entity.";

                Func<Func<MarkupBlock>, MarkupBlock> buildPartialStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            insideBuilder()));
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "@{<!}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "}"))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorEOFMatchingBrace, "!}"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!p}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "p}"))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorEOFMatchingBrace, "!p}"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!p /}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "p /", new MarkupBlock(factory.Markup("}"))))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorEOFMatchingBrace, "!p"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!p class=}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 13, 0, 13)),
                                        factory.Markup(" class=").With(SpanCodeGenerator.Null),
                                        factory.Markup("}").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                                value: new LocationTagged<string>("}", 12, 0, 12))))))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorEOFMatchingBrace, "!p"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!p class=\"btn}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 17, 0, 17)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn}").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn}", 13, 0, 13))))))),
                            new []
                            {
                                new RazorError(
                                    errorMatchingBrace,
                                    absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                                new RazorError(
                                    string.Format(errorEOFMatchingBrace, "!p"),
                                    absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                            }
                    },
                    {
                        "@{<!p class=\"btn@@}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeCodeGenerator(new LocationTagged<string>(string.Empty, 16, 0, 16), new LocationTagged<string>("@", 16, 0, 16))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanCodeGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("}").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 18, 0, 18),
                                                value: new LocationTagged<string>("}", 18, 0, 18))))))),
                            new []
                            {
                                new RazorError(
                                    errorMatchingBrace,
                                    absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                                new RazorError(
                                    string.Format(errorEOFMatchingBrace, "!p"),
                                    absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                            }
                    },
                    {
                        "@{<!p class=\"btn\"}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),

                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                        new MarkupBlock(factory.Markup("}"))))),
                                new []
                                {
                                    new RazorError(
                                        errorMatchingBrace,
                                        absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                                    new RazorError(
                                        string.Format(errorEOFMatchingBrace, "!p"),
                                        absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                                }
                    },
                    {
                        "@{<!p class=\"btn\" /}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),

                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                        factory.Markup(" /"),
                                        new MarkupBlock(
                                            factory.Markup("}"))))),
                                new []
                                {
                                    new RazorError(
                                        errorMatchingBrace,
                                        absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                                    new RazorError(
                                        string.Format(errorEOFMatchingBrace, "!p"),
                                        absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                                }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithPartialData_CSharp))]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors, "strong", "p");
        }

        public static TheoryData OptOut_WithPartialData_HTML
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<!",
                        new MarkupBlock(factory.Markup("<!"))
                    },
                    {
                        "<!p",
                        new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "p"))
                    },
                    {
                        "<!p /",
                        new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "p /"))
                    },
                    {
                        "<!p class=",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=", 3, 0, 3),
                                        suffix: new LocationTagged<string>(string.Empty, 10, 0, 10)),
                                    factory.Markup(" class=").With(SpanCodeGenerator.Null))))
                    },
                    {
                        "<!p class=\"btn",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>(string.Empty, 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))))))
                    },
                    {
                        "<!p class=\"btn\"",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null))))
                    },
                    {
                        "<!p class=\"btn\" /",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),

                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(" /")))
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithPartialData_HTML))]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, new RazorError[0], "strong", "p");
        }

        public static TheoryData OptOut_WithBlockData_CSharp
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorFormatMalformed =
                    "Found a malformed '{0}' tag helper. Tag helpers must have a start and end tag or be self " +
                    "closing.";
                var errorFormatNormalUnclosed =
                    "The \"{0}\" element was not closed.  All elements must be either self-closing or have a " +
                    "matching end tag.";
                var errorFormatNormalNotStarted =
                    "Encountered end tag \"{0}\" with no matching start tag.  Are your start/end tags properly " +
                    "balanced?";
                var errorMatchingBrace =
                    "The code block is missing a closing \"}\" character.  Make sure you have a matching \"}\" " +
                    "character for all the \"{\" characters within this block, and that none of the \"}\" " +
                    "characters are being interpreted as markup.";

                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml());
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "@{<!p>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                    factory.Markup("}")))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "!p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                        }
                    },
                    {
                        "@{</!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None))),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalNotStarted, "!p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                        }
                    },
                    {
                        "@{<!p></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!p>words and spaces</!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                factory.Markup("words and spaces"),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!p></p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                blockFactory.MarkupTagBlock("</p>", AcceptedCharacters.None))),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "!p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(errorFormatMalformed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 6, lineIndex: 0, columnIndex: 6)
                        }
                    },
                    {
                        "@{<p></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagHelperBlock("p",
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None)))),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(errorFormatMalformed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<p><!p></!p></p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagHelperBlock("p",
                                    blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None)))),
                        new RazorError[0]
                    },
                    {
                        "@{<p><!p></!p>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    new MarkupTagHelperBlock("p",
                                        blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                        blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None),
                                        factory.Markup("}"))))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(errorFormatMalformed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!p></!p></p>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None)),
                                new MarkupBlock(
                                    blockFactory.MarkupTagBlock("</p>", AcceptedCharacters.None)),
                                factory.EmptyCSharp().AsStatement(),
                                factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                            factory.EmptyHtml()),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalNotStarted, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 11, lineIndex: 0, columnIndex: 11),
                            new RazorError(
                                string.Format(errorFormatMalformed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 11, lineIndex: 0, columnIndex: 11)
                        }
                    },
                    {
                        "@{<strong></!p></strong>}",
                        new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            new MarkupBlock(
                                new MarkupTagHelperBlock("strong",
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None))),
                            new MarkupBlock(
                                blockFactory.MarkupTagBlock("</strong>", AcceptedCharacters.None)),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml()),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "strong", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(errorFormatMalformed, "strong", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(errorFormatNormalNotStarted, "strong", CultureInfo.InvariantCulture),
                                absoluteIndex: 15, lineIndex: 0, columnIndex: 15),
                            new RazorError(
                                string.Format(errorFormatMalformed, "strong", CultureInfo.InvariantCulture),
                                absoluteIndex: 15, lineIndex: 0, columnIndex: 15)
                        }
                    },
                    {
                        "@{<strong></strong><!p></!p>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    new MarkupTagHelperBlock("strong")),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None)),
                                factory.EmptyCSharp().AsStatement(),
                                factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                            factory.EmptyHtml()),
                        new RazorError[0]
                    },
                    {
                        "@{<p><strong></!strong><!p></strong></!p>}",
                            new MarkupBlock(
                                factory.EmptyHtml(),
                                new StatementBlock(
                                    factory.CodeTransition(),
                                    factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                    new MarkupBlock(
                                        new MarkupTagHelperBlock("p",
                                            new MarkupTagHelperBlock("strong",
                                                blockFactory.EscapedMarkupTagBlock("</", "strong>", AcceptedCharacters.None)))),
                                    new MarkupBlock(
                                        blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharacters.None),
                                        blockFactory.MarkupTagBlock("</strong>", AcceptedCharacters.None)),
                                    new MarkupBlock(
                                        blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None)),
                                    factory.EmptyCSharp().AsStatement(),
                                    factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                                factory.EmptyHtml()),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(errorFormatMalformed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(errorFormatMalformed, "strong", CultureInfo.InvariantCulture),
                                absoluteIndex: 5, lineIndex: 0, columnIndex: 5),
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "!p", CultureInfo.InvariantCulture),
                                absoluteIndex: 23, lineIndex: 0, columnIndex: 23),
                            new RazorError(
                                string.Format(errorFormatMalformed, "strong", CultureInfo.InvariantCulture),
                                absoluteIndex: 27, lineIndex: 0, columnIndex: 27),
                            new RazorError(
                                string.Format(errorFormatNormalNotStarted, "!p", CultureInfo.InvariantCulture),
                                absoluteIndex: 36, lineIndex: 0, columnIndex: 36),
                        }
                    },
                };
            }
        }

        public static TheoryData OptOut_WithAttributeData_CSharp
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorFormatNormalUnclosed =
                    "The \"{0}\" element was not closed.  All elements must be either self-closing or have a " +
                    "matching end tag.";
                var errorMatchingBrace =
                    "The code block is missing a closing \"}\" character.  Make sure you have a matching \"}\" " +
                    "character for all the \"{\" characters within this block, and that none of the \"}\" " +
                    "characters are being interpreted as markup.";

                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml());
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "@{<!p class=\"btn\">}",
                        new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                new MarkupBlock(
                                    new MarkupTagBlock(
                                        factory.Markup("<"),
                                        factory.BangEscape(),
                                        factory.Markup("p"),
                                        new MarkupBlock(
                                            new AttributeBlockCodeGenerator(
                                                name: "class",
                                                prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                                suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                            factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeCodeGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                    value: new LocationTagged<string>("btn", 13, 0, 13))),
                                            factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                        factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                    factory.Markup("}")))),
                        new []
                        {
                            new RazorError(
                                errorMatchingBrace,
                                absoluteIndex: 1, lineIndex: 0, columnIndex: 1),
                            new RazorError(
                                string.Format(errorFormatNormalUnclosed, "!p"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "@{<!p class=\"btn\"></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!p class=\"btn\">words with spaces</!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                factory.Markup("words with spaces"),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!p class='btn1 btn2' class2=btn></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 5, 0, 5),
                                            suffix: new LocationTagged<string>("'", 22, 0, 22)),
                                        factory.Markup(" class='").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn1", 13, 0, 13))),
                                        factory.Markup(" btn2").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(" ", 17, 0, 17),
                                                value: new LocationTagged<string>("btn2", 18, 0, 18))),
                                        factory.Markup("'").With(SpanCodeGenerator.Null)),
                                        new MarkupBlock(
                                            new AttributeBlockCodeGenerator(
                                                name: "class2",
                                                prefix: new LocationTagged<string>(" class2=", 23, 0, 23),
                                                suffix: new LocationTagged<string>(string.Empty, 34, 0, 34)),
                                            factory.Markup(" class2=").With(SpanCodeGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeCodeGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 31, 0, 31),
                                                    value: new LocationTagged<string>("btn", 31, 0, 31)))),
                                    factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                    {
                        "@{<!p class='btn1 @DateTime.Now btn2'></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 5, 0, 5),
                                            suffix: new LocationTagged<string>("'", 36, 0, 36)),
                                        factory.Markup(" class='").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn1", 13, 0, 13))),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockCodeGenerator(
                                                new LocationTagged<string>(" ", 17, 0, 17), 18, 0, 18),
                                            factory.Markup(" ").With(SpanCodeGenerator.Null),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.Code("DateTime.Now")
                                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                    .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    factory.Markup(" btn2").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(" ", 31, 0, 31),
                                                value: new LocationTagged<string>("btn2", 32, 0, 32))),
                                        factory.Markup("'").With(SpanCodeGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharacters.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharacters.None))),
                        new RazorError[0]
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithBlockData_CSharp))]
        [MemberData(nameof(OptOut_WithAttributeData_CSharp))]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors, "strong", "p");
        }

        public static TheoryData OptOut_WithBlockData_HTML
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorFormatUnclosed = "Found a malformed '{0}' tag helper. Tag helpers must have a start and " +
                                          "end tag or be self closing.";

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<!p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>")),
                        new RazorError[0]
                    },
                    {
                        "</!p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorError[0]
                    },
                    {
                        "<!p></!p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorError[0]
                    },
                    {
                        "<!p>words and spaces</!p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            factory.Markup("words and spaces"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorError[0]
                    },
                    {
                        "<!p></p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            blockFactory.MarkupTagBlock("</p>")),
                        new []
                        {
                            new RazorError(
                                string.Format(errorFormatUnclosed, "p", CultureInfo.InvariantCulture),
                                absoluteIndex: 4, lineIndex: 0, columnIndex: 4)
                        }
                    },
                    {
                        "<p></!p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p", blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new []
                        {
                            new RazorError(string.Format(errorFormatUnclosed, "p", CultureInfo.InvariantCulture),
                            SourceLocation.Zero)
                        }
                    },
                    {
                        "<p><!p></!p></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.EscapedMarkupTagBlock("<", "p>"),
                                blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new RazorError[0]
                    },
                    {
                        "<p><!p></!p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.EscapedMarkupTagBlock("<", "p>"),
                                blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new []
                        {
                            new RazorError(string.Format(errorFormatUnclosed, "p", CultureInfo.InvariantCulture),
                            SourceLocation.Zero)
                        }
                    },
                    {
                        "<!p></!p></p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>"),
                            blockFactory.MarkupTagBlock("</p>")),
                        new []
                        {
                            new RazorError(string.Format(errorFormatUnclosed, "p", CultureInfo.InvariantCulture),
                            absoluteIndex: 9, lineIndex: 0, columnIndex: 9)
                        }
                    },
                    {
                        "<strong></!p></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("strong",
                                blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new RazorError[0]
                    },
                    {
                        "<strong></strong><!p></!p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("strong"),
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorError[0]
                    },
                    {
                        "<p><strong></!strong><!p></strong></!p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong",
                                    blockFactory.EscapedMarkupTagBlock("</", "strong>"),
                                    blockFactory.EscapedMarkupTagBlock("<", "p>")),
                                blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new []
                        {
                            new RazorError(string.Format(errorFormatUnclosed, "p", CultureInfo.InvariantCulture),
                            SourceLocation.Zero)
                        }
                    },
                };
            }
        }

        public static TheoryData OptOut_WithAttributeData_HTML
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<!p class=\"btn\">",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(">"))),
                        new RazorError[0]
                    },
                    {
                        "<!p class=\"btn\"></!p>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(">")),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorError[0]
                    },
                    {
                        "<!p class=\"btn\">words and spaces</!p>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(">")),
                            factory.Markup("words and spaces"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorError[0]
                    },
                    {
                        "<!p class='btn1 btn2' class2=btn></!p>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class='", 3, 0, 3),
                                        suffix: new LocationTagged<string>("'", 20, 0, 20)),
                                    factory.Markup(" class='").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn1").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn1", 11, 0, 11))),
                                    factory.Markup(" btn2").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(" ", 15, 0, 15),
                                            value: new LocationTagged<string>("btn2", 16, 0, 16))),
                                    factory.Markup("'").With(SpanCodeGenerator.Null)),
                                    new MarkupBlock(
                                        new AttributeBlockCodeGenerator(
                                            name: "class2",
                                            prefix: new LocationTagged<string>(" class2=", 21, 0, 21),
                                            suffix: new LocationTagged<string>(string.Empty, 32, 0, 32)),
                                        factory.Markup(" class2=").With(SpanCodeGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeCodeGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 29, 0, 29),
                                                value: new LocationTagged<string>("btn", 29, 0, 29)))),
                                factory.Markup(">")),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorError[0]
                    },
                    {
                        "<!p class='btn1 @DateTime.Now btn2'></!p>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class='", 3, 0, 3),
                                        suffix: new LocationTagged<string>("'", 34, 0, 34)),
                                    factory.Markup(" class='").With(SpanCodeGenerator.Null),
                                    factory.Markup("btn1").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn1", 11, 0, 11))),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockCodeGenerator(
                                            new LocationTagged<string>(" ", 15, 0, 15), 16, 0, 16),
                                        factory.Markup(" ").With(SpanCodeGenerator.Null),
                                        new ExpressionBlock(
                                            factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                factory.Markup(" btn2").With(
                                        new LiteralAttributeCodeGenerator(
                                            prefix: new LocationTagged<string>(" ", 29, 0, 29),
                                            value: new LocationTagged<string>("btn2", 30, 0, 30))),
                                    factory.Markup("'").With(SpanCodeGenerator.Null)),
                                factory.Markup(">")),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorError[0]
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithBlockData_HTML))]
        [MemberData(nameof(OptOut_WithAttributeData_HTML))]
        public void Rewrite_AllowsTagHelperElementOptOutHTML(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors, "strong", "p");
        }

        public static TheoryData EmptyAttributeTagHelperData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<p class=\"\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class",  new MarkupBlock())
                            }))
                    },
                    {
                        "<p class=''></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class",  new MarkupBlock())
                            }))
                    },
                    {
                        "<p class=></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                // We expected a markup node here because attribute values without quotes can only ever
                                // be a single item, hence don't need to be enclosed by a block.
                                new KeyValuePair<string, SyntaxTreeNode>(
                                    "class",
                                    factory.Markup("").With(SpanCodeGenerator.Null)),
                            }))
                    },
                    {
                        "<p class1='' class2= class3=\"\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class1", new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "class2",
                                        factory.Markup("").With(SpanCodeGenerator.Null)),
                                    new KeyValuePair<string, SyntaxTreeNode>("class3", new MarkupBlock()),
                                }))
                    },
                    {
                        "<p class1=''class2=\"\"class3= />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class1",  new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>("class2",  new MarkupBlock()),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "class3",
                                        factory.Markup("").With(SpanCodeGenerator.Null)),
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EmptyAttributeTagHelperData))]
        public void Rewrite_UnderstandsEmptyAttributeTagHelpers(string documentContent, MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, new RazorError[0], "p");
        }

        public static TheoryData<string, MarkupBlock, RazorError[]> MalformedTagHelperAttributeBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorFormatUnclosed = "Found a malformed '{0}' tag helper. Tag helpers must have a start and " +
                                          "end tag or be self closing.";
                var errorFormatNoCloseAngle = "Missing close angle for tag helper '{0}'.";
                var errorFormatNoCSharp = "The tag helper '{0}' must not have C# in the element's attribute " +
                                           "declaration area.";
                Func<string, Block> createInvalidDoBlock = extraCode =>
                {
                    return new MarkupBlock(
                        new MarkupBlock(
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.Code("do {" + extraCode).AsStatement())));
                };
                var dateTimeNow = new MarkupBlock(
                    new MarkupBlock(
                        new ExpressionBlock(
                            factory.CodeTransition(),
                                factory.Code("DateTime.Now")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                    .Accepts(AcceptedCharacters.NonWhiteSpace))));

                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<p =\"false\"\" ></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>())),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p bar=\"false\"\" <strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bar", factory.Markup("false"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p bar='false  <strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "bar",
                                        new MarkupBlock(factory.Markup("false"), factory.Markup("  <strong>")))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p bar='false  <strong'",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "bar",
                                        new MarkupBlock(
                                            factory.Markup("false"),
                                            factory.Markup("  <strong")))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p bar=false'",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "bar",
                                        factory.Markup("false"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                "TagHelper attributes must be welformed.",
                                absoluteIndex: 12,
                                lineIndex: 0,
                                columnIndex: 12)
                        }
                    },
                    {
                        "<p bar=\"false'",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "bar",
                                        factory.Markup("false'"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p bar=\"false' ></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "bar",
                                        new MarkupBlock(
                                            factory.Markup("false'"),
                                            factory.Markup(" ></p>")))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p foo bar<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("foo", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bar", null)
                                },
                                new MarkupTagHelperBlock("strong"))),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                absoluteIndex: 10, lineIndex: 0, columnIndex: 10)
                        }
                    },
                    {
                        "<p class=btn\" bar<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p class=btn\" bar=\"foo\"<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p class=\"btn bar=\"foo\"<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "class",
                                        new MarkupBlock(factory.Markup("btn"), factory.Markup(" bar="))),
                                    new KeyValuePair<string, SyntaxTreeNode>("foo", null)
                                },
                                new MarkupTagHelperBlock("strong"))),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                absoluteIndex: 23, lineIndex: 0, columnIndex: 23)
                        }
                    },
                    {
                        "<p class=\"btn bar=\"foo\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", new MarkupBlock(factory.Markup("btn"), factory.Markup(" bar="))),
                                    new KeyValuePair<string, SyntaxTreeNode>("foo", null),
                                })),
                        new RazorError[0]
                    },
                    {
                        "<p @DateTime.Now class=\"btn\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCSharp, "p"),
                                absoluteIndex: 3, lineIndex: 0 , columnIndex: 3)
                        }
                    },
                    {
                        "<p @DateTime.Now=\"btn\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCSharp, "p"),
                                absoluteIndex: 3, lineIndex: 0 , columnIndex: 3)
                        }
                    },
                    {
                        "<p class=@DateTime.Now\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow)
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p class=\"@do {",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", createInvalidDoBlock(string.Empty))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("do", "}", "{"),
                                absoluteIndex: 11, lineIndex: 0, columnIndex: 11)
                        }
                    },
                    {
                        "<p class=\"@do {\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", createInvalidDoBlock("\"></p>"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("do", "}", "{"),
                                absoluteIndex: 11, lineIndex: 0, columnIndex: 11),
                            new RazorError(
                                RazorResources.ParseError_Unterminated_String_Literal,
                                absoluteIndex: 15, lineIndex: 0, columnIndex: 15)
                        }
                    },
                    {
                        "<p @do { someattribute=\"btn\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCSharp, "p"),
                                absoluteIndex: 3, lineIndex: 0 , columnIndex: 3),
                            new RazorError(
                                RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("do", "}", "{"),
                                absoluteIndex: 4, lineIndex: 0, columnIndex: 4),
                            new RazorError(
                                RazorResources.FormatParseError_UnexpectedEndTag("p"),
                                absoluteIndex: 29, lineIndex: 0, columnIndex: 29)
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MalformedTagHelperAttributeBlockData))]
        public void Rewrite_CreatesErrorForMalformedTagHelpersWithAttributes(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors, "strong", "p");
        }

        public static TheoryData<string, MarkupBlock, RazorError[]> MalformedTagHelperBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var errorFormatUnclosed = "Found a malformed '{0}' tag helper. Tag helpers must have a start and " +
                                          "end tag or be self closing.";
                var errorFormatNoCloseAngle = "Missing close angle for tag helper '{0}'.";

                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero)
                        }
                    },
                    {
                        "<p></p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                absoluteIndex: 3, lineIndex: 0, columnIndex: 3)
                        }
                    },
                    {
                        "<p><strong",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong"))),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "strong"),
                                absoluteIndex: 3, lineIndex: 0, columnIndex: 3),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                absoluteIndex: 3, lineIndex: 0, columnIndex: 3)
                        }
                    },
                    {
                        "<strong <p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("strong",
                                new MarkupTagHelperBlock("p"))),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "strong"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                absoluteIndex: 8, lineIndex: 0, columnIndex: 8)
                        }
                    },
                    {
                        "<strong </strong",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("strong")),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "strong"),
                                SourceLocation.Zero),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "strong"),
                                absoluteIndex: 8, lineIndex: 0, columnIndex: 8)
                        }
                    },
                    {
                        "<<</strong> <<p>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<"),
                            blockFactory.MarkupTagBlock("<"),
                            blockFactory.MarkupTagBlock("</strong>"),
                            factory.Markup(" "),
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                absoluteIndex: 13, lineIndex: 0, columnIndex: 13)
                        }
                    },
                    {
                        "<<<strong>> <<>>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<"),
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("strong",
                                factory.Markup("> "),
                                blockFactory.MarkupTagBlock("<"),
                                blockFactory.MarkupTagBlock("<>"),
                                factory.Markup(">"))),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                absoluteIndex: 2, lineIndex: 0, columnIndex: 2)
                        }
                    },
                    {
                        "<str<strong></p></strong>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<str"),
                            new MarkupTagHelperBlock("strong",
                                blockFactory.MarkupTagBlock("</p>"))),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                absoluteIndex: 12, lineIndex: 0, columnIndex: 12)
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MalformedTagHelperBlockData))]
        public void Rewrite_CreatesErrorForMalformedTagHelper(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors, "strong", "p");
        }

        public static TheoryData CodeTagHelperAttributesData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var dateTimeNow = new MarkupBlock(
                    factory.Markup(" "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                            factory.Code("DateTime.Now")
                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharacters.NonWhiteSpace)));

                return new TheoryData<string, Block>
                {
                    {
                        "<person age=\"12\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("age", factory.CodeMarkup("12"))
                                }))
                    },
                    {
                        "<person birthday=\"DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now"))
                                }))
                    },
                    {
                        "<person name=\"John\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("name", factory.Markup("John"))
                                }))
                    },
                    {
                        "<person name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "name",
                                        new MarkupBlock(factory.Markup("Time:"), dateTimeNow))
                                }))
                    },
                    {
                        "<person age=\"12\" birthday=\"DateTime.Now\" name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("age", factory.CodeMarkup("12")),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now")),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "name",
                                        new MarkupBlock(factory.Markup("Time:"), dateTimeNow))
                                }))
                    },
                    {
                        "<person age=\"12\" birthday=\"DateTime.Now\" name=\"Time: @@ @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("age", factory.CodeMarkup("12")),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now")),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "name",
                                        new MarkupBlock(
                                            factory.Markup("Time:"),
                                             new MarkupBlock(
                                                factory.Markup(" @").Accepts(AcceptedCharacters.None),
                                                factory.Markup("@").With(SpanCodeGenerator.Null).Accepts(AcceptedCharacters.None)),
                                            dateTimeNow))
                                }))
                    },
                    {
                        "<person age=\"12\" birthday=\"DateTime.Now\" name=\"@@BoundStringAttribute\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("age", factory.CodeMarkup("12")),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now")),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "name",
                                        new MarkupBlock(
                                             new MarkupBlock(
                                                factory.Markup("@").Accepts(AcceptedCharacters.None),
                                                factory.Markup("@").With(SpanCodeGenerator.Null).Accepts(AcceptedCharacters.None)),
                                            factory.Markup("BoundStringAttribute")))
                                }))
                    },
                    {
                        "<person age=\"@@@(11+1)\" birthday=\"DateTime.Now\" name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "age",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                factory.Markup("@").Accepts(AcceptedCharacters.None),
                                                factory.Markup("@").With(SpanCodeGenerator.Null).Accepts(AcceptedCharacters.None)),
                                            new MarkupBlock(
                                                factory.EmptyHtml(),
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                                                    factory.Code("11+1").AsExpression(),
                                                    factory.MetaCode(")").Accepts(AcceptedCharacters.None))))),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now")),
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "name",
                                        new MarkupBlock(factory.Markup("Time:"), dateTimeNow))
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CodeTagHelperAttributesData))]
        public void TagHelperParseTreeRewriter_CreatesMarkupCodeSpansForNonStringTagHelperAttributes(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("person", "PersonTagHelper", "personAssembly",
                    attributes: new[]
                    {
                        new TagHelperAttributeDescriptor("age", "Age", typeof(int).FullName),
                        new TagHelperAttributeDescriptor("birthday", "BirthDay", typeof(DateTime).FullName),
                        new TagHelperAttributeDescriptor("name", "Name", typeof(string).FullName),
                    })
            };
            var providerContext = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(providerContext,
                         documentContent,
                         expectedOutput,
                         expectedErrors: Enumerable.Empty<RazorError>());
        }

        public static IEnumerable<object[]> IncompleteHelperBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var malformedErrorFormat = "Found a malformed '{0}' tag helper. Tag helpers must have a start and " +
                                           "end tag or be self closing.";
                var dateTimeNow = new MarkupBlock(
                    new ExpressionBlock(
                        factory.CodeTransition(),
                            factory.Code("DateTime.Now")
                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharacters.NonWhiteSpace)));

                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;><strong></p></strong>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("dynamic", new MarkupBlock(dateTimeNow)),
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            },
                            new MarkupTagHelperBlock("strong")),
                            blockFactory.MarkupTagBlock("</strong>")),
                    new RazorError[]
                    {
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "strong"),
                            absoluteIndex: 52, lineIndex: 0, columnIndex: 52),
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "strong"),
                            absoluteIndex: 64, lineIndex: 0, columnIndex: 64)
                    }
                };
                yield return new object[] {
                    "<div><p>Hello <strong>World</strong></div>",
                    new MarkupBlock(
                        blockFactory.MarkupTagBlock("<div>"),
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("strong",
                                factory.Markup("World")),
                            blockFactory.MarkupTagBlock("</div>"))),
                    new RazorError[]
                    {
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "p"),
                            absoluteIndex: 5, lineIndex: 0, columnIndex: 5)
                    }
                };
                yield return new object[] {
                    "<div><p>Hello <strong>World</div>",
                    new MarkupBlock(
                        blockFactory.MarkupTagBlock("<div>"),
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("strong",
                                factory.Markup("World"),
                                blockFactory.MarkupTagBlock("</div>")))),
                    new RazorError[]
                    {
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "p"),
                            absoluteIndex: 5, lineIndex: 0, columnIndex: 5),
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "strong"),
                            absoluteIndex: 14, lineIndex: 0, columnIndex: 14)
                    }
                };
                yield return new object[] {
                    "<p class=\"foo\">Hello <p style=\"color:red;\">World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo"))
                            },
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("p",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                                },
                                factory.Markup("World")))),
                    new RazorError[]
                    {
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "p"),
                            SourceLocation.Zero)
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(IncompleteHelperBlockData))]
        public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors, "strong", "p");
        }

        public static TheoryData<string, MarkupBlock> InvalidHtmlBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNow = new ExpressionBlock(
                    factory.CodeTransition(),
                        factory.Code("DateTime.Now")
                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                            .Accepts(AcceptedCharacters.NonWhiteSpace));

                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<<<p>>></p>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<"),
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p",
                                factory.Markup(">>")))
                    },
                    {
                        "<<p />",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p", selfClosing: true))
                    },
                    {
                        "< p />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                new MarkupBlock(
                                    factory.Markup(" p")),
                                factory.Markup(" />")))
                    },
                    {
                        "<input <p />",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<input "),
                            new MarkupTagHelperBlock("p", selfClosing: true))
                    },
                    {
                        "< class=\"foo\" <p />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                new MarkupBlock(
                                    new AttributeBlockCodeGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 1, 0, 1),
                                        suffix: new LocationTagged<string>("\"", 12, 0, 12)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("foo").With(new LiteralAttributeCodeGenerator(
                                        prefix: new LocationTagged<string>(string.Empty, 9, 0, 9),
                                        value: new LocationTagged<string>("foo", 9, 0, 9))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(" ")),
                            new MarkupTagHelperBlock("p", selfClosing: true))
                    },
                    {
                        "</<<p>/></p>>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("</"),
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p",
                                factory.Markup("/>")),
                            factory.Markup(">"))
                    },
                    {
                        "</<<p>/><strong></p>>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("</"),
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p",
                                factory.Markup("/>"),
                                blockFactory.MarkupTagBlock("<strong>")),
                            factory.Markup(">"))
                    },
                    {
                        "</<<p>@DateTime.Now/><strong></p>>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("</"),
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p",
                                dateTimeNow,
                                factory.Markup("/>"),
                                blockFactory.MarkupTagBlock("<strong>")),
                            factory.Markup(">"))
                    },
                    {
                        "</  /<  ><p>@DateTime.Now / ><strong></p></        >",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("</  "),
                            factory.Markup("/"),
                            blockFactory.MarkupTagBlock("<  >"),
                            new MarkupTagHelperBlock("p",
                                dateTimeNow,
                                factory.Markup(" / >"),
                                blockFactory.MarkupTagBlock("<strong>")),
                            blockFactory.MarkupTagBlock("</        >"))
                    },
                    {
                        "<p>< @DateTime.Now ></ @DateTime.Now ></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagBlock(
                                    factory.Markup("< "),
                                    dateTimeNow,
                                    factory.Markup(" >")),
                                blockFactory.MarkupTagBlock("</ "),
                                dateTimeNow,
                                factory.Markup(" >")))
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidHtmlBlockData))]
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml(string documentContent, MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p");
        }

        public static IEnumerable<object[]> TextTagsBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                // Should re-write text tags that aren't in C# blocks
                yield return new object[] {
                    "<text>Hello World</text>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("text",
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "@{<text>Hello World</text>}",
                    new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.MarkupTransition("<text>")),
                                factory.Markup("Hello World").Accepts(AcceptedCharacters.None),
                                new MarkupTagBlock(
                                    factory.MarkupTransition("</text>"))),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml())
                };
                yield return new object[] {
                    "@{<text><p>Hello World</p></text>}",
                    new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.MarkupTransition("<text>")),
                                new MarkupTagHelperBlock("p",
                                    factory.Markup("Hello World")),
                                new MarkupTagBlock(
                                    factory.MarkupTransition("</text>"))),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml())
                };
                yield return new object[] {
                    "@{<p><text>Hello World</text></p>}",
                    new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                            new MarkupBlock(
                                new MarkupTagHelperBlock("p",
                                    new MarkupTagHelperBlock("text",
                                        factory.Markup("Hello World")))),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                        factory.EmptyHtml())
                };
            }
        }

        [Theory]
        [MemberData(nameof(TextTagsBlockData))]
        public void TagHelperParseTreeRewriter_DoesNotRewriteTextTagTransitionTagHelpers(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p", "text");
        }

        public static IEnumerable<object[]> SpecialTagsBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                yield return new object[] {
                    "<foo><!-- Hello World --></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<!-- Hello World -->"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[] {
                    "<foo><!-- @foo --></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<!-- "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Markup(" -->"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[] {
                    "<foo><?xml Hello World ?></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<?xml Hello World ?>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[] {
                    "<foo><?xml @foo ?></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<?xml "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Markup(" ?>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[] {
                    "<foo><!DOCTYPE @foo ></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<!DOCTYPE "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Markup(" >"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[] {
                    "<foo><!DOCTYPE hello=\"world\" ></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<!DOCTYPE hello=\"world\" >"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[] {
                    "<foo><![CDATA[ Hello World ]]></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<![CDATA[ Hello World ]]>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[] {
                    "<foo><![CDATA[ @foo ]]></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<![CDATA[ "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        factory.Markup(" ]]>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(SpecialTagsBlockData))]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        public static IEnumerable<object[]> OddlySpacedBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                yield return new object[] {
                    "<p      class=\"     foo\"    style=\"   color :  red  ;   \"    ></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<KeyValuePair<string, SyntaxTreeNode>>
                        {
                            new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("     foo")),
                            new KeyValuePair<string, SyntaxTreeNode>(
                                "style",
                                new MarkupBlock(
                                    factory.Markup("   color"),
                                    factory.Markup(" :"),
                                    factory.Markup("  red"),
                                    factory.Markup("  ;"),
                                    factory.Markup("   ")))
                        }))
                };
                yield return new object[] {
                    "<p      class=\"     foo\"    style=\"   color :  red  ;   \"    >Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("     foo")),
                                new KeyValuePair<string, SyntaxTreeNode>(
                                    "style",
                                    new MarkupBlock(
                                        factory.Markup("   color"),
                                        factory.Markup(" :"),
                                        factory.Markup("  red"),
                                        factory.Markup("  ;"),
                                        factory.Markup("   ")))
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "<p     class=\"   foo  \" >Hello</p> <p    style=\"  color:red; \" >World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>(
                                    "class",
                                    new MarkupBlock(factory.Markup("   foo"), factory.Markup("  ")))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>(
                                    "style",
                                    new MarkupBlock(factory.Markup("  color:red;"), factory.Markup(" ")))
                            },
                            factory.Markup("World")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(OddlySpacedBlockData))]
        public void TagHelperParseTreeRewriter_RewritesOddlySpacedTagHelperTagBlocks(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p");
        }

        public static IEnumerable<object[]> ComplexAttributeTagHelperBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNowString = "@DateTime.Now";
                var dateTimeNow = new MarkupBlock(
                                    new ExpressionBlock(
                                        factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                .Accepts(AcceptedCharacters.NonWhiteSpace)));
                var doWhileString = "@do { var foo = bar; <text>Foo</text> foo++; } while (foo<bar>);";
                var doWhile = new MarkupBlock(
                                new StatementBlock(
                                    factory.CodeTransition(),
                                    factory.Code("do { var foo = bar;").AsStatement(),
                                    new MarkupBlock(
                                        new MarkupTagBlock(
                                            factory.MarkupTransition("<text>")),
                                        factory.Markup("Foo").Accepts(AcceptedCharacters.None),
                                        new MarkupTagBlock(
                                            factory.MarkupTransition("</text>")),
                                        factory.Markup(" ").Accepts(AcceptedCharacters.None)),
                                    factory.Code("foo++; } while (foo<bar>);").AsStatement().Accepts(AcceptedCharacters.None)));

                var currentFormattedString = "<p class=\"{0}\" style='{0}'></p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<KeyValuePair<string, SyntaxTreeNode>>
                        {
                            new KeyValuePair<string, SyntaxTreeNode>("class", new MarkupBlock(dateTimeNow)),
                            new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock(dateTimeNow))
                        }))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<KeyValuePair<string, SyntaxTreeNode>>
                        {
                            new KeyValuePair<string, SyntaxTreeNode>("class", new MarkupBlock(doWhile)),
                            new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock(doWhile))
                        }))
                };

                currentFormattedString = "<p class=\"{0}\" style='{0}'>Hello World</p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", new MarkupBlock(dateTimeNow)),
                                new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock(dateTimeNow))
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", new MarkupBlock(doWhile)),
                                new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock(doWhile))
                            },
                            factory.Markup("Hello World")))
                };

                currentFormattedString = "<p class=\"{0}\">Hello</p> <p style='{0}'>World</p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", new MarkupBlock(dateTimeNow))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock(dateTimeNow))
                            },
                            factory.Markup("World")))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", new MarkupBlock(doWhile))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock(doWhile))
                            },
                            factory.Markup("World")))
                };

                currentFormattedString =
                    "<p class=\"{0}\" style='{0}'>Hello World <strong class=\"{0}\">inside of strong tag</strong></p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", new MarkupBlock(dateTimeNow)),
                                new KeyValuePair<string, SyntaxTreeNode>("style", new MarkupBlock(dateTimeNow))
                            },
                            factory.Markup("Hello World "),
                            new MarkupTagBlock(
                                factory.Markup("<strong"),
                                new MarkupBlock(new AttributeBlockCodeGenerator(name: "class",
                                                                                prefix: new LocationTagged<string>(" class=\"", 66, 0, 66),
                                                                                suffix: new LocationTagged<string>("\"", 87, 0, 87)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    new MarkupBlock(new DynamicAttributeBlockCodeGenerator(new LocationTagged<string>(string.Empty, 74, 0, 74), 74, 0, 74),
                                        new ExpressionBlock(
                                            factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                   .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(">")),
                            factory.Markup("inside of strong tag"),
                            blockFactory.MarkupTagBlock("</strong>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(ComplexAttributeTagHelperBlockData))]
        public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p");
        }

        public static IEnumerable<object[]> ComplexTagHelperBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNowString = "@DateTime.Now";
                var dateTimeNow = new ExpressionBlock(
                                        factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                .Accepts(AcceptedCharacters.NonWhiteSpace));
                var doWhileString = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";
                var doWhile = new StatementBlock(
                               factory.CodeTransition(),
                               factory.Code("do { var foo = bar;").AsStatement(),
                               new MarkupBlock(
                                    factory.Markup(" "),
                                    new MarkupTagHelperBlock("p",
                                        factory.Markup("Foo")),
                                    factory.Markup(" ").Accepts(AcceptedCharacters.None)),
                               factory.Code("foo++; } while (foo<bar>);").AsStatement().Accepts(AcceptedCharacters.None));

                var currentFormattedString = "<p>{0}</p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p", dateTimeNow))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p", doWhile))
                };

                currentFormattedString = "<p>Hello World {0}</p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World "),
                            dateTimeNow))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World "),
                            doWhile))
                };

                currentFormattedString = "<p>{0}</p> <p>{0}</p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p", dateTimeNow),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p", dateTimeNow))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p", doWhile),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p", doWhile))
                };

                currentFormattedString = "<p>Hello {0}<strong>inside of {0} strong tag</strong></p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            dateTimeNow,
                            blockFactory.MarkupTagBlock("<strong>"),
                            factory.Markup("inside of "),
                            dateTimeNow,
                            factory.Markup(" strong tag"),
                            blockFactory.MarkupTagBlock("</strong>")))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            doWhile,
                            blockFactory.MarkupTagBlock("<strong>"),
                            factory.Markup("inside of "),
                            doWhile,
                            factory.Markup(" strong tag"),
                            blockFactory.MarkupTagBlock("</strong>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(ComplexTagHelperBlockData))]
        public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p");
        }

        public static IEnumerable<object[]> ScriptBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                yield return new object[] {
                    "<script><script></foo></script>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            factory.Markup("<script></foo>")))
                };
                yield return new object[] {
                    "<script>Hello World <div></div></script>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            factory.Markup("Hello World <div></div>")))
                };
                yield return new object[] {
                    "<script>Hel<p>lo</p></script> <p><div>World</div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            factory.Markup("Hel<p>lo</p>")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new MarkupTagHelperBlock("div",
                                factory.Markup("World"))))
                };
                yield return new object[] {
                    "<script>Hel<strong>lo</strong></script> <script><span>World</span></script>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            factory.Markup("Hel<strong>lo</strong>")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("script",
                            factory.Markup("<span>World</span>")))
                };
                yield return new object[] {
                    "<script class=\"foo\" style=\"color:red;\" />",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            selfClosing: true,
                            attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            }))
                };
                yield return new object[] {
                    "<p>Hello <script class=\"foo\" style=\"color:red;\"></script> World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("script",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                    new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                                }),
                            factory.Markup(" World")))
                };
                yield return new object[] {
                    "<p>Hello <script class=\"@@foo@bar.com\" style=\"color:red;\"></script> World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("script",
                                new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>(
                                        "class",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                factory.Markup("@").Accepts(AcceptedCharacters.None),
                                                factory.Markup("@").With(SpanCodeGenerator.Null).Accepts(AcceptedCharacters.None)),
                                            factory.Markup("foo@bar.com"))),
                                    new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                                }),
                            factory.Markup(" World")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(ScriptBlockData))]
        public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p", "div", "script");
        }

        public static IEnumerable<object[]> SelfClosingBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                yield return new object[] {
                    "<p class=\"foo\" style=\"color:red;\" />",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            selfClosing: true,
                            attributes:  new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            }))
                };
                yield return new object[] {
                    "<p>Hello <p class=\"foo\" style=\"color:red;\" /> World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock(
                            "p",
                            selfClosing: false,
                            children: new SyntaxTreeNode[] {
                                factory.Markup("Hello "),
                                new MarkupTagHelperBlock(
                                    "p",
                                    selfClosing: true,
                                    attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                        {
                                            new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                            new KeyValuePair<string, SyntaxTreeNode>(
                                                "style",
                                                factory.Markup("color:red;"))
                                        }),
                                factory.Markup(" World")}))
                };
                yield return new object[] {
                    "Hello<p class=\"foo\" /> <p style=\"color:red;\" />World",
                    new MarkupBlock(
                        factory.Markup("Hello"),
                        new MarkupTagHelperBlock("p",
                            selfClosing: true,
                            attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo"))
                            }),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            selfClosing: true,
                            attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            }),
                        factory.Markup("World"))
                };
            }
        }

        [Theory]
        [MemberData(nameof(SelfClosingBlockData))]
        public void TagHelperParseTreeRewriter_RewritesSelfClosingTagHelpers(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p");
        }

        public static IEnumerable<object[]> QuotelessAttributeBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNow = new MarkupBlock(
                                    new ExpressionBlock(
                                        factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                .Accepts(AcceptedCharacters.NonWhiteSpace)));

                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<KeyValuePair<string, SyntaxTreeNode>>
                        {
                            new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                            new KeyValuePair<string, SyntaxTreeNode>("dynamic", new MarkupBlock(dateTimeNow)),
                            new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                        }))
                };
                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("dynamic", new MarkupBlock(dateTimeNow)),
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now style=color@@:red;>Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("dynamic", new MarkupBlock(dateTimeNow)),
                                new KeyValuePair<string, SyntaxTreeNode>(
                                    "style",
                                    new MarkupBlock(
                                        factory.Markup("color"),
                                         new MarkupBlock(
                                            factory.Markup("@").Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanCodeGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup(":red;")))
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now>Hello</p> <p style=color:red; dynamic=@DateTime.Now>World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("dynamic", new MarkupBlock(dateTimeNow))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;")),
                                new KeyValuePair<string, SyntaxTreeNode>("dynamic", new MarkupBlock(dateTimeNow))
                            },
                            factory.Markup("World")))
                };
                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World <strong class=\"foo\">inside of strong tag</strong></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("dynamic", new MarkupBlock(dateTimeNow)),
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("Hello World "),
                            new MarkupTagBlock(
                                factory.Markup("<strong"),
                                new MarkupBlock(new AttributeBlockCodeGenerator(name: "class",
                                                                                prefix: new LocationTagged<string>(" class=\"", 71, 0, 71),
                                                                                suffix: new LocationTagged<string>("\"", 82, 0, 82)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("foo").With(new LiteralAttributeCodeGenerator(prefix: new LocationTagged<string>(string.Empty, 79, 0, 79),
                                                                                                 value: new LocationTagged<string>("foo", 79, 0, 79))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(">")),
                            factory.Markup("inside of strong tag"),
                            blockFactory.MarkupTagBlock("</strong>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(QuotelessAttributeBlockData))]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p");
        }

        public static IEnumerable<object[]> PlainAttributeBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);

                yield return new object[] {
                    "<p class=\"foo\" style=\"color:red;\"></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<KeyValuePair<string, SyntaxTreeNode>>
                        {
                            new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                            new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                        }))
                };
                yield return new object[] {
                    "<p class=\"foo\" style=\"color:red;\">Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "<p class=\"foo\">Hello</p> <p style=\"color:red;\">World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo"))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("World")))
                };
                yield return new object[] {
                    "<p class=\"foo\" style=\"color:red;\">Hello World <strong class=\"foo\">inside of strong tag</strong></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<KeyValuePair<string, SyntaxTreeNode>>
                            {
                                new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("foo")),
                                new KeyValuePair<string, SyntaxTreeNode>("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("Hello World "),
                            new MarkupTagBlock(
                                factory.Markup("<strong"),
                                new MarkupBlock(new AttributeBlockCodeGenerator(name: "class",
                                                                                prefix: new LocationTagged<string>(" class=\"", 53, 0, 53),
                                                                                suffix: new LocationTagged<string>("\"", 64, 0, 64)),
                                    factory.Markup(" class=\"").With(SpanCodeGenerator.Null),
                                    factory.Markup("foo").With(new LiteralAttributeCodeGenerator(prefix: new LocationTagged<string>(string.Empty, 61, 0, 61),
                                                                                                 value: new LocationTagged<string>("foo", 61, 0, 61))),
                                    factory.Markup("\"").With(SpanCodeGenerator.Null)),
                                factory.Markup(">")),
                            factory.Markup("inside of strong tag"),
                            blockFactory.MarkupTagBlock("</strong>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(PlainAttributeBlockData))]
        public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p");
        }

        public static IEnumerable<object[]> PlainBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);

                yield return new object[] {
                    "<p></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p"))
                };
                yield return new object[] {
                    "<p>Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "<p>Hello</p> <p>World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            factory.Markup("World")))
                };
                yield return new object[] {
                    "<p>Hello World <strong>inside of strong tag</strong></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World "),
                            blockFactory.MarkupTagBlock("<strong>"),
                            factory.Markup("inside of strong tag"),
                            blockFactory.MarkupTagBlock("</strong>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(PlainBlockData))]
        public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p");
        }

        public static IEnumerable<object[]> NestedBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);

                yield return new object[] {
                    "<p><div></div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new MarkupTagHelperBlock("div")))
                };
                yield return new object[] {
                    "<p>Hello World <div></div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World "),
                            new MarkupTagHelperBlock("div")))
                };
                yield return new object[] {
                    "<p>Hel<p>lo</p></p> <p><div>World</div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hel"),
                            new MarkupTagHelperBlock("p",
                                factory.Markup("lo"))),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new MarkupTagHelperBlock("div",
                                factory.Markup("World"))))
                };
                yield return new object[] {
                    "<p>Hel<strong>lo</strong></p> <p><span>World</span></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hel"),
                            blockFactory.MarkupTagBlock("<strong>"),
                            factory.Markup("lo"),
                            blockFactory.MarkupTagBlock("</strong>")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            blockFactory.MarkupTagBlock("<span>"),
                            factory.Markup("World"),
                            blockFactory.MarkupTagBlock("</span>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(NestedBlockData))]
        public void TagHelperParseTreeRewriter_RewritesNestedTagHelperTagBlocks(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, expectedOutput, "p", "div");
        }
    }
}