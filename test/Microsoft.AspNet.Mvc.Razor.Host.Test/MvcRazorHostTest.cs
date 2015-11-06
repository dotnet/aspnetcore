// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if GENERATE_BASELINES
using System.IO;
using System.Linq;
#endif
using System.Reflection;
#if GENERATE_BASELINES
using System.Text;
#endif
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Mvc.Razor.Internal;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.CodeGenerators.Visitors;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHostTest
    {
        private static Assembly _assembly = typeof(MvcRazorHostTest).Assembly;

        public static TheoryData NormalizeChunkInheritanceUtilityPaths_Data
        {
            get
            {
                var data = new TheoryData<string> { "//" };

                // The following scenarios are not relevant in Mono.
                if (!TestPlatformHelper.IsMono)
                {
                    data.Add("C:/");
                    data.Add(@"\\");
                    data.Add(@"C:\");
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(NormalizeChunkInheritanceUtilityPaths_Data))]
        public void DecorateRazorParser_DesignTimeRazorPathNormalizer_NormalizesChunkInheritanceUtilityPaths(
            string rootPrefix)
        {
            // Arrange
            var rootedAppPath = $"{rootPrefix}SomeComputer/Location/Project/";
            var rootedFilePath = $"{rootPrefix}SomeComputer/Location/Project/src/file.cshtml";
            var chunkTreeCache = new DefaultChunkTreeCache(new TestFileProvider());
            var host = new MvcRazorHost(
                chunkTreeCache,
                pathNormalizer: new DesignTimeRazorPathNormalizer(rootedAppPath));
            var parser = new RazorParser(
                host.CodeLanguage.CreateCodeParser(),
                host.CreateMarkupParser(),
                tagHelperDescriptorResolver: null);
            var chunkInheritanceUtility = new PathValidatingChunkInheritanceUtility(host, chunkTreeCache);
            host.ChunkInheritanceUtility = chunkInheritanceUtility;

            // Act
            host.DecorateRazorParser(parser, rootedFilePath);

            // Assert
            Assert.Equal("src/file.cshtml", chunkInheritanceUtility.InheritedChunkTreePagePath, StringComparer.Ordinal);
        }

        [Theory]
        [MemberData(nameof(NormalizeChunkInheritanceUtilityPaths_Data))]
        public void DecorateCodeGenerator_DesignTimeRazorPathNormalizer_NormalizesChunkInheritanceUtilityPaths(
            string rootPrefix)
        {
            // Arrange
            var rootedAppPath = $"{rootPrefix}SomeComputer/Location/Project/";
            var rootedFilePath = $"{rootPrefix}SomeComputer/Location/Project/src/file.cshtml";
            var chunkTreeCache = new DefaultChunkTreeCache(new TestFileProvider());
            var host = new MvcRazorHost(
                chunkTreeCache,
                pathNormalizer: new DesignTimeRazorPathNormalizer(rootedAppPath));
            var chunkInheritanceUtility = new PathValidatingChunkInheritanceUtility(host, chunkTreeCache);
            var codeGeneratorContext = new CodeGeneratorContext(
                new ChunkGeneratorContext(
                    host,
                    host.DefaultClassName,
                    host.DefaultNamespace,
                    rootedFilePath,
                    shouldGenerateLinePragmas: true),
                new ErrorSink());
            var codeGenerator = new CSharpCodeGenerator(codeGeneratorContext);
            host.ChunkInheritanceUtility = chunkInheritanceUtility;

            // Act
            host.DecorateCodeGenerator(codeGenerator, codeGeneratorContext);

            // Assert
            Assert.Equal("src/file.cshtml", chunkInheritanceUtility.InheritedChunkTreePagePath, StringComparer.Ordinal);
        }

        [Fact]
        public void MvcRazorHost_EnablesInstrumentationByDefault()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHost(new DefaultChunkTreeCache(fileProvider));

            // Act
            var instrumented = host.EnableInstrumentation;

            // Assert
            Assert.True(instrumented);
        }

        [Fact]
        public void MvcRazorHost_GeneratesTagHelperModelExpressionCode_DesignTime()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHostWithNormalizedNewLine(new DefaultChunkTreeCache(fileProvider))
            {
                DesignTimeMode = true
            };
            var expectedLineMappings = new[]
            {
                BuildLineMapping(
                    documentAbsoluteIndex: 33,
                    documentLineIndex: 2,
                    documentCharacterIndex: 14,
                    generatedAbsoluteIndex: 590,
                    generatedLineIndex: 16,
                    generatedCharacterIndex: 48,
                    contentLength: 83),
                BuildLineMapping(
                    documentAbsoluteIndex: 7,
                    documentLineIndex: 0,
                    documentCharacterIndex: 7,
                    generatedAbsoluteIndex: 764,
                    generatedLineIndex: 18,
                    generatedCharacterIndex: 28,
                    contentLength: 8),
                BuildLineMapping(
                    documentAbsoluteIndex: 137,
                    documentLineIndex: 4,
                    documentCharacterIndex: 17,
                    generatedAbsoluteIndex: 2180,
                    generatedLineIndex: 47,
                    generatedCharacterIndex: 95,
                    contentLength: 3),
                BuildLineMapping(
                    documentAbsoluteIndex: 164,
                    documentLineIndex: 5,
                    documentCharacterIndex: 18,
                    generatedAbsoluteIndex: 2501,
                    generatedLineIndex: 53,
                    generatedCharacterIndex: 87,
                    contentLength: 5),
            };

            // Act and Assert
            RunDesignTimeTest(
                host,
                testName: "ModelExpressionTagHelper",
                expectedLineMappings: expectedLineMappings);
        }

        [Theory]
        [InlineData("Basic")]
        [InlineData("_ViewImports")]
        [InlineData("Inject")]
        [InlineData("InjectWithModel")]
        [InlineData("InjectWithSemicolon")]
        [InlineData("Model")]
        [InlineData("ModelExpressionTagHelper")]
        public void MvcRazorHost_ParsesAndGeneratesCodeForBasicScenarios(string scenarioName)
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new TestMvcRazorHost(new DefaultChunkTreeCache(fileProvider));

            // Act and Assert
            RunRuntimeTest(host, scenarioName);
        }

        [Fact]
        public void BasicVisitor_GeneratesCorrectLineMappings()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHostWithNormalizedNewLine(new DefaultChunkTreeCache(fileProvider))
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new[]
            {
                BuildLineMapping(
                    documentAbsoluteIndex: 13,
                    documentLineIndex: 0,
                    documentCharacterIndex: 13,
                    generatedAbsoluteIndex: 1269,
                    generatedLineIndex: 32,
                    generatedCharacterIndex: 13,
                    contentLength: 4),
                BuildLineMapping(
                    documentAbsoluteIndex: 43,
                    documentLineIndex: 2,
                    documentCharacterIndex: 5,
                    generatedAbsoluteIndex: 1353,
                    generatedLineIndex: 37,
                    generatedCharacterIndex: 6,
                    contentLength: 21),
            };

            // Act and Assert
            RunDesignTimeTest(host, "Basic", expectedLineMappings);
        }

        [Fact]
        public void MvcRazorHost_GeneratesCorrectLineMappingsAndUsingStatementsForViewImports()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHostWithNormalizedNewLine(new DefaultChunkTreeCache(fileProvider))
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new[]
            {
                BuildLineMapping(
                    documentAbsoluteIndex: 8,
                    documentLineIndex: 0,
                    documentCharacterIndex: 8,
                    generatedAbsoluteIndex: 661,
                    generatedLineIndex: 21,
                    generatedCharacterIndex: 8,
                    contentLength: 26),
            };

            // Act and Assert
            RunDesignTimeTest(host, "_ViewImports", expectedLineMappings);
        }

        [Fact]
        public void InjectVisitor_GeneratesCorrectLineMappings()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHostWithNormalizedNewLine(new DefaultChunkTreeCache(fileProvider))
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new[]
            {
                BuildLineMapping(
                    documentAbsoluteIndex: 1,
                    documentLineIndex: 0,
                    documentCharacterIndex: 1,
                    generatedAbsoluteIndex: 59,
                    generatedLineIndex: 3,
                    generatedCharacterIndex: 0,
                    contentLength: 17),
                BuildLineMapping(
                    documentAbsoluteIndex: 28,
                    documentLineIndex: 1,
                    documentCharacterIndex: 8,
                    generatedAbsoluteIndex: 706,
                    generatedLineIndex: 26,
                    generatedCharacterIndex: 8,
                    contentLength: 20),
            };

            // Act and Assert
            RunDesignTimeTest(host, "Inject", expectedLineMappings);
        }

        [Fact]
        public void InjectVisitorWithModel_GeneratesCorrectLineMappings()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHostWithNormalizedNewLine(new DefaultChunkTreeCache(fileProvider))
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new[]
            {
                BuildLineMapping(
                    documentAbsoluteIndex: 7,
                    documentLineIndex: 0,
                    documentCharacterIndex: 7,
                    generatedAbsoluteIndex: 391,
                    generatedLineIndex: 11,
                    generatedCharacterIndex: 28,
                    contentLength: 7),
                BuildLineMapping(
                    documentAbsoluteIndex: 24,
                    documentLineIndex: 1,
                    documentCharacterIndex: 8,
                    generatedAbsoluteIndex: 755,
                    generatedLineIndex: 25,
                    generatedCharacterIndex: 8,
                    contentLength: 20),
                BuildLineMapping(
                    documentAbsoluteIndex: 54,
                    documentLineIndex: 2,
                    documentCharacterIndex: 8,
                    generatedAbsoluteIndex: 981,
                    generatedLineIndex: 33,
                    generatedCharacterIndex: 8,
                    contentLength: 23),
            };

            // Act and Assert
            RunDesignTimeTest(host, "InjectWithModel", expectedLineMappings);
        }

        [Fact]
        public void InjectVisitorWithSemicolon_GeneratesCorrectLineMappings()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHostWithNormalizedNewLine(new DefaultChunkTreeCache(fileProvider))
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new[]
            {
                BuildLineMapping(
                    documentAbsoluteIndex: 7,
                    documentLineIndex: 0,
                    documentCharacterIndex: 7,
                    generatedAbsoluteIndex: 399,
                    generatedLineIndex: 11,
                    generatedCharacterIndex: 28,
                    contentLength: 7),
                BuildLineMapping(
                    documentAbsoluteIndex: 24,
                    documentLineIndex: 1,
                    documentCharacterIndex: 8,
                    generatedAbsoluteIndex: 771,
                    generatedLineIndex: 25,
                    generatedCharacterIndex: 8,
                    contentLength: 20),
                BuildLineMapping(
                    documentAbsoluteIndex: 58,
                    documentLineIndex: 2,
                    documentCharacterIndex: 8,
                    generatedAbsoluteIndex: 1001,
                    generatedLineIndex: 33,
                    generatedCharacterIndex: 8,
                    contentLength: 23),
                BuildLineMapping(
                    documentAbsoluteIndex: 93,
                    documentLineIndex: 3,
                    documentCharacterIndex: 8,
                    generatedAbsoluteIndex: 1234,
                    generatedLineIndex: 41,
                    generatedCharacterIndex: 8,
                    contentLength: 21),
                BuildLineMapping(
                    documentAbsoluteIndex: 129,
                    documentLineIndex: 4,
                    documentCharacterIndex: 8,
                    generatedAbsoluteIndex: 1465,
                    generatedLineIndex: 49,
                    generatedCharacterIndex: 8,
                    contentLength: 24),
            };

            // Act and Assert
            RunDesignTimeTest(host, "InjectWithSemicolon", expectedLineMappings);
        }

        [Fact]
        public void ModelVisitor_GeneratesCorrectLineMappings()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHostWithNormalizedNewLine(new DefaultChunkTreeCache(fileProvider))
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var expectedLineMappings = new[]
            {
                BuildLineMapping(
                    documentAbsoluteIndex: 7,
                    documentLineIndex: 0,
                    documentCharacterIndex: 7,
                    generatedAbsoluteIndex: 394,
                    generatedLineIndex: 11,
                    generatedCharacterIndex: 28,
                    contentLength: 30),
            };

            // Act and Assert
            RunDesignTimeTest(host, "Model", expectedLineMappings);
        }

        [Fact]
        public void ModelVisitor_GeneratesLineMappingsForLastModel_WhenMultipleModelsArePresent()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var host = new MvcRazorHostWithNormalizedNewLine(new DefaultChunkTreeCache(fileProvider))
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var inputFile = "TestFiles/Input/MultipleModels.cshtml";
            var outputFile = "TestFiles/Output/DesignTime/MultipleModels.cs";
            var expectedCode = ResourceFile.ReadResource(_assembly, outputFile, sourceFile: false);

            // Act
            GeneratorResults results;
            using (var stream = ResourceFile.GetResourceStream(_assembly, inputFile, sourceFile: true))
            {
                results = host.GenerateCode(inputFile, stream);
            }

            // Assert
            Assert.False(results.Success);
            var parserError = Assert.Single(results.ParserErrors);
            Assert.Equal("Only one 'model' statement is allowed in a file.", parserError.Message);
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedCode, results.GeneratedCode);
#else
            Assert.Equal(expectedCode, results.GeneratedCode, ignoreLineEndingDifferences: true);
