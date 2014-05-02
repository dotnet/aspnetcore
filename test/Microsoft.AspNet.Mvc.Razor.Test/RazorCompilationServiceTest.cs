// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Net.Runtime;
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
            var env = new Mock<IApplicationEnvironment>();
            env.SetupGet(e => e.ApplicationName).Returns("MyTestApplication");
            env.SetupGet(e => e.ApplicationBasePath).Returns(appPath);
            var host = new Mock<IMvcRazorHost>();
            host.Setup(h => h.GenerateCode("MyTestApplication", @"views\index\home.cshtml", It.IsAny<Stream>()))
                .Returns(new GeneratorResults(new Block(new BlockBuilder { Type = BlockType.Comment }), new RazorError[0], new CodeBuilderResult("", new LineMapping[0])))
                .Verifiable();
            var compiler = new Mock<ICompilationService>();
            compiler.Setup(c => c.Compile(It.IsAny<string>()))
                    .Returns(CompilationResult.Successful("", typeof(RazorCompilationServiceTest)));

            var razorService = new RazorCompilationService(env.Object, compiler.Object, host.Object);
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.PhysicalPath).Returns(viewPath);
            fileInfo.Setup(f => f.CreateReadStream()).Returns(Stream.Null);

            // Act
            razorService.CompileCore(fileInfo.Object);

            // Assert
            host.Verify();
        }
    }
}
