// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Framework
{
    public abstract class ParserTestBase
    {
        protected static Block IgnoreOutput = new IgnoreOutputBlock();

        public SpanFactory Factory { get; private set; }
        public BlockFactory BlockFactory { get; private set; }

        protected ParserTestBase()
        {
            Factory = CreateSpanFactory();
            BlockFactory = CreateBlockFactory();
        }

        public abstract ParserBase CreateMarkupParser();
        public abstract ParserBase CreateCodeParser();

        protected abstract ParserBase SelectActiveParser(ParserBase codeParser, ParserBase markupParser);

        public virtual ParserContext CreateParserContext(ITextDocument input,
                                                         ParserBase codeParser,
                                                         ParserBase markupParser,
                                                         ParserErrorSink errorSink)
        {
            return new ParserContext(input,
                                     codeParser,
                                     markupParser,
                                     SelectActiveParser(codeParser, markupParser),
                                     errorSink);
        }

        protected abstract SpanFactory CreateSpanFactory();
        protected abstract BlockFactory CreateBlockFactory();

        protected virtual void ParseBlockTest(string document)
        {
            ParseBlockTest(document, null, false, new RazorError[0]);
        }

        protected virtual void ParseBlockTest(string document, bool designTimeParser)
        {
            ParseBlockTest(document, null, designTimeParser, new RazorError[0]);
        }

        protected virtual void ParseBlockTest(string document, params RazorError[] expectedErrors)
        {
            ParseBlockTest(document, false, expectedErrors);
        }

        protected virtual void ParseBlockTest(string document, bool designTimeParser, params RazorError[] expectedErrors)
        {
            ParseBlockTest(document, null, designTimeParser, expectedErrors);
        }

        protected virtual void ParseBlockTest(string document, Block expectedRoot)
        {
            ParseBlockTest(document, expectedRoot, false, null);
        }

        protected virtual void ParseBlockTest(string document, Block expectedRoot, bool designTimeParser)
        {
            ParseBlockTest(document, expectedRoot, designTimeParser, null);
        }

        protected virtual void ParseBlockTest(string document, Block expectedRoot, params RazorError[] expectedErrors)
        {
            ParseBlockTest(document, expectedRoot, false, expectedErrors);
        }

        protected virtual void ParseBlockTest(string document, Block expectedRoot, bool designTimeParser, params RazorError[] expectedErrors)
        {
            RunParseTest(document, parser => parser.ParseBlock, expectedRoot, (expectedErrors ?? new RazorError[0]).ToList(), designTimeParser);
        }

        protected virtual void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            SingleSpanBlockTest(document, blockType, spanType, acceptedCharacters, expectedError: null);
        }

        protected virtual void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            SingleSpanBlockTest(document, spanContent, blockType, spanType, acceptedCharacters, expectedErrors: null);
        }

        protected virtual void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockType, spanType, expectedError);
        }

        protected virtual void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, params RazorError[] expectedErrors)
        {
            SingleSpanBlockTest(document, spanContent, blockType, spanType, AcceptedCharacters.Any, expectedErrors ?? new RazorError[0]);
        }

        protected virtual void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockType, spanType, acceptedCharacters, expectedError);
        }

        protected virtual void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters, params RazorError[] expectedErrors)
        {
            var builder = new BlockBuilder();
            builder.Type = blockType;
            ParseBlockTest(
                document,
                ConfigureAndAddSpanToBlock(
                    builder,
                    Factory.Span(spanType, spanContent, spanType == SpanKind.Markup)
                           .Accepts(acceptedCharacters)),
                expectedErrors ?? new RazorError[0]);
        }

        protected virtual void ParseDocumentTest(string document)
        {
            ParseDocumentTest(document, null, false);
        }

        protected virtual void ParseDocumentTest(string document, Block expectedRoot)
        {
            ParseDocumentTest(document, expectedRoot, false, null);
        }

        protected virtual void ParseDocumentTest(string document, Block expectedRoot, params RazorError[] expectedErrors)
        {
            ParseDocumentTest(document, expectedRoot, false, expectedErrors);
        }

        protected virtual void ParseDocumentTest(string document, bool designTimeParser)
        {
            ParseDocumentTest(document, null, designTimeParser);
        }

        protected virtual void ParseDocumentTest(string document, Block expectedRoot, bool designTimeParser)
        {
            ParseDocumentTest(document, expectedRoot, designTimeParser, null);
        }

        protected virtual void ParseDocumentTest(string document, Block expectedRoot, bool designTimeParser, params RazorError[] expectedErrors)
        {
            RunParseTest(document, parser => parser.ParseDocument, expectedRoot, expectedErrors, designTimeParser, parserSelector: c => c.MarkupParser);
        }

        protected virtual ParserResults ParseDocument(string document)
        {
            return ParseDocument(document, designTimeParser: false, errorSink: null);
        }

        protected virtual ParserResults ParseDocument(string document, ParserErrorSink errorSink)
        {
            return ParseDocument(document, designTimeParser: false, errorSink: errorSink);
        }

        protected virtual ParserResults ParseDocument(string document,
                                                      bool designTimeParser,
                                                      ParserErrorSink errorSink)
        {
            return RunParse(document,
                            parser => parser.ParseDocument,
                            designTimeParser,
                            parserSelector: c => c.MarkupParser,
                            errorSink: errorSink);
        }

        protected virtual ParserResults ParseBlock(string document)
        {
            return ParseBlock(document, designTimeParser: false);
        }

        protected virtual ParserResults ParseBlock(string document, bool designTimeParser)
        {
            return RunParse(document, parser => parser.ParseBlock, designTimeParser);
        }

        protected virtual ParserResults RunParse(string document,
                                                 Func<ParserBase, Action> parserActionSelector,
                                                 bool designTimeParser,
                                                 Func<ParserContext, ParserBase> parserSelector = null,
                                                 ParserErrorSink errorSink = null)
        {
            parserSelector = parserSelector ?? (c => c.ActiveParser);
            errorSink = errorSink ?? new ParserErrorSink();

            // Create the source
            ParserResults results = null;
            using (var reader = new SeekableTextReader(document))
            {
                try
                {
                    var codeParser = CreateCodeParser();
                    var markupParser = CreateMarkupParser();
                    var context = CreateParserContext(reader, codeParser, markupParser, errorSink);
                    context.DesignTimeMode = designTimeParser;

                    codeParser.Context = context;
                    markupParser.Context = context;

                    // Run the parser
                    parserActionSelector(parserSelector(context))();
                    results = context.CompleteParse();
                }
                finally
                {
                    if (results != null && results.Document != null)
                    {
                        WriteTraceLine(String.Empty);
                        WriteTraceLine("Actual Parse Tree:");
                        WriteNode(0, results.Document);
                    }
                }
            }
            return results;
        }

        protected virtual void RunParseTest(string document, Func<ParserBase, Action> parserActionSelector, Block expectedRoot, IList<RazorError> expectedErrors, bool designTimeParser, Func<ParserContext, ParserBase> parserSelector = null)
        {
            // Create the source
            var results = RunParse(document, parserActionSelector, designTimeParser, parserSelector);

            // Evaluate the results
            if (!ReferenceEquals(expectedRoot, IgnoreOutput))
            {
                EvaluateResults(results, expectedRoot, expectedErrors);
            }
        }

        [Conditional("PARSER_TRACE")]
        private void WriteNode(int indent, SyntaxTreeNode node)
        {
            var content = node.ToString().Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("{", "{{")
                .Replace("}", "}}");
            if (indent > 0)
            {
                content = new String('.', indent * 2) + content;
            }
            WriteTraceLine(content);
            var block = node as Block;
            if (block != null)
            {
                foreach (SyntaxTreeNode child in block.Children)
                {
                    WriteNode(indent + 1, child);
                }
            }
        }

        public static void EvaluateResults(ParserResults results, Block expectedRoot)
        {
            EvaluateResults(results, expectedRoot, null);
        }

        public static void EvaluateResults(ParserResults results, Block expectedRoot, IList<RazorError> expectedErrors)
        {
            EvaluateParseTree(results.Document, expectedRoot);
            EvaluateRazorErrors(results.ParserErrors, expectedErrors);
        }

        public static void EvaluateParseTree(Block actualRoot, Block expectedRoot)
        {
            // Evaluate the result
            var collector = new ErrorCollector();

            // Link all the nodes
            expectedRoot.LinkNodes();

            if (expectedRoot == null)
            {
                Assert.Null(actualRoot);
            }
            else
            {
                Assert.NotNull(actualRoot);
                EvaluateSyntaxTreeNode(collector, actualRoot, expectedRoot);
                if (collector.Success)
                {
                    WriteTraceLine("Parse Tree Validation Succeeded:" + Environment.NewLine + collector.Message);
                }
                else
                {
                    Assert.True(false, Environment.NewLine + collector.Message);
                }
            }
        }

        private static void EvaluateSyntaxTreeNode(ErrorCollector collector, SyntaxTreeNode actual, SyntaxTreeNode expected)
        {
            if (actual == null)
            {
                AddNullActualError(collector, actual, expected);
            }

            if (actual.IsBlock != expected.IsBlock)
            {
                AddMismatchError(collector, actual, expected);
            }
            else
            {
                if (expected.IsBlock)
                {
                    EvaluateBlock(collector, (Block)actual, (Block)expected);
                }
                else
                {
                    EvaluateSpan(collector, (Span)actual, (Span)expected);
                }
            }
        }

        private static void EvaluateTagHelperAttribute(ErrorCollector collector,
                                                       KeyValuePair<string, SyntaxTreeNode> actual,
                                                       KeyValuePair<string, SyntaxTreeNode> expected)
        {
            if (actual.Key != expected.Key)
            {
                collector.AddError("{0} - FAILED :: Attribute names do not match", expected.Key);
            }
            else
            {
                collector.AddMessage("{0} - PASSED :: Attribute names match", expected.Key);
            }

            EvaluateSyntaxTreeNode(collector, actual.Value, expected.Value);
        }

        private static void EvaluateSpan(ErrorCollector collector, Span actual, Span expected)
        {
            if (!Equals(expected, actual))
            {
                AddMismatchError(collector, actual, expected);
            }
            else
            {
                AddPassedMessage(collector, expected);
            }
        }

        private static void EvaluateBlock(ErrorCollector collector, Block actual, Block expected)
        {
            if (actual.Type != expected.Type || !expected.CodeGenerator.Equals(actual.CodeGenerator))
            {
                AddMismatchError(collector, actual, expected);
            }
            else
            {
                if (actual is TagHelperBlock)
                {
                    EvaluateTagHelperBlock(collector, actual as TagHelperBlock, expected as TagHelperBlock);
                }

                AddPassedMessage(collector, expected);
                using (collector.Indent())
                {
                    var expectedNodes = expected.Children.GetEnumerator();
                    var actualNodes = actual.Children.GetEnumerator();
                    while (expectedNodes.MoveNext())
                    {
                        if (!actualNodes.MoveNext())
                        {
                            collector.AddError("{0} - FAILED :: No more elements at this node", expectedNodes.Current);
                        }
                        else
                        {
                            EvaluateSyntaxTreeNode(collector, actualNodes.Current, expectedNodes.Current);
                        }
                    }
                    while (actualNodes.MoveNext())
                    {
                        collector.AddError("End of Node - FAILED :: Found Node: {0}", actualNodes.Current);
                    }
                }
            }
        }

        private static void EvaluateTagHelperBlock(ErrorCollector collector, TagHelperBlock actual, TagHelperBlock expected)
        {
            if (expected == null)
            {
                AddMismatchError(collector, actual, expected);
            }
            else
            {
                if (!string.Equals(expected.TagName, actual.TagName, StringComparison.Ordinal))
                {
                    collector.AddError(
                        "{0} - FAILED :: TagName mismatch for TagHelperBlock :: ACTUAL: {1}",
                        expected.TagName,
                        actual.TagName);
                }

                if (expected.SelfClosing != actual.SelfClosing)
                {
                    collector.AddError(
                        "{0} - FAILED :: SelfClosing for TagHelperBlock {1} :: ACTUAL: {2}",
                        expected.SelfClosing,
                        actual.TagName,
                        actual.SelfClosing);
                }
                
                var expectedAttributes = expected.Attributes.GetEnumerator();
                var actualAttributes = actual.Attributes.GetEnumerator();

                while (expectedAttributes.MoveNext())
                {
                    if (!actualAttributes.MoveNext())
                    {
                        collector.AddError("{0} - FAILED :: No more attributes on this node", expectedAttributes.Current);
                    }
                    else
                    {
                        EvaluateTagHelperAttribute(collector, actualAttributes.Current, expectedAttributes.Current);
                    }
                }
                while (actualAttributes.MoveNext())
                {
                    collector.AddError("End of Attributes - FAILED :: Found Attribute: {0}", actualAttributes.Current.Key);
                }
            }
        }

        private static void AddPassedMessage(ErrorCollector collector, SyntaxTreeNode expected)
        {
            collector.AddMessage("{0} - PASSED", expected);
        }

        private static void AddMismatchError(ErrorCollector collector, SyntaxTreeNode actual, SyntaxTreeNode expected)
        {
            collector.AddError("{0} - FAILED :: Actual: {1}", expected, actual);
        }

        private static void AddNullActualError(ErrorCollector collector, SyntaxTreeNode actual, SyntaxTreeNode expected)
        {
            collector.AddError("{0} - FAILED :: Actual: << Null >>", expected);
        }

        public static void EvaluateRazorErrors(IEnumerable<RazorError> actualErrors, IList<RazorError> expectedErrors)
        {
            var realCount = actualErrors.Count();

            // Evaluate the errors
            if (expectedErrors == null || expectedErrors.Count == 0)
            {
                Assert.True(
                    realCount == 0,
                    "Expected that no errors would be raised, but the following errors were:" + Environment.NewLine + FormatErrors(actualErrors));
            }
            else
            {
                Assert.True(
                    expectedErrors.Count == realCount,
                    $"Expected that {expectedErrors.Count} errors would be raised, but {realCount} errors were." +
                    $"{Environment.NewLine}Expected Errors: {Environment.NewLine}{FormatErrors(expectedErrors)}" +
                    $"{Environment.NewLine}Actual Errors: {Environment.NewLine}{FormatErrors(actualErrors)}");
                Assert.Equal(expectedErrors, actualErrors);
            }
            WriteTraceLine("Expected Errors were raised:" + Environment.NewLine + FormatErrors(expectedErrors));
        }

        public static string FormatErrors(IEnumerable<RazorError> errors)
        {
            if (errors == null)
            {
                return "\t<< No Errors >>";
            }

            var builder = new StringBuilder();
            foreach (RazorError err in errors)
            {
                builder.AppendFormat("\t{0}", err);
                builder.AppendLine();
            }
            return builder.ToString();
        }

        [Conditional("PARSER_TRACE")]
        private static void WriteTraceLine(string format, params object[] args)
        {
            Trace.WriteLine(String.Format(format, args));
        }

        protected virtual Block CreateSimpleBlockAndSpan(string spanContent, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            var span = Factory.Span(spanType, spanContent, spanType == SpanKind.Markup).Accepts(acceptedCharacters);
            var b = new BlockBuilder()
            {
                Type = blockType
            };
            return ConfigureAndAddSpanToBlock(b, span);
        }

        protected virtual Block ConfigureAndAddSpanToBlock(BlockBuilder block, SpanConstructor span)
        {
            switch (block.Type)
            {
                case BlockType.Markup:
                    span.With(new MarkupCodeGenerator());
                    break;
                case BlockType.Statement:
                    span.With(new StatementCodeGenerator());
                    break;
                case BlockType.Expression:
                    block.CodeGenerator = new ExpressionCodeGenerator();
                    span.With(new ExpressionCodeGenerator());
                    break;
            }
            block.Children.Add(span);
            return block.Build();
        }

        private class IgnoreOutputBlock : Block
        {
            public IgnoreOutputBlock() : base(BlockType.Template, Enumerable.Empty<SyntaxTreeNode>(), null) { }
        }
    }
}
