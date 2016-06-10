// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Microsoft.AspNetCore.Razor.Test.Parser.TagHelpers.Internal;
using Microsoft.AspNetCore.Razor.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Parser.TagHelpers.Internal
{
    public class TagHelperBlockRewriterTest : TagHelperRewritingTestBase
    {
        public static TheoryData SymbolBoundAttributeData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<ul bound [item]='items'></ul>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("ul",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("[item]", factory.CodeMarkup("items"), HtmlAttributeValueStyle.SingleQuotes)
                                }))
                    },
                    {
                        "<ul bound [(item)]='items'></ul>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("ul",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("[(item)]", factory.CodeMarkup("items"), HtmlAttributeValueStyle.SingleQuotes)
                                }))
                    },
                    {
                        "<button bound (click)='doSomething()'>Click Me</button>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("button",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode(
                                        "(click)",
                                        factory.CodeMarkup("doSomething()"),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                },
                                children: factory.Markup("Click Me")))
                    },
                    {
                        "<button bound (^click)='doSomething()'>Click Me</button>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("button",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode(
                                        "(^click)",
                                        factory.CodeMarkup("doSomething()"),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                },
                                children: factory.Markup("Click Me")))
                    },
                    {
                        "<template bound *something='value'></template>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("template",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode(
                                        "*something",
                                        factory.Markup("value"),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                }))
                    },
                    {
                        "<div bound #localminimized></div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("#localminimized", null, HtmlAttributeValueStyle.Minimized)
                                }))
                    },
                    {
                        "<div bound #local='value'></div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("#local", factory.Markup("value"), HtmlAttributeValueStyle.SingleQuotes)
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SymbolBoundAttributeData))]
        public void Rewrite_CanHandleSymbolBoundAttributes(string documentContent, MarkupBlock expectedOutput)
        {
            // Arrange
            var descriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "*",
                    TypeName = "CatchAllTagHelper",
                    AssemblyName = "SomeAssembly",
                    Attributes = new[]
                    {
                        new TagHelperAttributeDescriptor
                        {
                            Name = "[item]",
                            PropertyName = "ListItems",
                            TypeName = typeof(List<string>).FullName
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "[(item)]",
                            PropertyName = "ArrayItems",
                            TypeName = typeof(string[]).FullName
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "(click)",
                            PropertyName = "Event1",
                            TypeName = typeof(Action).FullName
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "(^click)",
                            PropertyName = "Event2",
                            TypeName = typeof(Action).FullName
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "*something",
                            PropertyName = "StringProperty1",
                            TypeName = typeof(string).FullName
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "#local",
                            PropertyName = "StringProperty2",
                            TypeName = typeof(string).FullName
                        },
                    },
                    RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "bound" } },
                },
            };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new RazorError[0]);
        }

        public static TheoryData WithoutEndTagElementData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<input>",
                        new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.StartTagOnly))
                    },
                    {
                        "<input type='text'>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagOnly,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("type", factory.Markup("text"), HtmlAttributeValueStyle.SingleQuotes)
                                }))
                    },
                    {
                        "<input><input>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly),
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly))
                    },
                    {
                        "<input type='text'><input>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagOnly,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("type", factory.Markup("text"), HtmlAttributeValueStyle.SingleQuotes)
                                }),
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly))
                    },
                    {
                        "<div><input><input></div>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<div>"),
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly),
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly),
                            blockFactory.MarkupTagBlock("</div>"))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(WithoutEndTagElementData))]
        public void Rewrite_CanHandleWithoutEndTagTagStructure(string documentContent, MarkupBlock expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        TagStructure = TagStructure.WithoutEndTag,
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new RazorError[0]);
        }

        public static TheoryData TagStructureCompatibilityData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, structure1, structure2, expectedOutput
                return new TheoryData<string, TagStructure, TagStructure, MarkupBlock>
                {
                    {
                        "<input></input>",
                        TagStructure.Unspecified,
                        TagStructure.Unspecified,
                        new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.StartTagAndEndTag))
                    },
                    {
                        "<input />",
                        TagStructure.Unspecified,
                        TagStructure.Unspecified,
                        new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.SelfClosing))
                    },
                    {
                        "<input type='text'>",
                        TagStructure.Unspecified,
                        TagStructure.WithoutEndTag,
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagOnly,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("type", factory.Markup("text"), HtmlAttributeValueStyle.SingleQuotes)
                                }))
                    },
                    {
                        "<input><input>",
                        TagStructure.WithoutEndTag,
                        TagStructure.WithoutEndTag,
                        new MarkupBlock(
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly),
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly))
                    },
                    {
                        "<input type='text'></input>",
                        TagStructure.Unspecified,
                        TagStructure.NormalOrSelfClosing,
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("type", factory.Markup("text"), HtmlAttributeValueStyle.SingleQuotes)
                                }))
                    },
                    {
                        "<input />",
                        TagStructure.Unspecified,
                        TagStructure.WithoutEndTag,
                        new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.SelfClosing))
                    },

                    {
                        "<input />",
                        TagStructure.NormalOrSelfClosing,
                        TagStructure.Unspecified,
                        new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.SelfClosing))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagStructureCompatibilityData))]
        public void Rewrite_AllowsCompatibleTagStructures(
            string documentContent,
            TagStructure structure1,
            TagStructure structure2,
            MarkupBlock expectedOutput)
        {
            // Arrange
            var factory = CreateDefaultSpanFactory();
            var blockFactory = new BlockFactory(factory);
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper1",
                        AssemblyName = "SomeAssembly",
                        TagStructure = structure1
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper2",
                        AssemblyName = "SomeAssembly",
                        TagStructure = structure2
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new RazorError[0]);
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
                            new DynamicAttributeBlockChunkGenerator(
                                new LocationTagged<string>(
                                    string.Empty,
                                    new SourceLocation(10, 0, 10)),
                                new SourceLocation(10, 0, 10)),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.Code("do {" + extraCode).AsStatement())));
                };

                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<p class='",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "class",
                                        factory.Markup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p bar=\"false\"\" <strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bar", factory.Markup("false"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p bar='false  <strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "bar",
                                        new MarkupBlock(factory.Markup("false"), factory.Markup("  <strong>")),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p bar='false  <strong'",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "bar",
                                        new MarkupBlock(
                                            factory.Markup("false"),
                                            factory.Markup("  <strong")),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p bar=false'",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "bar",
                                        factory.Markup("false"),
                                        HtmlAttributeValueStyle.DoubleQuotes)
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                "TagHelper attributes must be well-formed.",
                                new SourceLocation(12, 0, 12),
                                length: 1)
                        }
                    },
                    {
                        "<p bar=\"false'",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "bar",
                                        factory.Markup("false'"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p bar=\"false' ></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "bar",
                                        new MarkupBlock(
                                            factory.Markup("false'"),
                                            factory.Markup(" ></p>")))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p foo bar<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("foo", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bar", null, HtmlAttributeValueStyle.Minimized)
                                },
                                new MarkupTagHelperBlock("strong"))),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                new SourceLocation(11, 0, 11),
                                length: 6)
                        }
                    },
                    {
                        "<p class=btn\" bar<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p class=btn\" bar=\"foo\"<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p class=\"btn bar=\"foo\"<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "class",
                                        new MarkupBlock(factory.Markup("btn"), factory.Markup(" bar="))),
                                    new TagHelperAttributeNode("foo", null, HtmlAttributeValueStyle.Minimized)
                                },
                                new MarkupTagHelperBlock("strong"))),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                new SourceLocation(24, 0, 24),
                                length: 6)
                        }
                    },
                    {
                        "<p class=\"btn bar=\"foo\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "class",
                                        new MarkupBlock(factory.Markup("btn"), factory.Markup(" bar="))),
                                    new TagHelperAttributeNode("foo", null, HtmlAttributeValueStyle.Minimized),
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
                                absoluteIndex: 3, lineIndex: 0 , columnIndex: 3, length: 13)
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
                                absoluteIndex: 3, lineIndex: 0 , columnIndex: 3, length: 13)
                        }
                    },
                    {
                        "<p class=@DateTime.Now\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "class",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                new DynamicAttributeBlockChunkGenerator(
                                                    new LocationTagged<string>(
                                                        string.Empty,
                                                        new SourceLocation(9, 0, 9)),
                                                    new SourceLocation(9, 0, 9)),
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                        factory.Code("DateTime.Now")
                                                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                            .Accepts(AcceptedCharacters.NonWhiteSpace)))),
                                        HtmlAttributeValueStyle.DoubleQuotes)
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
                        }
                    },
                    {
                        "<p class=\"@do {",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "class",
                                        createInvalidDoBlock(string.Empty))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("do", "}", "{"),
                                absoluteIndex: 11, lineIndex: 0, columnIndex: 11, length: 1)
                        }
                    },
                    {
                        "<p class=\"@do {\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", createInvalidDoBlock("\"></p>"))
                                })),
                        new []
                        {
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("do", "}", "{"),
                                absoluteIndex: 11, lineIndex: 0, columnIndex: 11, length: 1),
                            new RazorError(
                                RazorResources.ParseError_Unterminated_String_Literal,
                                absoluteIndex: 15, lineIndex: 0, columnIndex: 15, length: 1)
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
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCSharp, "p"),
                                absoluteIndex: 3, lineIndex: 0 , columnIndex: 3, length: 30),
                            new RazorError(
                                RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("do", "}", "{"),
                                absoluteIndex: 4, lineIndex: 0, columnIndex: 4, length: 1),
                            new RazorError(
                                RazorResources.FormatParseError_UnexpectedEndTag("p"),
                                absoluteIndex: 31, lineIndex: 0, columnIndex: 31, length: 1)
                        }
                    },
                    {
                        "<p class=some=thing attr=\"@value\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("some"))
                                })),
                        new []
                        {
                            new RazorError(
                                "TagHelper attributes must be well-formed.",
                                new SourceLocation(13, 0, 13),
                                length: 13)
                        }
                    },
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
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1)
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
                                absoluteIndex: 5, lineIndex: 0, columnIndex: 5, length: 1)
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
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "strong"),
                                new SourceLocation(4, 0, 4),
                                length: 6),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                new SourceLocation(4, 0, 4),
                                length: 6)
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
                                new SourceLocation(1, 0, 1),
                                length: 6),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "strong"),
                                new SourceLocation(1, 0, 1),
                                length: 6),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(9, 0, 9),
                                length: 1)
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
                                new SourceLocation(1, 0, 1),
                                length: 6),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatNoCloseAngle, "strong"),
                                new SourceLocation(10, 0, 10),
                                length: 6)
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
                                new SourceLocation(4, 0, 4),
                                length: 6),
                            new RazorError(
                                string.Format(CultureInfo.InvariantCulture, errorFormatUnclosed, "p"),
                                new SourceLocation(14, 0, 14),
                                length: 1)
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
                                new SourceLocation(3, 0, 3),
                                length: 6)
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
                                new SourceLocation(14, 0, 14),
                                length: 1)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("age", factory.CodeMarkup("12"))
                                }))
                    },
                    {
                        "<person birthday=\"DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now"))
                                }))
                    },
                    {
                        "<person age=\"@DateTime.Now.Year\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "age",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory
                                                        .Code("DateTime.Now.Year")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace)))))
                                }))
                    },
                    {
                        "<person age=\" @DateTime.Now.Year\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "age",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                factory.CodeMarkup(" "),
                                                new ExpressionBlock(
                                                    factory
                                                        .CodeTransition()
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory
                                                        .Code("DateTime.Now.Year")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace)))))
                                }))
                    },
                    {
                        "<person name=\"John\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("name", factory.Markup("John"))
                                }))
                    },
                    {
                        "<person name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "name",
                                        new MarkupBlock(factory.Markup("Time:"), dateTimeNow))
                                }))
                    },
                    {
                        "<person age=\"1 + @value + 2\" birthday='(bool)@Bag[\"val\"] ? @@DateTime : @DateTime.Now'/>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "age",
                                        new MarkupBlock(
                                            factory.CodeMarkup("1"),
                                            factory.CodeMarkup(" +"),
                                            new MarkupBlock(
                                                factory.CodeMarkup(" "),
                                                new ExpressionBlock(
                                                    factory
                                                        .CodeTransition()
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory
                                                        .Code("value")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                            factory.CodeMarkup(" +"),
                                            factory.CodeMarkup(" 2"))),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        new MarkupBlock(
                                            factory.CodeMarkup("(bool)"),
                                            new MarkupBlock(
                                                new ExpressionBlock(
                                                    factory
                                                        .CodeTransition()
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory
                                                        .Code("Bag[\"val\"]")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                            factory.CodeMarkup(" ?"),
                                            new MarkupBlock(
                                                factory.CodeMarkup(" @").Accepts(AcceptedCharacters.None),
                                                factory.CodeMarkup("@")
                                                    .With(SpanChunkGenerator.Null)
                                                    .Accepts(AcceptedCharacters.None)),
                                            factory.CodeMarkup("DateTime"),
                                            factory.CodeMarkup(" :"),
                                            new MarkupBlock(
                                                factory.CodeMarkup(" "),
                                                new ExpressionBlock(
                                                    factory
                                                        .CodeTransition()
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory
                                                        .Code("DateTime.Now")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace)))),
                                        HtmlAttributeValueStyle.SingleQuotes)
                                }))
                    },
                    {
                        "<person age=\"12\" birthday=\"DateTime.Now\" name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("age", factory.CodeMarkup("12")),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now")),
                                    new TagHelperAttributeNode(
                                        "name",
                                        new MarkupBlock(factory.Markup("Time:"), dateTimeNow))
                                }))
                    },
                    {
                        "<person age=\"12\" birthday=\"DateTime.Now\" name=\"Time: @@ @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("age", factory.CodeMarkup("12")),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now")),
                                    new TagHelperAttributeNode(
                                        "name",
                                        new MarkupBlock(
                                            factory.Markup("Time:"),
                                             new MarkupBlock(
                                                factory.Markup(" @").Accepts(AcceptedCharacters.None),
                                                factory.Markup("@")
                                                    .With(SpanChunkGenerator.Null)
                                                    .Accepts(AcceptedCharacters.None)),
                                            dateTimeNow))
                                }))
                    },
                    {
                        "<person age=\"12\" birthday=\"DateTime.Now\" name=\"@@BoundStringAttribute\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("age", factory.CodeMarkup("12")),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now")),
                                    new TagHelperAttributeNode(
                                        "name",
                                        new MarkupBlock(
                                             new MarkupBlock(
                                                factory.Markup("@").Accepts(AcceptedCharacters.None),
                                                factory.Markup("@")
                                                    .With(SpanChunkGenerator.Null)
                                                    .Accepts(AcceptedCharacters.None)),
                                            factory.Markup("BoundStringAttribute")))
                                }))
                    },
                    {
                        "<person age=\"@@@(11+1)\" birthday=\"DateTime.Now\" name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "age",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                factory.CodeMarkup("@")
                                                    .Accepts(AcceptedCharacters.None)
                                                    .With(new MarkupChunkGenerator()),
                                                factory.CodeMarkup("@")
                                                    .With(SpanChunkGenerator.Null)
                                                    .Accepts(AcceptedCharacters.None)),
                                            new MarkupBlock(
                                                factory.EmptyHtml().As(SpanKind.Code),
                                                new ExpressionBlock(
                                                    factory.CodeTransition()
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory.MetaCode("(")
                                                        .Accepts(AcceptedCharacters.None)
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory.Code("11+1").AsExpression(),
                                                    factory.MetaCode(")")
                                                        .Accepts(AcceptedCharacters.None)
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()))))),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now")),
                                    new TagHelperAttributeNode(
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
                new TagHelperDescriptor
                {
                    TagName = "person",
                    TypeName = "PersonTagHelper",
                    AssemblyName = "personAssembly",
                    Attributes = new[]
                    {
                        new TagHelperAttributeDescriptor
                        {
                            Name = "age",
                            PropertyName = "Age",
                            TypeName = typeof(int).FullName
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "birthday",
                            PropertyName = "BirthDay",
                            TypeName = typeof(DateTime).FullName
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "name",
                            PropertyName = "Name",
                            TypeName = typeof(string).FullName,
                            IsStringProperty = true
                        }
                    }
                }
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

                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;><strong></p></strong>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode(
                                    "dynamic",
                                    new MarkupBlock(
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(
                                                new LocationTagged<string>(
                                                    string.Empty,
                                                    new SourceLocation(21, 0, 21)),
                                                new SourceLocation(21, 0, 21)),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                    factory.Code("DateTime.Now")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace)))),
                                    HtmlAttributeValueStyle.DoubleQuotes),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                            },
                            new MarkupTagHelperBlock("strong")),
                            blockFactory.MarkupTagBlock("</strong>")),
                    new RazorError[]
                    {
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "strong"),
                            absoluteIndex: 53, lineIndex: 0, columnIndex: 53, length: 6),
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "strong"),
                            absoluteIndex: 66, lineIndex: 0, columnIndex: 66, length: 6)
                    }
                };
                yield return new object[]
                {
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
                            absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 1)
                    }
                };
                yield return new object[]
                {
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
                            absoluteIndex: 6, lineIndex: 0, columnIndex: 6, length: 1),
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "strong"),
                            absoluteIndex: 15, lineIndex: 0, columnIndex: 15, length: 6)
                    }
                };
                yield return new object[]
                {
                    "<p class=\"foo\">Hello <p style=\"color:red;\">World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo"))
                            },
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                                },
                                factory.Markup("World")))),
                    new RazorError[]
                    {
                        new RazorError(
                            string.Format(CultureInfo.InvariantCulture, malformedErrorFormat, "p"),
                            new SourceLocation(1, 0, 1),
                            length: 1)
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


        public static IEnumerable<object[]> OddlySpacedBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                yield return new object[]
                {
                    "<p      class=\"     foo\"    style=\"   color :  red  ;   \"    ></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<TagHelperAttributeNode>
                        {
                            new TagHelperAttributeNode("class", factory.Markup("     foo")),
                            new TagHelperAttributeNode(
                                "style",
                                new MarkupBlock(
                                    factory.Markup("   color"),
                                    factory.Markup(" :"),
                                    factory.Markup("  red"),
                                    factory.Markup("  ;"),
                                    factory.Markup("   ")))
                        }))
                };
                yield return new object[]
                {
                    "<p      class=\"     foo\"    style=\"   color :  red  ;   \"    >Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("     foo")),
                                new TagHelperAttributeNode(
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
                yield return new object[]
                {
                    "<p     class=\"   foo  \" >Hello</p> <p    style=\"  color:red; \" >World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode(
                                    "class",
                                    new MarkupBlock(factory.Markup("   foo"), factory.Markup("  ")))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode(
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
                var dateTimeNow = new Func<int, SyntaxTreeNode>(index =>
                    new MarkupBlock(
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(
                                new LocationTagged<string>(
                                    string.Empty,
                                    new SourceLocation(index, 0, index)),
                                new SourceLocation(index, 0, index)),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                    factory.Code("DateTime.Now")
                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                        .Accepts(AcceptedCharacters.NonWhiteSpace)))));
                var doWhileString = "@do { var foo = bar; <text>Foo</text> foo++; } while (foo<bar>);";
                var doWhile = new Func<int, SyntaxTreeNode>(index =>
                    new MarkupBlock(
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(
                                new LocationTagged<string>(
                                    string.Empty,
                                    new SourceLocation(index, 0, index)),
                                new SourceLocation(index, 0, index)),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.Code("do { var foo = bar;").AsStatement(),
                                new MarkupBlock(
                                    new MarkupTagBlock(
                                        factory.MarkupTransition("<text>")),
                                    factory.Markup("Foo").Accepts(AcceptedCharacters.None),
                                    new MarkupTagBlock(
                                        factory.MarkupTransition("</text>"))),
                                factory
                                    .Code(" foo++; } while (foo<bar>);")
                                    .AsStatement()
                                    .Accepts(AcceptedCharacters.None)))));

                var currentFormattedString = "<p class=\"{0}\" style='{0}'></p>";
                yield return new object[]
                {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<TagHelperAttributeNode>
                        {
                            new TagHelperAttributeNode("class", dateTimeNow(10)),
                            new TagHelperAttributeNode("style", dateTimeNow(32), HtmlAttributeValueStyle.SingleQuotes)
                        }))
                };
                yield return new object[]
                {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<TagHelperAttributeNode>
                        {
                            new TagHelperAttributeNode("class", doWhile(10)),
                            new TagHelperAttributeNode("style", doWhile(83), HtmlAttributeValueStyle.SingleQuotes)
                        }))
                };

                currentFormattedString = "<p class=\"{0}\" style='{0}'>Hello World</p>";
                yield return new object[]
                {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", dateTimeNow(10)),
                                new TagHelperAttributeNode("style", dateTimeNow(32), HtmlAttributeValueStyle.SingleQuotes)
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[]
                {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", doWhile(10)),
                                new TagHelperAttributeNode("style", doWhile(83), HtmlAttributeValueStyle.SingleQuotes)
                            },
                            factory.Markup("Hello World")))
                };

                currentFormattedString = "<p class=\"{0}\">Hello</p> <p style='{0}'>World</p>";
                yield return new object[]
                {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", dateTimeNow(10))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("style", dateTimeNow(45), HtmlAttributeValueStyle.SingleQuotes)
                            },
                            factory.Markup("World")))
                };
                yield return new object[]
                {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", doWhile(10))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("style", doWhile(96), HtmlAttributeValueStyle.SingleQuotes)
                            },
                            factory.Markup("World")))
                };

                currentFormattedString =
                    "<p class=\"{0}\" style='{0}'>Hello World <strong class=\"{0}\">inside of strong tag</strong></p>";
                yield return new object[]
                {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", dateTimeNow(10)),
                                new TagHelperAttributeNode("style", dateTimeNow(32), HtmlAttributeValueStyle.SingleQuotes)
                            },
                            factory.Markup("Hello World "),
                            new MarkupTagBlock(
                                factory.Markup("<strong"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 66, 0, 66),
                                        suffix: new LocationTagged<string>("\"", 87, 0, 87)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(
                                            new LocationTagged<string>(string.Empty, 74, 0, 74), 74, 0, 74),
                                        new ExpressionBlock(
                                            factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                   .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                               factory.Code("foo++; } while (foo<bar>);")
                                .AsStatement()
                                .Accepts(AcceptedCharacters.None));

                var currentFormattedString = "<p>{0}</p>";
                yield return new object[]
                {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p", dateTimeNow))
                };
                yield return new object[]
                {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p", doWhile))
                };

                currentFormattedString = "<p>Hello World {0}</p>";
                yield return new object[]
                {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World "),
                            dateTimeNow))
                };
                yield return new object[]
                {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World "),
                            doWhile))
                };

                currentFormattedString = "<p>{0}</p> <p>{0}</p>";
                yield return new object[]
                {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p", dateTimeNow),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p", dateTimeNow))
                };
                yield return new object[]
                {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p", doWhile),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p", doWhile))
                };

                currentFormattedString = "<p>Hello {0}<strong>inside of {0} strong tag</strong></p>";
                yield return new object[]
                {
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
                yield return new object[]
                {
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
                            new MarkupTagHelperBlock("p", TagMode.SelfClosing))
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
                            new MarkupTagHelperBlock("p", TagMode.SelfClosing))
                    },
                    {
                        "< class=\"foo\" <p />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 1, 0, 1),
                                        suffix: new LocationTagged<string>("\"", 12, 0, 12)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("foo").With(new LiteralAttributeChunkGenerator(
                                        prefix: new LocationTagged<string>(string.Empty, 9, 0, 9),
                                        value: new LocationTagged<string>("foo", 9, 0, 9))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(" ")),
                            new MarkupTagHelperBlock("p", TagMode.SelfClosing))
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
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class",  new MarkupBlock())
                            }))
                    },
                    {
                        "<p class=''></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class",  new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes)
                            }))
                    },
                    {
                        "<p class=></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                // We expected a markup node here because attribute values without quotes can only ever
                                // be a single item, hence don't need to be enclosed by a block.
                                new TagHelperAttributeNode(
                                    "class",
                                    factory.Markup("").With(SpanChunkGenerator.Null),
                                    HtmlAttributeValueStyle.DoubleQuotes),
                            }))
                    },
                    {
                        "<p class1='' class2= class3=\"\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class1", new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "class2",
                                        factory.Markup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.DoubleQuotes),
                                    new TagHelperAttributeNode("class3", new MarkupBlock()),
                                }))
                    },
                    {
                        "<p class1=''class2=\"\"class3= />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class1",  new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("class2",  new MarkupBlock()),
                                    new TagHelperAttributeNode(
                                        "class3",
                                        factory.Markup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.DoubleQuotes),
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", factory.CodeMarkup("    true"), HtmlAttributeValueStyle.SingleQuotes)
                                })),
                        new RazorError[0]
                    },
                    {
                        "<myth bound='    ' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", factory.CodeMarkup("    "), HtmlAttributeValueStyle.SingleQuotes)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound", new MarkupBlock())
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", factory.CodeMarkup(" "), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound", factory.CodeMarkup("  "))
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", factory.CodeMarkup("true"), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.DoubleQuotes)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.DoubleQuotes),
                                    new TagHelperAttributeNode("name", new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.DoubleQuotes),
                                    new TagHelperAttributeNode("name", factory.Markup("  "), HtmlAttributeValueStyle.SingleQuotes)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", factory.CodeMarkup("true"), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("name", factory.Markup("john"), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.DoubleQuotes),
                                    new TagHelperAttributeNode(
                                        "name",
                                        factory.Markup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.DoubleQuotes)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("BouND", new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("BOUND", new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bOUnd", new MarkupBlock())
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
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "BOUND",
                                        factory.CodeMarkup(string.Empty).With(SpanChunkGenerator.Null),
                                        HtmlAttributeValueStyle.DoubleQuotes),
                                    new TagHelperAttributeNode("nAMe", factory.Markup("john"), HtmlAttributeValueStyle.SingleQuotes)
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
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    {
                                        new TagHelperAttributeNode(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                factory.CodeMarkup("    "),
                                                new ExpressionBlock(
                                                    factory.CodeTransition()
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory.Code("true")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                                factory.CodeMarkup("  ")),
                                            HtmlAttributeValueStyle.SingleQuotes)
                                    }
                                })),
                        new RazorError[0]
                    },
                    {
                        "<myth bound='    @(true)  ' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    {
                                        new TagHelperAttributeNode(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                factory.CodeMarkup("    "),
                                                new ExpressionBlock(
                                                    factory.CodeTransition()
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory.MetaCode("(")
                                                        .Accepts(AcceptedCharacters.None)
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()),
                                                    factory.Code("true").AsExpression(),
                                                    factory.MetaCode(")")
                                                        .Accepts(AcceptedCharacters.None)
                                                        .As(SpanKind.Code)
                                                        .With(new MarkupChunkGenerator()))),
                                                factory.CodeMarkup("  ")),
                                            HtmlAttributeValueStyle.SingleQuotes)
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
                    new TagHelperDescriptor
                    {
                        TagName = "myth",
                        TypeName = "mythTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound",
                                PropertyName = "Bound",
                                TypeName = typeof(bool).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "name",
                                PropertyName = "Name",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        }
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors);
        }

        public static IEnumerable<object[]> ScriptBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                yield return new object[]
                {
                    "<script><script></foo></script>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            factory.Markup("<script></foo>")))
                };
                yield return new object[]
                {
                    "<script>Hello World <div></div></script>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            factory.Markup("Hello World <div></div>")))
                };
                yield return new object[]
                {
                    "<script>Hel<p>lo</p></script> <p><div>World</div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            factory.Markup("Hel<p>lo</p>")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new MarkupTagHelperBlock("div",
                                factory.Markup("World"))))
                };
                yield return new object[]
                {
                    "<script>Hel<strong>lo</strong></script> <script><span>World</span></script>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            factory.Markup("Hel<strong>lo</strong>")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("script",
                            factory.Markup("<span>World</span>")))
                };
                yield return new object[]
                {
                    "<script class=\"foo\" style=\"color:red;\" />",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("script",
                            TagMode.SelfClosing,
                            attributes: new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                            }))
                };
                yield return new object[]
                {
                    "<p>Hello <script class=\"foo\" style=\"color:red;\"></script> World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("script",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("foo")),
                                    new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                                }),
                            factory.Markup(" World")))
                };
                yield return new object[]
                {
                    "<p>Hello <script class=\"@@foo@bar.com\" style=\"color:red;\"></script> World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("script",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "class",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                factory.Markup("@").Accepts(AcceptedCharacters.None),
                                                factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                            factory.Markup("foo@bar.com"))),
                                    new TagHelperAttributeNode("style", factory.Markup("color:red;"))
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

                yield return new object[]
                {
                    "<p class=\"foo\" style=\"color:red;\" />",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            TagMode.SelfClosing,
                            attributes:  new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                            }))
                };
                yield return new object[]
                {
                    "<p>Hello <p class=\"foo\" style=\"color:red;\" /> World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock(
                            "p",
                            TagMode.StartTagAndEndTag,
                            children: new SyntaxTreeNode[]
                            {
                                factory.Markup("Hello "),
                                new MarkupTagHelperBlock(
                                    "p",
                                    TagMode.SelfClosing,
                                    attributes: new List<TagHelperAttributeNode>
                                        {
                                            new TagHelperAttributeNode("class", factory.Markup("foo")),
                                            new TagHelperAttributeNode(
                                                "style",
                                                factory.Markup("color:red;"))
                                        }),
                                factory.Markup(" World")
                            }))
                };
                yield return new object[]
                {
                    "Hello<p class=\"foo\" /> <p style=\"color:red;\" />World",
                    new MarkupBlock(
                        factory.Markup("Hello"),
                        new MarkupTagHelperBlock("p",
                            TagMode.SelfClosing,
                            attributes: new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo"))
                            }),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            TagMode.SelfClosing,
                            attributes: new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
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
                var dateTimeNow = new Func<int, SyntaxTreeNode>(index =>
                    new MarkupBlock(
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(
                                new LocationTagged<string>(
                                    string.Empty,
                                    new SourceLocation(index, 0, index)),
                                new SourceLocation(index, 0, index)),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                    factory.Code("DateTime.Now")
                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                        .Accepts(AcceptedCharacters.NonWhiteSpace)))));

                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<TagHelperAttributeNode>
                        {
                            new TagHelperAttributeNode("class", factory.Markup("foo")),
                            new TagHelperAttributeNode("dynamic", dateTimeNow(21)),
                            new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                        }))
                };
                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(21)),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now style=color@@:red;>Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(21)),
                                new TagHelperAttributeNode(
                                    "style",
                                    new MarkupBlock(
                                        factory.Markup("color"),
                                         new MarkupBlock(
                                            factory.Markup("@").Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup(":red;")),
                                    HtmlAttributeValueStyle.DoubleQuotes)
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now>Hello</p> <p style=color:red; dynamic=@DateTime.Now>World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(21))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("style", factory.Markup("color:red;")),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(73))
                            },
                            factory.Markup("World")))
                };
                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World <strong class=\"foo\">inside of strong tag</strong></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(21)),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("Hello World "),
                            new MarkupTagBlock(
                                factory.Markup("<strong"),
                                new MarkupBlock(new AttributeBlockChunkGenerator(name: "class",
                                                                                prefix: new LocationTagged<string>(" class=\"", 71, 0, 71),
                                                                                suffix: new LocationTagged<string>("\"", 82, 0, 82)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("foo").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(string.Empty, 79, 0, 79),
                                                                                                 value: new LocationTagged<string>("foo", 79, 0, 79))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
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

                yield return new object[]
                {
                    "<p class=\"foo\" style=\"color:red;\"></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<TagHelperAttributeNode>
                        {
                            new TagHelperAttributeNode("class", factory.Markup("foo")),
                            new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                        }))
                };
                yield return new object[]
                {
                    "<p class=\"foo\" style=\"color:red;\">Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[]
                {
                    "<p class=\"foo\">Hello</p> <p style=\"color:red;\">World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo"))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("World")))
                };
                yield return new object[]
                {
                    "<p class=\"foo\" style=\"color:red;\">Hello World <strong class=\"foo\">inside of strong tag</strong></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo")),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"))
                            },
                            factory.Markup("Hello World "),
                            new MarkupTagBlock(
                                factory.Markup("<strong"),
                                new MarkupBlock(new AttributeBlockChunkGenerator(name: "class",
                                                                                prefix: new LocationTagged<string>(" class=\"", 53, 0, 53),
                                                                                suffix: new LocationTagged<string>("\"", 64, 0, 64)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("foo").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(string.Empty, 61, 0, 61),
                                                                                                 value: new LocationTagged<string>("foo", 61, 0, 61))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
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

                yield return new object[]
                {
                    "<p></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p"))
                };
                yield return new object[]
                {
                    "<p>Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World")))
                };
                yield return new object[]
                {
                    "<p>Hello</p> <p>World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            factory.Markup("World")))
                };
                yield return new object[]
                {
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

        public static TheoryData DataDashAttributeData_Document
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var dateTimeNowString = "@DateTime.Now";
                var dateTimeNow = new ExpressionBlock(
                    factory.CodeTransition(),
                        factory.Code("DateTime.Now")
                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                            .Accepts(AcceptedCharacters.NonWhiteSpace));

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        $"<input data-required='{dateTimeNowString}' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode(
                                        "data-required",
                                        new MarkupBlock(dateTimeNow),
                                        HtmlAttributeValueStyle.SingleQuotes),
                                }))
                    },
                    {
                        "<input data-required='value' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("data-required", factory.Markup("value"), HtmlAttributeValueStyle.SingleQuotes),
                                }))
                    },
                    {
                        $"<input data-required='prefix {dateTimeNowString}' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode(
                                        "data-required",
                                        new MarkupBlock(factory.Markup("prefix "), dateTimeNow),
                                        HtmlAttributeValueStyle.SingleQuotes),
                                }))
                    },
                    {
                        $"<input data-required='{dateTimeNowString} suffix' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode(
                                        "data-required",
                                        new MarkupBlock(dateTimeNow, factory.Markup(" suffix")),
                                        HtmlAttributeValueStyle.SingleQuotes),
                                }))
                    },
                    {
                        $"<input data-required='prefix {dateTimeNowString} suffix' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode(
                                        "data-required",
                                        new MarkupBlock(
                                            factory.Markup("prefix "),
                                            dateTimeNow,
                                            factory.Markup(" suffix")),
                                        HtmlAttributeValueStyle.SingleQuotes),
                                }))
                    },
                    {
                        $"<input pre-attribute data-required='prefix {dateTimeNowString} suffix' post-attribute />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("pre-attribute", value: null, valueStyle: HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode(
                                        "data-required",
                                        new MarkupBlock(
                                            factory.Markup("prefix "),
                                            dateTimeNow,
                                            factory.Markup(" suffix")),
                                        HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("post-attribute", value: null, valueStyle: HtmlAttributeValueStyle.Minimized),
                                }))
                    },
                    {
                        $"<input data-required='{dateTimeNowString} middle {dateTimeNowString}' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode(
                                        "data-required",
                                        new MarkupBlock(
                                            dateTimeNow,
                                            factory.Markup(" middle "),
                                            dateTimeNow),
                                        HtmlAttributeValueStyle.SingleQuotes),
                                }))
                    },
                };
            }
        }

        public static TheoryData DataDashAttributeData_CSharpBlock
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var documentData = DataDashAttributeData_Document;
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

                foreach (var data in documentData)
                {
                    data[0] = $"@{{{data[0]}}}";
                    data[1] = buildStatementBlock(() => data[1] as MarkupBlock);
                }

                return documentData;
            }
        }

        [Theory]
        [MemberData(nameof(DataDashAttributeData_Document))]
        [MemberData(nameof(DataDashAttributeData_CSharpBlock))]
        public void Rewrite_GeneratesExpectedOutputForUnboundDataDashAttributes(
            string documentContent,
            MarkupBlock expectedOutput)
        {
            // Act & Assert
            RunParseTreeRewriterTest(documentContent, expectedOutput, Enumerable.Empty<RazorError>(), "input");
        }

        public static TheoryData MinimizedAttributeData_Document
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var noErrors = new RazorError[0];
                var errorFormat = "Attribute '{0}' on tag helper element '{1}' requires a value. Tag helper bound " +
                    "attributes of type '{2}' cannot be empty or contain only whitespace.";
                var emptyKeyFormat = "The tag helper attribute '{0}' in element '{1}' is missing a key. The " +
                    "syntax is '<{1} {0}{{ key }}=\"value\">'.";
                var stringType = typeof(string).FullName;
                var intType = typeof(int).FullName;
                var expressionString = "@DateTime.Now + 1";
                var expression = new Func<int, SyntaxTreeNode>(index =>
                    new MarkupBlock(
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(
                                new LocationTagged<string>(
                                    string.Empty,
                                    new SourceLocation(index, 0, index)),
                                new SourceLocation(index, 0, index)),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                    factory.Code("DateTime.Now")
                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                        .Accepts(AcceptedCharacters.NonWhiteSpace))),
                        factory.Markup(" +")
                            .With(new LiteralAttributeChunkGenerator(
                                prefix: new LocationTagged<string>(" ", index + 13, 0, index + 13),
                                value: new LocationTagged<string>("+", index + 14, 0, index + 14))),
                        factory.Markup(" 1")
                            .With(new LiteralAttributeChunkGenerator(
                                prefix: new LocationTagged<string>(" ", index + 15, 0, index + 15),
                                value: new LocationTagged<string>("1", index + 16, 0, index + 16)))));

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<input unbound-required />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        noErrors
                    },
                    {
                        "<p bound-string></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-string", "p", stringType), 3, 0, 3, 12)
                        }
                    },
                    {
                        "<input bound-required-string />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-string", "input", stringType), 7, 0, 7, 21)
                        }
                    },
                    {
                        "<input bound-required-int />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-int", "input", intType), 7, 0, 7, 18)
                        }
                    },
                    {
                        "<p bound-int></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[] { new RazorError(string.Format(errorFormat, "bound-int", "p", intType), 3, 0, 3, 9) }
                    },
                    {
                        "<input int-dictionary/>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("int-dictionary", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "int-dictionary", "input", typeof(IDictionary<string, int>).FullName),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 14),
                        }
                    },
                    {
                        "<input string-dictionary />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("string-dictionary", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "string-dictionary", "input", typeof(IDictionary<string, string>).FullName),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 17),
                        }
                    },
                    {
                        "<input int-prefix- />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("int-prefix-", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "int-prefix-", "input", typeof(int).FullName),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 11),
                            new RazorError(
                                string.Format(emptyKeyFormat, "int-prefix-", "input"),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 11),
                        }
                    },
                    {
                        "<input string-prefix-/>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("string-prefix-", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "string-prefix-", "input", typeof(string).FullName),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 14),
                            new RazorError(
                                string.Format(emptyKeyFormat, "string-prefix-", "input"),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 14),
                        }
                    },
                    {
                        "<input int-prefix-value/>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("int-prefix-value", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "int-prefix-value", "input", typeof(int).FullName),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 16),
                        }
                    },
                    {
                        "<input string-prefix-value />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("string-prefix-value", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "string-prefix-value", "input", typeof(string).FullName),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 19),
                        }
                    },
                    {
                        "<input int-prefix-value='' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("int-prefix-value", new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "int-prefix-value", "input", typeof(int).FullName),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 16),
                        }
                    },
                    {
                        "<input string-prefix-value=''/>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("string-prefix-value", new MarkupBlock(), HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        new RazorError[0]
                    },
                    {
                        "<input int-prefix-value='3'/>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("int-prefix-value", factory.CodeMarkup("3"), HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        new RazorError[0]
                    },
                    {
                        "<input string-prefix-value='some string' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "string-prefix-value",
                                        new MarkupBlock(
                                            factory.Markup("some"),
                                            factory.Markup(" string")),
                                        HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        new RazorError[0]
                    },
                    {
                        "<input unbound-required bound-required-string />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-string", "input", stringType),
                                absoluteIndex: 24,
                                lineIndex: 0,
                                columnIndex: 24,
                                length: 21)
                        }
                    },
                    {
                        "<p bound-int bound-string></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormat, "bound-int", "p", intType), 3, 0, 3, 9),
                            new RazorError(string.Format(errorFormat, "bound-string", "p", stringType), 13, 0, 13, 12),
                        }
                    },
                    {
                        "<input bound-required-int unbound-required bound-required-string />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-int", "input", intType), 7, 0, 7, 18),
                            new RazorError(
                                string.Format(errorFormat, "bound-required-string", "input", stringType),
                                absoluteIndex: 43,
                                lineIndex: 0,
                                columnIndex: 43,
                                length: 21)
                        }
                    },
                    {
                        "<p bound-int bound-string bound-string></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormat, "bound-int", "p", intType), 3, 0, 3, 9),
                            new RazorError(string.Format(errorFormat, "bound-string", "p", stringType), 13, 0, 13, 12),
                            new RazorError(string.Format(errorFormat, "bound-string", "p", stringType), 26, 0, 26, 12),
                        }
                    },
                    {
                        "<input unbound-required class='btn' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        noErrors
                    },
                    {
                        "<p bound-string class='btn'></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-string", "p", stringType),
                                absoluteIndex: 3,
                                lineIndex: 0,
                                columnIndex: 3,
                                length: 12)
                        }
                    },
                    {
                        "<input class='btn' unbound-required />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        noErrors
                    },
                    {
                        "<p class='btn' bound-string></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-string", "p", stringType),
                                absoluteIndex: 15,
                                lineIndex: 0,
                                columnIndex: 15,
                                length: 12)
                        }
                    },
                    {
                        "<input bound-required-string class='btn' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-string", "input", stringType),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 21)
                        }
                    },
                    {
                        "<input class='btn' bound-required-string />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-string", "input", stringType),
                                absoluteIndex: 19,
                                lineIndex: 0,
                                columnIndex: 19,
                                length: 21)
                        }
                    },
                    {
                        "<input bound-required-int class='btn' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-int", "input", intType), 7, 0, 7, 18)
                        }
                    },
                    {
                        "<p bound-int class='btn'></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormat, "bound-int", "p", intType), 3, 0, 3, 9)
                        }
                    },
                    {
                        "<input class='btn' bound-required-int />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormat, "bound-required-int", "input", intType), 19, 0, 19, 18)
                        }
                    },
                    {
                        "<p class='btn' bound-int></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormat, "bound-int", "p", intType), 15, 0, 15, 9)
                        }
                    },
                    {
                        $"<input class='{expressionString}' bound-required-int />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("class", expression(14), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-int", "input", intType), 33, 0, 33, 18)
                        }
                    },
                    {
                        $"<p class='{expressionString}' bound-int></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("class", expression(10), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormat, "bound-int", "p", intType), 29, 0, 29, 9)
                        }
                    },
                    {
                        $"<input    bound-required-int class='{expressionString}'   bound-required-string " +
                        $"class='{expressionString}'  unbound-required  />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", expression(36), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", expression(86), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormat, "bound-required-int", "input", intType), 10, 0, 10, 18),
                            new RazorError(
                                string.Format(errorFormat, "bound-required-string", "input", stringType),
                                absoluteIndex: 57,
                                lineIndex: 0,
                                columnIndex: 57,
                                length: 21),
                        }
                    },
                    {
                        $"<p    bound-int class='{expressionString}'   bound-string " +
                        $"class='{expressionString}'  bound-string></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", expression(23), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("class", expression(64), HtmlAttributeValueStyle.SingleQuotes),
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormat, "bound-int", "p", intType), 6, 0, 6, 9),
                            new RazorError(string.Format(errorFormat, "bound-string", "p", stringType), 44, 0, 44, 12),
                            new RazorError(string.Format(errorFormat, "bound-string", "p", stringType), 84, 0, 84, 12),
                        }
                    },
                };
            }
        }

        public static TheoryData MinimizedAttributeData_CSharpBlock
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var documentData = MinimizedAttributeData_Document;
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
                Action<MarkupBlock> updateDynamicChunkGenerators = (block) =>
                {
                    var tagHelperBlock = block.Children.First() as MarkupTagHelperBlock;

                    for (var i = 0; i < tagHelperBlock.Attributes.Count; i++)
                    {
                        var attribute = tagHelperBlock.Attributes[i];
                        var holderBlock = attribute.Value as Block;

                        if (holderBlock == null)
                        {
                            continue;
                        }

                        var valueBlock = holderBlock.Children.FirstOrDefault() as Block;
                        if (valueBlock != null)
                        {
                            var chunkGenerator = valueBlock.ChunkGenerator as DynamicAttributeBlockChunkGenerator;

                            if (chunkGenerator != null)
                            {
                                var blockBuilder = new BlockBuilder(holderBlock);
                                var expressionBlockBuilder = new BlockBuilder(valueBlock);
                                var newChunkGenerator = new DynamicAttributeBlockChunkGenerator(
                                    new LocationTagged<string>(
                                        chunkGenerator.Prefix.Value,
                                        new SourceLocation(
                                            chunkGenerator.Prefix.Location.AbsoluteIndex + 2,
                                            chunkGenerator.Prefix.Location.LineIndex,
                                            chunkGenerator.Prefix.Location.CharacterIndex + 2)),
                                    new SourceLocation(
                                        chunkGenerator.ValueStart.AbsoluteIndex + 2,
                                        chunkGenerator.ValueStart.LineIndex,
                                        chunkGenerator.ValueStart.CharacterIndex + 2));

                                expressionBlockBuilder.ChunkGenerator = newChunkGenerator;
                                blockBuilder.Children[0] = expressionBlockBuilder.Build();

                                for (var j = 1; j < blockBuilder.Children.Count; j++)
                                {
                                    var span = blockBuilder.Children[j] as Span;
                                    if (span != null)
                                    {
                                        var literalChunkGenerator =
                                            span.ChunkGenerator as LiteralAttributeChunkGenerator;

                                        var spanBuilder = new SpanBuilder(span);
                                        spanBuilder.ChunkGenerator = new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(
                                                literalChunkGenerator.Prefix.Value,
                                                new SourceLocation(
                                                    literalChunkGenerator.Prefix.Location.AbsoluteIndex + 2,
                                                    literalChunkGenerator.Prefix.Location.LineIndex,
                                                    literalChunkGenerator.Prefix.Location.CharacterIndex + 2)),
                                            value: new LocationTagged<string>(
                                                literalChunkGenerator.Value.Value,
                                                new SourceLocation(
                                                    literalChunkGenerator.Value.Location.AbsoluteIndex + 2,
                                                    literalChunkGenerator.Value.Location.LineIndex,
                                                    literalChunkGenerator.Value.Location.CharacterIndex + 2)));

                                        blockBuilder.Children[j] = spanBuilder.Build();
                                    }
                                }

                                tagHelperBlock.Attributes[i] = new TagHelperAttributeNode(
                                    attribute.Name,
                                    blockBuilder.Build(),
                                    attribute.ValueStyle);
                            }
                        }
                    }
                };

                foreach (var data in documentData)
                {
                    data[0] = $"@{{{data[0]}}}";

                    updateDynamicChunkGenerators(data[1] as MarkupBlock);

                    data[1] = buildStatementBlock(() => data[1] as MarkupBlock);

                    var errors = data[2] as RazorError[];

                    for (var i = 0; i < errors.Length; i++)
                    {
                        var error = errors[i];
                        error.Location = SourceLocation.Advance(error.Location, "@{");
                    }
                }

                return documentData;
            }
        }

        public static TheoryData MinimizedAttributeData_PartialTags
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var noErrors = new RazorError[0];
                var errorFormatUnclosed = "Found a malformed '{0}' tag helper. Tag helpers must have a start and " +
                    "end tag or be self closing.";
                var errorFormatNoCloseAngle = "Missing close angle for tag helper '{0}'.";
                var errorFormatNoValue = "Attribute '{0}' on tag helper element '{1}' requires a value. Tag helper bound " +
                    "attributes of type '{2}' cannot be empty or contain only whitespace.";
                var stringType = typeof(string).FullName;
                var intType = typeof(int).FullName;

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<input unbound-required",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                        }
                    },
                    {
                        "<input bound-required-string",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-required-string", "input", stringType),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 21),
                        }
                    },
                    {
                        "<input bound-required-int",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-required-int", "input", intType),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 18),
                        }
                    },
                    {
                        "<input bound-required-int unbound-required bound-required-string",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-required-int", "input", intType),
                                absoluteIndex: 7,
                                lineIndex: 0,
                                columnIndex: 7,
                                length: 18),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-required-string", "input", stringType),
                                absoluteIndex: 43,
                                lineIndex: 0,
                                columnIndex: 43,
                                length: 21),
                        }
                    },
                    {
                        "<p bound-string",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-string", "p", stringType), 3, 0, 3, 12),
                        }
                    },
                    {
                        "<p bound-int",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(string.Format(errorFormatNoValue, "bound-int", "p", intType), 3, 0, 3, 9),
                        }
                    },
                    {
                        "<p bound-int bound-string",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                })),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "p"),
                                new SourceLocation(1, 0, 1),
                                length: 1),
                            new RazorError(string.Format(errorFormatNoValue, "bound-int", "p", intType), 3, 0, 3, 9),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-string", "p", stringType), 13, 0, 13, 12),
                        }
                    },
                    {
                        "<input bound-required-int unbound-required bound-required-string<p bound-int bound-string",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("bound-required-int", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("unbound-required", null, HtmlAttributeValueStyle.Minimized),
                                    new TagHelperAttributeNode("bound-required-string", null, HtmlAttributeValueStyle.Minimized),
                                },
                                children: new MarkupTagHelperBlock(
                                    "p",
                                    TagMode.StartTagAndEndTag,
                                    attributes: new List<TagHelperAttributeNode>()
                                    {
                                        new TagHelperAttributeNode("bound-int", null, HtmlAttributeValueStyle.Minimized),
                                        new TagHelperAttributeNode("bound-string", null, HtmlAttributeValueStyle.Minimized),
                                    }))),
                        new[]
                        {
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "input"),
                                new SourceLocation(1, 0, 1),
                                length: 5),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-required-int", "input", intType), 7, 0, 7, 18),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-required-string", "input", stringType),
                                absoluteIndex: 43,
                                lineIndex: 0,
                                columnIndex: 43,
                                length: 21),
                            new RazorError(
                                string.Format(errorFormatNoCloseAngle, "p"),
                                new SourceLocation(65, 0, 65),
                                length: 1),
                            new RazorError(
                                string.Format(errorFormatUnclosed, "p"),
                                new SourceLocation(65, 0, 65),
                                length: 1),
                            new RazorError(string.Format(errorFormatNoValue, "bound-int", "p", intType), 67, 0, 67, 9),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-string", "p", stringType),
                                absoluteIndex: 77,
                                lineIndex: 0,
                                columnIndex: 77,
                                length: 12),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MinimizedAttributeData_Document))]
        [MemberData(nameof(MinimizedAttributeData_CSharpBlock))]
        [MemberData(nameof(MinimizedAttributeData_PartialTags))]
        public void Rewrite_UnderstandsMinimizedAttributes(
            string documentContent,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper1",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound-required-string",
                                PropertyName = "BoundRequiredString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "unbound-required" }
                        }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper1",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound-required-string",
                                PropertyName = "BoundRequiredString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "bound-required-string" }
                        }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper2",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound-required-int",
                                PropertyName = "BoundRequiredInt",
                                TypeName = typeof(int).FullName
                            }
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "bound-required-int" }
                        }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper3",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-dictionary",
                                PropertyName ="DictionaryOfIntProperty",
                                TypeName = typeof(IDictionary<string, int>).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-dictionary",
                                PropertyName = "DictionaryOfStringProperty",
                                TypeName = typeof(IDictionary<string, string>).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-prefix-",
                                PropertyName = "DictionaryOfIntProperty",
                                TypeName = typeof(int).FullName,
                                IsIndexer = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-prefix-",
                                PropertyName = "DictionaryOfStringProperty",
                                TypeName = typeof(string).FullName,
                                IsIndexer = true,
                                IsStringProperty = true
                            }
                        }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "PTagHelper",
                        AssemblyName = "SomeAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound-string",
                                PropertyName = "BoundRequiredString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound-int",
                                PropertyName = "BoundRequiredString",
                                TypeName = typeof(int).FullName
                            }
                        }
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors);
        }
    }
}
