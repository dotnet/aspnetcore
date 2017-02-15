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
#endif
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    [IntializeTestFile]
    public abstract class IntegrationTestBase
    {
        private static readonly string ThisProjectName = typeof(IntializeTestFileAttribute).GetTypeInfo().Assembly.GetName().Name;

        private static string FindTestProjectRoot()
        {
#if NET452
            var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
#else
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
#endif

            while (currentDirectory != null &&
                !string.Equals(currentDirectory.Name, ThisProjectName, StringComparison.Ordinal))
            {
                currentDirectory = currentDirectory.Parent;
            }

            var normalizedSeparators = currentDirectory.FullName.Replace(Path.DirectorySeparatorChar, '/');
            return currentDirectory.FullName;
        }

#if GENERATE_BASELINES
        private static readonly bool GenerateBaselines = true;
#else
        private static readonly bool GenerateBaselines = false;
#endif

#if !NET452
        private static readonly AsyncLocal<string> _filename = new AsyncLocal<string>();
#endif

        protected static string TestProjectRoot { get; } = FindTestProjectRoot();


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

            var sourceFilename = Path.ChangeExtension(Filename, ".cshtml");
            var testFile = TestFile.Create(sourceFilename);
            if (!testFile.Exists())
            {
                throw new XunitException($"The resource {sourceFilename} was not found.");
            }

            var imports = new List<RazorSourceDocument>();
            while (true)
            {
                var importsFilename = Path.ChangeExtension(Filename + "_Imports" + imports.Count.ToString(), ".cshtml");
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
    }
}
