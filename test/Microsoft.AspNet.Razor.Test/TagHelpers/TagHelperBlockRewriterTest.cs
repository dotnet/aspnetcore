// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Test.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class TagHelperBlockRewriterTest : TagHelperRewritingTestBase
    {
        public static TheoryData MinimizedAttributeData_Document
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var noErrors = new RazorError[0];
                var errorFormat = "Attribute '{0}' on tag helper element '{1}' requires a value. Tag helper bound " +
                    "attributes of type '{2}' cannot be empty or contain only whitespace.";
                var stringType = typeof(string).FullName;
                var intType = typeof(int).FullName;
                var expressionString = "@DateTime.Now + 1";
                var expression = new MarkupBlock(
                    new MarkupBlock(
                        new ExpressionBlock(
                            factory.CodeTransition(),
                                factory.Code("DateTime.Now")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                    .Accepts(AcceptedCharacters.NonWhiteSpace))),
                    factory.Markup(" +"),
                    factory.Markup(" 1"));

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorError[]>
                {
                    {
                        "<input unbound-required />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
                                })),
                        noErrors
                    },
                    {
                        "<p bound-string></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
                                })),
                        new[] { new RazorError(string.Format(errorFormat, "bound-int", "p", intType), 3, 0, 3, 9) }
                    },
                    {
                        "<input unbound-required bound-required-string />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                })),
                        noErrors
                    },
                    {
                        "<p bound-string class='btn'></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
                                })),
                        noErrors
                    },
                    {
                        "<p class='btn' bound-string></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", factory.Markup("btn")),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", expression),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", expression),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
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
                                selfClosing: true,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", expression),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", expression),
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", expression),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", expression),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
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

                foreach (var data in documentData)
                {
                    data[0] = $"@{{{data[0]}}}";
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormatNoCloseAngle, "input"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatUnclosed, "input"), SourceLocation.Zero),
                        }
                    },
                    {
                        "<input bound-required-string",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "input",
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormatNoCloseAngle, "input"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatUnclosed, "input"), SourceLocation.Zero),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormatNoCloseAngle, "input"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatUnclosed, "input"), SourceLocation.Zero),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormatNoCloseAngle, "input"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatUnclosed, "input"), SourceLocation.Zero),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormatNoCloseAngle, "p"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatUnclosed, "p"), SourceLocation.Zero),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-string", "p", stringType), 3, 0, 3, 12),
                        }
                    },
                    {
                        "<p bound-int",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormatNoCloseAngle, "p"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatUnclosed, "p"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatNoValue, "bound-int", "p", intType), 3, 0, 3, 9),
                        }
                    },
                    {
                        "<p bound-int bound-string",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
                                })),
                        new[]
                        {
                            new RazorError(string.Format(errorFormatNoCloseAngle, "p"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatUnclosed, "p"), SourceLocation.Zero),
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
                                selfClosing: false,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-int", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("unbound-required", null),
                                    new KeyValuePair<string, SyntaxTreeNode>("bound-required-string", null),
                                },
                                children: new MarkupTagHelperBlock(
                                    "p",
                                    selfClosing: false,
                                    attributes: new List<KeyValuePair<string, SyntaxTreeNode>>()
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>("bound-int", null),
                                        new KeyValuePair<string, SyntaxTreeNode>("bound-string", null),
                                    }))),
                        new[]
                        {
                            new RazorError(string.Format(errorFormatNoCloseAngle, "input"), SourceLocation.Zero),
                            new RazorError(string.Format(errorFormatUnclosed, "input"), SourceLocation.Zero),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-required-int", "input", intType), 7, 0, 7, 18),
                            new RazorError(
                                string.Format(errorFormatNoValue, "bound-required-string", "input", stringType),
                                absoluteIndex: 43,
                                lineIndex: 0,
                                columnIndex: 43,
                                length: 21),
                            new RazorError(string.Format(errorFormatNoCloseAngle, "p"), 64, 0, 64),
                            new RazorError(string.Format(errorFormatUnclosed, "p"), 64, 0, 64),
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
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper1",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                "bound-required-string",
                                "BoundRequiredString",
                                typeof(string).FullName)
                        },
                        requiredAttributes: new[] { "unbound-required" }),
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper1",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                "bound-required-string",
                                "BoundRequiredString",
                                typeof(string).FullName)
                        },
                        requiredAttributes: new[] { "bound-required-string" }),
                    new TagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper2",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                "bound-required-int",
                                "BoundRequiredInt",
                                typeof(int).FullName)
                        },
                        requiredAttributes: new[] { "bound-required-int" }),
                    new TagHelperDescriptor(
                        tagName: "p",
                        typeName: "PTagHelper",
                        assemblyName: "SomeAssembly",
                        attributes: new[]
                        {
                            new TagHelperAttributeDescriptor(
                                "bound-string",
                                "BoundRequiredString",
                                typeof(string).FullName),
                            new TagHelperAttributeDescriptor(
                                "bound-int",
                                "BoundRequiredString",
                                typeof(int).FullName)
                        },
                        requiredAttributes: Enumerable.Empty<string>()),
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors);
        }
    }
}
