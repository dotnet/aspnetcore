// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Test
{
    public class RazorCompilationServiceTest
    {
        [Theory]
        [InlineData(@"src\work\myapp", @"src\work\myapp\Views\index\home.cshtml")]
        [InlineData(@"src\work\myapp\", @"src\work\myapp\Views\index\home.cshtml")]
        public void CompileCalculatesRootRelativePath(string appPath, string viewPath)
        {
            // Arrange
            var host = new Mock<IMvcRazorHost>();
            host.Setup(h => h.GenerateCode(@"Views\index\home.cshtml", It.IsAny<Stream>()))
                .Returns(GetGeneratorResult())
                .Verifiable();

            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.PhysicalPath).Returns(viewPath);
            fileInfo.Setup(f => f.CreateReadStream()).Returns(Stream.Null);

            var relativeFileInfo = new RelativeFileInfo(fileInfo.Object, @"Views\index\home.cshtml");

            var compiler = new Mock<ICompilationService>();
            compiler.Setup(c => c.Compile(relativeFileInfo, It.IsAny<string>()))
                    .Returns(CompilationResult.Successful(typeof(RazorCompilationServiceTest)));

            var razorService = new RazorCompilationService(compiler.Object, host.Object);

            // Act
            razorService.Compile(relativeFileInfo);

            // Assert
            host.Verify();
        }

        [Fact]
        public void Compile_ReturnsFailedResultIfParseFails()
        {
            // Arrange
            var errorSink = new ParserErrorSink();
            errorSink.OnError(new RazorError("some message", 1, 1, 1, 1));
            var generatorResult = new GeneratorResults(
                    new Block(new BlockBuilder { Type = BlockType.Comment }),
                    Enumerable.Empty<TagHelperDescriptor>(),
                    errorSink,
                    new CodeBuilderResult("", new LineMapping[0]),
                    new CodeTree());
            var host = new Mock<IMvcRazorHost>();
            host.Setup(h => h.GenerateCode(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(generatorResult)
                .Verifiable();

            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.CreateReadStream())
                    .Returns(Stream.Null);

            var compiler = new Mock<ICompilationService>(MockBehavior.Strict);
            var relativeFileInfo = new RelativeFileInfo(fileInfo.Object, @"Views\index\home.cshtml");
            var razorService = new RazorCompilationService(compiler.Object, host.Object);

            // Act
            var result = razorService.Compile(relativeFileInfo);

            // Assert
            Assert.NotNull(result.CompilationFailure);
            var message = Assert.Single(result.CompilationFailure.Messages);
            Assert.Equal("some message", message.Message);
            host.Verify();
        }

        [Fact]
        public void Compile_ReturnsResultFromCompilationServiceIfParseSucceeds()
        {
            // Arrange
            var code = "compiled-content";
            var generatorResult = new GeneratorResults(
                    new Block(new BlockBuilder { Type = BlockType.Comment }),
                    Enumerable.Empty<TagHelperDescriptor>(),
                    new ParserErrorSink(),
                    new CodeBuilderResult(code, new LineMapping[0]),
                    new CodeTree());
            var host = new Mock<IMvcRazorHost>();
            host.Setup(h => h.GenerateCode(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(generatorResult);

            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.CreateReadStream())
                    .Returns(Stream.Null);
            var relativeFileInfo = new RelativeFileInfo(fileInfo.Object, @"Views\index\home.cshtml");

            var compilationResult = CompilationResult.Successful(typeof(object));
            var compiler = new Mock<ICompilationService>();
            compiler.Setup(c => c.Compile(relativeFileInfo, code))
                    .Returns(compilationResult)
                    .Verifiable();
            var razorService = new RazorCompilationService(compiler.Object, host.Object);

            // Act
            var result = razorService.Compile(relativeFileInfo);

            // Assert
            Assert.Same(compilationResult, result);
            compiler.Verify();
        }

        private static GeneratorResults GetGeneratorResult()
        {
            return new GeneratorResults(
                    new Block(new BlockBuilder { Type = BlockType.Comment }),
                    Enumerable.Empty<TagHelperDescriptor>(),
                    new ParserErrorSink(),
                    new CodeBuilderResult("", new LineMapping[0]),
                    new CodeTree());
        }
    }
}
