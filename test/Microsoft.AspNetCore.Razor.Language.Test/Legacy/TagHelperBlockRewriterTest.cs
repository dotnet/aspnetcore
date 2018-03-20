// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TagHelperBlockRewriterTest : TagHelperRewritingTestBase
    {
        public static TheoryData SymbolBoundAttributeData
        {
            get
            {
                var factory = new SpanFactory();

                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<ul bound [item]='items'></ul>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("ul",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode(
                                        "[item]",
                                        factory.CodeMarkup("items").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes)
                                }))
                    },
                    {
                        "<ul bound [(item)]='items'></ul>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("ul",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode(
                                        "[(item)]",
                                        factory.CodeMarkup("items").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes)
                                }))
                    },
                    {
                        "<button bound (click)='doSomething()'>Click Me</button>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("button",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode(
                                        "(click)",
                                        factory.CodeMarkup("doSomething()").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes)
                                },
                                children: factory.Markup("Click Me")))
                    },
                    {
                        "<button bound (^click)='doSomething()'>Click Me</button>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("button",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode(
                                        "(^click)",
                                        factory.CodeMarkup("doSomething()").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes)
                                },
                                children: factory.Markup("Click Me")))
                    },
                    {
                        "<template bound *something='value'></template>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("template",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode(
                                        "*something",
                                        factory.Markup("value"),
                                        AttributeStructure.SingleQuotes)
                                }))
                    },
                    {
                        "<div bound #localminimized></div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("#localminimized", null, AttributeStructure.Minimized)
                                }))
                    },
                    {
                        "<div bound #local='value'></div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("#local", factory.Markup("value"), AttributeStructure.SingleQuotes)
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SymbolBoundAttributeData))]
        public void Rewrite_CanHandleSymbolBoundAttributes(string documentContent, object expectedOutput)
        {
            // Arrange
            var descriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("*")
                        .RequireAttributeDescriptor(attribute => attribute.Name("bound")))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("[item]")
                        .PropertyName("ListItems")
                        .TypeName(typeof(List<string>).Namespace + "List<System.String>"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("[(item)]")
                        .PropertyName("ArrayItems")
                        .TypeName(typeof(string[]).Namespace + "System.String[]"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("(click)")
                        .PropertyName("Event1")
                        .TypeName(typeof(Action).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("(^click)")
                        .PropertyName("Event2")
                        .TypeName(typeof(Action).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("*something")
                        .PropertyName("StringProperty1")
                        .TypeName(typeof(string).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("#local")
                        .PropertyName("StringProperty2")
                        .TypeName(typeof(string).FullName))
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        public static TheoryData WithoutEndTagElementData
        {
            get
            {
                var factory = new SpanFactory();
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
                                    new TagHelperAttributeNode("type", factory.Markup("text"), AttributeStructure.SingleQuotes)
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
                                    new TagHelperAttributeNode("type", factory.Markup("text"), AttributeStructure.SingleQuotes)
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
        public void Rewrite_CanHandleWithoutEndTagTagStructure(string documentContent, object expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(TagStructure.WithoutEndTag))
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        public static TheoryData TagStructureCompatibilityData
        {
            get
            {
                var factory = new SpanFactory();
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
                                    new TagHelperAttributeNode("type", factory.Markup("text"), AttributeStructure.SingleQuotes)
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
                                    new TagHelperAttributeNode("type", factory.Markup("text"), AttributeStructure.SingleQuotes)
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
            object expectedOutput)
        {
            // Arrange
            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper1", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(structure1))
                    .Build(),
                TagHelperDescriptorBuilder.Create("InputTagHelper2", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(structure2))
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        public static TheoryData MalformedTagHelperAttributeBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
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

                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
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
                                        AttributeStructure.SingleQuotes)
                                })),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
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
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
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
                                        AttributeStructure.SingleQuotes)
                                })),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
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
                                        AttributeStructure.SingleQuotes)
                                })),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
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
                                        AttributeStructure.NoQuotes)
                                })),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperAttributeListMustBeWellFormed(
                                new SourceSpan(12, 0, 12, 1))
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
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
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
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p foo bar<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("foo", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bar", null, AttributeStructure.Minimized)
                                },
                                new MarkupTagHelperBlock("strong"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(11, 0, 11), contentLength: 6), "strong"),
                        }
                    },
                    {
                        "<p class=btn\" bar<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.NoQuotes)
                                })),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p class=btn\" bar=\"foo\"<strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.NoQuotes)
                                })),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
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
                                    new TagHelperAttributeNode("foo", null, AttributeStructure.Minimized)
                                },
                                new MarkupTagHelperBlock("strong"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(24, 0, 24), contentLength: 6), "strong")
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
                                    new TagHelperAttributeNode("foo", null, AttributeStructure.Minimized),
                                })),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<p @DateTime.Now class=\"btn\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(new SourceSpan(3, 0, 3, 13), "p")
                        }
                    },
                    {
                        "<p @DateTime.Now=\"btn\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(new SourceSpan(3, 0, 3, 13), "p")
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
                                                            .Accepts(AcceptedCharactersInternal.NonWhiteSpace)))),
                                        AttributeStructure.DoubleQuotes)
                                })),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
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
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(11, 0, 11), contentLength: 1), "do", "}", "{"),
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
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(11, 0, 11), contentLength: 1), "do", "}", "{"),
                            RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                                new SourceSpan(filePath: null, absoluteIndex: 15, lineIndex: 0, characterIndex: 15, length: 1))
                        }
                    },
                    {
                        "<p @do { someattribute=\"btn\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(new SourceSpan(3, 0, 3, 30), "p"),
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(4, 0, 4), contentLength: 1), "do", "}", "{"),
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                        new SourceSpan(filePath: null, absoluteIndex: 31, lineIndex: 0, characterIndex: 31, length: 1), "p")
                        }
                    },
                    {
                        "<p class=some=thing attr=\"@value\"></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("some"), AttributeStructure.NoQuotes)
                                })),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperAttributeListMustBeWellFormed(new SourceSpan(13, 0, 13, 13))
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MalformedTagHelperAttributeBlockData))]
        public void Rewrite_CreatesErrorForMalformedTagHelpersWithAttributes(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors, "strong", "p");
        }

        public static TheoryData MalformedTagHelperBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p></p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p")),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(absoluteIndex: 5, lineIndex: 0, characterIndex: 5, length: 1), "p")
                        }
                    },
                    {
                        "<p><strong",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(4, 0, 4), contentLength: 6), "strong"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(4, 0, 4), contentLength: 6), "strong")
                        }
                    },
                    {
                        "<strong <p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("strong",
                                new MarkupTagHelperBlock("p"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 6), "strong"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 6), "strong"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(9, 0, 9), contentLength: 1), "p")
                        }
                    },
                    {
                        "<strong </strong",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("strong")),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 6), "strong"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(10, 0, 10), contentLength: 6), "strong")
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
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(4, 0, 4), contentLength: 6), "strong"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(14, 0, 14), contentLength: 1), "p")
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
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(3, 0, 3), contentLength: 6), "strong")
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
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(14, 0, 14), contentLength: 1), "p")
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MalformedTagHelperBlockData))]
        public void Rewrite_CreatesErrorForMalformedTagHelper(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors, "strong", "p");
        }

        public static TheoryData CodeTagHelperAttributesData
        {
            get
            {
                var factory = new SpanFactory();
                var dateTimeNow = new MarkupBlock(
                    factory.Markup(" "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                            factory.Code("DateTime.Now")
                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharactersInternal.NonWhiteSpace)));

                return new TheoryData<string, Block>
                {
                    {
                        "<person age=\"12\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("age", factory.CodeMarkup("12").With(new ExpressionChunkGenerator()))
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
                                        factory.CodeMarkup("DateTime.Now").With(new ExpressionChunkGenerator()))
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
                                                        .CSharpCodeMarkup("DateTime.Now.Year")
                                                        .With(new ExpressionChunkGenerator())))))
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
                                                factory.CodeMarkup(" ").With(new ExpressionChunkGenerator()),
                                                new ExpressionBlock(
                                                    factory.CSharpCodeMarkup("@").With(new ExpressionChunkGenerator()),
                                                    factory
                                                        .CSharpCodeMarkup("DateTime.Now.Year")
                                                        .With(new ExpressionChunkGenerator())))))
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
                                            factory.CodeMarkup("1").With(new ExpressionChunkGenerator()),
                                            factory.CodeMarkup(" +").With(new ExpressionChunkGenerator()),
                                            new MarkupBlock(
                                                factory.CodeMarkup(" ").With(new ExpressionChunkGenerator()),
                                                new ExpressionBlock(
                                                    factory.CSharpCodeMarkup("@").With(new ExpressionChunkGenerator()),
                                                    factory.CSharpCodeMarkup("value")
                                                        .With(new ExpressionChunkGenerator()))),
                                            factory.CodeMarkup(" +").With(new ExpressionChunkGenerator()),
                                            factory.CodeMarkup(" 2").With(new ExpressionChunkGenerator()))),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        new MarkupBlock(
                                            factory.CodeMarkup("(bool)").With(new ExpressionChunkGenerator()),
                                            new MarkupBlock(
                                                new ExpressionBlock(
                                                    factory.CSharpCodeMarkup("@").With(new ExpressionChunkGenerator()),
                                                    factory
                                                        .CSharpCodeMarkup("Bag[\"val\"]")
                                                        .With(new ExpressionChunkGenerator()))),
                                            factory.CodeMarkup(" ?").With(new ExpressionChunkGenerator()),
                                            new MarkupBlock(
                                                factory.CodeMarkup(" @").With(new ExpressionChunkGenerator())
                                                    .As(SpanKindInternal.Code),
                                                factory.CodeMarkup("@").With(SpanChunkGenerator.Null)
                                                    .As(SpanKindInternal.Code)),
                                            factory.CodeMarkup("DateTime").With(new ExpressionChunkGenerator()),
                                            factory.CodeMarkup(" :").With(new ExpressionChunkGenerator()),
                                            new MarkupBlock(
                                                factory.CodeMarkup(" ").With(new ExpressionChunkGenerator()),
                                                new ExpressionBlock(
                                                    factory.CSharpCodeMarkup("@").With(new ExpressionChunkGenerator()),
                                                    factory
                                                        .CSharpCodeMarkup("DateTime.Now")
                                                        .With(new ExpressionChunkGenerator())))),
                                        AttributeStructure.SingleQuotes)
                                }))
                    },
                    {
                        "<person age=\"12\" birthday=\"DateTime.Now\" name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("age", factory.CodeMarkup("12").With(new ExpressionChunkGenerator())),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now").With(new ExpressionChunkGenerator())),
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
                                    new TagHelperAttributeNode("age", factory.CodeMarkup("12").With(new ExpressionChunkGenerator())),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now").With(new ExpressionChunkGenerator())),
                                    new TagHelperAttributeNode(
                                        "name",
                                        new MarkupBlock(
                                            factory.Markup("Time:"),
                                             new MarkupBlock(
                                                factory.Markup(" @").Accepts(AcceptedCharactersInternal.None),
                                                factory.Markup("@")
                                                    .With(SpanChunkGenerator.Null)
                                                    .Accepts(AcceptedCharactersInternal.None)),
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
                                    new TagHelperAttributeNode("age", factory.CodeMarkup("12").With(new ExpressionChunkGenerator())),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now").With(new ExpressionChunkGenerator())),
                                    new TagHelperAttributeNode(
                                        "name",
                                        new MarkupBlock(
                                             new MarkupBlock(
                                                factory.Markup("@").Accepts(AcceptedCharactersInternal.None),
                                                factory.Markup("@")
                                                    .With(SpanChunkGenerator.Null)
                                                    .Accepts(AcceptedCharactersInternal.None)),
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
                                                factory.CodeMarkup("@").With(new ExpressionChunkGenerator()),
                                                factory.CodeMarkup("@").With(SpanChunkGenerator.Null)),
                                            new MarkupBlock(
                                                factory.EmptyHtml()
                                                    .AsCodeMarkup().With(new ExpressionChunkGenerator())
                                                    .As(SpanKindInternal.Code),
                                                new ExpressionBlock(
                                                    factory.CSharpCodeMarkup("@").With(new ExpressionChunkGenerator()),
                                                    factory.CSharpCodeMarkup("(").With(new ExpressionChunkGenerator()),
                                                    factory.CSharpCodeMarkup("11+1")
                                                        .With(new ExpressionChunkGenerator()),
                                                    factory.CSharpCodeMarkup(")").With(new ExpressionChunkGenerator()))))),
                                    new TagHelperAttributeNode(
                                        "birthday",
                                        factory.CodeMarkup("DateTime.Now").With(new ExpressionChunkGenerator())),
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
        public void Rewrite_CreatesMarkupCodeSpansForNonStringTagHelperAttributes(
            string documentContent,
            object expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PersonTagHelper", "personAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("person"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("age")
                        .PropertyName("Age")
                        .TypeName(typeof(int).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("birthday")
                        .PropertyName("BirthDay")
                        .TypeName(typeof(DateTime).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("name")
                        .PropertyName("Name")
                        .TypeName(typeof(string).FullName))
                    .Build()
            };

            // Act & Assert
            EvaluateData(
                descriptors,
                documentContent,
                (MarkupBlock)expectedOutput,
                expectedErrors: Enumerable.Empty<RazorDiagnostic>());
        }

        public static IEnumerable<object[]> IncompleteHelperBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;><strong></p></strong>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo"), AttributeStructure.NoQuotes),
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
                                                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace)))),
                                    AttributeStructure.DoubleQuotes),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"), AttributeStructure.NoQuotes)
                            },
                            new MarkupTagHelperBlock("strong")),
                            blockFactory.MarkupTagBlock("</strong>")),
                    new RazorDiagnostic[]
                    {
                        RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                            new SourceSpan(absoluteIndex: 53, lineIndex: 0, characterIndex: 53, length: 6), "strong"),
                        RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                            new SourceSpan(absoluteIndex: 66, lineIndex: 0, characterIndex: 66, length: 6), "strong")
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
                    new RazorDiagnostic[]
                    {
                        RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                            new SourceSpan(absoluteIndex: 6, lineIndex: 0, characterIndex: 6, length: 1), "p")
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
                    new RazorDiagnostic[]
                    {
                        RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                            new SourceSpan(absoluteIndex: 6, lineIndex: 0, characterIndex: 6, length: 1), "p"),
                        RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                            new SourceSpan(absoluteIndex: 15, lineIndex: 0, characterIndex: 15, length: 6), "strong")
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
                    new RazorDiagnostic[]
                    {
                        RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                            new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(IncompleteHelperBlockData))]
        public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors, "strong", "p");
        }


        public static IEnumerable<object[]> OddlySpacedBlockData
        {
            get
            {
                var factory = new SpanFactory();

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
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p");
        }

        public static IEnumerable<object[]> ComplexAttributeTagHelperBlockData
        {
            get
            {
                var factory = new SpanFactory();
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
                                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace)))));
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
                                factory.Code("do { var foo = bar; ").AsStatement(),
                                new MarkupBlock(
                                    new MarkupTagBlock(
                                        factory.MarkupTransition("<text>")),
                                    factory.Markup("Foo").Accepts(AcceptedCharactersInternal.None),
                                    new MarkupTagBlock(
                                        factory.MarkupTransition("</text>"))),
                                factory
                                    .Code(" foo++; } while (foo<bar>);")
                                    .AsStatement()
                                    .Accepts(AcceptedCharactersInternal.None)))));

                var currentFormattedString = "<p class=\"{0}\" style='{0}'></p>";
                yield return new object[]
                {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<TagHelperAttributeNode>
                        {
                            new TagHelperAttributeNode("class", dateTimeNow(10)),
                            new TagHelperAttributeNode("style", dateTimeNow(32), AttributeStructure.SingleQuotes)
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
                            new TagHelperAttributeNode("style", doWhile(83), AttributeStructure.SingleQuotes)
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
                                new TagHelperAttributeNode("style", dateTimeNow(32), AttributeStructure.SingleQuotes)
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
                                new TagHelperAttributeNode("style", doWhile(83), AttributeStructure.SingleQuotes)
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
                                new TagHelperAttributeNode("style", dateTimeNow(45), AttributeStructure.SingleQuotes)
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
                                new TagHelperAttributeNode("style", doWhile(96), AttributeStructure.SingleQuotes)
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
                                new TagHelperAttributeNode("style", dateTimeNow(32), AttributeStructure.SingleQuotes)
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
                                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
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
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p");
        }

        public static IEnumerable<object[]> ComplexTagHelperBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNowString = "@DateTime.Now";
                var dateTimeNow = new ExpressionBlock(
                                        factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                .Accepts(AcceptedCharactersInternal.NonWhiteSpace));
                var doWhileString = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";
                var doWhile = new StatementBlock(
                               factory.CodeTransition(),
                               factory.Code("do { var foo = bar;").AsStatement(),
                               new MarkupBlock(
                                    factory.Markup(" "),
                                    new MarkupTagHelperBlock("p",
                                        factory.Markup("Foo")),
                                    factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                               factory.Code("foo++; } while (foo<bar>);")
                                .AsStatement()
                                .Accepts(AcceptedCharactersInternal.None));

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
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p");
        }


        public static TheoryData InvalidHtmlBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNow = new ExpressionBlock(
                    factory.CodeTransition(),
                        factory.Code("DateTime.Now")
                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                            .Accepts(AcceptedCharactersInternal.NonWhiteSpace));

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
        public void TagHelperParseTreeRewriter_AllowsInvalidHtml(string documentContent, object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p");
        }

        public static TheoryData EmptyAttributeTagHelperData
        {
            get
            {
                var factory = new SpanFactory();

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
                                new TagHelperAttributeNode("class",  new MarkupBlock(), AttributeStructure.SingleQuotes)
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
                                    AttributeStructure.DoubleQuotes),
                            }))
                    },
                    {
                        "<p class1='' class2= class3=\"\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class1", new MarkupBlock(), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "class2",
                                        factory.Markup(string.Empty).With(SpanChunkGenerator.Null),
                                        AttributeStructure.DoubleQuotes),
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
                                    new TagHelperAttributeNode("class1",  new MarkupBlock(), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("class2",  new MarkupBlock()),
                                    new TagHelperAttributeNode(
                                        "class3",
                                        factory.Markup(string.Empty).With(SpanChunkGenerator.Null),
                                        AttributeStructure.DoubleQuotes),
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EmptyAttributeTagHelperData))]
        public void Rewrite_UnderstandsEmptyAttributeTagHelpers(string documentContent, object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, new RazorDiagnostic[0], "p");
        }

        public static TheoryData EmptyTagHelperBoundAttributeData
        {
            get
            {
                var factory = new SpanFactory();
                var boolTypeName = typeof(bool).FullName;

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<myth bound='' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("bound", new MarkupBlock(), AttributeStructure.SingleQuotes)
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "bound",
                                "myth",
                                boolTypeName)
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
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup("    true").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes)
                                })),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<myth bound='    ' />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "myth",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup("    ").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes)
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "bound",
                                "myth",
                                boolTypeName),
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
                                    new TagHelperAttributeNode("bound", new MarkupBlock(), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound", new MarkupBlock())
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "bound",
                                "myth",
                                boolTypeName),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(16, 0, 16, 5),
                                "bound",
                                "myth",
                                boolTypeName),
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
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup(" ").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup("  "))
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "bound",
                                "myth",
                                boolTypeName),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(17, 0, 17, 5),
                                "bound",
                                "myth",
                                boolTypeName),
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
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup("true").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup(string.Empty).With(SpanChunkGenerator.Null),
                                        AttributeStructure.DoubleQuotes)
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(19, 0, 19, 5),
                                "bound",
                                "myth",
                                boolTypeName),
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
                                        AttributeStructure.DoubleQuotes),
                                    new TagHelperAttributeNode("name", new MarkupBlock(), AttributeStructure.SingleQuotes)
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "bound",
                                "myth",
                                boolTypeName),
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
                                        AttributeStructure.DoubleQuotes),
                                    new TagHelperAttributeNode("name", factory.Markup("  "), AttributeStructure.SingleQuotes)
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "bound",
                                "myth",
                                boolTypeName),
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
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup("true").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("name", factory.Markup("john"), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "bound",
                                        factory.CodeMarkup(string.Empty).With(SpanChunkGenerator.Null),
                                        AttributeStructure.DoubleQuotes),
                                    new TagHelperAttributeNode(
                                        "name",
                                        factory.Markup(string.Empty).With(SpanChunkGenerator.Null),
                                        AttributeStructure.DoubleQuotes)
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(31, 0, 31, 5),
                                "bound",
                                "myth",
                                boolTypeName),
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
                                    new TagHelperAttributeNode("BouND", new MarkupBlock(), AttributeStructure.SingleQuotes)
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "BouND",
                                "myth",
                                boolTypeName),
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
                                    new TagHelperAttributeNode("BOUND", new MarkupBlock(), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bOUnd", new MarkupBlock())
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "BOUND",
                                "myth",
                                boolTypeName),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(18, 0, 18, 5),
                                "bOUnd",
                                "myth",
                                boolTypeName),
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
                                        AttributeStructure.DoubleQuotes),
                                    new TagHelperAttributeNode("nAMe", factory.Markup("john"), AttributeStructure.SingleQuotes)
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 5),
                                "BOUND",
                                "myth",
                                boolTypeName),
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
                                                factory.CodeMarkup("    ").With(new ExpressionChunkGenerator()),
                                                new ExpressionBlock(
                                                    factory.CSharpCodeMarkup("@").With(new ExpressionChunkGenerator()),
                                                    factory.CSharpCodeMarkup("true")
                                                        .With(new ExpressionChunkGenerator()))),
                                                factory.CodeMarkup("  ").With(new ExpressionChunkGenerator())),
                                            AttributeStructure.SingleQuotes)
                                    }
                                })),
                        new RazorDiagnostic[0]
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
                                                factory.CodeMarkup("    ").With(new ExpressionChunkGenerator()),
                                                new ExpressionBlock(
                                                    factory.CSharpCodeMarkup("@").With(new ExpressionChunkGenerator()),
                                                    factory.CSharpCodeMarkup("(").With(new ExpressionChunkGenerator()),
                                                    factory.CSharpCodeMarkup("true")
                                                        .With(new ExpressionChunkGenerator()),
                                                    factory.CSharpCodeMarkup(")").With(new ExpressionChunkGenerator()))),
                                                factory.CodeMarkup("  ").With(new ExpressionChunkGenerator())),
                                            AttributeStructure.SingleQuotes)
                                    }
                                })),
                        new RazorDiagnostic[0]
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EmptyTagHelperBoundAttributeData))]
        public void Rewrite_CreatesErrorForEmptyTagHelperBoundAttributes(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("mythTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("myth"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("bound")
                        .PropertyName("Bound")
                        .TypeName(typeof(bool).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("name")
                        .PropertyName("Name")
                        .TypeName(typeof(string).FullName))
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors);
        }

        public static IEnumerable<object[]> ScriptBlockData
        {
            get
            {
                var factory = new SpanFactory();

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
                                                factory.Markup("@").Accepts(AcceptedCharactersInternal.None),
                                                factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
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
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p", "div", "script");
        }

        public static IEnumerable<object[]> SelfClosingBlockData
        {
            get
            {
                var factory = new SpanFactory();

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
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p");
        }

        public static IEnumerable<object[]> QuotelessAttributeBlockData
        {
            get
            {
                var factory = new SpanFactory();
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
                                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace)))));

                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new List<TagHelperAttributeNode>
                        {
                            new TagHelperAttributeNode("class", factory.Markup("foo"), AttributeStructure.NoQuotes),
                            new TagHelperAttributeNode("dynamic", dateTimeNow(21)),
                            new TagHelperAttributeNode("style", factory.Markup("color:red;"), AttributeStructure.NoQuotes)
                        }))
                };
                yield return new object[]
                {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("class", factory.Markup("foo"), AttributeStructure.NoQuotes),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(21)),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"), AttributeStructure.NoQuotes)
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
                                new TagHelperAttributeNode("class", factory.Markup("foo"), AttributeStructure.NoQuotes),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(21)),
                                new TagHelperAttributeNode(
                                    "style",
                                    new MarkupBlock(
                                        factory.Markup("color"),
                                         new MarkupBlock(
                                            factory.Markup("@").Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup(":red;")),
                                    AttributeStructure.DoubleQuotes)
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
                                new TagHelperAttributeNode("class", factory.Markup("foo"), AttributeStructure.NoQuotes),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(21))
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new List<TagHelperAttributeNode>
                            {
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"), AttributeStructure.NoQuotes),
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
                                new TagHelperAttributeNode("class", factory.Markup("foo"), AttributeStructure.NoQuotes),
                                new TagHelperAttributeNode("dynamic", dateTimeNow(21)),
                                new TagHelperAttributeNode("style", factory.Markup("color:red;"), AttributeStructure.NoQuotes)
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
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p");
        }

        public static IEnumerable<object[]> PlainAttributeBlockData
        {
            get
            {
                var factory = new SpanFactory();
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
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p");
        }

        public static IEnumerable<object[]> PlainBlockData
        {
            get
            {
                var factory = new SpanFactory();
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
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p");
        }

        public static TheoryData DataDashAttributeData_Document
        {
            get
            {
                var factory = new SpanFactory();
                var dateTimeNowString = "@DateTime.Now";
                var dateTimeNow = new ExpressionBlock(
                    factory.CodeTransition(),
                        factory.Code("DateTime.Now")
                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                            .Accepts(AcceptedCharactersInternal.NonWhiteSpace));

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
                                        AttributeStructure.SingleQuotes),
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
                                    new TagHelperAttributeNode("data-required", factory.Markup("value"), AttributeStructure.SingleQuotes),
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
                                        AttributeStructure.SingleQuotes),
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
                                        AttributeStructure.SingleQuotes),
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
                                        AttributeStructure.SingleQuotes),
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
                                    new TagHelperAttributeNode("pre-attribute", value: null, attributeStructure: AttributeStructure.Minimized),
                                    new TagHelperAttributeNode(
                                        "data-required",
                                        new MarkupBlock(
                                            factory.Markup("prefix "),
                                            dateTimeNow,
                                            factory.Markup(" suffix")),
                                        AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("post-attribute", value: null, attributeStructure: AttributeStructure.Minimized),
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
                                        AttributeStructure.SingleQuotes),
                                }))
                    },
                };
            }
        }

        public static TheoryData DataDashAttributeData_CSharpBlock
        {
            get
            {
                var factory = new SpanFactory();
                var documentData = DataDashAttributeData_Document;
                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
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
            object expectedOutput)
        {
            // Act & Assert
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, Enumerable.Empty<RazorDiagnostic>(), "input");
        }

        public static TheoryData MinimizedAttributeData_Document
        {
            get
            {
                var factory = new SpanFactory();
                var noErrors = new RazorDiagnostic[0];
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
                                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                        factory.Markup(" +")
                            .With(new LiteralAttributeChunkGenerator(
                                prefix: new LocationTagged<string>(" ", index + 13, 0, index + 13),
                                value: new LocationTagged<string>("+", index + 14, 0, index + 14))),
                        factory.Markup(" 1")
                            .With(new LiteralAttributeChunkGenerator(
                                prefix: new LocationTagged<string>(" ", index + 15, 0, index + 15),
                                value: new LocationTagged<string>("1", index + 16, 0, index + 16)))));

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<input unbound-required />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
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
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 12),
                                "bound-string",
                                "p",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 21),
                                "bound-required-string",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 18),
                                "bound-required-int",
                                "input",
                                intType),
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
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 9),
                                "bound-int",
                                "p",
                                intType),
                        }
                    },
                    {
                        "<input int-dictionary/>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("int-dictionary", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 14),
                                "int-dictionary",
                                "input",
                                typeof(IDictionary<string, int>).Namespace + ".IDictionary<System.String, System.Int32>"),
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
                                    new TagHelperAttributeNode("string-dictionary", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 17),
                                "string-dictionary",
                                "input",
                                typeof(IDictionary<string, string>).Namespace + ".IDictionary<System.String, System.String>"),
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
                                    new TagHelperAttributeNode("int-prefix-", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 11),
                                "int-prefix-",
                                "input",
                                intType),
                            RazorDiagnosticFactory.CreateParsing_TagHelperIndexerAttributeNameMustIncludeKey(
                                new SourceSpan(7, 0, 7, 11),
                                "int-prefix-",
                                "input"),
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
                                    new TagHelperAttributeNode("string-prefix-", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 14),
                                "string-prefix-",
                                "input",
                                stringType),
                            RazorDiagnosticFactory.CreateParsing_TagHelperIndexerAttributeNameMustIncludeKey(
                                new SourceSpan(7, 0, 7, 14),
                                "string-prefix-",
                                "input"),
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
                                    new TagHelperAttributeNode("int-prefix-value", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 16),
                                "int-prefix-value",
                                "input",
                                intType),
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
                                    new TagHelperAttributeNode("string-prefix-value", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 19),
                                "string-prefix-value",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("int-prefix-value", new MarkupBlock(), AttributeStructure.SingleQuotes),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 16),
                                "int-prefix-value",
                                "input",
                                intType),
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
                                    new TagHelperAttributeNode("string-prefix-value", new MarkupBlock(), AttributeStructure.SingleQuotes),
                                })),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<input int-prefix-value='3'/>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "int-prefix-value",
                                        factory.CodeMarkup("3").With(new ExpressionChunkGenerator()),
                                        AttributeStructure.SingleQuotes),
                                })),
                        new RazorDiagnostic[0]
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
                                        AttributeStructure.SingleQuotes),
                                })),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<input unbound-required bound-required-string />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(24, 0, 24, 21),
                                "bound-required-string",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 9),
                                "bound-int",
                                "p",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(13, 0, 13, 12),
                                "bound-string",
                                "p",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 18),
                                "bound-required-int",
                                "input",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(43, 0, 43, 21),
                                "bound-required-string",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 9),
                                "bound-int",
                                "p",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(13, 0, 13, 12),
                                "bound-string",
                                "p",
                                stringType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(26, 0, 26, 12),
                                "bound-string",
                                "p",
                                stringType),
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
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
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
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 12),
                                "bound-string",
                                "p",
                                stringType),
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
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
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
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(15, 0, 15, 12),
                                "bound-string",
                                "p",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 21),
                                "bound-required-string",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(19, 0, 19, 21),
                                "bound-required-string",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 18),
                                "bound-required-int",
                                "input",
                                intType),
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
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 9),
                                "bound-int",
                                "p",
                                intType),
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
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(19, 0, 19, 18),
                                "bound-required-int",
                                "input",
                                intType),
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
                                    new TagHelperAttributeNode("class", factory.Markup("btn"), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(15, 0, 15, 9),
                                "bound-int",
                                "p",
                                intType),
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
                                    new TagHelperAttributeNode("class", expression(14), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(33, 0, 33, 18),
                                "bound-required-int",
                                "input",
                                intType),
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
                                    new TagHelperAttributeNode("class", expression(10), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(29, 0, 29, 9),
                                "bound-int",
                                "p",
                                intType),
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
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", expression(36), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", expression(86), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(10, 0, 10, 18),
                                "bound-required-int",
                                "input",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(57, 0, 57, 21),
                                "bound-required-string",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", expression(23), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("class", expression(64), AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(6, 0, 6, 9),
                                "bound-int",
                                "p",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(44, 0, 44, 12),
                                "bound-string",
                                "p",
                                stringType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(84, 0, 84, 12),
                                "bound-string",
                                "p",
                                stringType),
                        }
                    },
                };
            }
        }

        public static TheoryData MinimizedAttributeData_CSharpBlock
        {
            get
            {
                var factory = new SpanFactory();
                var documentData = MinimizedAttributeData_Document;
                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
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
                                    attribute.AttributeStructure);
                            }
                        }
                    }
                };

                foreach (var data in documentData)
                {
                    data[0] = $"@{{{data[0]}}}";

                    updateDynamicChunkGenerators(data[1] as MarkupBlock);

                    data[1] = buildStatementBlock(() => data[1] as MarkupBlock);

                    var errors = data[2] as RazorDiagnostic[];

                    for (var i = 0; i < errors.Length; i++)
                    {
                        var error = errors[i] as DefaultRazorDiagnostic;
                        var currentErrorLocation = new SourceLocation(error.Span.AbsoluteIndex, error.Span.LineIndex, error.Span.CharacterIndex);
                        var newErrorLocation = SourceLocationTracker.Advance(currentErrorLocation, "@{");
                        var copiedDiagnostic = new DefaultRazorDiagnostic(error.Descriptor, new SourceSpan(newErrorLocation, error.Span.Length), error.Args);
                        errors[i] = copiedDiagnostic;
                    }
                }

                return documentData;
            }
        }

        public static TheoryData MinimizedAttributeData_PartialTags
        {
            get
            {
                var factory = new SpanFactory();
                var noErrors = new RazorDiagnostic[0];
                var stringType = typeof(string).FullName;
                var intType = typeof(int).FullName;

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<input unbound-required",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                TagMode.StartTagAndEndTag,
                                attributes: new List<TagHelperAttributeNode>()
                                {
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
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
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 21),
                                "bound-required-string",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 18),
                                "bound-required-int",
                                "input",
                                intType),
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
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 18),
                                "bound-required-int",
                                "input",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(43, 0, 43, 21),
                                "bound-required-string",
                                "input",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 12),
                                "bound-string",
                                "p",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 9),
                                "bound-int",
                                "p",
                                intType),
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
                                    new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(3, 0, 3, 9),
                                "bound-int",
                                "p",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(13, 0, 13, 12),
                                "bound-string",
                                "p",
                                stringType),
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
                                    new TagHelperAttributeNode("bound-required-int", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("unbound-required", null, AttributeStructure.Minimized),
                                    new TagHelperAttributeNode("bound-required-string", null, AttributeStructure.Minimized),
                                },
                                children: new MarkupTagHelperBlock(
                                    "p",
                                    TagMode.StartTagAndEndTag,
                                    attributes: new List<TagHelperAttributeNode>()
                                    {
                                        new TagHelperAttributeNode("bound-int", null, AttributeStructure.Minimized),
                                        new TagHelperAttributeNode("bound-string", null, AttributeStructure.Minimized),
                                    }))),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 5), "input"),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(7, 0, 7, 18),
                                "bound-required-int",
                                "input",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(43, 0, 43, 21),
                                "bound-required-string",
                                "input",
                                stringType),
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(65, 0, 65), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(65, 0, 65), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(67, 0, 67, 9),
                                "bound-int",
                                "p",
                                intType),
                            RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                                new SourceSpan(77, 0, 77, 12),
                                "bound-string",
                                "p",
                                stringType),
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
            object expectedOutput,
            object expectedErrors)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper1", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireAttributeDescriptor(attribute => attribute.Name("unbound-required")))
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireAttributeDescriptor(attribute => attribute.Name("bound-required-string")))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("bound-required-string")
                        .PropertyName("BoundRequiredString")
                        .TypeName(typeof(string).FullName))
                    .Build(),
                TagHelperDescriptorBuilder.Create("InputTagHelper2", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireAttributeDescriptor(attribute => attribute.Name("bound-required-int")))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("bound-required-int")
                        .PropertyName("BoundRequiredInt")
                        .TypeName(typeof(int).FullName))
                    .Build(),
                TagHelperDescriptorBuilder.Create("InputTagHelper3", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("int-dictionary")
                        .PropertyName("DictionaryOfIntProperty")
                        .TypeName(typeof(IDictionary<string, int>).Namespace + ".IDictionary<System.String, System.Int32>")
                        .AsDictionaryAttribute("int-prefix-", typeof(int).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("string-dictionary")
                        .PropertyName("DictionaryOfStringProperty")
                        .TypeName(typeof(IDictionary<string, string>).Namespace + ".IDictionary<System.String, System.String>")
                        .AsDictionaryAttribute("string-prefix-", typeof(string).FullName))
                    .Build(),
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("bound-string")
                        .PropertyName("BoundRequiredString")
                        .TypeName(typeof(string).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("bound-int")
                        .PropertyName("BoundRequiredString")
                        .TypeName(typeof(int).FullName))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors);
        }

        [Fact]
        public void Rewrite_UnderstandsMinimizedBooleanBoundAttributes()
        {
            // Arrange
            var documentContent = "<input boundbool boundbooldict-key />";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("boundbool")
                        .PropertyName("BoundBoolProp")
                        .TypeName(typeof(bool).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("boundbooldict")
                        .PropertyName("BoundBoolDictProp")
                        .TypeName("System.Collections.Generic.IDictionary<string, bool>")
                        .AsDictionary("boundbooldict-", typeof(bool).FullName))
                    .Build(),
            };

            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock(
                    "input",
                    TagMode.SelfClosing,
                    attributes: new List<TagHelperAttributeNode>()
                    {
                        new TagHelperAttributeNode("boundbool", null, AttributeStructure.Minimized),
                        new TagHelperAttributeNode("boundbooldict-key", null, AttributeStructure.Minimized),
                    }));

            // Act & Assert
            EvaluateData(descriptors, documentContent, expectedOutput, new RazorDiagnostic[] { });
        }

        [Fact]
        public void Rewrite_FeatureDisabled_AddsErrorForMinimizedBooleanBoundAttributes()
        {
            // Arrange
            var documentContent = "<input boundbool boundbooldict-key />";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("boundbool")
                        .PropertyName("BoundBoolProp")
                        .TypeName(typeof(bool).FullName))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                        .Name("boundbooldict")
                        .PropertyName("BoundBoolDictProp")
                        .TypeName("System.Collections.Generic.IDictionary<string, bool>")
                        .AsDictionary("boundbooldict-", typeof(bool).FullName))
                    .Build(),
            };

            var featureFlags = new TestRazorParserFeatureFlags(allowMinimizedBooleanTagHelperAttributes: false, allowHtmlCommentsInTagHelper: false);

            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock(
                    "input",
                    TagMode.SelfClosing,
                    attributes: new List<TagHelperAttributeNode>()
                    {
                        new TagHelperAttributeNode("boundbool", null, AttributeStructure.Minimized),
                        new TagHelperAttributeNode("boundbooldict-key", null, AttributeStructure.Minimized),
                    }));

            var expectedErrors = new[]
            {
                RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                    new SourceSpan(7, 0, 7, 9),
                    "boundbool",
                    "input",
                    "System.Boolean"),
                RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                    new SourceSpan(17, 0, 17, 17),
                    "boundbooldict-key",
                    "input",
                    "System.Boolean"),
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors, featureFlags: featureFlags);
        }

        private class TestRazorParserFeatureFlags : RazorParserFeatureFlags
        {
            public TestRazorParserFeatureFlags(bool allowMinimizedBooleanTagHelperAttributes, bool allowHtmlCommentsInTagHelper)
            {
                AllowMinimizedBooleanTagHelperAttributes = allowMinimizedBooleanTagHelperAttributes;
                AllowHtmlCommentsInTagHelpers = allowHtmlCommentsInTagHelper;
            }

            public override bool AllowMinimizedBooleanTagHelperAttributes { get; }

            public override bool AllowHtmlCommentsInTagHelpers { get; }
        }
    }
}