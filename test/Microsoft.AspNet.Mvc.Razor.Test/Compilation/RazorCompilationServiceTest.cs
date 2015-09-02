// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
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

            var razorService = new RazorCompilationService(compiler.Object, host.Object, GetOptions());

            // Act
            razorService.Compile(relativeFileInfo);

            // Assert
            host.Verify();
        }

        [Fact]
        public void Compile_ReturnsFailedResultIfParseFails()
        {
            // Arrange
            var errorSink = new ErrorSink();
            errorSink.OnError(new RazorError("some message", 1, 1, 1, 1));
            var generatorResult = new GeneratorResults(
                    new Block(new BlockBuilder { Type = BlockType.Comment }),
                    Enumerable.Empty<TagHelperDescriptor>(),
                    errorSink,
                    new CodeGeneratorResult("", new LineMapping[0]),
                    new ChunkTree());
            var host = new Mock<IMvcRazorHost>();
            host.Setup(h => h.GenerateCode(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(generatorResult)
                .Verifiable();

            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.CreateReadStream())
                    .Returns(Stream.Null);

            var compiler = new Mock<ICompilationService>(MockBehavior.Strict);
            var relativeFileInfo = new RelativeFileInfo(fileInfo.Object, @"Views\index\home.cshtml");
            var razorService = new RazorCompilationService(compiler.Object, host.Object, GetOptions());

            // Act
            var result = razorService.Compile(relativeFileInfo);

            // Assert
            Assert.NotNull(result.CompilationFailures);
            Assert.Collection(result.CompilationFailures,
                failure =>
                {
                    var message = Assert.Single(failure.Messages);
                    Assert.Equal("some message", message.Message);
                });
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
                    new ErrorSink(),
                    new CodeGeneratorResult(code, new LineMapping[0]),
                    new ChunkTree());
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
            var razorService = new RazorCompilationService(compiler.Object, host.Object, GetOptions());

            // Act
            var result = razorService.Compile(relativeFileInfo);

            // Assert
            Assert.Same(compilationResult, result);
            compiler.Verify();
        }

        [Fact]
        public void GetCompilationFailedResult_ReturnsCompilationResult_WithGroupedMessages()
        {
            // Arrange
            var viewPath = @"views/index.razor";
            var viewImportsPath = @"views/global.import.cshtml";
            var host = Mock.Of<IMvcRazorHost>();

            var fileProvider = new TestFileProvider();
            var file = fileProvider.AddFile(viewPath, "View Content");
            fileProvider.AddFile(viewImportsPath, "Global Import Content");
            var relativeFileInfo = new RelativeFileInfo(file, viewPath);
            var razorService = new RazorCompilationService(
                Mock.Of<ICompilationService>(),
                Mock.Of<IMvcRazorHost>(),
                GetOptions(fileProvider));
            var errors = new[]
            {
                new RazorError("message-1", new SourceLocation(1, 2, 17)),
                new RazorError("message-2", new SourceLocation(viewPath, 1, 4, 6), 7),
                new RazorError { Message = "message-3" },
                new RazorError("message-4", new SourceLocation(viewImportsPath, 1, 3, 8), 4),
            };

            // Act
            var result = razorService.GetCompilationFailedResult(relativeFileInfo, errors);

            // Assert
            Assert.NotNull(result.CompilationFailures);
            Assert.Collection(result.CompilationFailures,
                failure =>
                {
                    Assert.Equal(viewPath, failure.SourceFilePath);
                    Assert.Equal("View Content", failure.SourceFileContent);
                    Assert.Collection(failure.Messages,
                        message =>
                        {
                            Assert.Equal(errors[0].Message, message.Message);
                            Assert.Equal(viewPath, message.SourceFilePath);
                            Assert.Equal(3, message.StartLine);
                            Assert.Equal(17, message.StartColumn);
                            Assert.Equal(3, message.EndLine);
                            Assert.Equal(18, message.EndColumn);
                        },
                        message =>
                        {
                            Assert.Equal(errors[1].Message, message.Message);
                            Assert.Equal(viewPath, message.SourceFilePath);
                            Assert.Equal(5, message.StartLine);
                            Assert.Equal(6, message.StartColumn);
                            Assert.Equal(5, message.EndLine);
                            Assert.Equal(13, message.EndColumn);
                        },
                        message =>
                        {
                            Assert.Equal(errors[2].Message, message.Message);
                            Assert.Equal(viewPath, message.SourceFilePath);
                            Assert.Equal(0, message.StartLine);
                            Assert.Equal(-1, message.StartColumn);
                            Assert.Equal(0, message.EndLine);
                            Assert.Equal(0, message.EndColumn);
                        });
                },
                failure =>
                {
                    Assert.Equal(viewImportsPath, failure.SourceFilePath);
                    Assert.Equal("Global Import Content", failure.SourceFileContent);
                    Assert.Collection(failure.Messages,
                        message =>
                        {
                            Assert.Equal(errors[3].Message, message.Message);
                            Assert.Equal(viewImportsPath, message.SourceFilePath);
                            Assert.Equal(4, message.StartLine);
                            Assert.Equal(8, message.StartColumn);
                            Assert.Equal(4, message.EndLine);
                            Assert.Equal(12, message.EndColumn);
                        });
                });
        }

        private static GeneratorResults GetGeneratorResult()
        {
            return new GeneratorResults(
                    new Block(new BlockBuilder { Type = BlockType.Comment }),
                    Enumerable.Empty<TagHelperDescriptor>(),
                    new ErrorSink(),
                    new CodeGeneratorResult("", new LineMapping[0]),
                    new ChunkTree());
        }

        private static IOptions<RazorViewEngineOptions> GetOptions(IFileProvider fileProvider = null)
        {
            var razorViewEngineOptions = new RazorViewEngineOptions
            {
                FileProvider = fileProvider ?? new TestFileProvider()
            };
            var options = new Mock<IOptions<RazorViewEngineOptions>>();
            options.SetupGet(o => o.Value)
                .Returns(razorViewEngineOptions);

            return options.Object;
        }
    }
}
