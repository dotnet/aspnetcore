// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
                .Returns(GetGeneratorResult())
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

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(It.Is<Type>(t => t == typeof(ICompilationService))))
                           .Returns(compiler.Object);

            var razorService = new RazorCompilationService(serviceProvider.Object, ap.Object, host.Object);

            var relativeFileInfo = new RelativeFileInfo()
            {
                FileInfo = fileInfo.Object,
                RelativePath = @"views\index\home.cshtml",
            };

            // Act
            razorService.CompileCore(relativeFileInfo, isInstrumented: false);

            // Assert
            host.Verify();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CompileCoreSetsEnableInstrumentationOnHost(bool enableInstrumentation)
        {
            // Arrange
            var host = new Mock<IMvcRazorHost>();
            host.SetupAllProperties();
            host.Setup(h => h.GenerateCode(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(GetGeneratorResult());            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider.SetupGet(e => e.CandidateAssemblies)
                            .Returns(Enumerable.Empty<Assembly>());

            var compiler = new Mock<ICompilationService>();
            compiler.Setup(c => c.Compile(It.IsAny<IFileInfo>(), It.IsAny<string>()))
                    .Returns(CompilationResult.Successful(GetType()));

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(It.Is<Type>(t => t == typeof(ICompilationService))))
                           .Returns(compiler.Object);

            var razorService = new RazorCompilationService(serviceProvider.Object, assemblyProvider.Object, host.Object);
            var relativeFileInfo = new RelativeFileInfo()
            {
                FileInfo = Mock.Of<IFileInfo>(),
                RelativePath = @"views\index\home.cshtml",
            };

            // Act
            razorService.CompileCore(relativeFileInfo, isInstrumented: enableInstrumentation);

            // Assert
            Assert.Equal(enableInstrumentation, host.Object.EnableInstrumentation);
        }

        private static GeneratorResults GetGeneratorResult()
        {
            return new GeneratorResults(
                    new Block(
                        new BlockBuilder { Type = BlockType.Comment }),
                        new RazorError[0],
                        new CodeBuilderResult("", new LineMapping[0]));
        }
    }
}
