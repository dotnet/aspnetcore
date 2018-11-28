// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    [InitializeTestFile]
    public abstract class RazorBaselineIntegrationTestBase : RazorIntegrationTestBase
    {
        private static readonly AsyncLocal<string> _directoryPath = new AsyncLocal<string>();

        protected RazorBaselineIntegrationTestBase(bool? generateBaselines = null)
        {
            TestProjectRoot = TestProject.GetProjectDirectory(GetType());

            if (generateBaselines.HasValue)
            {
                GenerateBaselines = generateBaselines.Value;
            }
        }

        // Used by the test framework to set the directory for test files.
        public static string DirectoryPath
        {
            get { return _directoryPath.Value; }
            set { _directoryPath.Value = value; }
        }

#if GENERATE_BASELINES
        protected bool GenerateBaselines { get; } = true;
#else
        protected bool GenerateBaselines { get; } = false;
#endif
        
        protected string TestProjectRoot { get; }

        // For consistent line endings because the character counts are going to be recorded in files.
        internal override string LineEnding => "\r\n";

        internal override bool NormalizeSourceLineEndings => true;

        internal override string PathSeparator => "\\";

        // Force consistent paths since they are going to be recorded in files.
        internal override string WorkingDirectory => ArbitraryWindowsPath;

        [Fact]
        public void GenerateBaselinesMustBeFalse()
        {
            Assert.False(GenerateBaselines, "GenerateBaselines should be set back to false before you check in!");
        }

        protected void AssertDocumentNodeMatchesBaseline(RazorCodeDocument codeDocument)
        {
            var document = codeDocument.GetDocumentIntermediateNode();
            var baselineFilePath = GetBaselineFilePath(codeDocument, ".ir.txt");

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(baselineFullPath));
                WriteBaseline(IntermediateNodeSerializer.Serialize(document), baselineFullPath);

                return;
            }

            var irFile = TestFile.Create(baselineFilePath, GetType().Assembly);
            if (!irFile.Exists())
            {
                throw new XunitException($"The resource {baselineFilePath} was not found.");
            }

            // Normalize newlines by splitting into an array.
            var baseline = irFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            IntermediateNodeVerifier.Verify(document, baseline);
        }

        protected void AssertCSharpDocumentMatchesBaseline(RazorCodeDocument codeDocument)
        {
            var document = codeDocument.GetCSharpDocument();

            // Normalize newlines to match those in the baseline.
            var actualCode = document.GeneratedCode.Replace("\r", "").Replace("\n", "\r\n");

            var baselineFilePath = GetBaselineFilePath(codeDocument, ".codegen.cs");
            var baselineDiagnosticsFilePath = GetBaselineFilePath(codeDocument, ".diagnostics.txt");
            var baselineMappingsFilePath = GetBaselineFilePath(codeDocument, ".mappings.txt");

            var serializedMappings = SourceMappingsSerializer.Serialize(document, codeDocument.Source);

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(baselineFullPath));
                WriteBaseline(actualCode, baselineFullPath);

                var baselineDiagnosticsFullPath = Path.Combine(TestProjectRoot, baselineDiagnosticsFilePath);
                var lines = document.Diagnostics.Select(RazorDiagnosticSerializer.Serialize).ToArray();
                if (lines.Any())
                {
                    WriteBaseline(lines, baselineDiagnosticsFullPath);
                }
                else if (File.Exists(baselineDiagnosticsFullPath))
                {
                    File.Delete(baselineDiagnosticsFullPath);
                }

                var baselineMappingsFullPath = Path.Combine(TestProjectRoot, baselineMappingsFilePath);
                var text = SourceMappingsSerializer.Serialize(document, codeDocument.Source);
                if (!string.IsNullOrEmpty(text))
                {
                    WriteBaseline(text, baselineMappingsFullPath);
                }
                else if (File.Exists(baselineMappingsFullPath))
                {
                    File.Delete(baselineMappingsFullPath);
                }

                return;
            }

            var codegenFile = TestFile.Create(baselineFilePath, GetType().Assembly);
            if (!codegenFile.Exists())
            {
                throw new XunitException($"The resource {baselineFilePath} was not found.");
            }

            var baseline = codegenFile.ReadAllText();
            Assert.Equal(baseline, actualCode);

            var baselineDiagnostics = string.Empty;
            var diagnosticsFile = TestFile.Create(baselineDiagnosticsFilePath, GetType().Assembly);
            if (diagnosticsFile.Exists())
            {
                baselineDiagnostics = diagnosticsFile.ReadAllText();
            }

            var actualDiagnostics = string.Concat(document.Diagnostics.Select(d => RazorDiagnosticSerializer.Serialize(d) + "\r\n"));
            Assert.Equal(baselineDiagnostics, actualDiagnostics);

            var baselineMappings = string.Empty;
            var mappingsFile = TestFile.Create(baselineMappingsFilePath, GetType().Assembly);
            if (mappingsFile.Exists())
            {
                baselineMappings = mappingsFile.ReadAllText();
            }

            var actualMappings = SourceMappingsSerializer.Serialize(document, codeDocument.Source);
            actualMappings = actualMappings.Replace("\r", "").Replace("\n", "\r\n");
            Assert.Equal(baselineMappings, actualMappings);
        }

        private string GetBaselineFilePath(RazorCodeDocument codeDocument, string extension)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            var lastSlash = codeDocument.Source.FilePath.LastIndexOfAny(new []{ '/', '\\' });
            var fileName = lastSlash == -1 ? null : codeDocument.Source.FilePath.Substring(lastSlash + 1);
            if (string.IsNullOrEmpty(fileName))
            {
                var message = "Integration tests require a filename";
                throw new InvalidOperationException(message);
            }

            if (DirectoryPath == null)
            {
                var message = $"{nameof(AssertDocumentNodeMatchesBaseline)} should only be called from an integration test..";
                throw new InvalidOperationException(message);
            }

            return Path.Combine(DirectoryPath, Path.ChangeExtension(fileName, extension));
        }

        private static void WriteBaseline(string text, string filePath)
        {
            var lines = text.Replace("\r", "").Replace("\n", "\r\n");
            File.WriteAllText(filePath, text);
        }

        private static void WriteBaseline(string[] lines, string filePath)
        {
            using (var writer = new StreamWriter(File.Open(filePath, FileMode.Create)))
            {
                // Force windows-style line endings so that we're consistent. This isn't
                // required for correctness, but will prevent churn when developing on OSX.
                writer.NewLine = "\r\n";

                for (var i = 0; i < lines.Length; i++)
                {
                    writer.WriteLine(lines[i]);
                }
            }
        }
    }
}
