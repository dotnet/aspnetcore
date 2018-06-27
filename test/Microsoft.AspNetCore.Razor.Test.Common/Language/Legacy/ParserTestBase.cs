// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
#if NET46
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif
using System.Text;
using Xunit;
using Xunit.Sdk;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    [IntializeTestFile]
    public abstract class ParserTestBase
    {
#if !NET46
        private static readonly AsyncLocal<string> _fileName = new AsyncLocal<string>();
        private static readonly AsyncLocal<bool> _isTheory = new AsyncLocal<bool>();
#endif

        internal static Block IgnoreOutput = new IgnoreOutputBlock();

        internal ParserTestBase()
        {
            Factory = CreateSpanFactory();
            BlockFactory = CreateBlockFactory();
            TestProjectRoot = TestProject.GetProjectDirectory(GetType());
        }

        /// <summary>
        /// Set to true to autocorrect the locations of spans to appear in document order with no gaps.
        /// Use this when spans were not created in document order.
        /// </summary>
        protected bool FixupSpans { get; set; }

        internal SpanFactory Factory { get; private set; }

        internal BlockFactory BlockFactory { get; private set; }

#if GENERATE_BASELINES
        protected bool GenerateBaselines { get; set; } = true;
#else
        protected bool GenerateBaselines { get; set; } = false;
#endif

        protected string TestProjectRoot { get; }

        protected bool UseBaselineTests { get; set; }

        // Used by the test framework to set the 'base' name for test files.
        public static string FileName
        {
#if NET46
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("ParserTestBase_FileName");
                return (string)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("ParserTestBase_FileName", new ObjectHandle(value));
            }
#elif NETCOREAPP2_2
            get { return _fileName.Value; }
            set { _fileName.Value = value; }
#endif
        }

        public static bool IsTheory
        {
#if NET46
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("ParserTestBase_IsTheory");
                return (bool)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("ParserTestBase_IsTheory", new ObjectHandle(value));
            }
#elif NETCOREAPP2_2
            get { return _isTheory.Value; }
            set { _isTheory.Value = value; }
