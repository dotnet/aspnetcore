// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
#if NET46
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    [IntializeTestFile]
    public abstract class IntegrationTestBase
    {
#if !NET46
        private static readonly AsyncLocal<string> _filename = new AsyncLocal<string>();
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
        public static string Filename
        {
#if NET46
            get
            {
                var handle = (ObjectHandle)CallContext.LogicalGetData("IntegrationTestBase_Filename");
                return (string)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData("IntegrationTestBase_Filename", new ObjectHandle(value));
            }
#elif NETCOREAPP2_0
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
            var testFile = TestFile.Create(sourceFilename, GetType().GetTypeInfo().Assembly);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {sourceFilename} was not found.");
            }

            var source = TestRazorSourceDocument.CreateResource(sourceFilename, GetType(), normalizeNewLines: true);

            var imports = new List<RazorSourceDocument>();
            while (true)
            {
                var importsFilename = Path.ChangeExtension(normalizedFileName + "_Imports" + imports.Count.ToString(), ".cshtml");
                if (!TestFile.Create(importsFilename, GetType().GetTypeInfo().Assembly).Exists())
                {
                    break;
                }

                imports.Add(TestRazorSourceDocument.CreateResource(importsFilename, GetType(), normalizeNewLines: true));
            }

            OnCreatingCodeDocument(ref source, imports);

            var codeDocument = RazorCodeDocument.Create(source, imports);

            // This will ensure that we're not putting any randomly generated data in a baseline.
            codeDocument.Items[DefaultRazorCSharpLoweringPhase.SuppressUniqueIds] = "test";

            // This is to make tests work cross platform.
            codeDocument.Items[DefaultRazorCSharpLoweringPhase.NewLineString] = "\r\n";

            OnCreatedCodeDocument(ref codeDocument);

            return codeDocument;
        }

        protected virtual void OnCreatingCodeDocument(ref RazorSourceDocument source, IList<RazorSourceDocument> imports)
        {
        }

        protected virtual void OnCreatedCodeDocument(ref RazorCodeDocument codeDocument)
        {
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

            var testFile = TestFile.Create(baselineFilename, GetType().GetTypeInfo().Assembly);
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

            var testFile = TestFile.Create(baselineFilename, GetType().GetTypeInfo().Assembly);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {baselineFilename} was not found.");
            }

            var baseline = testFile.ReadAllText();

            // Normalize newlines to match those in the baseline.
            var actual = document.GeneratedCode.Replace("\r", "").Replace("\n", "\r\n");

            Assert.Equal(baseline, actual);
        }

        protected void AssertLineMappingsMatchBaseline(RazorCodeDocument document)
        {
            if (Filename == null)
            {
                var message = $"{nameof(AssertLineMappingsMatchBaseline)} should only be called from an integration test ({nameof(Filename)} is null).";
                throw new InvalidOperationException(message);
            }

            var csharpDocument = document.GetCSharpDocument();
            Assert.NotNull(csharpDocument);

            var baselineFilename = Path.ChangeExtension(Filename, ".mappings.txt");
            var serializedMappings = LineMappingsSerializer.Serialize(csharpDocument, document.Source);

            if (GenerateBaselines)
            {
                var baselineFullPath = Path.Combine(TestProjectRoot, baselineFilename);
                File.WriteAllText(baselineFullPath, serializedMappings);
                return;
            }

            var testFile = TestFile.Create(baselineFilename, GetType().GetTypeInfo().Assembly);
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
            protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
            {
                var walker = new ApiSetsIRWalker();
                walker.Visit(irDocument);
            }

            private class ApiSetsIRWalker : RazorIRNodeWalker
            {
                public override void VisitClassDeclaration(ClassDeclarationIRNode node)
                {
                    node.Name = Filename.Replace('/', '_');
                    node.AccessModifier = "public";

                    VisitDefault(node);
                }

                public override void VisitNamespaceDeclaration(NamespaceDeclarationIRNode node)
                {
                    node.Content = "Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles";

                    VisitDefault(node);
                }

                public override void VisitMethodDeclaration(MethodDeclarationIRNode node)
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
