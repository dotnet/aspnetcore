
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

        internal ParserTestBase()
        {
            TestProjectRoot = TestProject.GetProjectDirectory(GetType());
        }

        /// <summary>
        /// Set to true to autocorrect the locations of spans to appear in document order with no gaps.
        /// Use this when spans were not created in document order.
        /// </summary>
        protected bool FixupSpans { get; set; }

#if GENERATE_BASELINES
        protected bool GenerateBaselines { get; set; } = true;
#else
        protected bool GenerateBaselines { get; set; } = false;
#endif

        protected string TestProjectRoot { get; }

        // Used by the test framework to set the 'base' name for test files.
        public static string FileName
        {
#if NETFRAMEWORK
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("ParserTestBase_FileName");
                return (string)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("ParserTestBase_FileName", new ObjectHandle(value));
            }
#elif NETCOREAPP
            get { return _fileName.Value; }
            set { _fileName.Value = value; }
#else
#error Target frameworks need to be updated.
#endif
        }

        public static bool IsTheory
        {
#if NETFRAMEWORK
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("ParserTestBase_IsTheory");
                return (bool)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("ParserTestBase_IsTheory", new ObjectHandle(value));
            }
#elif NETCOREAPP
            get { return _isTheory.Value; }
            set { _isTheory.Value = value; }
