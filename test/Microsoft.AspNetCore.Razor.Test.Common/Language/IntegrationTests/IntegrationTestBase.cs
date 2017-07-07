// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#if NET46
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    [IntializeTestFile]
    public abstract class IntegrationTestBase
    {
#if !NET46
        private static readonly AsyncLocal<string> _fileName = new AsyncLocal<string>();
#endif

        protected IntegrationTestBase()
        {
            TestProjectRoot = TestProject.GetProjectDirectory(GetType());
        }

#if GENERATE_BASELINES
        protected bool GenerateBaselines { get; set; } = true;
#else
        protected bool GenerateBaselines { get; set; } = false;
#endif

        protected string TestProjectRoot { get; }

        // Used by the test framework to set the 'base' name for test files.
        public static string FileName
        {
#if NET46
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("IntegrationTestBase_FileName");
                return (string)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("IntegrationTestBase_FileName", new ObjectHandle(value));
            }
#elif NETCOREAPP2_0
            get { return _fileName.Value; }
            set { _fileName.Value = value; }
#endif
        }

        protected virtual RazorCodeDocument CreateCodeDocument()
        {
            if (FileName == null)
            {
                var message = $"{nameof(CreateCodeDocument)} should only be called from an integration test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var suffixIndex = FileName.LastIndexOf("_");
            var normalizedFileName = suffixIndex == -1 ? FileName : FileName.Substring(0, suffixIndex);
            var sourceFileName = Path.ChangeExtension(normalizedFileName, ".cshtml");
            var testFile = TestFile.Create(sourceFileName, GetType().GetTypeInfo().Assembly);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {sourceFileName} was not found.");
            }

            var source = TestRazorSourceDocument.CreateResource(sourceFileName, GetType(), normalizeNewLines: true);

            var imports = new List<RazorSourceDocument>();
            while (true)
            {
                var importsFileName = Path.ChangeExtension(normalizedFileName + "_Imports" + imports.Count.ToString(), ".cshtml");
                if (!TestFile.Create(importsFileName, GetType().GetTypeInfo().Assembly).Exists())
                {
                    break;
                }

                imports.Add(TestRazorSourceDocument.CreateResource(importsFileName, GetType(), normalizeNewLines: true));
            }

            OnCreatingCodeDocument(ref source, imports);

            var codeDocument = RazorCodeDocument.Create(source, imports);

            // This will ensure that we're not putting any randomly generated data in a baseline.
            codeDocument.Items[CodeRenderingContext.SuppressUniqueIds] = "test";

            // This is to make tests work cross platform.
            codeDocument.Items[CodeRenderingContext.NewLineString] = "\r\n";

            OnCreatedCodeDocument(ref codeDocument);

            return codeDocument;
        }

        protected virtual void OnCreatingCodeDocument(ref RazorSourceDocument source, IList<RazorSourceDocument> imports)
        {
        }

        protected virtual void OnCreatedCodeDocument(ref RazorCodeDocument codeDocument)
        {
        }

        protected void AssertDocumentNodeMatchesBaseline(DocumentIntermediateNode document)
        {
            if (FileName == null)
            {
                var message = $"{nameof(AssertDocumentNodeMatchesBaseline)} should only be called from an integration test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var baselineFileName = Path.ChangeExtension(FileName, ".ir.txt");

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
                File.WriteAllText(baselineFullPath, IntermediateNodeSerializer.Serialize(document));
                return;
            }

            var irFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!irFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }

            var baseline = irFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            IntermediateNodeVerifier.Verify(document, baseline);
        }

        protected void AssertCSharpDocumentMatchesBaseline(RazorCSharpDocument document)
        {
            if (FileName == null)
            {
                var message = $"{nameof(AssertCSharpDocumentMatchesBaseline)} should only be called from an integration test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var baselineFileName = Path.ChangeExtension(FileName, ".codegen.cs");
            var baselineDiagnosticsFileName = Path.ChangeExtension(FileName, ".diagnostics.txt");

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
                File.WriteAllText(baselineFullPath, document.GeneratedCode);

                var baselineDiagnosticsFullPath = Path.Combine(TestProjectRoot, baselineDiagnosticsFileName);
                var lines = document.Diagnostics.Select(RazorDiagnosticSerializer.Serialize).ToArray();
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

            var codegenFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!codegenFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }

            var baseline = codegenFile.ReadAllText();

            // Normalize newlines to match those in the baseline.
            var actual = document.GeneratedCode.Replace("\r", "").Replace("\n", "\r\n");
            Assert.Equal(baseline, actual);

            var baselineDiagnostics = string.Empty;
            var diagnosticsFile = TestFile.Create(baselineDiagnosticsFileName, GetType().GetTypeInfo().Assembly);
            if (diagnosticsFile.Exists())
            {
                baselineDiagnostics = diagnosticsFile.ReadAllText();
            }

            var actualDiagnostics = string.Concat(document.Diagnostics.Select(d => RazorDiagnosticSerializer.Serialize(d) + "\r\n"));
            Assert.Equal(baselineDiagnostics, actualDiagnostics);
        }

        protected void AssertSourceMappingsMatchBaseline(RazorCodeDocument document)
        {
            if (FileName == null)
            {
                var message = $"{nameof(AssertSourceMappingsMatchBaseline)} should only be called from an integration test ({nameof(FileName)} is null).";
                throw new InvalidOperationException(message);
            }

            var csharpDocument = document.GetCSharpDocument();
            Assert.NotNull(csharpDocument);

            var baselineFileName = Path.ChangeExtension(FileName, ".mappings.txt");
            var serializedMappings = SourceMappingsSerializer.Serialize(csharpDocument, document.Source);

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFileName);
                File.WriteAllText(baselineFullPath, serializedMappings);
                return;
            }

            var testFile = TestFile.Create(baselineFileName, GetType().GetTypeInfo().Assembly);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {baselineFileName} was not found.");
            }

            var baseline = testFile.ReadAllText();

            // Normalize newlines to match those in the baseline.
            var actual = serializedMappings.Replace("\r", "").Replace("\n", "\r\n");

            Assert.Equal(baseline, actual);
        }

        protected class ApiSetsIRTestAdapter : IntermediateNodePassBase, IRazorOptimizationPass
        {
            protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
            {
                var walker = new ApiSetsIRWalker();
                walker.Visit(documentNode);
            }

            private class ApiSetsIRWalker : IntermediateNodeWalker
            {
                public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
                {
                    node.ClassName = FileName.Replace('/', '_');

                    node.Modifiers.Clear();
                    node.Modifiers.Add("public");

                    VisitDefault(node);
                }

                public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
                {
                    node.Content = "Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles";

                    VisitDefault(node);
                }

                public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
                {
                    node.Modifiers.Clear();
                    node.Modifiers.Add("public");
                    node.Modifiers.Add("async");
                    node.MethodName = "ExecuteAsync";
                    node.ReturnType = typeof(Task).FullName;

                    VisitDefault(node);
                }
            }
        }
    }
}