#endif
        }

        private static void RunRuntimeTest(
            MvcRazorHost host,
            string testName)
        {
            var inputFile = "TestFiles/Input/" + testName + ".cshtml";
            var outputFile = "TestFiles/Output/Runtime/" + testName + ".cs";
            var expectedCode = ResourceFile.ReadResource(_assembly, outputFile, sourceFile: false);

            // Act
            GeneratorResults results;
            using (var stream = ResourceFile.GetResourceStream(_assembly, inputFile, sourceFile: true))
            {
                results = host.GenerateCode(inputFile, stream);
            }

            // Assert
            Assert.True(results.Success);
            Assert.Empty(results.ParserErrors);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedCode, results.GeneratedCode);
#else
            Assert.Equal(expectedCode, results.GeneratedCode, ignoreLineEndingDifferences: true);
#endif
        }

        private static void RunDesignTimeTest(
            MvcRazorHost host,
            string testName,
            IEnumerable<LineMapping> expectedLineMappings)
        {
            var inputFile = "TestFiles/Input/" + testName + ".cshtml";
            var outputFile = "TestFiles/Output/DesignTime/" + testName + ".cs";
            var expectedCode = ResourceFile.ReadResource(_assembly, outputFile, sourceFile: false);

            // Act
            GeneratorResults results;
            using (var stream = ResourceFile.GetResourceStream(_assembly, inputFile, sourceFile: true))
            {
                // VS tooling passes in paths in all lower case. We'll mimic this behavior in our tests.
                results = host.GenerateCode(inputFile.ToLowerInvariant(), stream);
            }

            // Assert
            Assert.True(results.Success);
            Assert.Empty(results.ParserErrors);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedCode, results.GeneratedCode);

            Assert.NotNull(results.DesignTimeLineMappings); // Guard
            if (expectedLineMappings == null ||
                !Enumerable.SequenceEqual(expectedLineMappings, results.DesignTimeLineMappings))
            {
                var lineMappings = new StringBuilder();
                lineMappings.AppendLine($"// !!! Do not check in. Instead paste content into test method. !!!");
                lineMappings.AppendLine();

                var indent = "            ";
                lineMappings.AppendLine($"{ indent }var expectedLineMappings = new[]");
                lineMappings.AppendLine($"{ indent }{{");
                foreach (var lineMapping in results.DesignTimeLineMappings)
                {
                    var innerIndent = indent + "    ";
                    var documentLocation = lineMapping.DocumentLocation;
                    var generatedLocation = lineMapping.GeneratedLocation;
                    lineMappings.AppendLine($"{ innerIndent }{ nameof(BuildLineMapping) }(");

                    innerIndent += "    ";
                    lineMappings.AppendLine($"{ innerIndent }documentAbsoluteIndex: { documentLocation.AbsoluteIndex },");
                    lineMappings.AppendLine($"{ innerIndent }documentLineIndex: { documentLocation.LineIndex },");
                    lineMappings.AppendLine($"{ innerIndent }documentCharacterIndex: { documentLocation.CharacterIndex },");
                    lineMappings.AppendLine($"{ innerIndent }generatedAbsoluteIndex: { generatedLocation.AbsoluteIndex },");
                    lineMappings.AppendLine($"{ innerIndent }generatedLineIndex: { generatedLocation.LineIndex },");
                    lineMappings.AppendLine($"{ innerIndent }generatedCharacterIndex: { generatedLocation.CharacterIndex },");
                    lineMappings.AppendLine($"{ innerIndent }contentLength: { generatedLocation.ContentLength }),");
                }

                lineMappings.AppendLine($"{ indent }}};");

                var lineMappingFile = Path.ChangeExtension(outputFile, "lineMappings.cs");
                ResourceFile.UpdateFile(_assembly, lineMappingFile, previousContent: null, content: lineMappings.ToString());
            }
