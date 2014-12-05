// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RoslynCompilationServiceTest
    {
        [Fact]
        public void Compile_ReturnsUncachedCompilationResultWithCompiledContent()
        {
            // Arrange
            var content = @"
public class MyTestType  {}";
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryManager = GetLibraryManager();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationBasePath,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions());
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns(string.Empty);

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryManager,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost.Object);

            // Act
            var result = compilationService.Compile(new TestFileInfo { PhysicalPath = "SomePath" }, content);

            // Assert
            var uncachedResult = Assert.IsType<UncachedCompilationResult>(result);
            Assert.Equal("MyTestType", result.CompiledType.Name);
            Assert.Equal(content, result.CompiledContent);
        }

        [Fact]
        public void Compile_UsesApplicationsCompilationSettings_ForParsingAndCompilation()
        {
            // Arrange
            var content = @"
#if MY_CUSTOM_DEFINE
public class MyCustomDefinedClass {}
#else
public class MyNonCustomDefinedClass {}
#endif
";
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryManager = GetLibraryManager();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationBasePath,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions { Defines = new[] { "MY_CUSTOM_DEFINE" } });
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns("My");

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryManager,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost.Object);

            // Act
            var result = compilationService.Compile(new TestFileInfo { PhysicalPath = "SomePath" }, content);

            // Assert
            Assert.NotNull(result.CompiledType);
            Assert.Equal("MyCustomDefinedClass", result.CompiledType.Name);
        }

        [Fact]
        public void Compile_ReturnsSingleTypeThatStartsWithMainClassNamePrefix()
        {
            // Arrange
            var content = @"
public class RazorPrefixType  {}
public class NotRazorPrefixType {}";
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryManager = GetLibraryManager();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationBasePath,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions());
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns("RazorPrefix");

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryManager,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost.Object);

            // Act
            var result = compilationService.Compile(new TestFileInfo { PhysicalPath = "SomePath" }, content);

            // Assert
            Assert.NotNull(result.CompiledType);
            Assert.Equal("RazorPrefixType", result.CompiledType.Name);
        }

        private static ILibraryManager GetLibraryManager()
        {
            var fileReference = new Mock<IMetadataFileReference>();
            fileReference.SetupGet(f => f.Path)
                         .Returns(typeof(string).Assembly.Location);
            var libraryExport = new Mock<ILibraryExport>();
            libraryExport.SetupGet(e => e.MetadataReferences)
                         .Returns(new[] { fileReference.Object });
            libraryExport.SetupGet(e => e.SourceReferences)
                         .Returns(new ISourceReference[0]);

            var libraryManager = new Mock<ILibraryManager>();
            libraryManager.Setup(l => l.GetAllExports(It.IsAny<string>()))
                          .Returns(libraryExport.Object);
            return libraryManager.Object;
        }

        private static IAssemblyLoadContextAccessor GetLoadContextAccessor()
        {
            var loadContext = new Mock<IAssemblyLoadContext>();
            loadContext.Setup(s => s.LoadStream(It.IsAny<Stream>(), It.IsAny<Stream>()))
                       .Returns((Stream stream, Stream pdb) =>
                       {
                           var memoryStream = (MemoryStream)stream;
                           return Assembly.Load(memoryStream.ToArray());
                       });

            var accessor = new Mock<IAssemblyLoadContextAccessor>();
            accessor.Setup(a => a.GetLoadContext(typeof(RoslynCompilationService).Assembly))
                    .Returns(loadContext.Object);
            return accessor.Object;
        }

        private IApplicationEnvironment GetApplicationEnvironment()
        {
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.SetupGet(a => a.ApplicationName)
                                  .Returns("MyApp");
            applicationEnvironment.SetupGet(a => a.RuntimeFramework)
                                  .Returns(new FrameworkName("ASPNET", new Version(5, 0)));
            applicationEnvironment.SetupGet(a => a.Configuration)
                                  .Returns("Debug");
            applicationEnvironment.SetupGet(a => a.ApplicationBasePath)
                                  .Returns("MyBasePath");

            return applicationEnvironment.Object;
        }
    }
}