#endif
        }

        internal void AssertSyntaxTreeNodeMatchesBaseline(RazorSyntaxTree syntaxTree)
        {
            if (FileName == null)
            {
                var message = $"{nameof(AssertSyntaxTreeNodeMatchesBaseline)} should only be called from a parser test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var baselineFileName = Path.ChangeExtension(FileName, ".syntaxtree.txt");
            var baselineDiagnosticsFileName = Path.ChangeExtension(FileName, ".diagnostics.txt");

            var root = syntaxTree.Root;
            var diagnostics = syntaxTree.Diagnostics;
            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
                File.WriteAllText(baselineFullPath, SyntaxTreeNodeSerializer.Serialize(root));

                var baselineDiagnosticsFullPath = Path.Combine(TestProjectRoot, baselineDiagnosticsFileName);
                var lines = diagnostics.Select(SerializeDiagnostic).ToArray();
                if (lines.Any())
                {
                    File.WriteAllLines(baselineDiagnosticsFullPath, lines);
                }
                else if (File.Exists(baselineDiagnosticsFullPath))
                {
                    File.Delete(baselineDiagnosticsFullPath);
                }

                return;
            }

            var stFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!stFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }

            var baseline = stFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            SyntaxTreeNodeVerifier.Verify(root, baseline);

            var baselineDiagnostics = string.Empty;
            var diagnosticsFile = TestFile.Create(baselineDiagnosticsFileName, GetType().GetTypeInfo().Assembly);
            if (diagnosticsFile.Exists())
            {
                baselineDiagnostics = diagnosticsFile.ReadAllText();
            }

            var actualDiagnostics = string.Concat(diagnostics.Select(d => SerializeDiagnostic(d) + "\r\n"));
            Assert.Equal(baselineDiagnostics, actualDiagnostics);
        }

        private static string SerializeDiagnostic(RazorDiagnostic diagnostic)
        {
            var content = RazorDiagnosticSerializer.Serialize(diagnostic);
            var normalized = NormalizeNewLines(content);

            return normalized;
        }

        private static string NormalizeNewLines(string content)
        {
            return Regex.Replace(content, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
        }

        internal RazorSyntaxTree ParseBlock(string document, bool designTime)
        {
            return ParseBlock(RazorLanguageVersion.Latest, document, designTime);
        }

        internal RazorSyntaxTree ParseBlock(RazorLanguageVersion version, string document, bool designTime)
        {
            return ParseBlock(version, document, null, designTime);
        }

        internal RazorSyntaxTree ParseBlock(string document, IEnumerable<DirectiveDescriptor> directives, bool designTime)
        {
            return ParseBlock(RazorLanguageVersion.Latest, document, directives, designTime);
        }

        internal abstract RazorSyntaxTree ParseBlock(RazorLanguageVersion version, string document, IEnumerable<DirectiveDescriptor> directives, bool designTime);

        internal RazorSyntaxTree ParseDocument(string document, bool designTime = false)
        {
            return ParseDocument(RazorLanguageVersion.Latest, document, designTime);
        }

        internal RazorSyntaxTree ParseDocument(RazorLanguageVersion version, string document, bool designTime = false)
        {
            return ParseDocument(version, document, null, designTime);
        }

        internal RazorSyntaxTree ParseDocument(string document, IEnumerable<DirectiveDescriptor> directives, bool designTime = false)
        {
            return ParseDocument(RazorLanguageVersion.Latest, document, directives, designTime);
        }

        internal virtual RazorSyntaxTree ParseDocument(RazorLanguageVersion version, string document, IEnumerable<DirectiveDescriptor> directives, bool designTime = false)
        {
            directives = directives ?? Array.Empty<DirectiveDescriptor>();

            var source = TestRazorSourceDocument.Create(document, filePath: null, normalizeNewLines: UseBaselineTests);

            var options = CreateParserOptions(version, directives, designTime);
            var context = new ParserContext(source, options);

            var codeParser = new CSharpCodeParser(directives, context);
            var markupParser = new HtmlMarkupParser(context);

            codeParser.HtmlParser = markupParser;
            markupParser.CodeParser = codeParser;

            markupParser.ParseDocument();

            var root = context.Builder.Build();
            var diagnostics = context.ErrorSink.Errors;

            var codeDocument = RazorCodeDocument.Create(source);

            var syntaxTree = RazorSyntaxTree.Create(root, source, diagnostics, options);
            codeDocument.SetSyntaxTree(syntaxTree);

            var defaultDirectivePass = new DefaultDirectiveSyntaxTreePass();
            syntaxTree = defaultDirectivePass.Execute(codeDocument, syntaxTree);

            return syntaxTree;
        }

        internal virtual RazorSyntaxTree ParseHtmlBlock(RazorLanguageVersion version, string document, IEnumerable<DirectiveDescriptor> directives, bool designTime = false)
        {
            directives = directives ?? Array.Empty<DirectiveDescriptor>();

            var source = TestRazorSourceDocument.Create(document, filePath: null, normalizeNewLines: UseBaselineTests);
            var options = CreateParserOptions(version, directives, designTime);
            var context = new ParserContext(source, options);

            var parser = new HtmlMarkupParser(context);
            parser.CodeParser = new CSharpCodeParser(directives, context)
            {
                HtmlParser = parser,
            };

            parser.ParseBlock();

            var root = context.Builder.Build();
            var diagnostics = context.ErrorSink.Errors;

            return RazorSyntaxTree.Create(root, source, diagnostics, options);
        }

        internal RazorSyntaxTree ParseCodeBlock(string document, bool designTime = false)
        {
            return ParseCodeBlock(RazorLanguageVersion.Latest, document, Enumerable.Empty<DirectiveDescriptor>(), designTime);
        }

        internal virtual RazorSyntaxTree ParseCodeBlock(
            RazorLanguageVersion version,
            string document,
            IEnumerable<DirectiveDescriptor> directives,
            bool designTime)
        {
            directives = directives ?? Array.Empty<DirectiveDescriptor>();

            var source = TestRazorSourceDocument.Create(document, filePath: null, normalizeNewLines: UseBaselineTests);
            var options = CreateParserOptions(version, directives, designTime);
            var context = new ParserContext(source, options);

            var parser = new CSharpCodeParser(directives, context);
            parser.HtmlParser = new HtmlMarkupParser(context)
            {
                CodeParser = parser,
            };

            parser.ParseBlock();

            var root = context.Builder.Build();
            var diagnostics = context.ErrorSink.Errors;

            return RazorSyntaxTree.Create(root, source, diagnostics, options);
        }

        internal SpanFactory CreateSpanFactory()
        {
            return new SpanFactory();
        }

        internal abstract BlockFactory CreateBlockFactory();

        internal virtual void ParseBlockTest(string document)
        {
            ParseBlockTest(document, null, false, new RazorDiagnostic[0]);
        }

        internal virtual void ParseBlockTest(string document, bool designTime)
        {
            ParseBlockTest(document, null, designTime, new RazorDiagnostic[0]);
        }

        internal virtual void ParseBlockTest(string document, IEnumerable<DirectiveDescriptor> directives)
        {
            ParseBlockTest(document, directives, null);
        }

        internal virtual void ParseBlockTest(string document, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(document, false, expectedErrors);
        }

        internal virtual void ParseBlockTest(string document, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(document, null, designTime, expectedErrors);
        }

        internal virtual void ParseBlockTest(RazorLanguageVersion version, string document, Block expectedRoot)
        {
            ParseBlockTest(version, document, expectedRoot, false, null);
        }

        internal virtual void ParseBlockTest(string document, Block expectedRoot)
        {
            ParseBlockTest(document, expectedRoot, false, null);
        }

        internal virtual void ParseBlockTest(string document, IEnumerable<DirectiveDescriptor> directives, Block expectedRoot)
        {
            ParseBlockTest(document, directives, expectedRoot, false, null);
        }

        internal virtual void ParseBlockTest(string document, Block expectedRoot, bool designTime)
        {
            ParseBlockTest(document, expectedRoot, designTime, null);
        }

        internal virtual void ParseBlockTest(string document, Block expectedRoot, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(document, expectedRoot, false, expectedErrors);
        }

        internal virtual void ParseBlockTest(string document, IEnumerable<DirectiveDescriptor> directives, Block expectedRoot, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(document, directives, expectedRoot, false, expectedErrors);
        }

        internal virtual void ParseBlockTest(string document, Block expected, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(document, null, expected, designTime, expectedErrors);
        }

        internal virtual void ParseBlockTest(RazorLanguageVersion version, string document, Block expected, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(version, document, null, expected, designTime, expectedErrors);
        }

        internal virtual void ParseBlockTest(string document, IEnumerable<DirectiveDescriptor> directives, Block expected, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(RazorLanguageVersion.Latest, document, directives, expected, designTime, expectedErrors);
        }

        internal virtual void ParseBlockTest(RazorLanguageVersion version, string document, IEnumerable<DirectiveDescriptor> directives, Block expected, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            var result = ParseBlock(version, document, directives, designTime);

            if (UseBaselineTests && !IsTheory)
            {
                SyntaxTreeVerifier.Verify(result);
                AssertSyntaxTreeNodeMatchesBaseline(result);
                return;
            }

            if (FixupSpans)
            {
                SpancestryCorrector.Correct(expected);

                var span = expected.FindFirstDescendentSpan();
                span.ChangeStart(SourceLocation.Zero);
            }

            SyntaxTreeVerifier.Verify(result);
            SyntaxTreeVerifier.Verify(expected);

            if (!ReferenceEquals(expected, IgnoreOutput))
            {
                EvaluateResults(result, expected, expectedErrors);
            }
        }

        internal virtual void SingleSpanBlockTest(string document)
        {
            SingleSpanBlockTest(document, default, default);
        }

        internal virtual void SingleSpanBlockTest(string document, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            SingleSpanBlockTest(document, blockKind, spanType, acceptedCharacters, expectedError: null);
        }

        internal virtual void SingleSpanBlockTest(string document, string spanContent, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            SingleSpanBlockTest(document, spanContent, blockKind, spanType, acceptedCharacters, expectedErrors: null);
        }

        internal virtual void SingleSpanBlockTest(string document, BlockKindInternal blockKind, SpanKindInternal spanType, params RazorDiagnostic[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockKind, spanType, expectedError);
        }

        internal virtual void SingleSpanBlockTest(string document, string spanContent, BlockKindInternal blockKind, SpanKindInternal spanType, params RazorDiagnostic[] expectedErrors)
        {
            SingleSpanBlockTest(document, spanContent, blockKind, spanType, AcceptedCharactersInternal.Any, expectedErrors ?? new RazorDiagnostic[0]);
        }

        internal virtual void SingleSpanBlockTest(string document, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters, params RazorDiagnostic[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockKind, spanType, acceptedCharacters, expectedError);
        }

        internal virtual void SingleSpanBlockTest(string document, string spanContent, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters, params RazorDiagnostic[] expectedErrors)
        {
            var result = ParseBlock(document, designTime: false);

            if (UseBaselineTests && !IsTheory)
            {
                AssertSyntaxTreeNodeMatchesBaseline(result);
                return;
            }

            var builder = new BlockBuilder();
            builder.Type = blockKind;
            var expected = ConfigureAndAddSpanToBlock(builder, Factory.Span(spanType, spanContent, spanType == SpanKindInternal.Markup).Accepts(acceptedCharacters));

            if (FixupSpans)
            {
                SpancestryCorrector.Correct(expected);

                var span = expected.FindFirstDescendentSpan();
                span.ChangeStart(SourceLocation.Zero);
            }

            SyntaxTreeVerifier.Verify(result);
            SyntaxTreeVerifier.Verify(expected);

            if (!ReferenceEquals(expected, IgnoreOutput))
            {
                EvaluateResults(result, expected, expectedErrors);
            }
        }

        internal virtual void ParseDocumentTest(string document)
        {
            ParseDocumentTest(document, null, false);
        }

        internal virtual void ParseDocumentTest(string document, IEnumerable<DirectiveDescriptor> directives)
        {
            ParseDocumentTest(document, directives, expected: null);
        }

        internal virtual void ParseDocumentTest(string document, Block expectedRoot)
        {
            ParseDocumentTest(document, expectedRoot, false, null);
        }

        internal virtual void ParseDocumentTest(string document, Block expectedRoot, params RazorDiagnostic[] expectedErrors)
        {
            ParseDocumentTest(document, expectedRoot, false, expectedErrors);
        }

        internal virtual void ParseDocumentTest(string document, IEnumerable<DirectiveDescriptor> directives, Block expected, params RazorDiagnostic[] expectedErrors)
        {
            ParseDocumentTest(document, directives, expected, false, expectedErrors);
        }

        internal virtual void ParseDocumentTest(string document, bool designTime)
        {
            ParseDocumentTest(document, null, designTime);
        }

        internal virtual void ParseDocumentTest(string document, Block expectedRoot, bool designTime)
        {
            ParseDocumentTest(document, expectedRoot, designTime, null);
        }

        internal virtual void ParseDocumentTest(string document, Block expected, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            ParseDocumentTest(document, null, expected, designTime, expectedErrors);
        }

        internal virtual void ParseDocumentTest(string document, IEnumerable<DirectiveDescriptor> directives, Block expected, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            var result = ParseDocument(document, directives, designTime);

            if (UseBaselineTests && !IsTheory)
            {
                SyntaxTreeVerifier.Verify(result);
                AssertSyntaxTreeNodeMatchesBaseline(result);
                return;
            }

            if (FixupSpans)
            {
                SpancestryCorrector.Correct(expected);

                var span = expected.FindFirstDescendentSpan();
                span.ChangeStart(SourceLocation.Zero);
            }

            SyntaxTreeVerifier.Verify(result);
            SyntaxTreeVerifier.Verify(expected);

            if (!ReferenceEquals(expected, IgnoreOutput))
            {
                EvaluateResults(result, expected, expectedErrors);
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

        internal static void EvaluateResults(RazorSyntaxTree result, Block expectedRoot)
        {
            EvaluateResults(result, expectedRoot, null);
        }

        internal static void EvaluateResults(RazorSyntaxTree result, Block expectedRoot, IList<RazorDiagnostic> expectedErrors)
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

        private static void EvaluateTagHelperAttribute(
            ErrorCollector collector,
            TagHelperAttributeNode actual,
            TagHelperAttributeNode expected)
        {
            if (actual.Name != expected.Name)
            {
                collector.AddError("{0} - FAILED :: Attribute names do not match", expected.Name);
            }
            else
            {
                collector.AddMessage("{0} - PASSED :: Attribute names match", expected.Name);
            }

            if (actual.AttributeStructure != expected.AttributeStructure)
            {
                collector.AddError("{0} - FAILED :: Attribute value styles do not match", expected.AttributeStructure.ToString());
            }
            else
            {
                collector.AddMessage("{0} - PASSED :: Attribute value style match", expected.AttributeStructure);
            }

            if (actual.AttributeStructure != AttributeStructure.Minimized)
            {
                EvaluateSyntaxTreeNode(collector, actual.Value, expected.Value);
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

                if (expected.TagMode != actual.TagMode)
                {
                    collector.AddError(
                        $"{expected.TagMode} - FAILED :: {nameof(TagMode)} for {nameof(TagHelperBlock)} " +
                        $"{actual.TagName} :: ACTUAL: {actual.TagMode}");
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
                    collector.AddError("End of Attributes - FAILED :: Found Attribute: {0}", actualAttributes.Current.Name);
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

        internal static void EvaluateRazorErrors(IEnumerable<RazorDiagnostic> actualErrors, IList<RazorDiagnostic> expectedErrors)
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

        internal static string FormatErrors(IEnumerable<RazorDiagnostic> errors)
        {
            if (errors == null)
            {
                return "\t<< No Errors >>";
            }

            var builder = new StringBuilder();
            foreach (var error in errors)
            {
                builder.AppendFormat("\t{0}", error);
                builder.AppendLine();
            }
            return builder.ToString();
        }

        [Conditional("PARSER_TRACE")]
        private static void WriteTraceLine(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }

        internal virtual Block CreateSimpleBlockAndSpan(string spanContent, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            var span = Factory.Span(spanType, spanContent, spanType == SpanKindInternal.Markup).Accepts(acceptedCharacters);
            var b = new BlockBuilder()
            {
                Type = blockKind
            };
            return ConfigureAndAddSpanToBlock(b, span);
        }

        internal virtual Block ConfigureAndAddSpanToBlock(BlockBuilder block, SpanConstructor span)
        {
            switch (block.Type)
            {
                case BlockKindInternal.Markup:
                    span.With(new MarkupChunkGenerator());
                    break;
                case BlockKindInternal.Statement:
                    span.With(new StatementChunkGenerator());
                    break;
                case BlockKindInternal.Expression:
                    block.ChunkGenerator = new ExpressionChunkGenerator();
                    span.With(new ExpressionChunkGenerator());
                    break;
            }
            block.Children.Add(span);
            return block.Build();
        }

        private static RazorParserOptions CreateParserOptions(
            RazorLanguageVersion version, 
            IEnumerable<DirectiveDescriptor> directives, 
            bool designTime)
        {
            return new DefaultRazorParserOptions(
                directives.ToArray(),
                designTime,
                parseLeadingDirectives: false,
                version: version);
        }

        private class IgnoreOutputBlock : Block
        {
            public IgnoreOutputBlock() : base(BlockKindInternal.Template, new SyntaxTreeNode[0], null) { }
        }

        // Corrects the parents and previous/next information for spans
        internal class SpancestryCorrector : ParserVisitor
        {
            private SpancestryCorrector()
            {
            }

            protected Block CurrentBlock { get; set; }

            protected Span LastSpan { get; set; }

            public static void Correct(Block block)
            {
                new SpancestryCorrector().VisitBlock(block);
            }

            public override void VisitBlock(Block block)
            {
                CurrentBlock = block;
                base.VisitBlock(block);
            }

            public override void VisitSpan(Span span)
            {
                span.Parent = CurrentBlock;

                span.Previous = LastSpan;
                if (LastSpan != null)
                {
                    LastSpan.Next = span;
                }

                LastSpan = span;
            }
        } 
    }
}