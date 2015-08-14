// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.TagHelpers
{
    public class TagHelperParseTreeRewriterTest : TagHelperRewritingTestBase
    {
        [Fact]
        public void Rewrite_CanHandleInvalidChildrenWithWhitespace()
        {
            // Arrange
            var factory = CreateDefaultSpanFactory();
            var blockFactory = new BlockFactory(factory);
            var documentContent = $"<p>{Environment.NewLine}    <strong>{Environment.NewLine}        Hello" +
                $"{Environment.NewLine}    </strong>{Environment.NewLine}</p>";
            var newLineLength = Environment.NewLine.Length;
            var expectedErrors = new[] {
                new RazorError(
                    RazorResources.FormatTagHelperParseTreeRewriter_InvalidNestedTag("strong", "p", "br"),
                    absoluteIndex: 7 + newLineLength,
                    lineIndex: 1,
                    columnIndex: 9 + newLineLength,
                    length: 8),
                new RazorError(
                    RazorResources.FormatTagHelperParseTreeRewriter_CannotHaveNonTagContent("p", "br"),
                    absoluteIndex: 23 + newLineLength * 2,
                    lineIndex: 2,
                    columnIndex: 23 + newLineLength * 2,
                    length: 5),
                new RazorError(
                    RazorResources.FormatTagHelperParseTreeRewriter_InvalidNestedTag("strong", "p", "br"),
                    absoluteIndex: 32 + newLineLength * 3,
                    lineIndex: 3,
                    columnIndex: 32 + newLineLength * 3,
                    length: 9),
            };
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    factory.Markup(Environment.NewLine + "    "),
                    blockFactory.MarkupTagBlock("<strong>"),
                    factory.Markup(Environment.NewLine + "        Hello" + Environment.NewLine + "    "),
                    blockFactory.MarkupTagBlock("</strong>"),
                    factory.Markup(Environment.NewLine)));
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "PTagHelper",
                        AssemblyName = "SomeAssembly",
                        AllowedChildren = new[] { "br" },
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors);
        }

        [Fact]
        public void Rewrite_RecoversWhenRequiredAttributeMismatchAndRestrictedChildren()
        {
            // Arrange
            var factory = CreateDefaultSpanFactory();
            var blockFactory = new BlockFactory(factory);
            var documentContent = "<strong required><strong></strong></strong>";

            var expectedErrors = new[] {
                new RazorError(
                    RazorResources.FormatTagHelperParseTreeRewriter_InvalidNestedTag("strong", "strong", "br"),
                    absoluteIndex: 17,
                    lineIndex: 0,
                    columnIndex: 17,
                    length: 8),
                new RazorError(
                    RazorResources.FormatTagHelperParseTreeRewriter_InvalidNestedTag("strong", "strong", "br"),
                    absoluteIndex: 25,
                    lineIndex: 0,
                    columnIndex: 25,
                    length: 9),
            };
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("strong",
                    new List<KeyValuePair<string, SyntaxTreeNode>>
                    {
                        new KeyValuePair<string, SyntaxTreeNode>("required", null)
                    },
                    blockFactory.MarkupTagBlock("<strong>"),
                    blockFactory.MarkupTagBlock("</strong>")));
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "strong",
                        TypeName = "StrongTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "required" },
                        AllowedChildren = new[] { "br" }
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors);
        }

        [Fact]
        public void Rewrite_CanHandleMultipleTagHelpersWithAllowedChildren_OneNull()
        {
            // Arrange
            var factory = CreateDefaultSpanFactory();
            var documentContent = "<p><strong>Hello World</strong><br></p>";
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    new MarkupTagHelperBlock("strong",
                        factory.Markup("Hello World")),
                    new MarkupTagHelperBlock("br", TagMode.StartTagOnly)));
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "PTagHelper1",
                        AssemblyName = "SomeAssembly",
                        AllowedChildren = new[] { "strong", "br" }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "PTagHelper2",
                        AssemblyName = "SomeAssembly"
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "strong",
                        TypeName = "StrongTagHelper",
                        AssemblyName = "SomeAssembly"
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "br",
                        TypeName = "BRTagHelper",
                        AssemblyName = "SomeAssembly",
                        TagStructure = TagStructure.WithoutEndTag
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new RazorError[0]);
        }

        [Fact]
        public void Rewrite_CanHandleMultipleTagHelpersWithAllowedChildren()
        {
            // Arrange
            var factory = CreateDefaultSpanFactory();
            var documentContent = "<p><strong>Hello World</strong><br></p>";
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    new MarkupTagHelperBlock("strong",
                        factory.Markup("Hello World")),
                    new MarkupTagHelperBlock("br", TagMode.StartTagOnly)));
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "PTagHelper1",
                        AssemblyName = "SomeAssembly",
                        AllowedChildren = new[] { "strong" }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "PTagHelper2",
                        AssemblyName = "SomeAssembly",
                        AllowedChildren = new[] { "br" }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "strong",
                        TypeName = "StrongTagHelper",
                        AssemblyName = "SomeAssembly"
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "br",
                        TypeName = "BRTagHelper",
                        AssemblyName = "SomeAssembly",
                        TagStructure = TagStructure.WithoutEndTag
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new RazorError[0]);
        }

        public static TheoryData AllowedChildrenData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);
                Func<string, string, string, int, int, RazorError> nestedTagError =
                    (childName, parentName, allowed, location, length) => new RazorError(
                        RazorResources.FormatTagHelperParseTreeRewriter_InvalidNestedTag(
                            childName,
                            parentName,
                            allowed),
                        absoluteIndex: location,
                        lineIndex: 0,
                        columnIndex: location,
                        length: length);
                Func<string, string, int, int, RazorError> nestedContentError =
                    (parentName, allowed, location, length) => new RazorError(
                        RazorResources.FormatTagHelperParseTreeRewriter_CannotHaveNonTagContent(parentName, allowed),
                        absoluteIndex: location,
                        lineIndex: 0,
                        columnIndex: location,
                        length: length);

                return new TheoryData<string, IEnumerable<string>, MarkupBlock, RazorError[]>
                {
                    {
                        "<p><br /></p>",
                        new[] { "br" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing))),
                        new RazorError[0]
                    },
                    {
                        $"<p>{Environment.NewLine}<br />{Environment.NewLine}</p>",
                        new[] { "br" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                factory.Markup(Environment.NewLine),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup(Environment.NewLine))),
                        new RazorError[0]
                    },
                    {
                        "<p><br></p>",
                        new[] { "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("br", TagMode.StartTagOnly))),
                        new[] { nestedTagError("br", "p", "strong", 3, 4) }
                    },
                    {
                        "<p>Hello</p>",
                        new[] { "strong" },
                        new MarkupBlock(new MarkupTagHelperBlock("p", factory.Markup("Hello"))),
                        new[] { nestedContentError("p", "strong", 3, 5) }
                    },
                    {
                        "<p><hr /></p>",
                        new[] { "br", "strong" },
                        new MarkupBlock(new MarkupTagHelperBlock("p", blockFactory.MarkupTagBlock("<hr />"))),
                        new[] { nestedTagError("hr", "p", "br, strong", 3, 6) }
                    },
                    {
                        "<p><br>Hello</p>",
                        new[] { "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("br", TagMode.StartTagOnly),
                                factory.Markup("Hello"))),
                        new[] { nestedTagError("br", "p", "strong", 3, 4), nestedContentError("p", "strong", 7, 5) }
                    },
                    {
                        "<p><strong>Title:</strong><br />Something</p>",
                        new[] { "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong", factory.Markup("Title:")),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("Something"))),
                        new[]
                        {
                            nestedContentError("strong", "strong", 11, 6),
                            nestedTagError("br", "p", "strong", 26, 6),
                            nestedContentError("p", "strong", 32, 9),
                        }
                    },
                    {
                        "<p><strong>Title:</strong><br />Something</p>",
                        new[] { "strong", "br" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong", factory.Markup("Title:")),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("Something"))),
                        new[]
                        {
                            nestedContentError("strong", "strong, br", 11, 6),
                            nestedContentError("p", "strong, br", 32, 9),
                        }
                    },
                    {
                        "<p>  <strong>Title:</strong>  <br />  Something</p>",
                        new[] { "strong", "br" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                factory.Markup("  "),
                                new MarkupTagHelperBlock("strong", factory.Markup("Title:")),
                                factory.Markup("  "),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("  Something"))),
                        new[]
                        {
                            nestedContentError("strong", "strong, br", 13, 6),
                            nestedContentError("p", "strong, br", 38, 9),
                        }
                    },
                    {
                        "<p><strong>Title:<br><em>A Very Cool</em></strong><br />Something</p>",
                        new[] { "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong",
                                    factory.Markup("Title:"),
                                    new MarkupTagHelperBlock("br", TagMode.StartTagOnly),
                                    blockFactory.MarkupTagBlock("<em>"),
                                    factory.Markup("A Very Cool"),
                                    blockFactory.MarkupTagBlock("</em>")),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("Something"))),
                        new[]
                        {
                            nestedContentError("strong", "strong", 11, 6),
                            nestedTagError("br", "strong", "strong", 17, 4),
                            nestedTagError("em", "strong", "strong", 21, 4),
                            nestedContentError("strong", "strong", 25, 11),
                            nestedTagError("em", "strong", "strong", 36, 5),
                            nestedTagError("br", "p", "strong", 50, 6),
                            nestedContentError("p", "strong", 56, 9)
                        }
                    },
                    {
                        "<p><custom>Title:<br><em>A Very Cool</em></custom><br />Something</p>",
                        new[] { "custom" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<custom>"),
                                factory.Markup("Title:"),
                                new MarkupTagHelperBlock("br", TagMode.StartTagOnly),
                                blockFactory.MarkupTagBlock("<em>"),
                                factory.Markup("A Very Cool"),
                                blockFactory.MarkupTagBlock("</em>"),
                                blockFactory.MarkupTagBlock("</custom>"),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("Something"))),
                        new[]
                        {
                            nestedTagError("custom", "p", "custom", 3, 8),
                            nestedContentError("p", "custom", 11, 6),
                            nestedTagError("br", "p", "custom", 17, 4),
                            nestedTagError("em", "p", "custom", 21, 4),
                            nestedContentError("p", "custom", 25, 11),
                            nestedTagError("em", "p", "custom", 36, 5),
                            nestedTagError("custom", "p", "custom", 41, 9),
                            nestedTagError("br", "p", "custom", 50, 6),
                            nestedContentError("p", "custom", 56, 9)
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(AllowedChildrenData))]
        public void Rewrite_UnderstandsAllowedChildren(
            string documentContent,
            IEnumerable<string> allowedChildren,
            MarkupBlock expectedOutput,
            RazorError[] expectedErrors)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "PTagHelper",
                        AssemblyName = "SomeAssembly",
                        AllowedChildren = allowedChildren
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "strong",
                        TypeName = "StrongTagHelper",
                        AssemblyName = "SomeAssembly",
                        AllowedChildren = allowedChildren
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "br",
                        TypeName = "BRTagHelper",
                        AssemblyName = "SomeAssembly",
                        TagStructure = TagStructure.WithoutEndTag
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors);
        }

        [Fact]
        public void Rewrite_CanHandleStartTagOnlyTagTagMode()
        {
            // Arrange
            var documentContent = "<input>";
            var expectedOutput = new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.StartTagOnly));
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        TagStructure = TagStructure.WithoutEndTag
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new RazorError[0]);
        }

        [Fact]
        public void Rewrite_CreatesErrorForWithoutEndTagTagStructureForEndTags()
        {
            // Arrange
            var factory = CreateDefaultSpanFactory();
            var blockFactory = new BlockFactory(factory);
            var expectedError = new RazorError(
                RazorResources.FormatTagHelperParseTreeRewriter_EndTagTagHelperMustNotHaveAnEndTag(
                    "input",
                    "InputTagHelper",
                    TagStructure.WithoutEndTag),
                absoluteIndex: 0,
                lineIndex: 0,
                columnIndex: 0,
                length: 8);
            var documentContent = "</input>";
            var expectedOutput = new MarkupBlock(blockFactory.MarkupTagBlock("</input>"));
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper",
                        AssemblyName = "SomeAssembly",
                        TagStructure = TagStructure.WithoutEndTag
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new[] { expectedError });
        }

        [Fact]
        public void Rewrite_CreatesErrorForInconsistentTagStructures()
        {
            // Arrange
            var factory = CreateDefaultSpanFactory();
            var blockFactory = new BlockFactory(factory);
            var expectedError = new RazorError(
                RazorResources.FormatTagHelperParseTreeRewriter_InconsistentTagStructure(
                    "InputTagHelper1",
                    "InputTagHelper2",
                    "input",
                    nameof(TagHelperDescriptor.TagStructure)),
                absoluteIndex: 0,
                lineIndex: 0,
                columnIndex: 0,
                length: 7);
            var documentContent = "<input>";
            var expectedOutput = new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.StartTagOnly));
            var descriptors = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper1",
                        AssemblyName = "SomeAssembly",
                        TagStructure = TagStructure.WithoutEndTag
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper2",
                        AssemblyName = "SomeAssembly",
                        TagStructure = TagStructure.NormalOrSelfClosing
                    }
                };
            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);

            // Act & Assert
            EvaluateData(descriptorProvider, documentContent, expectedOutput, expectedErrors: new[] { expectedError });
        }

        public static TheoryData RequiredAttributeData
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow(10))
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
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow(10))
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", dateTimeNow(18))
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
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", dateTimeNow(18))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 4, 0, 4),
                                        suffix: new LocationTagged<string>("\"", 15, 0, 15)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                            value: new LocationTagged<string>("btn", 12, 0, 12))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        "<div class=\"btn\"></div>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 4, 0, 4),
                                        suffix: new LocationTagged<string>("\"", 15, 0, 15)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                            value: new LocationTagged<string>("btn", 12, 0, 12))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            blockFactory.MarkupTagBlock("</div>"))
                    },
                    {
                        "<p notRequired=\"a\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("notRequired", dateTimeNow(16)),
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    new KeyValuePair<string, SyntaxTreeNode>("style", dateTimeNow(12)),
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
                                    new KeyValuePair<string, SyntaxTreeNode>("style", dateTimeNow(12)),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow(34))
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
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
                                                factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
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
                                    new KeyValuePair<string, SyntaxTreeNode>("style", dateTimeNow(12)),
                                    new KeyValuePair<string, SyntaxTreeNode>("class", dateTimeNow(34)),
                                    new KeyValuePair<string, SyntaxTreeNode>("catchAll", dateTimeNow(59))
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
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "pTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "class" }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "divTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "class", "style" }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "catchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "catchAll" }
                    }
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
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "pTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "class" }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "catchAllTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "catchAll" }
                    }
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
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "pTagHelper",
                        AssemblyName = "SomeAssembly",
                        RequiredAttributes = new[] { "class" }
                    }
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
                    new TagHelperDescriptor
                    {
                        Prefix = "th:",
                        TagName = "myth",
                        TypeName = "mythTagHelper",
                        AssemblyName = "SomeAssembly"
                    },
                    new TagHelperDescriptor
                    {
                        Prefix = "th:",
                        TagName = "myth2",
                        TypeName = "mythTagHelper2",
                        AssemblyName = "SomeAssembly",
                        Attributes = new []
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound",
                                PropertyName = "Bound",
                                TypeName = typeof(bool).FullName
                            }
                        }
                    }
                };
                var availableDescriptorsText = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        Prefix = "PREFIX",
                        TagName = "myth",
                        TypeName = "mythTagHelper",
                        AssemblyName = "SomeAssembly"
                    },
                    new TagHelperDescriptor
                    {
                        Prefix = "PREFIX",
                        TagName = "myth2",
                        TypeName = "mythTagHelper2",
                        AssemblyName = "SomeAssembly",
                        Attributes = new []
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound",
                                PropertyName = "Bound",
                                TypeName = typeof(bool).FullName
                            },
                        }
                    }
                };
                var availableDescriptorsCatchAll = new TagHelperDescriptor[]
                {
                    new TagHelperDescriptor
                    {
                        Prefix = "myth",
                        TagName = "*",
                        TypeName = "mythTagHelper",
                        AssemblyName = "SomeAssembly"
                    }
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
                            new MarkupTagHelperBlock("th:myth", tagMode: TagMode.SelfClosing)),
                        availableDescriptorsColon
                    },
                    {
                        "<PREFIXmyth />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("PREFIXmyth", tagMode: TagMode.SelfClosing)),
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
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
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<KeyValuePair<string, SyntaxTreeNode>>
                                {
                                    {
                                        new KeyValuePair<string, SyntaxTreeNode>(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                    factory
                                                        .CodeMarkup("@")
                                                        .With(new MarkupChunkGenerator())
                                                        .Accepts(AcceptedCharacters.None),
                                                    factory
                                                        .CodeMarkup("@")
                                                        .With(SpanChunkGenerator.Null)
                                                        .Accepts(AcceptedCharacters.None)),
                                                new MarkupBlock(
                                                    factory.EmptyHtml().As(SpanKind.Code),
                                                    new ExpressionBlock(
                                                        factory
                                                            .CodeTransition()
                                                            .As(SpanKind.Code)
                                                            .With(new MarkupChunkGenerator()),
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
                                            new AttributeBlockChunkGenerator(
                                                name: "class",
                                                prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                                suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                            factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeChunkGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                    value: new LocationTagged<string>("btn", 16, 0, 16))),
                                            factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 8, 0, 8),
                                            suffix: new LocationTagged<string>("'", 25, 0, 25)),
                                        factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn1", 16, 0, 16))),
                                        factory.Markup(" btn2").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 20, 0, 20),
                                                value: new LocationTagged<string>("btn2", 21, 0, 21))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                        new MarkupBlock(
                                            new AttributeBlockChunkGenerator(
                                                name: "class2",
                                                prefix: new LocationTagged<string>(" class2=", 26, 0, 26),
                                                suffix: new LocationTagged<string>(string.Empty, 37, 0, 37)),
                                            factory.Markup(" class2=").With(SpanChunkGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeChunkGenerator(
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 8, 0, 8),
                                            suffix: new LocationTagged<string>("'", 39, 0, 39)),
                                        factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn1", 16, 0, 16))),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(
                                                new LocationTagged<string>(" ", 20, 0, 20), 21, 0, 21),
                                            factory.Markup(" ").With(SpanChunkGenerator.Null),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.Code("DateTime.Now")
                                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                    .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    factory.Markup(" btn2").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 34, 0, 34),
                                                value: new LocationTagged<string>("btn2", 35, 0, 35))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=", 8, 0, 8),
                                            suffix: new LocationTagged<string>(string.Empty, 16, 0, 16)),
                                        factory.Markup(" class=").With(SpanChunkGenerator.Null),
                                        factory.Markup("}").With(
                                            new LiteralAttributeChunkGenerator(
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>(string.Empty, 20, 0, 20)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn}").With(
                                            new LiteralAttributeChunkGenerator(
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 13, 0, 13)),
                                        factory.Markup(" class=").With(SpanChunkGenerator.Null),
                                        factory.Markup("}").With(
                                            new LiteralAttributeChunkGenerator(
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 17, 0, 17)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn}").With(
                                            new LiteralAttributeChunkGenerator(
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 16, 0, 16), new LocationTagged<string>("@", 16, 0, 16))).Accepts(AcceptedCharacters.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                                        factory.Markup("}").With(
                                            new LiteralAttributeChunkGenerator(
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=", 3, 0, 3),
                                        suffix: new LocationTagged<string>(string.Empty, 10, 0, 10)),
                                    factory.Markup(" class=").With(SpanChunkGenerator.Null))))
                    },
                    {
                        "<!p class=\"btn",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>(string.Empty, 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
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
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null))))
                    },
                    {
                        "<!p class=\"btn\" /",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),

                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                            new AttributeBlockChunkGenerator(
                                                name: "class",
                                                prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                                suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                            factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeChunkGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                    value: new LocationTagged<string>("btn", 13, 0, 13))),
                                            factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 5, 0, 5),
                                            suffix: new LocationTagged<string>("'", 22, 0, 22)),
                                        factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn1", 13, 0, 13))),
                                        factory.Markup(" btn2").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 17, 0, 17),
                                                value: new LocationTagged<string>("btn2", 18, 0, 18))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                        new MarkupBlock(
                                            new AttributeBlockChunkGenerator(
                                                name: "class2",
                                                prefix: new LocationTagged<string>(" class2=", 23, 0, 23),
                                                suffix: new LocationTagged<string>(string.Empty, 34, 0, 34)),
                                            factory.Markup(" class2=").With(SpanChunkGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeChunkGenerator(
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
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 5, 0, 5),
                                            suffix: new LocationTagged<string>("'", 36, 0, 36)),
                                        factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn1", 13, 0, 13))),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(
                                                new LocationTagged<string>(" ", 17, 0, 17), 18, 0, 18),
                                            factory.Markup(" ").With(SpanChunkGenerator.Null),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.Code("DateTime.Now")
                                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                    .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                    factory.Markup(" btn2").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 31, 0, 31),
                                                value: new LocationTagged<string>("btn2", 32, 0, 32))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
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
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
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
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class='", 3, 0, 3),
                                        suffix: new LocationTagged<string>("'", 20, 0, 20)),
                                    factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn1").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn1", 11, 0, 11))),
                                    factory.Markup(" btn2").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(" ", 15, 0, 15),
                                            value: new LocationTagged<string>("btn2", 16, 0, 16))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class2",
                                            prefix: new LocationTagged<string>(" class2=", 21, 0, 21),
                                            suffix: new LocationTagged<string>(string.Empty, 32, 0, 32)),
                                        factory.Markup(" class2=").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
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
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class='", 3, 0, 3),
                                        suffix: new LocationTagged<string>("'", 34, 0, 34)),
                                    factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn1").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn1", 11, 0, 11))),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(
                                            new LocationTagged<string>(" ", 15, 0, 15), 16, 0, 16),
                                        factory.Markup(" ").With(SpanChunkGenerator.Null),
                                        new ExpressionBlock(
                                            factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                .Accepts(AcceptedCharacters.NonWhiteSpace))),
                                factory.Markup(" btn2").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(" ", 29, 0, 29),
                                            value: new LocationTagged<string>("btn2", 30, 0, 30))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
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

        public static IEnumerable<object[]> TextTagsBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();

                // Should re-write text tags that aren't in C# blocks
                yield return new object[]
                {
                    "<text>Hello World</text>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("text",
                            factory.Markup("Hello World")))
                };
                yield return new object[]
                {
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
                yield return new object[]
                {
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
                yield return new object[]
                {
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

                yield return new object[]
                {
                    "<foo><!-- Hello World --></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<!-- Hello World -->"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
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
                yield return new object[]
                {
                    "<foo><?xml Hello World ?></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<?xml Hello World ?>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
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
                yield return new object[]
                {
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
                yield return new object[]
                {
                    "<foo><!DOCTYPE hello=\"world\" ></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<!DOCTYPE hello=\"world\" >"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
                    "<foo><![CDATA[ Hello World ]]></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<![CDATA[ Hello World ]]>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
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

        public static IEnumerable<object[]> NestedBlockData
        {
            get
            {
                var factory = CreateDefaultSpanFactory();
                var blockFactory = new BlockFactory(factory);

                yield return new object[]
                {
                    "<p><div></div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new MarkupTagHelperBlock("div")))
                };
                yield return new object[]
                {
                    "<p>Hello World <div></div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World "),
                            new MarkupTagHelperBlock("div")))
                };
                yield return new object[]
                {
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
                yield return new object[]
                {
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