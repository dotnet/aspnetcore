// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.Test.Utils;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.CodeGenerators
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

            var sourceLocation = string.Format("TestFiles/CodeGenerator/Source/{0}.{1}", name, FileExtension);
            var testFile = TestFile
                .Create(string.Format("TestFiles/CodeGenerator/Output/{0}.{1}", baselineName, BaselineExtension));

            string expectedOutput;
#if GENERATE_BASELINES
            if (testFile.Exists())
            {
                expectedOutput = testFile.ReadAllText();
            }
            else
            {
                expectedOutput = null;
            }
#else
            expectedOutput = testFile.ReadAllText();
#endif

            // Set up the host and engine
            var host = CreateHost();
            host.NamespaceImports.Add("System");
            host.DesignTimeMode = designTimeMode;
            host.StaticHelpers = true;
            host.DefaultClassName = name;

            // Add support for templates, etc.
            host.GeneratedClassContext = new GeneratedClassContext(
                GeneratedClassContext.DefaultExecuteMethodName,
                GeneratedClassContext.DefaultWriteMethodName,
                GeneratedClassContext.DefaultWriteLiteralMethodName,
                "WriteTo",
                "WriteLiteralTo",
                "Template",
                "DefineSection",
                "Instrumentation.BeginContext",
                "Instrumentation.EndContext",
                new GeneratedTagHelperContext());
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
                var sourceFileName = generatePragmas ? string.Format("{0}.{1}", name, FileExtension) : null;
                results = engine.GenerateCode(
                    sourceFile,
                    className: name,
                    rootNamespace: TestRootNamespaceName,
                    sourceFileName: sourceFileName);
            }

            var textOutput = results.GeneratedCode;
#if GENERATE_BASELINES
            var outputFile = string.Format(
                @"test\Microsoft.AspNetCore.Razor.Test\TestFiles\CodeGenerator\Output\{0}.{1}",
                baselineName,
                BaselineExtension);

            // Update baseline files if files do not already match.
            if (!string.Equals(expectedOutput, textOutput, StringComparison.Ordinal))
            {
                BaselineWriter.WriteBaseline(outputFile, textOutput);
            }
#else
            if (onResults != null)
            {
                onResults(results);
            }

            // Verify code against baseline
            Assert.Equal(expectedOutput, textOutput);
#endif

            var generatedSpans = results.Document.Flatten();

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
                    Assert.NotNull(results.DesignTimeLineMappings); // Guard
#if GENERATE_BASELINES
                    if (expectedDesignTimePragmas == null ||
                        !Enumerable.SequenceEqual(expectedDesignTimePragmas, results.DesignTimeLineMappings))
                    {
                        var lineMappingFile = Path.ChangeExtension(outputFile, "lineMappings.cs");
                        var lineMappingCode = GetDesignTimeLineMappingsCode(results.DesignTimeLineMappings);
                        BaselineWriter.WriteBaseline(lineMappingFile, lineMappingCode);
                    }
#else
                    for (var i = 0; i < expectedDesignTimePragmas.Count && i < results.DesignTimeLineMappings.Count; i++)
                    {
                        Assert.Equal(expectedDesignTimePragmas[i], results.DesignTimeLineMappings[i]);
                    }

                    Assert.Equal(expectedDesignTimePragmas.Count, results.DesignTimeLineMappings.Count);
#endif
                }
            }
        }

        private static string GetDesignTimeLineMappingsCode(IList<LineMapping> designTimeLineMappings)
        {
            var lineMappings = new StringBuilder();
            lineMappings.AppendLine($"// !!! Do not check in. Instead paste content into test method. !!!");
            lineMappings.AppendLine();

            var indent = "            ";
            lineMappings.AppendLine($"{ indent }var expectedLineMappings = new[]");
            lineMappings.AppendLine($"{ indent }{{");
            foreach (var lineMapping in designTimeLineMappings)
            {
                var innerIndent = indent + "    ";
                var documentLocation = lineMapping.DocumentLocation;
                var generatedLocation = lineMapping.GeneratedLocation;
                lineMappings.AppendLine($"{ innerIndent }BuildLineMapping(");

                innerIndent += "    ";
                lineMappings.AppendLine($"{ innerIndent }documentAbsoluteIndex: { documentLocation.AbsoluteIndex },");
                lineMappings.AppendLine($"{ innerIndent }documentLineIndex: { documentLocation.LineIndex },");
                if (documentLocation.CharacterIndex != generatedLocation.CharacterIndex)
                {
                    lineMappings.AppendLine($"{ innerIndent }documentCharacterOffsetIndex: { documentLocation.CharacterIndex },");
                }

                lineMappings.AppendLine($"{ innerIndent }generatedAbsoluteIndex: { generatedLocation.AbsoluteIndex },");
                lineMappings.AppendLine($"{ innerIndent }generatedLineIndex: { generatedLocation.LineIndex },");
                if (documentLocation.CharacterIndex != generatedLocation.CharacterIndex)
                {
                    lineMappings.AppendLine($"{ innerIndent }generatedCharacterOffsetIndex: { generatedLocation.CharacterIndex },");
                }
                else
                {
                    lineMappings.AppendLine($"{ innerIndent }characterOffsetIndex: { generatedLocation.CharacterIndex },");
                }

                lineMappings.AppendLine($"{ innerIndent }contentLength: { generatedLocation.ContentLength }),");
            }

            lineMappings.AppendLine($"{ indent }}};");

            return lineMappings.ToString();
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
