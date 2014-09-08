// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

//#define GENERATE_BASELINES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.WebPages.TestUtils;
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
            return new RazorEngineHost(new TLanguage());
        }

        protected void RunTest(string name,
                               string baselineName = null,
                               bool generatePragmas = true,
                               bool designTimeMode = false,
                               IList<LineMapping> expectedDesignTimePragmas = null,
                               TestSpan[] spans = null,
                               TabTest tabTest = TabTest.Both,
                               Action<RazorEngineHost> hostConfig = null,
                               Action<GeneratorResults> onResults = null)
        {
            bool testRun = false;

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
                        onResults: onResults);
                }

                testRun = true;
            }

            Assert.True(testRun, "No test was run because TabTest is not set correctly");
        }

        private void RunTestInternal(string name,
                               string baselineName,
                               bool generatePragmas,
                               bool designTimeMode,
                               IList<LineMapping> expectedDesignTimePragmas,
                               TestSpan[] spans,
                               bool withTabs,
                               Action<RazorEngineHost> hostConfig,
                                Action<GeneratorResults> onResults = null)
        {
            // Load the test files
            if (baselineName == null)
            {
                baselineName = name;
            }

            string sourceLocation = string.Format("/CodeGenerator/{1}/Source/{0}.{2}", name, LanguageName, FileExtension);
            string source = TestFile.Create(string.Format("TestFiles/CodeGenerator/CS/Source/{0}.{1}", name, FileExtension)).ReadAllText();
            string expectedOutput = TestFile.Create(string.Format("TestFiles/CodeGenerator/CS/Output/{0}.{1}", baselineName, BaselineExtension)).ReadAllText();

            // Set up the host and engine
            RazorEngineHost host = CreateHost();
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
                                                                   "Instrumentation.EndContext")
                                                                   {
                                                                       LayoutPropertyName = "Layout",
                                                                       ResolveUrlMethodName = "Href"
                                                                   };
            if (hostConfig != null)
            {
                hostConfig(host);
            }

            host.IsIndentingWithTabs = withTabs;
            host.EnableInstrumentation = true;

            RazorTemplateEngine engine = new RazorTemplateEngine(host);

            // Generate code for the file
            GeneratorResults results = null;
            using (StringTextBuffer buffer = new StringTextBuffer(source))
            {
                results = engine.GenerateCode(buffer, className: name, rootNamespace: TestRootNamespaceName, sourceFileName: generatePragmas ? String.Format("{0}.{1}", name, FileExtension) : null);
            }

            // Only called if GENERATE_BASELINES is set, otherwise compiled out.
            BaselineWriter.WriteBaseline(String.Format(@"test\Microsoft.AspNet.Razor.Test\TestFiles\CodeGenerator\{0}\Output\{1}.{2}", LanguageName, baselineName, BaselineExtension), results.GeneratedCode);

#if !GENERATE_BASELINES
            string textOutput = results.GeneratedCode;

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
                    Assert.True(results.DesignTimeLineMappings != null && results.DesignTimeLineMappings.Count > 0);

                    Assert.Equal(expectedDesignTimePragmas.Count, results.DesignTimeLineMappings.Count);

                    for (var i = 0; i < expectedDesignTimePragmas.Count; i++)
                    {
                        if(!expectedDesignTimePragmas[i].Equals(results.DesignTimeLineMappings[i]))
                        {
                            Assert.True(false, String.Format("Line mapping {0} is not equivalent.", i));
                        }
                    }
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
