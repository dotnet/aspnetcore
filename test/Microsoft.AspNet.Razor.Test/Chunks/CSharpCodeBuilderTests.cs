// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using Microsoft.AspNet.Razor.CodeGeneration;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test;
using Microsoft.AspNet.Razor.Test.Generator;
using Microsoft.AspNet.Razor.Test.Utils;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Razor.Chunks
{
    public class CSharpCodeBuilderTests
    {
        [Fact]
        public void ChunkTreeWithUsings()
        {
            var syntaxTreeNode = new Mock<Span>(new SpanBuilder());
            var language = new CSharpRazorCodeLanguage();
            var host = new CodeGenTestHost(language);
            var codeBuilderContext = new CodeBuilderContext(
                host,
                "TestClass",
                "TestNamespace",
                "Foo.cs",
                shouldGenerateLinePragmas: false,
                errorSink: new ErrorSink());
            codeBuilderContext.ChunkTreeBuilder.AddUsingChunk("FakeNamespace1", syntaxTreeNode.Object);
            codeBuilderContext.ChunkTreeBuilder.AddUsingChunk("FakeNamespace2.SubNamespace", syntaxTreeNode.Object);
            var codeBuilder = new CodeGenTestCodeBuilder(codeBuilderContext);

            // Act
            var result = codeBuilder.Build();

            BaselineWriter.WriteBaseline(
                @"test\Microsoft.AspNet.Razor.Test\TestFiles\CodeGenerator\Output\CSharpCodeBuilder.cs",
                result.Code);

            var expectedOutput = TestFile.Create("TestFiles/CodeGenerator/Output/CSharpCodeBuilder.cs").ReadAllText();

            // Assert
            Assert.Equal(expectedOutput, result.Code);
        }
    }
}
#endif
