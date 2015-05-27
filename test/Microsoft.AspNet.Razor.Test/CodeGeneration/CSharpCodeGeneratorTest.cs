// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test;
using Microsoft.AspNet.Razor.Test.Generator;
using Microsoft.AspNet.Razor.Test.Utils;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Razor.CodeGeneration
{
    public class CSharpCodeGeneratorTest
    {
        [Fact]
        public void ChunkTreeWithUsings()
        {
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

            // Act
            var result = codeGenerator.Generate();

            BaselineWriter.WriteBaseline(
                @"test\Microsoft.AspNet.Razor.Test\TestFiles\CodeGenerator\Output\CSharpCodeGenerator.cs",
                result.Code);

            var expectedOutput = TestFile.Create("TestFiles/CodeGenerator/Output/CSharpCodeGenerator.cs").ReadAllText();

            // Assert
            Assert.Equal(expectedOutput, result.Code);
        }
    }
}
#endif