#endif
        }

        protected int BaselineTestCount { get; set; }

        internal virtual void AssertSyntaxTreeNodeMatchesBaseline(RazorSyntaxTree syntaxTree)
        {
            var root = syntaxTree.Root;
            var diagnostics = syntaxTree.Diagnostics;
            var filePath = syntaxTree.Source.FilePath;
            if (FileName == null)
            {
                var message = $"{nameof(AssertSyntaxTreeNodeMatchesBaseline)} should only be called from a parser test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            if (IsTheory)
            {
                var message = $"{nameof(AssertSyntaxTreeNodeMatchesBaseline)} should not be called from a [Theory] test.";
                throw new InvalidOperationException(message);
            }

            var fileName = BaselineTestCount > 0 ? FileName + $"_{BaselineTestCount}" : FileName;
            var baselineFileName = Path.ChangeExtension(fileName, ".stree.txt");
            var baselineDiagnosticsFileName = Path.ChangeExtension(fileName, ".diag.txt");
            var baselineClassifiedSpansFileName = Path.ChangeExtension(fileName, ".cspans.txt");
            var baselineTagHelperSpansFileName = Path.ChangeExtension(fileName, ".tspans.txt");
            BaselineTestCount++;

            if (GenerateBaselines)
            {
                // Write syntax tree baseline
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
                File.WriteAllText(baselineFullPath, SyntaxNodeSerializer.Serialize(root));

                // Write diagnostics baseline
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

                // Write classified spans baseline
                var classifiedSpansBaselineFullPath = Path.Combine(TestProjectRoot, baselineClassifiedSpansFileName);
                File.WriteAllText(classifiedSpansBaselineFullPath, ClassifiedSpanSerializer.Serialize(syntaxTree));

                // Write tag helper spans baseline
                var tagHelperSpansBaselineFullPath = Path.Combine(TestProjectRoot, baselineTagHelperSpansFileName);
                var serializedTagHelperSpans = TagHelperSpanSerializer.Serialize(syntaxTree);
                if (!string.IsNullOrEmpty(serializedTagHelperSpans))
                {
                    File.WriteAllText(tagHelperSpansBaselineFullPath, serializedTagHelperSpans);
                }
                else if (File.Exists(tagHelperSpansBaselineFullPath))
                {
                    File.Delete(tagHelperSpansBaselineFullPath);
                }

                return;
            }

            // Verify syntax tree
            var stFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!stFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }

            var baseline = stFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            SyntaxNodeVerifier.Verify(root, baseline);

            // Verify diagnostics
            var baselineDiagnostics = string.Empty;
            var diagnosticsFile = TestFile.Create(baselineDiagnosticsFileName, GetType().GetTypeInfo().Assembly);
            if (diagnosticsFile.Exists())
            {
                baselineDiagnostics = diagnosticsFile.ReadAllText();
            }

            var actualDiagnostics = string.Concat(diagnostics.Select(d => SerializeDiagnostic(d) + "\r\n"));
            Assert.Equal(baselineDiagnostics, actualDiagnostics);

            // Verify classified spans
            var classifiedSpanFile = TestFile.Create(baselineClassifiedSpansFileName, GetType().GetTypeInfo().Assembly);
            if (!classifiedSpanFile.Exists())
            {
                throw new XunitException($"The resource {baselineClassifiedSpansFileName} was not found.");
            }
            else
            {
                var classifiedSpanBaseline = new string[0];
                classifiedSpanBaseline = classifiedSpanFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                ClassifiedSpanVerifier.Verify(syntaxTree, classifiedSpanBaseline);
            }

            // Verify tag helper spans
            var tagHelperSpanFile = TestFile.Create(baselineTagHelperSpansFileName, GetType().GetTypeInfo().Assembly);
            var tagHelperSpanBaseline = new string[0];
            if (tagHelperSpanFile.Exists())
            {
                tagHelperSpanBaseline = tagHelperSpanFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                TagHelperSpanVerifier.Verify(syntaxTree, tagHelperSpanBaseline);
            }
        }

        protected static string SerializeDiagnostic(RazorDiagnostic diagnostic)
        {
            var content = RazorDiagnosticSerializer.Serialize(diagnostic);
            var normalized = NormalizeNewLines(content);

            return normalized;
        }

        private static string NormalizeNewLines(string content)
        {
            return Regex.Replace(content, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
        }

        internal virtual void BaselineTest(RazorSyntaxTree syntaxTree, bool verifySyntaxTree = true)
        {
            if (verifySyntaxTree)
            {
                SyntaxTreeVerifier.Verify(syntaxTree);
            }

            AssertSyntaxTreeNodeMatchesBaseline(syntaxTree);
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

        internal RazorSyntaxTree ParseDocument(string document, bool designTime = false, RazorParserFeatureFlags featureFlags = null)
        {
            return ParseDocument(RazorLanguageVersion.Latest, document, designTime, featureFlags);
        }

        internal RazorSyntaxTree ParseDocument(RazorLanguageVersion version, string document, bool designTime = false, RazorParserFeatureFlags featureFlags = null)
        {
            return ParseDocument(version, document, null, designTime, featureFlags);
        }

        internal RazorSyntaxTree ParseDocument(string document, IEnumerable<DirectiveDescriptor> directives, bool designTime = false, RazorParserFeatureFlags featureFlags = null)
        {
            return ParseDocument(RazorLanguageVersion.Latest, document, directives, designTime, featureFlags);
        }

        internal virtual RazorSyntaxTree ParseDocument(RazorLanguageVersion version, string document, IEnumerable<DirectiveDescriptor> directives, bool designTime = false, RazorParserFeatureFlags featureFlags = null)
        {
            directives = directives ?? Array.Empty<DirectiveDescriptor>();

            var source = TestRazorSourceDocument.Create(document, filePath: null, relativePath: null, normalizeNewLines: true);

            var options = CreateParserOptions(version, directives, designTime, featureFlags);
            var context = new ParserContext(source, options);

            var codeParser = new CSharpCodeParser(directives, context);
            var markupParser = new HtmlMarkupParser(context);

            codeParser.HtmlParser = markupParser;
            markupParser.CodeParser = codeParser;

            var root = markupParser.ParseDocument().CreateRed();

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

            var source = TestRazorSourceDocument.Create(document, filePath: null, relativePath: null, normalizeNewLines: true);

            var options = CreateParserOptions(version, directives, designTime);
            var context = new ParserContext(source, options);

            var codeParser = new CSharpCodeParser(directives, context);
            var markupParser = new HtmlMarkupParser(context);

            codeParser.HtmlParser = markupParser;
            markupParser.CodeParser = codeParser;

            var root = markupParser.ParseBlock().CreateRed();

            var diagnostics = context.ErrorSink.Errors;

            var syntaxTree = RazorSyntaxTree.Create(root, source, diagnostics, options);

            return syntaxTree;
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

            var source = TestRazorSourceDocument.Create(document, filePath: null, relativePath: null, normalizeNewLines: true);

            var options = CreateParserOptions(version, directives, designTime);
            var context = new ParserContext(source, options);

            var codeParser = new CSharpCodeParser(directives, context);
            var markupParser = new HtmlMarkupParser(context);

            codeParser.HtmlParser = markupParser;
            markupParser.CodeParser = codeParser;

            var root = codeParser.ParseBlock().CreateRed();

            var diagnostics = context.ErrorSink.Errors;

            var syntaxTree = RazorSyntaxTree.Create(root, source, diagnostics, options);

            return syntaxTree;
        }

        internal virtual void ParseBlockTest(string document)
        {
            ParseBlockTest(document, null, false, new RazorDiagnostic[0]);
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

        internal virtual void ParseBlockTest(RazorLanguageVersion version, string document)
        {
            ParseBlockTest(version, document, false, null);
        }

        internal virtual void ParseBlockTest(string document, IEnumerable<DirectiveDescriptor> directives, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(document, directives, false, expectedErrors);
        }

        internal virtual void ParseBlockTest(RazorLanguageVersion version, string document, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(version, document, null, designTime, expectedErrors);
        }

        internal virtual void ParseBlockTest(string document, IEnumerable<DirectiveDescriptor> directives, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            ParseBlockTest(RazorLanguageVersion.Latest, document, directives, designTime, expectedErrors);
        }

        internal virtual void ParseBlockTest(RazorLanguageVersion version, string document, IEnumerable<DirectiveDescriptor> directives, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            var result = ParseBlock(version, document, directives, designTime);

            BaselineTest(result);
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

            BaselineTest(result);
        }

        internal virtual void ParseDocumentTest(string document)
        {
            ParseDocumentTest(document, null, false);
        }

        internal virtual void ParseDocumentTest(string document, params RazorDiagnostic[] expectedErrors)
        {
            ParseDocumentTest(document, false, expectedErrors);
        }

        internal virtual void ParseDocumentTest(string document, IEnumerable<DirectiveDescriptor> directives, params RazorDiagnostic[] expectedErrors)
        {
            ParseDocumentTest(document, directives, false, expectedErrors);
        }

        internal virtual void ParseDocumentTest(string document, bool designTime)
        {
            ParseDocumentTest(document, null, designTime);
        }

        internal virtual void ParseDocumentTest(string document, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            ParseDocumentTest(document, null, designTime, expectedErrors);
        }

        internal virtual void ParseDocumentTest(string document, IEnumerable<DirectiveDescriptor> directives, bool designTime, params RazorDiagnostic[] expectedErrors)
        {
            var result = ParseDocument(document, directives, designTime);

            BaselineTest(result);
        }

        internal static RazorParserOptions CreateParserOptions(
            RazorLanguageVersion version, 
            IEnumerable<DirectiveDescriptor> directives, 
            bool designTime,
            RazorParserFeatureFlags featureFlags = null)
        {
            return new TestRazorParserOptions(
                directives.ToArray(),
                designTime,
                parseLeadingDirectives: false,
                version: version,
                featureFlags: featureFlags);
        }

        private class TestRazorParserOptions : RazorParserOptions
        {
            public TestRazorParserOptions(DirectiveDescriptor[] directives, bool designTime, bool parseLeadingDirectives, RazorLanguageVersion version, RazorParserFeatureFlags featureFlags = null)
            {
                if (directives == null)
                {
                    throw new ArgumentNullException(nameof(directives));
                }

                Directives = directives;
                DesignTime = designTime;
                ParseLeadingDirectives = parseLeadingDirectives;
                Version = version;
                FeatureFlags = featureFlags ?? RazorParserFeatureFlags.Create(Version);
            }

            public override bool DesignTime { get; }

            public override IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

            public override bool ParseLeadingDirectives { get; }

            public override RazorLanguageVersion Version { get; }

            internal override RazorParserFeatureFlags FeatureFlags { get; }
        }
    }
}
