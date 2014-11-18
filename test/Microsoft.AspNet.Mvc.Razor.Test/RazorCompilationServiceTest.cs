// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
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

            var compiler = new Mock<ICompilationService>();
            compiler.Setup(c => c.Compile(fileInfo.Object, It.IsAny<string>()))
                    .Returns(CompilationResult.Successful(typeof(RazorCompilationServiceTest)));

            var razorService = new RazorCompilationService(compiler.Object, host.Object);

            var relativeFileInfo = new RelativeFileInfo()
            {
                FileInfo = fileInfo.Object,
                RelativePath = @"Views\index\home.cshtml",
            };

            // Act
            razorService.Compile(relativeFileInfo);

            // Assert
            host.Verify();
        }

        private static GeneratorResults GetGeneratorResult()
        {
            return new GeneratorResults(
                    new Block(
                        new BlockBuilder { Type = BlockType.Comment }),
                        new RazorError[0],
                        new CodeBuilderResult("", new LineMapping[0]),
                        new CodeTree());
        }
    }
}
