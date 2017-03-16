// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Host;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class RazorEngineTest
    {
        private static Assembly _assembly = typeof(RazorEngineTest).GetTypeInfo().Assembly;

        #region Runtime
        [Fact]
        public void RazorEngine_Basic_Runtime()
        {
            RunRuntimeTest("Basic");
        }

        [Fact]
        public void RazorEngine_ViewImports_Runtime()
        {
            RunRuntimeTest("_ViewImports");
        }

        [Fact]
        public void RazorEngine_Inject_Runtime()
        {
            RunRuntimeTest("Inject");
        }

        [Fact]
        public void RazorEngine_InjectWithModel_Runtime()
        {
            RunRuntimeTest("InjectWithModel");
        }

        [Fact]
        public void RazorEngine_InjectWithSemicolon_Runtime()
        {
            RunRuntimeTest("InjectWithSemicolon");
        }

        [Fact]
        public void RazorEngine_Model_Runtime()
        {
            RunRuntimeTest("Model");
        }

        [Fact]
        public void RazorEngine_ModelExpressionTagHelper_Runtime()
        {
            RunRuntimeTest("ModelExpressionTagHelper");
        }
        #endregion

        #region DesignTime
        [Fact]
        public void RazorEngine_Basic_DesignTime()
        {
            RunDesignTimeTest("Basic");
        }

        [Fact]
        public void RazorEngine_ViewImports_DesignTime()
        {
            RunDesignTimeTest("_ViewImports");
        }

        [Fact]
        public void RazorEngine_Inject_DesignTime()
        {
            RunDesignTimeTest("Inject");
        }

        [Fact]
        public void RazorEngine_InjectWithModel_DesignTime()
        {
            RunDesignTimeTest("InjectWithModel");
        }

        [Fact]
        public void RazorEngine_InjectWithSemicolon_DesignTime()
        {
            RunDesignTimeTest("InjectWithSemicolon");
        }

        [Fact]
        public void RazorEngine_Model_DesignTime()
        {
            RunDesignTimeTest("Model");
        }

        [Fact]
        public void RazorEngine_MultipleModels_DesignTime()
        {
            RunDesignTimeTest("MultipleModels");
        }

        [Fact]
        public void RazorEngine_ModelExpressionTagHelper_DesignTime()
        {
            RunDesignTimeTest("ModelExpressionTagHelper");
        }
        #endregion

        private static void RunRuntimeTest(string testName)
        {
            // Arrange
            var inputFile = "TestFiles/Input/" + testName + ".cshtml";
            var outputFile = "TestFiles/Output/Runtime/" + testName + ".cs";
            var expectedCode = ResourceFile.ReadResource(_assembly, outputFile, sourceFile: false);

            var engine = RazorEngine.Create(b =>
            {
                InjectDirective.Register(b);
                ModelDirective.Register(b);

                b.AddTargetExtension(new InjectDirectiveTargetExtension());

                b.Features.Add(new ModelExpressionPass());
                b.Features.Add(new MvcViewDocumentClassifierPass());
                b.Features.Add(new DefaultInstrumentationPass());

                b.Features.Add(new DefaultTagHelperFeature());
                b.Features.Add(GetMetadataReferenceFeature());
            });

            var inputContent = ResourceFile.ReadResource(_assembly, inputFile, sourceFile: true);
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(inputFile, inputContent);
            var fileInfo = fileProvider.GetFileInfo(inputFile);
            var razorTemplateEngine = new MvcRazorTemplateEngine(engine, GetRazorProject(fileProvider));
            var razorProjectItem = new DefaultRazorProjectItem(fileInfo, basePath: null, path: inputFile);
            var codeDocument = razorTemplateEngine.CreateCodeDocument(razorProjectItem);
            codeDocument.Items["SuppressUniqueIds"] = "test";
            codeDocument.Items["NewLineString"] = "\r\n";

            // Act
            var csharpDocument = razorTemplateEngine.GenerateCode(codeDocument);

            // Assert
            Assert.Empty(csharpDocument.Diagnostics);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedCode, csharpDocument.GeneratedCode);
#else
            Assert.Equal(expectedCode, csharpDocument.GeneratedCode, ignoreLineEndingDifferences: true);
#endif
        }

        private static void RunDesignTimeTest(string testName)
        {
            // Arrange
            var inputFile = "TestFiles/Input/" + testName + ".cshtml";
            var outputFile = "TestFiles/Output/DesignTime/" + testName + ".cs";
            var expectedCode = ResourceFile.ReadResource(_assembly, outputFile, sourceFile: false);

            var lineMappingOutputFile = "TestFiles/Output/DesignTime/" + testName + ".mappings.txt";
            var expectedMappings = ResourceFile.ReadResource(_assembly, lineMappingOutputFile, sourceFile: false);

            var engine = RazorEngine.CreateDesignTime(b =>
            {
                InjectDirective.Register(b);
                ModelDirective.Register(b);

                b.AddTargetExtension(new InjectDirectiveTargetExtension());

                b.Features.Add(new ModelExpressionPass());
                b.Features.Add(new MvcViewDocumentClassifierPass());

                b.Features.Add(new DefaultTagHelperFeature());
                b.Features.Add(GetMetadataReferenceFeature());
            });

            var inputContent = ResourceFile.ReadResource(_assembly, inputFile, sourceFile: true);
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(inputFile, inputContent);
            var fileInfo = fileProvider.GetFileInfo(inputFile);
            var razorTemplateEngine = new MvcRazorTemplateEngine(engine, GetRazorProject(fileProvider));
            var razorProjectItem = new DefaultRazorProjectItem(fileInfo, basePath: null, path: inputFile);
            var codeDocument = razorTemplateEngine.CreateCodeDocument(razorProjectItem);
            codeDocument.Items["SuppressUniqueIds"] = "test";
            codeDocument.Items["NewLineString"] = "\r\n";

            // Act
            var csharpDocument = razorTemplateEngine.GenerateCode(codeDocument);

            // Assert
            Assert.Empty(csharpDocument.Diagnostics);

            var serializedMappings = LineMappingsSerializer.Serialize(csharpDocument, codeDocument.Source);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedCode, csharpDocument.GeneratedCode);
            ResourceFile.UpdateFile(_assembly, lineMappingOutputFile, expectedMappings, serializedMappings);
#else
            Assert.Equal(expectedCode, csharpDocument.GeneratedCode, ignoreLineEndingDifferences: true);
            Assert.Equal(expectedMappings, serializedMappings, ignoreLineEndingDifferences: true);
#endif
        }

        private static IRazorEngineFeature GetMetadataReferenceFeature()
        {
            var currentAssembly = typeof(RazorEngineTest).GetTypeInfo().Assembly;
            var dependencyContext = DependencyContext.Load(currentAssembly);

            var references = dependencyContext.CompileLibraries.SelectMany(l => l.ResolveReferencePaths())
                .Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath))
                .ToList<MetadataReference>();

            var feature = new DefaultMetadataReferenceFeature()
            {
                References = references,
            };

            return feature;
        }

        private static DefaultRazorProject GetRazorProject(IFileProvider fileProvider)
        {
            var fileProviderAccessor = new Mock<IRazorViewEngineFileProviderAccessor>();
            fileProviderAccessor.SetupGet(f => f.FileProvider)
                .Returns(fileProvider);

            return new DefaultRazorProject(fileProviderAccessor.Object);
        }
    }
}