#else
            Assert.Equal(expectedCode, results.GeneratedCode, ignoreLineEndingDifferences: true);
            Assert.Equal(expectedLineMappings, results.DesignTimeLineMappings);
#endif
        }

        private static LineMapping BuildLineMapping(
            int documentAbsoluteIndex,
            int documentLineIndex,
            int documentCharacterIndex,
            int generatedAbsoluteIndex,
            int generatedLineIndex,
            int generatedCharacterIndex,
            int contentLength)
        {
            var documentLocation = new SourceLocation(documentAbsoluteIndex,
                                                      documentLineIndex,
                                                      documentCharacterIndex);
            var generatedLocation = new SourceLocation(generatedAbsoluteIndex,
                                                       generatedLineIndex,
                                                       generatedCharacterIndex);

            return new LineMapping(
                documentLocation: new MappingLocation(documentLocation, contentLength),
                generatedLocation: new MappingLocation(generatedLocation, contentLength));
        }

        private class PathValidatingChunkInheritanceUtility : ChunkInheritanceUtility
        {
            public PathValidatingChunkInheritanceUtility(MvcRazorHost razorHost, IChunkTreeCache chunkTreeCache)
                : base(razorHost, chunkTreeCache, defaultInheritedChunks: new Chunk[0])
            {
            }

            public string InheritedChunkTreePagePath { get; private set; }

            public override IReadOnlyList<ChunkTreeResult> GetInheritedChunkTreeResults(string pagePath)
            {
                InheritedChunkTreePagePath = pagePath;

                return new ChunkTreeResult[0];
            }
        }

        // Normalizes the newlines in different OS platforms.
        private class MvcRazorHostWithNormalizedNewLine : MvcRazorHost
        {
            public MvcRazorHostWithNormalizedNewLine(IChunkTreeCache codeTreeCache)
                : base(codeTreeCache)
            { }

            public override CodeGenerator DecorateCodeGenerator(
                CodeGenerator incomingBuilder,
                CodeGeneratorContext context)
            {
                base.DecorateCodeGenerator(incomingBuilder, context);

                return new TestCSharpCodeGenerator(
                    context,
                    DefaultModel,
                    "Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute",
                    new GeneratedTagHelperAttributeContext
                    {
                        ModelExpressionTypeName = ModelExpressionType,
                        CreateModelExpressionMethodName = CreateModelExpressionMethod
                    });
            }

            protected class TestCSharpCodeGenerator : MvcCSharpCodeGenerator
            {
                private readonly GeneratedTagHelperAttributeContext _tagHelperAttributeContext;

                public TestCSharpCodeGenerator(
                    CodeGeneratorContext context,
                    string defaultModel,
                    string activateAttribute,
                    GeneratedTagHelperAttributeContext tagHelperAttributeContext)
                    : base(context, defaultModel, activateAttribute, tagHelperAttributeContext)
                {
                    _tagHelperAttributeContext = tagHelperAttributeContext;
                }

                protected override CSharpCodeWriter CreateCodeWriter()
                {
                    // We normalize newlines so no matter what platform we're on
                    // they're consistent (for code generation tests).
                    var codeWriter = base.CreateCodeWriter();
                    codeWriter.NewLine = "\r\n";

                    return codeWriter;
                }
            }
        }

        /// <summary>
        /// Used when testing Tag Helpers, it disables the unique ID generation feature.
        /// </summary>
        private class TestMvcRazorHost : MvcRazorHost
        {
            public TestMvcRazorHost(IChunkTreeCache ChunkTreeCache)
                : base(ChunkTreeCache)
            {
            }

            public override CodeGenerator DecorateCodeGenerator(
                CodeGenerator incomingBuilder,
                CodeGeneratorContext context)
            {
                base.DecorateCodeGenerator(incomingBuilder, context);

                return new TestCSharpCodeGenerator(
                    context,
                    DefaultModel,
                    "Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute",
                    new GeneratedTagHelperAttributeContext
                    {
                        ModelExpressionTypeName = ModelExpressionType,
                        CreateModelExpressionMethodName = CreateModelExpressionMethod
                    });
            }

            protected class TestCSharpCodeGenerator : MvcCSharpCodeGenerator
            {
                private readonly GeneratedTagHelperAttributeContext _tagHelperAttributeContext;

                public TestCSharpCodeGenerator(
                    CodeGeneratorContext context,
                    string defaultModel,
                    string activateAttribute,
                    GeneratedTagHelperAttributeContext tagHelperAttributeContext)
                    : base(context, defaultModel, activateAttribute, tagHelperAttributeContext)
                {
                    _tagHelperAttributeContext = tagHelperAttributeContext;
                }

                protected override CSharpCodeVisitor CreateCSharpCodeVisitor(
                    CSharpCodeWriter writer,
                    CodeGeneratorContext context)
                {
                    var visitor = base.CreateCSharpCodeVisitor(writer, context);
                    visitor.TagHelperRenderer = new NoUniqueIdsTagHelperCodeRenderer(visitor, writer, context)
                    {
                        AttributeValueCodeRenderer =
                            new MvcTagHelperAttributeValueCodeRenderer(_tagHelperAttributeContext)
                    };
                    return visitor;
                }

                private class NoUniqueIdsTagHelperCodeRenderer : CSharpTagHelperCodeRenderer
                {
                    public NoUniqueIdsTagHelperCodeRenderer(
                        IChunkVisitor bodyVisitor,
                        CSharpCodeWriter writer,
                        CodeGeneratorContext context)
                        : base(bodyVisitor, writer, context)
                    {
                    }

                    protected override string GenerateUniqueId()
                    {
                        return "test";
                    }
                }
            }
        }
    }
}