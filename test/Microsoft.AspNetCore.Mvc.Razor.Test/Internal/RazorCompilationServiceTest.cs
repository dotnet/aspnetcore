// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class RazorCompilationServiceTest
    {
        [Fact]
        public void CompileCalculatesRootRelativePath()
        {
            // Arrange
            var viewPath = @"src\work\myapp\Views\index\home.cshtml";
            var relativePath = @"Views\index\home.cshtml";

            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.PhysicalPath).Returns(viewPath);
            fileInfo.Setup(f => f.CreateReadStream()).Returns(new MemoryStream(new byte[] { 0 }));
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(relativePath, fileInfo.Object);
            var relativeFileInfo = new RelativeFileInfo(fileInfo.Object, relativePath);

            var compiler = new Mock<ICompilationService>();
            compiler.Setup(c => c.Compile(It.IsAny<RazorCodeDocument>(), It.IsAny<RazorCSharpDocument>()))
                    .Returns(new CompilationResult(typeof(RazorCompilationServiceTest)));

            var engine = new Mock<RazorEngine>();
            engine.Setup(e => e.Process(It.IsAny<RazorCodeDocument>()))
                .Callback<RazorCodeDocument>(document =>
                {
                    document.SetCSharpDocument(new RazorCSharpDocument()
                    {
                        Diagnostics = new List<RazorError>()
                    });

                    Assert.Equal(viewPath, document.Source.Filename);  // Assert if source file name is the root relative path
                }).Verifiable();

            var razorService = new RazorCompilationService(
                compiler.Object,
                engine.Object,
                new DefaultRazorProject(fileProvider),
                GetFileProviderAccessor(fileProvider),
                NullLoggerFactory.Instance);

            // Act
            razorService.Compile(relativeFileInfo);

            // Assert
            engine.Verify();
        }

        [Fact]
        public void Compile_ReturnsFailedResultIfParseFails()
        {
            // Arrange
            var relativePath = @"Views\index\home.cshtml";
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.CreateReadStream()).Returns(new MemoryStream(new byte[] { 0 }));
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(relativePath, fileInfo.Object);
            var relativeFileInfo = new RelativeFileInfo(fileInfo.Object, relativePath);

            var compiler = new Mock<ICompilationService>(MockBehavior.Strict);

            var engine = new Mock<RazorEngine>();
            engine.Setup(e => e.Process(It.IsAny<RazorCodeDocument>()))
                .Callback<RazorCodeDocument>(document =>
                {
                    document.SetCSharpDocument(new RazorCSharpDocument()
                    {
                        Diagnostics = new List<RazorError>()
                        {
                            new RazorError("some message", 1, 1, 1, 1)
                        }
                    });
                }).Verifiable();

            var razorService = new RazorCompilationService(
                compiler.Object,
                engine.Object,
                new DefaultRazorProject(fileProvider),
                GetFileProviderAccessor(fileProvider),
                NullLoggerFactory.Instance);

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
            engine.Verify();
        }

        [Fact]
        public void Compile_ReturnsResultFromCompilationServiceIfParseSucceeds()
        {
            var relativePath = @"Views\index\home.cshtml";
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.CreateReadStream()).Returns(new MemoryStream(new byte[] { 0 }));
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(relativePath, fileInfo.Object);
            var relativeFileInfo = new RelativeFileInfo(fileInfo.Object, relativePath);

            var compilationResult = new CompilationResult(typeof(object));
            var compiler = new Mock<ICompilationService>();
            compiler.Setup(c => c.Compile(It.IsAny<RazorCodeDocument>(), It.IsAny<RazorCSharpDocument>()))
                    .Returns(compilationResult)
                    .Verifiable();

            var engine = new Mock<RazorEngine>();
            engine.Setup(e => e.Process(It.IsAny<RazorCodeDocument>()))
                .Callback<RazorCodeDocument>(document =>
                {
                    document.SetCSharpDocument(new RazorCSharpDocument()
                    {
                        Diagnostics = new List<RazorError>()
                    });
                });

            var razorService = new RazorCompilationService(
                compiler.Object,
                engine.Object,
                new DefaultRazorProject(fileProvider),
                GetFileProviderAccessor(fileProvider),
                NullLoggerFactory.Instance);

            // Act
            var result = razorService.Compile(relativeFileInfo);

            // Assert
            Assert.Same(compilationResult.CompiledType, result.CompiledType);
            compiler.Verify();
        }

        [Fact]
        public void GetCompilationFailedResult_ReturnsCompilationResult_WithGroupedMessages()
        {
            // Arrange
            var viewPath = @"views/index.razor";
            var viewImportsPath = @"views/global.import.cshtml";

            var fileProvider = new TestFileProvider();
            var file = fileProvider.AddFile(viewPath, "View Content");
            fileProvider.AddFile(viewImportsPath, "Global Import Content");
            var razorService = new RazorCompilationService(
                Mock.Of<ICompilationService>(),
                Mock.Of<RazorEngine>(),
                new DefaultRazorProject(fileProvider),
                GetFileProviderAccessor(fileProvider),
                NullLoggerFactory.Instance);
            var errors = new[]
            {
                new RazorError("message-1", new SourceLocation(1, 2, 17), length: 1),
                new RazorError("message-2", new SourceLocation(viewPath, 1, 4, 6), 7),
                new RazorError { Message = "message-3" },
                new RazorError("message-4", new SourceLocation(viewImportsPath, 1, 3, 8), 4),
            };

            // Act
            var result = razorService.GetCompilationFailedResult(viewPath, errors);

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
                            Assert.Equal(-2, message.EndColumn);
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

        private static IRazorViewEngineFileProviderAccessor GetFileProviderAccessor(IFileProvider fileProvider = null)
        {
            var options = new Mock<IRazorViewEngineFileProviderAccessor>();
            options.SetupGet(o => o.FileProvider)
                .Returns(fileProvider ?? new TestFileProvider());

            return options.Object;
        }
    }
}
