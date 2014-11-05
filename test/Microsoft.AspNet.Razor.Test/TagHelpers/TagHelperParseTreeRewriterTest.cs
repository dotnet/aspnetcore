// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers.Internal;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.TagHelpers
{
    public class TagHelperParseTreeRewriterTest : CsHtmlMarkupParserTestBase
    {
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
                                new Dictionary<string, SyntaxTreeNode>())),
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "bar", factory.Markup("false") }
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "bar", new MarkupBlock(factory.Markup("false"), factory.Markup("  <strong>")) }
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", factory.Markup("btn") }
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", factory.Markup("btn") }
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", new MarkupBlock(factory.Markup("btn"), factory.Markup(" bar=")) },
                                    { "foo", factory.Markup(string.Empty) }
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", new MarkupBlock(factory.Markup("btn"), factory.Markup(" bar=")) },
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", dateTimeNow }
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", createInvalidDoBlock(string.Empty) }
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
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", createInvalidDoBlock("\"></p>") }
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
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "age", factory.CodeMarkup("12") }
                            }))
                    },
                    {
                        "<person birthday=\"DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "birthday", factory.CodeMarkup("DateTime.Now") }
                            }))
                    },
                    {
                        "<person name=\"John\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "name", factory.Markup("John") }
                            }))
                    },
                    {
                        "<person name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "name", new MarkupBlock(factory.Markup("Time:"), dateTimeNow) }
                            }))
                    },
                    {
                        "<person age=\"12\" birthday=\"DateTime.Now\" name=\"Time: @DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("person",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "age", factory.CodeMarkup("12") },
                                { "birthday", factory.CodeMarkup("DateTime.Now") },
                                { "name", new MarkupBlock(factory.Markup("Time:"), dateTimeNow) }
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
                new TagHelperDescriptor("person", "PersonTagHelper", "personAssembly", ContentBehavior.None,
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
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") },
                                { "dynamic", new MarkupBlock(dateTimeNow) },
                                { "style", factory.Markup("color:red;") }
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
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") }
                            },
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("p",
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "style", factory.Markup("color:red;") }
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
                            new MarkupTagHelperBlock("p"))
                    },
                    {
                        "< p />",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("< p />"))
                    },
                    {
                        "<input <p />",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<input "),
                            new MarkupTagHelperBlock("p"))
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
                            new MarkupTagHelperBlock("p"))
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
                        new Dictionary<string, SyntaxTreeNode>
                        {
                            { "class", factory.Markup("     foo") },
                            { "style",
                                new MarkupBlock(
                                    factory.Markup("   color"),
                                    factory.Markup(" :"),
                                    factory.Markup("  red"),
                                    factory.Markup("  ;"),
                                    factory.Markup("   "))
                            }
                        }))
                };
                yield return new object[] {
                    "<p      class=\"     foo\"    style=\"   color :  red  ;   \"    >Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("     foo") },
                                { "style",
                                    new MarkupBlock(
                                        factory.Markup("   color"),
                                        factory.Markup(" :"),
                                        factory.Markup("  red"),
                                        factory.Markup("  ;"),
                                        factory.Markup("   "))
                                }
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "<p     class=\"   foo  \" >Hello</p> <p    style=\"  color:red; \" >World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", new MarkupBlock(factory.Markup("   foo"), factory.Markup("  ")) }
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "style", new MarkupBlock(factory.Markup("  color:red;"), factory.Markup(" ")) }
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
                                        factory.Markup(" "),
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
                        new Dictionary<string, SyntaxTreeNode>
                        {
                            { "class", new MarkupBlock(dateTimeNow) },
                            { "style", new MarkupBlock(dateTimeNow) }
                        }))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                        new Dictionary<string, SyntaxTreeNode>
                        {
                            { "class", new MarkupBlock(doWhile) },
                            { "style", new MarkupBlock(doWhile) }
                        }))
                };

                currentFormattedString = "<p class=\"{0}\" style='{0}'>Hello World</p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", new MarkupBlock(dateTimeNow) },
                                { "style", new MarkupBlock(dateTimeNow) }
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", new MarkupBlock(doWhile) },
                                { "style", new MarkupBlock(doWhile) }
                            },
                            factory.Markup("Hello World")))
                };

                currentFormattedString = "<p class=\"{0}\">Hello</p> <p style='{0}'>World</p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", new MarkupBlock(dateTimeNow) }
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "style", new MarkupBlock(dateTimeNow) }
                            },
                            factory.Markup("World")))
                };
                yield return new object[] {
                    string.Format(currentFormattedString, doWhileString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", new MarkupBlock(doWhile) }
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "style", new MarkupBlock(doWhile) }
                            },
                            factory.Markup("World")))
                };

                currentFormattedString =
                    "<p class=\"{0}\" style='{0}'>Hello World <strong class=\"{0}\">inside of strong tag</strong></p>";
                yield return new object[] {
                    string.Format(currentFormattedString, dateTimeNowString),
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", new MarkupBlock(dateTimeNow) },
                                { "style", new MarkupBlock(dateTimeNow) }
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
                        new Dictionary<string, SyntaxTreeNode>
                        {
                            { "class", factory.Markup("foo") },
                            { "style", factory.Markup("color:red;") }
                        }))
                };
                yield return new object[] {
                    "<p>Hello <script class=\"foo\" style=\"color:red;\"></script> World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("script",
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", factory.Markup("foo") },
                                    { "style", factory.Markup("color:red;") }
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
                        new Dictionary<string, SyntaxTreeNode>
                        {
                            { "class", factory.Markup("foo") },
                            { "style", factory.Markup("color:red;") }
                        }))
                };
                yield return new object[] {
                    "<p>Hello <p class=\"foo\" style=\"color:red;\" /> World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello "),
                            new MarkupTagHelperBlock("p",
                                new Dictionary<string, SyntaxTreeNode>
                                {
                                    { "class", factory.Markup("foo") },
                                    { "style", factory.Markup("color:red;") }
                                }),
                            factory.Markup(" World")))
                };
                yield return new object[] {
                    "Hello<p class=\"foo\" /> <p style=\"color:red;\" />World",
                    new MarkupBlock(
                        factory.Markup("Hello"),
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") }
                            }),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "style", factory.Markup("color:red;") }
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
                        new Dictionary<string, SyntaxTreeNode>
                        {
                            { "class", factory.Markup("foo") },
                            { "dynamic", new MarkupBlock(dateTimeNow) },
                            { "style", factory.Markup("color:red;") }
                        }))
                };
                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") },
                                { "dynamic", new MarkupBlock(dateTimeNow) },
                                { "style", factory.Markup("color:red;") }
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now>Hello</p> <p style=color:red; dynamic=@DateTime.Now>World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") },
                                { "dynamic", new MarkupBlock(dateTimeNow) }
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "style", factory.Markup("color:red;") },
                                { "dynamic", new MarkupBlock(dateTimeNow) }
                            },
                            factory.Markup("World")))
                };
                yield return new object[] {
                    "<p class=foo dynamic=@DateTime.Now style=color:red;>Hello World <strong class=\"foo\">inside of strong tag</strong></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") },
                                { "dynamic", new MarkupBlock(dateTimeNow) },
                                { "style", factory.Markup("color:red;") }
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
                        new Dictionary<string, SyntaxTreeNode>
                        {
                            { "class", factory.Markup("foo") },
                            { "style", factory.Markup("color:red;") }
                        }))
                };
                yield return new object[] {
                    "<p class=\"foo\" style=\"color:red;\">Hello World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") },
                                { "style", factory.Markup("color:red;") }
                            },
                            factory.Markup("Hello World")))
                };
                yield return new object[] {
                    "<p class=\"foo\">Hello</p> <p style=\"color:red;\">World</p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") }
                            },
                            factory.Markup("Hello")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "style", factory.Markup("color:red;") }
                            },
                            factory.Markup("World")))
                };
                yield return new object[] {
                    "<p class=\"foo\" style=\"color:red;\">Hello World <strong class=\"foo\">inside of strong tag</strong></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new Dictionary<string, SyntaxTreeNode>
                            {
                                { "class", factory.Markup("foo") },
                                { "style", factory.Markup("color:red;") }
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

        private void RunParseTreeRewriterTest(string documentContent,
                                              MarkupBlock expectedOutput,
                                              params string[] tagNames)
        {
            RunParseTreeRewriterTest(documentContent,
                                     expectedOutput,
                                     errors: Enumerable.Empty<RazorError>(),
                                     tagNames: tagNames);
        }

        private void RunParseTreeRewriterTest(string documentContent,
                                              MarkupBlock expectedOutput,
                                              IEnumerable<RazorError> errors,
                                              params string[] tagNames)
        {
            // Arrange
            var providerContext = BuildProviderContext(tagNames);

            // Act & Assert
            EvaluateData(providerContext, documentContent, expectedOutput, errors);
        }

        private TagHelperDescriptorProvider BuildProviderContext(params string[] tagNames)
        {
            var descriptors = new List<TagHelperDescriptor>();

            foreach (var tagName in tagNames)
            {
                descriptors.Add(
                    new TagHelperDescriptor(tagName, tagName + "taghelper", "SomeAssembly", ContentBehavior.None));
            }

            return new TagHelperDescriptorProvider(descriptors);
        }

        public override ParserContext CreateParserContext(ITextDocument input,
                                                          ParserBase codeParser,
                                                          ParserBase markupParser,
                                                          ParserErrorSink errorSink)
        {
            return base.CreateParserContext(input, codeParser, markupParser, errorSink);
        }

        private void EvaluateData(TagHelperDescriptorProvider provider,
                                  string documentContent,
                                  MarkupBlock expectedOutput,
                                  IEnumerable<RazorError> expectedErrors)
        {
            var errorSink = new ParserErrorSink();
            var results = ParseDocument(documentContent, errorSink);
            var rewritingContext = new RewritingContext(results.Document, errorSink);
            new TagHelperParseTreeRewriter(provider).Rewrite(rewritingContext);
            var rewritten = rewritingContext.SyntaxTree;
            var actualErrors = errorSink.Errors.OrderBy(error => error.Location.AbsoluteIndex)
                                               .ToList();

            EvaluateRazorErrors(actualErrors, expectedErrors.ToList());
            EvaluateParseTree(rewritten, expectedOutput);
        }

        private static SpanFactory CreateDefaultSpanFactory()
        {
            return new SpanFactory
            {
                MarkupTokenizerFactory = doc => new HtmlTokenizer(doc),
                CodeTokenizerFactory = doc => new CSharpTokenizer(doc)
            };
        }
    }
}