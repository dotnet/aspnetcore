// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
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
        [InlineData(@"src\work\myapp", @"src\work\myapp\views\index\home.cshtml")]
        [InlineData(@"src\work\myapp\", @"src\work\myapp\views\index\home.cshtml")]
        public void CompileCoreCalculatesRootRelativePath(string appPath, string viewPath)
        {
            // Arrange
            var host = new Mock<IMvcRazorHost>();
            host.Setup(h => h.GenerateCode(@"views\index\home.cshtml", It.IsAny<Stream>()))
                .Returns(new GeneratorResults(new Block(new BlockBuilder { Type = BlockType.Comment }), new RazorError[0], new CodeBuilderResult("", new LineMapping[0])))
                .Verifiable();

            var ap = new Mock<IControllerAssemblyProvider>();
            ap.SetupGet(e => e.CandidateAssemblies)
                .Returns(Enumerable.Empty<Assembly>())
                .Verifiable();

            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.PhysicalPath).Returns(viewPath);
            fileInfo.Setup(f => f.CreateReadStream()).Returns(Stream.Null);
            var compiler = new Mock<ICompilationService>();
            compiler.Setup(c => c.Compile(fileInfo.Object, It.IsAny<string>()))
                    .Returns(CompilationResult.Successful(typeof(RazorCompilationServiceTest)));
            var razorService = new RazorCompilationService(compiler.Object, ap.Object, host.Object);

            var relativeFileInfo = new RelativeFileInfo()
            {
                FileInfo = fileInfo.Object,
                RelativePath = @"views\index\home.cshtml",
            };

            // Act
            razorService.CompileCore(relativeFileInfo);

            // Assert
            host.Verify();
        }
    }
}
