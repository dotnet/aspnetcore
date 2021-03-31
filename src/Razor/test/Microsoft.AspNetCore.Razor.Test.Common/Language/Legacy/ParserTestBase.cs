// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    [IntializeTestFile]
    public abstract class ParserTestBase
    {
        private static readonly AsyncLocal<string> _fileName = new AsyncLocal<string>();
        private static readonly AsyncLocal<bool> _isTheory = new AsyncLocal<bool>();

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
            get { return _fileName.Value; }
            set { _fileName.Value = value; }
        }

        public static bool IsTheory
        {
            get { return _isTheory.Value; }
            set { _isTheory.Value = value; }
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

        internal virtual void BaselineTest(RazorSyntaxTree syntaxTree, bool verifySyntaxTree = true, bool ensureFullFidelity = true)
        {
            if (verifySyntaxTree)
            {
                SyntaxTreeVerifier.Verify(syntaxTree, ensureFullFidelity);
            }

            AssertSyntaxTreeNodeMatchesBaseline(syntaxTree);
        }

        internal RazorSyntaxTree ParseDocument(string document, bool designTime = false, IEnumerable<DirectiveDescriptor> directives = null, RazorParserFeatureFlags featureFlags = null, string fileKind = null)
        {
            return ParseDocument(RazorLanguageVersion.Latest, document, directives, designTime, featureFlags, fileKind);
        }

        internal virtual RazorSyntaxTree ParseDocument(RazorLanguageVersion version, string document, IEnumerable<DirectiveDescriptor> directives, bool designTime = false, RazorParserFeatureFlags featureFlags = null, string fileKind = null)
        {
            directives = directives ?? Array.Empty<DirectiveDescriptor>();

            var source = TestRazorSourceDocument.Create(document, filePath: null, relativePath: null, normalizeNewLines: true);

            var options = CreateParserOptions(version, directives, designTime, featureFlags, fileKind);
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

        internal virtual void ParseDocumentTest(string document)
        {
            ParseDocumentTest(document, null, false);
        }

        internal virtual void ParseDocumentTest(string document, string fileKind)
        {
            ParseDocumentTest(document, null, false, fileKind);
        }

        internal virtual void ParseDocumentTest(string document, IEnumerable<DirectiveDescriptor> directives)
        {
            ParseDocumentTest(document, directives, false);
        }

        internal virtual void ParseDocumentTest(string document, bool designTime)
        {
            ParseDocumentTest(document, null, designTime);
        }

        internal virtual void ParseDocumentTest(string document, IEnumerable<DirectiveDescriptor> directives, bool designTime, string fileKind = null)
        {
            ParseDocumentTest(RazorLanguageVersion.Latest, document, directives, designTime, fileKind);
        }

        internal virtual void ParseDocumentTest(RazorLanguageVersion version, string document, IEnumerable<DirectiveDescriptor> directives, bool designTime, string fileKind = null)
        {
            var result = ParseDocument(version, document, directives, designTime, fileKind: fileKind);

            BaselineTest(result);
        }

        internal static RazorParserOptions CreateParserOptions(
            RazorLanguageVersion version, 
            IEnumerable<DirectiveDescriptor> directives, 
            bool designTime,
            RazorParserFeatureFlags featureFlags = null,
            string fileKind = null)
        {
            return new TestRazorParserOptions(
                directives.ToArray(),
                designTime,
                parseLeadingDirectives: false,
                version: version,
                fileKind: fileKind ?? FileKinds.Legacy,
                featureFlags: featureFlags);
        }

        private class TestRazorParserOptions : RazorParserOptions
        {
            public TestRazorParserOptions(DirectiveDescriptor[] directives, bool designTime, bool parseLeadingDirectives, RazorLanguageVersion version, string fileKind, RazorParserFeatureFlags featureFlags = null)
            {
                if (directives == null)
                {
                    throw new ArgumentNullException(nameof(directives));
                }

                Directives = directives;
                DesignTime = designTime;
                ParseLeadingDirectives = parseLeadingDirectives;
                Version = version;
                FileKind = fileKind;
                FeatureFlags = featureFlags ?? RazorParserFeatureFlags.Create(Version, fileKind);
            }

            public override bool DesignTime { get; }

            internal override string FileKind { get; }

            public override IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

            public override bool ParseLeadingDirectives { get; }

            public override RazorLanguageVersion Version { get; }

            internal override RazorParserFeatureFlags FeatureFlags { get; }
        }
    }
}
