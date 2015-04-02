// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Utils;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public abstract class RazorCodeGeneratorTest<TLanguage>
        where TLanguage : RazorCodeLanguage, new()
    {
        protected static readonly string TestRootNamespaceName = "TestOutput";

        protected abstract string FileExtension { get; }
        protected abstract string LanguageName { get; }
        protected abstract string BaselineExtension { get; }

        protected RazorEngineHost CreateHost()
        {
            return new CodeGenTestHost(new TLanguage());
        }

        protected void RunTest(
            string name,
            string baselineName = null,
            bool generatePragmas = true,
            bool designTimeMode = false,
            IList<LineMapping> expectedDesignTimePragmas = null,
            TestSpan[] spans = null,
            TabTest tabTest = TabTest.Both,
            Func<RazorEngineHost, RazorEngineHost> hostConfig = null,
            Func<RazorTemplateEngine, RazorTemplateEngine> templateEngineConfig = null,
            Action<GeneratorResults> onResults = null)
        {
            var testRun = false;

            if ((tabTest & TabTest.Tabs) == TabTest.Tabs)
            {
                using (new CultureReplacer())
                {
                    RunTestInternal(
                        name: name,
                        baselineName: baselineName,
                        generatePragmas: generatePragmas,
                        designTimeMode: designTimeMode,
                        expectedDesignTimePragmas: expectedDesignTimePragmas,
                        spans: spans,
                        withTabs: true,
                        hostConfig: hostConfig,
                        templateEngineConfig: templateEngineConfig,
                        onResults: onResults);
                }

                testRun = true;
            }

            if ((tabTest & TabTest.NoTabs) == TabTest.NoTabs)
            {
                using (new CultureReplacer())
                {
                    RunTestInternal(
                        name: name,
                        baselineName: baselineName,
                        generatePragmas: generatePragmas,
                        designTimeMode: designTimeMode,
                        expectedDesignTimePragmas: expectedDesignTimePragmas,
                        spans: spans,
                        withTabs: false,
                        hostConfig: hostConfig,
                        templateEngineConfig: templateEngineConfig,
                        onResults: onResults);
                }

                testRun = true;
            }

            Assert.True(testRun, "No test was run because TabTest is not set correctly");
        }

        private Stream NormalizeNewLines(Stream inputStream)
        {
            if (!inputStream.CanSeek)
            {
                var memoryStream = new MemoryStream();
                inputStream.CopyTo(memoryStream);

                // We don't have to dispose the input stream since it is owned externally.
                inputStream = memoryStream;
            }

            inputStream.Position = 0;
            var reader = new StreamReader(inputStream);

            // Normalize newlines to be \r\n. This is to ensure when running tests cross plat the final test output 
            // is compared against test files in a normalized fashion.
            var fileContents = reader.ReadToEnd().Replace(Environment.NewLine, "\r\n");

            // Since this is a test we can normalize to utf8.
            inputStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContents));

            return inputStream;
        }

        private void RunTestInternal(
            string name,
            string baselineName,
            bool generatePragmas,
            bool designTimeMode,
            IList<LineMapping> expectedDesignTimePragmas,
            TestSpan[] spans,
            bool withTabs,
            Func<RazorEngineHost, RazorEngineHost> hostConfig,
            Func<RazorTemplateEngine, RazorTemplateEngine> templateEngineConfig,
            Action<GeneratorResults> onResults = null)
        {
            // Load the test files
            if (baselineName == null)
            {
                baselineName = name;
            }

            var sourceLocation = string.Format("TestFiles/CodeGenerator/{1}/Source/{0}.{2}", name, LanguageName, FileExtension);
            var expectedOutput = TestFile.Create(string.Format("TestFiles/CodeGenerator/CS/Output/{0}.{1}", baselineName, BaselineExtension)).ReadAllText();

            // Set up the host and engine
            var host = CreateHost();
            host.NamespaceImports.Add("System");
            host.DesignTimeMode = designTimeMode;
            host.StaticHelpers = true;
            host.DefaultClassName = name;

            // Add support for templates, etc.
            host.GeneratedClassContext = new GeneratedClassContext(GeneratedClassContext.DefaultExecuteMethodName,
                                                                   GeneratedClassContext.DefaultWriteMethodName,
                                                                   GeneratedClassContext.DefaultWriteLiteralMethodName,
                                                                   "WriteTo",
                                                                   "WriteLiteralTo",
                                                                   "Template",
                                                                   "DefineSection",
                                                                   "Instrumentation.BeginContext",
                                                                   "Instrumentation.EndContext",
                                                                   new GeneratedTagHelperContext())
            {
                LayoutPropertyName = "Layout",
                ResolveUrlMethodName = "Href"
            };
            if (hostConfig != null)
            {
                host = hostConfig(host);
            }

            host.IsIndentingWithTabs = withTabs;
            host.EnableInstrumentation = true;

            var engine = new RazorTemplateEngine(host);

            if (templateEngineConfig != null)
            {
                engine = templateEngineConfig(engine);
            }

            // Generate code for the file
            GeneratorResults results = null;
            using (var source = TestFile.Create(sourceLocation).OpenRead())
            {
                var sourceFile = NormalizeNewLines(source);
                var sourceFileName = generatePragmas ? String.Format("{0}.{1}", name, FileExtension) : null;
                results = engine.GenerateCode(sourceFile, className: name, rootNamespace: TestRootNamespaceName, sourceFileName: sourceFileName);
            }
            // Only called if GENERATE_BASELINES is set, otherwise compiled out.
            BaselineWriter.WriteBaseline(String.Format(@"test\Microsoft.AspNet.Razor.Test\TestFiles\CodeGenerator\{0}\Output\{1}.{2}", LanguageName, baselineName, BaselineExtension), results.GeneratedCode);

#if !GENERATE_BASELINES
            var textOutput = results.GeneratedCode;

            if (onResults != null)
            {
                onResults(results);
            }

            //// Verify code against baseline
            Assert.Equal(expectedOutput, textOutput);
#endif

            IEnumerable<Span> generatedSpans = results.Document.Flatten();

            foreach (var span in generatedSpans)
            {
                VerifyNoBrokenEndOfLines(span.Content);
            }

            // Verify design-time pragmas
            if (designTimeMode)
            {
                if (spans != null)
                {
                    Assert.Equal(spans, generatedSpans.Select(span => new TestSpan(span)).ToArray());
                }

                if (expectedDesignTimePragmas != null)
                {
                    Assert.True(results.DesignTimeLineMappings != null); // Guard
                    for (var i = 0; i < expectedDesignTimePragmas.Count && i < results.DesignTimeLineMappings.Count; i++)
                    {
                        Assert.Equal(expectedDesignTimePragmas[i], results.DesignTimeLineMappings[i]);
                    }

                    Assert.Equal(expectedDesignTimePragmas.Count, results.DesignTimeLineMappings.Count);
                }
            }
        }

        private void VerifyNoBrokenEndOfLines(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r')
                {
                    Assert.True(text.Length > i + 1);
                    Assert.Equal('\n', text[i + 1]);
                }
                else if (text[i] == '\n')
                {
                    Assert.True(i > 0);
                    Assert.Equal('\r', text[i - 1]);
                }
            }
        }
    }
}
