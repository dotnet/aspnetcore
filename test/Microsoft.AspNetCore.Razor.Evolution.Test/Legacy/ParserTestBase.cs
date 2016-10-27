// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public abstract class ParserTestBase
    {
        internal static Block IgnoreOutput = new IgnoreOutputBlock();

        internal SpanFactory Factory { get; private set; }
        internal BlockFactory BlockFactory { get; private set; }

        internal ParserTestBase()
        {
            Factory = CreateSpanFactory();
            BlockFactory = CreateBlockFactory();
        }

        internal abstract RazorSyntaxTree ParseBlock(string document, bool designTime);

        internal virtual RazorSyntaxTree ParseDocument(string document, bool designTime = false)
        {
            using (var reader = new SeekableTextReader(document))
            {
                var parser = new RazorParser()
                {
                    DesignTimeMode = designTime,
                };

                return parser.Parse((ITextDocument)reader);
            }
        }

        internal virtual RazorSyntaxTree ParseHtmlBlock(string document, bool designTime = false)
        {
            using (var reader = new SeekableTextReader(document))
            {
                var context = new ParserContext(reader, designTime);

                var parser = new HtmlMarkupParser(context);
                parser.CodeParser = new CSharpCodeParser(context)
                {
                    HtmlParser = parser,
                };

                parser.ParseBlock();

                var razorSyntaxTree = context.BuildRazorSyntaxTree();

                return razorSyntaxTree;
            }
        }

        internal virtual RazorSyntaxTree ParseCodeBlock(string document, bool designTime = false)
        {
            using (var reader = new SeekableTextReader(document))
            {
                var context = new ParserContext(reader, designTime);

                var parser = new CSharpCodeParser(context);
                parser.HtmlParser = new HtmlMarkupParser(context)
                {
                    CodeParser = parser,
                };

                parser.ParseBlock();

                var razorSyntaxTree = context.BuildRazorSyntaxTree();

                return razorSyntaxTree;
            }
        }

        internal SpanFactory CreateSpanFactory()
        {
            return new SpanFactory();
        }

        internal abstract BlockFactory CreateBlockFactory();

        internal virtual void ParseBlockTest(string document)
        {
            ParseBlockTest(document, null, false, new RazorError[0]);
        }

        internal virtual void ParseBlockTest(string document, bool designTime)
        {
            ParseBlockTest(document, null, designTime, new RazorError[0]);
        }

        internal virtual void ParseBlockTest(string document, params RazorError[] expectedErrors)
        {
            ParseBlockTest(document, false, expectedErrors);
        }

        internal virtual void ParseBlockTest(string document, bool designTime, params RazorError[] expectedErrors)
        {
            ParseBlockTest(document, null, designTime, expectedErrors);
        }

        internal virtual void ParseBlockTest(string document, Block expectedRoot)
        {
            ParseBlockTest(document, expectedRoot, false, null);
        }

        internal virtual void ParseBlockTest(string document, Block expectedRoot, bool designTime)
        {
            ParseBlockTest(document, expectedRoot, designTime, null);
        }

        internal virtual void ParseBlockTest(string document, Block expectedRoot, params RazorError[] expectedErrors)
        {
            ParseBlockTest(document, expectedRoot, false, expectedErrors);
        }

        internal virtual void ParseBlockTest(string document, Block expected, bool designTime, params RazorError[] expectedErrors)
        {
            var result = ParseBlock(document, designTime);

            if (!ReferenceEquals(expected, IgnoreOutput))
            {
                EvaluateResults(result, expected, expectedErrors);
            }
        }

        internal virtual void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            SingleSpanBlockTest(document, blockType, spanType, acceptedCharacters, expectedError: null);
        }

        internal virtual void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            SingleSpanBlockTest(document, spanContent, blockType, spanType, acceptedCharacters, expectedErrors: null);
        }

        internal virtual void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockType, spanType, expectedError);
        }

        internal virtual void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, params RazorError[] expectedErrors)
        {
            SingleSpanBlockTest(document, spanContent, blockType, spanType, AcceptedCharacters.Any, expectedErrors ?? new RazorError[0]);
        }

        internal virtual void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockType, spanType, acceptedCharacters, expectedError);
        }

        internal virtual void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters, params RazorError[] expectedErrors)
        {
            var result = ParseBlock(document, designTime: false);

            var builder = new BlockBuilder();
            builder.Type = blockType;
            var expected = ConfigureAndAddSpanToBlock(builder, Factory.Span(spanType, spanContent, spanType == SpanKind.Markup).Accepts(acceptedCharacters));

            if (!ReferenceEquals(expected, IgnoreOutput))
            {
                EvaluateResults(result, expected, expectedErrors);
            }
        }

        internal virtual void ParseDocumentTest(string document)
        {
            ParseDocumentTest(document, null, false);
        }

        internal virtual void ParseDocumentTest(string document, Block expectedRoot)
        {
            ParseDocumentTest(document, expectedRoot, false, null);
        }

        internal virtual void ParseDocumentTest(string document, Block expectedRoot, params RazorError[] expectedErrors)
        {
            ParseDocumentTest(document, expectedRoot, false, expectedErrors);
        }

        internal virtual void ParseDocumentTest(string document, bool designTime)
        {
            ParseDocumentTest(document, null, designTime);
        }

        internal virtual void ParseDocumentTest(string document, Block expectedRoot, bool designTime)
        {
            ParseDocumentTest(document, expectedRoot, designTime, null);
        }

        internal virtual void ParseDocumentTest(string document, Block expectedRoot, bool designTime, params RazorError[] expectedErrors)
        {
            var result = ParseDocument(document, designTime);

            if (!ReferenceEquals(expectedRoot, IgnoreOutput))
            {
                EvaluateResults(result, expectedRoot, expectedErrors);
            }
        }

        internal virtual RazorSyntaxTree RunParse(
            string document,
            Func<ParserBase, Action> parserActionSelector,
            bool designTimeParser,
            Func<ParserContext, ParserBase> parserSelector = null,
            ErrorSink errorSink = null)
        {
            throw null;
        }

        internal virtual void RunParseTest(
            string document,
            Func<ParserBase, Action> parserActionSelector,
            Block expectedRoot, IList<RazorError> expectedErrors,
            bool designTimeParser, Func<ParserContext, ParserBase> parserSelector = null)
        {
            throw null;
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

        internal static void EvaluateResults(RazorSyntaxTree result, Block expectedRoot)
        {
            EvaluateResults(result, expectedRoot, null);
        }

        internal static void EvaluateResults(RazorSyntaxTree result, Block expectedRoot, IList<RazorError> expectedErrors)
        {
            EvaluateParseTree(result.Root, expectedRoot);
            EvaluateRazorErrors(result.Diagnostics, expectedErrors);
        }

        internal static void EvaluateParseTree(Block actualRoot, Block expectedRoot)
        {
            // Evaluate the result
            var collector = new ErrorCollector();

            if (expectedRoot == null)
            {
                Assert.Null(actualRoot);
            }
            else
            {
                // Link all the nodes
                expectedRoot.LinkNodes();
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
                return;
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
            if (actual.Type != expected.Type || !expected.ChunkGenerator.Equals(actual.ChunkGenerator))
            {
                AddMismatchError(collector, actual, expected);
            }
            else
            {
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

        internal static void EvaluateRazorErrors(IEnumerable<RazorError> actualErrors, IList<RazorError> expectedErrors)
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

        internal static string FormatErrors(IEnumerable<RazorError> errors)
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
            Trace.WriteLine(string.Format(format, args));
        }

        internal virtual Block CreateSimpleBlockAndSpan(string spanContent, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            var span = Factory.Span(spanType, spanContent, spanType == SpanKind.Markup).Accepts(acceptedCharacters);
            var b = new BlockBuilder()
            {
                Type = blockType
            };
            return ConfigureAndAddSpanToBlock(b, span);
        }

        internal virtual Block ConfigureAndAddSpanToBlock(BlockBuilder block, SpanConstructor span)
        {
            switch (block.Type)
            {
                case BlockType.Markup:
                    span.With(new MarkupChunkGenerator());
                    break;
                case BlockType.Statement:
                    span.With(new StatementChunkGenerator());
                    break;
                case BlockType.Expression:
                    block.ChunkGenerator = new ExpressionChunkGenerator();
                    span.With(new ExpressionChunkGenerator());
                    break;
            }
            block.Children.Add(span);
            return block.Build();
        }

        private class IgnoreOutputBlock : Block
        {
            public IgnoreOutputBlock() : base(BlockType.Template, new SyntaxTreeNode[0], null) { }
        }
    }
}