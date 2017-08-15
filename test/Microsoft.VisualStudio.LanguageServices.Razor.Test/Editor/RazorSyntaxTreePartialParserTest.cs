// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    public class RazorSyntaxTreePartialParserTest
    {
        public static TheoryData TagHelperPartialParseRejectData
        {
            get
            {
                // change, (Block)expectedDocument
                return new TheoryData<TestEdit, MarkupBlock>
                {
                    {
                        CreateInsertionChange("<p></p>", 2, " "),
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p"))
                    },
                    {
                        CreateInsertionChange("<p></p>", 6, " "),
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p"))
                    },
                    {
                        CreateInsertionChange("<p some-attr></p>", 12, " "),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "some-attr",
                                        value: null,
                                        attributeStructure: AttributeStructure.Minimized)
                                }))
                    },
                    {
                        CreateInsertionChange("<p some-attr></p>", 12, "ibute"),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "some-attribute",
                                        value: null,
                                        attributeStructure: AttributeStructure.Minimized)
                                }))
                    },
                    {
                        CreateInsertionChange("<p some-attr></p>", 2, " before"),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "before",
                                        value: null,
                                        attributeStructure: AttributeStructure.Minimized),
                                    new TagHelperAttributeNode(
                                        "some-attr",
                                        value: null,
                                        attributeStructure: AttributeStructure.Minimized)
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagHelperPartialParseRejectData))]
        public void TagHelperTagBodiesRejectPartialChanges(object objectEdit, object expectedDocument)
        {
            // Arrange
            var edit = (TestEdit)objectEdit;
            var builder = TagHelperDescriptorBuilder.Create("PTagHelper", "TestAssembly");
            builder.SetTypeName("PTagHelper");
            builder.TagMatchingRule(rule => rule.TagName = "p");
            var descriptors = new[]
            {
                builder.Build()
            };
            var templateEngine = CreateTemplateEngine(tagHelpers: descriptors);
            var document = TestRazorCodeDocument.Create(
                TestRazorSourceDocument.Create(edit.OldSnapshot.GetText()),
                new[] { templateEngine.Options.DefaultImports });
            templateEngine.Engine.Process(document);
            var syntaxTree = document.GetSyntaxTree();
            var parser = new RazorSyntaxTreePartialParser(syntaxTree);

            // Act
            var result = parser.Parse(edit.Change);

            // Assert
            Assert.Equal(PartialParseResultInternal.Rejected, result);
        }

        public static TheoryData TagHelperAttributeAcceptData
        {
            get
            {
                var factory = new SpanFactory();

                // change, (Block)expectedDocument, partialParseResult
                return new TheoryData<TestEdit, MarkupBlock, PartialParseResultInternal>
                {
                    {
                        CreateInsertionChange("<p str-attr='@DateTime'></p>", 22, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "str-attr",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory
                                                        .Code("DateTime.")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace)))),
                                        AttributeStructure.SingleQuotes)
                                })),
                        PartialParseResultInternal.Accepted | PartialParseResultInternal.Provisional
                    },
                    {
                        CreateInsertionChange("<p obj-attr='DateTime'></p>", 21, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "obj-attr",
                                        factory.CodeMarkup("DateTime."),
                                        AttributeStructure.SingleQuotes)
                                })),
                        PartialParseResultInternal.Accepted
                    },
                    {
                        CreateInsertionChange("<p obj-attr='1 + DateTime'></p>", 25, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "obj-attr",
                                        factory.CodeMarkup("1 + DateTime."),
                                        AttributeStructure.SingleQuotes)
                                })),
                        PartialParseResultInternal.Accepted
                    },
                    {
                        CreateInsertionChange("<p before-attr str-attr='@DateTime' after-attr></p>", 34, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "before-attr",
                                        value: null,
                                        attributeStructure: AttributeStructure.Minimized),
                                    new TagHelperAttributeNode(
                                        "str-attr",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory
                                                        .Code("DateTime.")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace)))),
                                        AttributeStructure.SingleQuotes),
                                    new TagHelperAttributeNode(
                                        "after-attr",
                                        value: null,
                                        attributeStructure: AttributeStructure.Minimized),
                                })),
                        PartialParseResultInternal.Accepted | PartialParseResultInternal.Provisional
                    },
                    {
                        CreateInsertionChange("<p str-attr='before @DateTime after'></p>", 29, "."),
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode(
                                        "str-attr",
                                        new MarkupBlock(
                                            factory.Markup("before"),
                                            new MarkupBlock(
                                                factory.Markup(" "),
                                                new ExpressionBlock(
                                                    factory.CodeTransition(),
                                                    factory
                                                        .Code("DateTime.")
                                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                                            factory.Markup(" after")),
                                        AttributeStructure.SingleQuotes)
                                })),
                        PartialParseResultInternal.Accepted | PartialParseResultInternal.Provisional
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagHelperAttributeAcceptData))]
        public void TagHelperAttributesAreLocatedAndAcceptChangesCorrectly(
            object editObject,
            object expectedDocument,
            object partialParseResultObject)
        {
            // Arrange
            var edit = (TestEdit)editObject;
            var partialParseResult = (PartialParseResultInternal)partialParseResultObject;
            var builder = TagHelperDescriptorBuilder.Create("PTagHelper", "Test");
            builder.SetTypeName("PTagHelper");
            builder.TagMatchingRule(rule => rule.TagName = "p");
            builder.BindAttribute(attribute =>
            {
                attribute.Name = "obj-attr";
                attribute.TypeName = typeof(object).FullName;
                attribute.SetPropertyName("ObjectAttribute");
            });
            builder.BindAttribute(attribute =>
            {
                attribute.Name = "str-attr";
                attribute.TypeName = typeof(string).FullName;
                attribute.SetPropertyName("StringAttribute");
            });
            var descriptors = new[] { builder.Build() };
            var templateEngine = CreateTemplateEngine(tagHelpers: descriptors);
            var document = TestRazorCodeDocument.Create(
                TestRazorSourceDocument.Create(edit.OldSnapshot.GetText()),
                new[] { templateEngine.Options.DefaultImports });
            templateEngine.Engine.Process(document);
            var syntaxTree = document.GetSyntaxTree();
            var parser = new RazorSyntaxTreePartialParser(syntaxTree);

            // Act
            var result = parser.Parse(edit.Change);

            // Assert
            Assert.Equal(partialParseResult, result);
        }

        [Fact]
        public void ImplicitExpressionAcceptsInnerInsertionsInStatementBlock()
        {
            // Arrange
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @DateTime..Now" + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @DateTime.Now" + Environment.NewLine
                                                + "}");

            // Act and Assert
            RunPartialParseTest(new TestEdit(17, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("DateTime..Now")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsInnerInsertions()
        {
            // Arrange
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @DateTime..Now baz");
            var old = new StringTextSnapshot("foo @DateTime.Now baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(13, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime..Now").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")), additionalFlags: PartialParseResultInternal.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsWholeIdentifierReplacement()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @date baz");
            var changed = new StringTextSnapshot("foo @DateTime baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(5, 4, old, 8, changed, "DateTime"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionRejectsWholeIdentifierReplacementToKeyword()
        {
            // Arrange
            var old = new StringTextSnapshot("foo @date baz");
            var changed = new StringTextSnapshot("foo @if baz");
            var edit = new TestEdit(5, 4, old, 2, changed, "if");

            // Act & Assert
            RunPartialParseRejectionTest(edit);
        }

        [Fact]
        public void ImplicitExpressionRejectsWholeIdentifierReplacementToDirective()
        {
            // Arrange
            var old = new StringTextSnapshot("foo @date baz");
            var changed = new StringTextSnapshot("foo @inherits baz");
            var edit = new TestEdit(5, 4, old, 8, changed, "inherits");

            // Act & Assert
            RunPartialParseRejectionTest(edit, PartialParseResultInternal.SpanContextChanged);
        }

        [Fact]
        public void ImplicitExpressionAcceptsPrefixIdentifierReplacements_SingleSymbol()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @dTime baz");
            var changed = new StringTextSnapshot("foo @DateTime baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(5, 1, old, 4, changed, "Date"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsPrefixIdentifierReplacements_MultipleSymbols()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @dTime.Now baz");
            var changed = new StringTextSnapshot("foo @DateTime.Now baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(5, 1, old, 4, changed, "Date"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime.Now").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsSuffixIdentifierReplacements_SingleSymbol()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @Datet baz");
            var changed = new StringTextSnapshot("foo @DateTime baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(9, 1, old, 4, changed, "Time"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsSuffixIdentifierReplacements_MultipleSymbols()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @DateTime.n baz");
            var changed = new StringTextSnapshot("foo @DateTime.Now baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(14, 1, old, 3, changed, "Now"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime.Now").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsSurroundedIdentifierReplacements()
        {
            // Arrange
            var factory = new SpanFactory();
            var old = new StringTextSnapshot("foo @DateTime.n.ToString() baz");
            var changed = new StringTextSnapshot("foo @DateTime.Now.ToString() baz");

            // Act and Assert
            RunPartialParseTest(new TestEdit(14, 1, old, 3, changed, "Now"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("DateTime.Now.ToString()").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDeleteOfIdentifierPartsIfDotRemains()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @User. baz");
            var old = new StringTextSnapshot("foo @User.Name baz");
            RunPartialParseTest(new TestEdit(10, 4, old, 0, changed, string.Empty),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("User.").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")),
                additionalFlags: PartialParseResultInternal.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsDeleteOfIdentifierPartsIfSomeOfIdentifierRemains()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @Us baz");
            var old = new StringTextSnapshot("foo @User baz");
            RunPartialParseTest(new TestEdit(7, 2, old, 0, changed, string.Empty),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("Us").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsMultipleInsertionIfItCausesIdentifierExpansionAndTrailingDot()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @User. baz");
            var old = new StringTextSnapshot("foo @U baz");
            RunPartialParseTest(new TestEdit(6, 0, old, 4, changed, "ser."),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("User.").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")),
                additionalFlags: PartialParseResultInternal.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsMultipleInsertionIfItOnlyCausesIdentifierExpansion()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @barbiz baz");
            var old = new StringTextSnapshot("foo @bar baz");
            RunPartialParseTest(new TestEdit(8, 0, old, 3, changed, "biz"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("barbiz").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" baz")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierExpansionAtEndOfNonWhitespaceCharacters()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @food" + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @foo" + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TestEdit(10 + Environment.NewLine.Length, 0, old, 1, changed, "d"),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("food")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsIdentifierAfterDotAtEndOfNonWhitespaceCharacters()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @foo.d" + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @foo." + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TestEdit(11 + Environment.NewLine.Length, 0, old, 1, changed, "d"),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.d")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotAtEndOfNonWhitespaceCharacters()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{" + Environment.NewLine
                                                    + "    @foo." + Environment.NewLine
                                                    + "}");
            var old = new StringTextSnapshot("@{" + Environment.NewLine
                                                + "    @foo" + Environment.NewLine
                                                + "}");
            RunPartialParseTest(new TestEdit(10 + Environment.NewLine.Length, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.Code(Environment.NewLine + "    ")
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code(@"foo.")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Code(Environment.NewLine).AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionProvisionallyAcceptsDotAfterIdentifierInMarkup()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @foo. bar");
            var old = new StringTextSnapshot("foo @foo bar");
            RunPartialParseTest(new TestEdit(8, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foo.")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" bar")),
                additionalFlags: PartialParseResultInternal.Provisional);
        }

        [Fact]
        public void ImplicitExpressionAcceptsAdditionalIdentifierCharactersIfEndOfSpanIsIdentifier()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("foo @foob bar");
            var old = new StringTextSnapshot("foo @foo bar");
            RunPartialParseTest(new TestEdit(8, 0, old, 1, changed, "b"),
                new MarkupBlock(
                    factory.Markup("foo "),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("foob")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    factory.Markup(" bar")));
        }

        [Fact]
        public void ImplicitExpressionAcceptsAdditionalIdentifierStartCharactersIfEndOfSpanIsDot()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{@foo.b}");
            var old = new StringTextSnapshot("@{@foo.}");
            RunPartialParseTest(new TestEdit(7, 0, old, 1, changed, "b"),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.b")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.EmptyCSharp().AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        [Fact]
        public void ImplicitExpressionAcceptsDotIfTrailingDotsAreAllowed()
        {
            var factory = new SpanFactory();
            var changed = new StringTextSnapshot("@{@foo.}");
            var old = new StringTextSnapshot("@{@foo}");
            RunPartialParseTest(new TestEdit(6, 0, old, 1, changed, "."),
                new MarkupBlock(
                    factory.EmptyHtml(),
                    new StatementBlock(
                        factory.CodeTransition(),
                        factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo.")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.EmptyCSharp().AsStatement(),
                        factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    factory.EmptyHtml()));
        }

        private void RunPartialParseRejectionTest(TestEdit edit, PartialParseResultInternal additionalFlags = 0)
        {
            var templateEngine = CreateTemplateEngine();
            var document = TestRazorCodeDocument.Create(edit.OldSnapshot.GetText());
            templateEngine.Engine.Process(document);
            var syntaxTree = document.GetSyntaxTree();
            var parser = new RazorSyntaxTreePartialParser(syntaxTree);

            var result = parser.Parse(edit.Change);
            Assert.Equal(PartialParseResultInternal.Rejected | additionalFlags, result);
        }

        private static void RunPartialParseTest(TestEdit edit, Block expectedTree, PartialParseResultInternal additionalFlags = 0)
        {
            var templateEngine = CreateTemplateEngine();
            var document = TestRazorCodeDocument.Create(edit.OldSnapshot.GetText());
            templateEngine.Engine.Process(document);
            var syntaxTree = document.GetSyntaxTree();
            var parser = new RazorSyntaxTreePartialParser(syntaxTree);

            var result = parser.Parse(edit.Change);
            Assert.Equal(PartialParseResultInternal.Accepted | additionalFlags, result);
            ParserTestBase.EvaluateParseTree(expectedTree, syntaxTree.Root);
        }

        private static TestEdit CreateInsertionChange(string initialText, int insertionLocation, string insertionText)
        {
            var changedText = initialText.Insert(insertionLocation, insertionText);
            var sourceChange = new SourceChange(insertionLocation, 0, insertionText);
            var oldSnapshot = new StringTextSnapshot(initialText);
            var changedSnapshot = new StringTextSnapshot(changedText);
            return new TestEdit
            {
                Change = sourceChange,
                OldSnapshot = oldSnapshot,
                NewSnapshot = changedSnapshot,
            };
        }

        private static RazorTemplateEngine CreateTemplateEngine(
            string path = "C:\\This\\Path\\Is\\Just\\For\\Line\\Pragmas.cshtml",
            IEnumerable<TagHelperDescriptor> tagHelpers = null)
        {
            var engine = RazorEngine.CreateDesignTime(builder =>
            {
                RazorExtensions.Register(builder);

                if (tagHelpers != null)
                {
                    builder.AddTagHelpers(tagHelpers);
                }
            });

            // GetImports on RazorTemplateEngine will at least check that the item exists, so we need to pretend
            // that it does.
            var items = new List<RazorProjectItem>();
            items.Add(new TestRazorProjectItem(path));

            var project = new TestRazorProject(items);

            var templateEngine = new RazorTemplateEngine(engine, project);
            templateEngine.Options.DefaultImports = RazorSourceDocument.Create("@addTagHelper *, Test", "_TestImports.cshtml");
            return templateEngine;
        }

        private class TestEdit
        {
            public TestEdit()
            {
            }

            public TestEdit(int position, int oldLength, ITextSnapshot oldSnapshot, int newLength, ITextSnapshot newSnapshot, string newText)
            {
                Change = new SourceChange(position, oldLength, newText);
                OldSnapshot = oldSnapshot;
                NewSnapshot = newSnapshot;
            }

            public SourceChange Change { get; set; }

            public ITextSnapshot OldSnapshot { get; set; }

            public ITextSnapshot NewSnapshot { get; set; }
        }
    }
}
