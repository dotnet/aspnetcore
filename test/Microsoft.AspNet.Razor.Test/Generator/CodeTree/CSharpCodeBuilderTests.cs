// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Utils;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator.CodeTree
{
    public class CSharpCodeBuilderTests
    {
        [Fact]
        public void CodeTreeWithUsings()
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
                errorSink: new ParserErrorSink());
            codeBuilderContext.CodeTreeBuilder.AddUsingChunk("FakeNamespace1", syntaxTreeNode.Object);
            codeBuilderContext.CodeTreeBuilder.AddUsingChunk("FakeNamespace2.SubNamespace", syntaxTreeNode.Object);
            var codeBuilder = new CodeGenTestCodeBuilder(codeBuilderContext);

            // Act
            var result = codeBuilder.Build();

            BaselineWriter.WriteBaseline(
                @"test\Microsoft.AspNet.Razor.Test\TestFiles\CodeGenerator\CS\Output\CSharpCodeBuilder.cs",
                result.Code);

            var expectedOutput = TestFile.Create("TestFiles/CodeGenerator/CS/Output/CSharpCodeBuilder.cs").ReadAllText();

            // Assert
            Assert.Equal(expectedOutput, result.Code);
        }
    }
}
#endif
