// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
#if NET451
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
        private static readonly string TestProjectRoot = FindTestProjectRoot();

        private static string FindTestProjectRoot()
        {
            var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

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

#if !NET451
        private static readonly AsyncLocal<string> _filename = new AsyncLocal<string>();
#endif

        // Used by the test framework to set the 'base' name for test files.
        public static string Filename
        {
#if NET451
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

        protected RazorCodeDocument CreateCodeDocument()
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

            return RazorCodeDocument.Create(TestRazorSourceDocument.CreateResource(sourceFilename));
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
    }
}
