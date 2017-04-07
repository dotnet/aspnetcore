// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if NET452
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
using System.Threading.Tasks;
#endif
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    [IntializeTestFile]
    public abstract class IntegrationTestBase
    {
#if GENERATE_BASELINES
        private static readonly bool GenerateBaselines = true;
#else
        private static readonly bool GenerateBaselines = false;
#endif

#if !NET452
        private static readonly AsyncLocal<string> _filename = new AsyncLocal<string>();
#endif

        protected static string TestProjectRoot { get; } = TestProject.GetProjectDirectory();

        // Used by the test framework to set the 'base' name for test files.
        public static string Filename
        {
#if NET452
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("IntegrationTestBase_Filename");
                return (string)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("IntegrationTestBase_Filename", new ObjectHandle(value));
            }
#else
            get { return _filename.Value; }
            set { _filename.Value = value; }
#endif
        }

        protected virtual RazorCodeDocument CreateCodeDocument()
        {
            if (Filename == null)
            {
                var message = $"{nameof(CreateCodeDocument)} should only be called from an integration test ({nameof(Filename)} is null).";
                throw new InvalidOperationException(message);
            }

            var suffixIndex = Filename.LastIndexOf("_");
            var normalizedFileName = suffixIndex == -1 ? Filename : Filename.Substring(0, suffixIndex);
            var sourceFilename = Path.ChangeExtension(normalizedFileName, ".cshtml");
            var testFile = TestFile.Create(sourceFilename);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {sourceFilename} was not found.");
            }

            var imports = new List<RazorSourceDocument>();
            while (true)
            {
                var importsFilename = Path.ChangeExtension(normalizedFileName + "_Imports" + imports.Count.ToString(), ".cshtml");
                if (!TestFile.Create(importsFilename).Exists())
                {
                    break;
                }

                imports.Add(
                    TestRazorSourceDocument.CreateResource(importsFilename, encoding: null, normalizeNewLines: true));
            }

            var codeDocument = RazorCodeDocument.Create(
                TestRazorSourceDocument.CreateResource(sourceFilename, encoding: null, normalizeNewLines: true), imports);

            // This will ensure that we're not putting any randomly generated data in a baseline.
            codeDocument.Items[DefaultRazorCSharpLoweringPhase.SuppressUniqueIds] = "test";

            // This is to make tests work cross platform.
            codeDocument.Items[DefaultRazorCSharpLoweringPhase.NewLineString] = "\r\n";

            return codeDocument;
        }

        protected void AssertIRMatchesBaseline(DocumentIRNode document)
        {
            if (Filename == null)
            {
                var message = $"{nameof(AssertIRMatchesBaseline)} should only be called from an integration test ({nameof(Filename)} is null).";
                throw new InvalidOperationException(message);
            }

            var baselineFilename = Path.ChangeExtension(Filename, ".ir.txt");

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilename);
                File.WriteAllText(baselineFullPath, RazorIRNodeSerializer.Serialize(document));
                return;
            }

            var testFile = TestFile.Create(baselineFilename);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {baselineFilename} was not found.");
            }

            var baseline = testFile.ReadAllText().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            RazorIRNodeVerifier.Verify(document, baseline);
        }

        protected void AssertCSharpDocumentMatchesBaseline(RazorCSharpDocument document)
        {
            if (Filename == null)
            {
                var message = $"{nameof(AssertCSharpDocumentMatchesBaseline)} should only be called from an integration test ({nameof(Filename)} is null).";
                throw new InvalidOperationException(message);
            }

            var baselineFilename = Path.ChangeExtension(Filename, ".codegen.cs");

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilename);
                File.WriteAllText(baselineFullPath, document.GeneratedCode);
                return;
            }

            var testFile = TestFile.Create(baselineFilename);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {baselineFilename} was not found.");
            }

            var baseline = testFile.ReadAllText();

            // Normalize newlines to match those in the baseline.
            var actual = document.GeneratedCode.Replace("\r", "").Replace("\n", "\r\n");

            Assert.Equal(baseline, actual);
        }

        protected void AssertDesignTimeDocumentMatchBaseline(RazorCodeDocument document)
        {
            if (Filename == null)
            {
                var message = $"{nameof(AssertDesignTimeDocumentMatchBaseline)} should only be called from an integration test ({nameof(Filename)} is null).";
                throw new InvalidOperationException(message);
            }

            var csharpDocument = document.GetCSharpDocument();
            Assert.NotNull(csharpDocument);

            var syntaxTree = document.GetSyntaxTree();
            Assert.NotNull(syntaxTree);
            Assert.True(syntaxTree.Options.DesignTimeMode);

            // Validate generated code.
            AssertCSharpDocumentMatchesBaseline(csharpDocument);

            var baselineFilename = Path.ChangeExtension(Filename, ".mappings.txt");
            var serializedMappings = LineMappingsSerializer.Serialize(csharpDocument, document.Source);

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilename);
                File.WriteAllText(baselineFullPath, serializedMappings);
                return;
            }

            var testFile = TestFile.Create(baselineFilename);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {baselineFilename} was not found.");
            }

            var baseline = testFile.ReadAllText();

            // Normalize newlines to match those in the baseline.
            var actual = serializedMappings.Replace("\r", "").Replace("\n", "\r\n");

            Assert.Equal(baseline, actual);
        }

        protected class ApiSetsIRTestAdapter : RazorIRPassBase, IRazorIROptimizationPass
        {
            public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
            {
                var walker = new ApiSetsIRWalker();
                walker.Visit(irDocument);
            }

            private class ApiSetsIRWalker : RazorIRNodeWalker
            {
                public override void VisitClass(ClassDeclarationIRNode node)
                {
                    node.Name = Filename.Replace('/', '_');
                    node.AccessModifier = "public";

                    VisitDefault(node);
                }

                public override void VisitNamespace(NamespaceDeclarationIRNode node)
                {
                    node.Content = typeof(CodeGenerationIntegrationTest).Namespace + ".TestFiles";

                    VisitDefault(node);
                }

                public override void VisitRazorMethodDeclaration(RazorMethodDeclarationIRNode node)
                {
                    node.AccessModifier = "public";
                    node.Modifiers = new[] { "async" };
                    node.ReturnType = typeof(Task).FullName;
                    node.Name = "ExecuteAsync";

                    VisitDefault(node);
                }
            }
        }
    }
}
