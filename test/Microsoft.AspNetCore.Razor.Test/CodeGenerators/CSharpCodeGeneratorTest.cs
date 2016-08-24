// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if GENERATE_BASELINES
using System;
#endif
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.CodeGenerators;
using Microsoft.AspNetCore.Razor.Test.Utils;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.CodeGenerators
{
    public class CSharpCodeGeneratorTest
    {
#if GENERATE_BASELINES
        private static readonly string _baselinePathStart = "test/Microsoft.AspNetCore.Razor.Test";
#endif
        private static readonly string _testOutputDirectory = "TestFiles/CodeGenerator/Output/CSharpCodeGenerator";

        [Fact]
        public void ChunkTreeWithUsings()
        {
            // Arrange
            var syntaxTreeNode = new Mock<Span>(new SpanBuilder());
            var language = new CSharpRazorCodeLanguage();
            var host = new CodeGenTestHost(language);
            var codeGeneratorContext = new CodeGeneratorContext(
                host,
                "TestClass",
                "TestNamespace",
                "Foo.cs",
                shouldGenerateLinePragmas: false,
                errorSink: new ErrorSink());
            codeGeneratorContext.ChunkTreeBuilder.AddUsingChunk("FakeNamespace1", syntaxTreeNode.Object);
            codeGeneratorContext.ChunkTreeBuilder.AddUsingChunk("FakeNamespace2.SubNamespace", syntaxTreeNode.Object);
            var codeGenerator = new CodeGenTestCodeGenerator(codeGeneratorContext);

            var path = $"{_testOutputDirectory}/ChunkTreeWithUsings.cs";
            var testFile = TestFile.Create(path);

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

            // Act
            var result = codeGenerator.Generate();

            // Assert
#if GENERATE_BASELINES
            // Update baseline files if files do not already match.
            if (!string.Equals(expectedOutput, result.Code, StringComparison.Ordinal))
            {
                var baselinePath = $"{_baselinePathStart}/{_testOutputDirectory}/ChunkTreeWithUsings.cs";
                BaselineWriter.WriteBaseline( baselinePath, result.Code);
            }
#else
            Assert.Equal(expectedOutput, result.Code);
#endif
        }

        public static TheoryData ModifyOutputData
        {
            get
            {
                var addFileName = "AddGenerateChunkTest.cs";
                var addGenerator = new AddGenerateTestCodeGenerator(CreateContext());
                var addResult = CreateCodeGeneratorResult(addFileName);

                var clearFileName = "ClearGenerateChunkTest.cs";
                var clearGenerator = new ClearGenerateTestCodeGenerator(CreateContext());
                var clearResult = CreateCodeGeneratorResult(clearFileName);

                var defaultFileName = "DefaultGenerateChunkTest.cs";
                var defaultGenerator = new CSharpCodeGenerator(CreateContext());
                var defaultResult = CreateCodeGeneratorResult(defaultFileName);

                var commentFileName = "BuildAfterExecuteContentTest.cs";
                var commentGenerator = new BuildAfterExecuteContentTestCodeGenerator(CreateContext());
                var commentResult = CreateCodeGeneratorResult(commentFileName);

                return new TheoryData<CSharpCodeGenerator, CodeGeneratorResult, string>
                {
                    {addGenerator, addResult, addFileName},
                    {clearGenerator, clearResult, clearFileName },
                    {defaultGenerator, defaultResult, defaultFileName },
                    {commentGenerator, commentResult, commentFileName }
                };
            }
        }
        
        [Theory]
        [MemberData(nameof(ModifyOutputData))]
        public void BuildAfterExecuteContent_ModifyChunks_ModifyOutput(
            CSharpCodeGenerator generator, 
            CodeGeneratorResult expectedResult,
            string fileName)
        {
            // Arrange, Act
            var result = generator.Generate();

            // Assert
#if GENERATE_BASELINES
            // Update baseline files if files do not already match.
            if (!string.Equals(expectedResult.Code, result.Code, StringComparison.Ordinal))
            {
                var baselinePath = $"{_baselinePathStart}/{_testOutputDirectory}/{fileName}";
                BaselineWriter.WriteBaseline( baselinePath, result.Code);
            }
#else
            Assert.Equal(result.Code, expectedResult.Code);
#endif
        }


        private static CodeGeneratorResult CreateCodeGeneratorResult(string fileName)
        {
            var path = $"{_testOutputDirectory}/{fileName}";
            var file = TestFile.Create(path);
            string code;

#if GENERATE_BASELINES
            if (file.Exists())
            {
                code = file.ReadAllText();
            }
            else
            {
                code = null;
            }
#else
            code = file.ReadAllText();
#endif

            var result = new CodeGeneratorResult(code, new List<LineMapping>());
            return result;
        }

        // Returns a context with two literal chunks.
        private static CodeGeneratorContext CreateContext()
        {
            var language = new CSharpRazorCodeLanguage();
            var host = new CodeGenTestHost(language);

            var codeGeneratorContext = new CodeGeneratorContext(
                new ChunkGeneratorContext(
                    host,
                    host.DefaultClassName,
                    host.DefaultNamespace,
                    "",
                    shouldGenerateLinePragmas: false),
                new ErrorSink());

            codeGeneratorContext.ChunkTreeBuilder = new ChunkTreeBuilder();
            var syntaxTreeNode = new Mock<Span>(new SpanBuilder());
            codeGeneratorContext.ChunkTreeBuilder.AddLiteralChunk("hello", syntaxTreeNode.Object);
            codeGeneratorContext.ChunkTreeBuilder.AddStatementChunk("// asdf", syntaxTreeNode.Object);
            codeGeneratorContext.ChunkTreeBuilder.AddLiteralChunk("world", syntaxTreeNode.Object);
            return codeGeneratorContext;
        }

        private class BuildAfterExecuteContentTestCodeGenerator : CSharpCodeGenerator
        {
            public BuildAfterExecuteContentTestCodeGenerator(CodeGeneratorContext context) : base(context)
            {
            }

            protected override void BuildAfterExecuteContent(CSharpCodeWriter writer, IList<Chunk> chunks)
            {
                writer.WriteLine("// test add content.");
            }
        }

        private class AddGenerateTestCodeGenerator : CSharpCodeGenerator
        {
            public AddGenerateTestCodeGenerator(CodeGeneratorContext context) : base(context)
            {
            }

            public override CodeGeneratorResult Generate()
            {
                var firstChunk = Tree.Children.First();
                Tree.Children.Add(firstChunk);
                return base.Generate();
            }
        }

        private class ClearGenerateTestCodeGenerator : CSharpCodeGenerator
        {
            public ClearGenerateTestCodeGenerator(CodeGeneratorContext context) : base(context)
            {
            }

            public override CodeGeneratorResult Generate()
            {
                Tree.Children.Clear();
                return base.Generate();
            }
        }
    }
}