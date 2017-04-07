// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class CodeGenerationTests
    {
        private static Assembly _assembly = typeof(CodeGenerationTests).GetTypeInfo().Assembly;

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

        [Fact]
        public void RazorEngine_RazorPages_Runtime()
        {
            RunRuntimeTest("RazorPages", BuildDivDescriptors());
        }

        [Fact]
        public void RazorEngine_RazorPagesWithoutModel_Runtime()
        {
            RunRuntimeTest("RazorPagesWithoutModel", BuildDivDescriptors());
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

        [Fact]
        public void RazorEngine_RazorPages_DesignTime()
        {
            RunDesignTimeTest("RazorPages", BuildDivDescriptors());
        }

        [Fact]
        public void RazorEngine_RazorPagesWithoutModel_DesignTime()
        {
            RunDesignTimeTest("RazorPagesWithoutModel", BuildDivDescriptors());
        }
        #endregion

        private static void RunRuntimeTest(string testName, IEnumerable<TagHelperDescriptor> descriptors = null)
        {
            // Arrange
            var inputFile = "TestFiles/Input/" + testName + ".cshtml";
            var outputFile = "TestFiles/Output/Runtime/" + testName + ".cs";
            var expectedCode = ResourceFile.ReadResource(_assembly, outputFile, sourceFile: false);

            var engine = RazorEngine.Create(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(GetMetadataReferenceFeature());

                if (descriptors != null)
                {
                    b.AddTagHelpers(descriptors);
                }
                else
                {
                    b.Features.Add(new DefaultTagHelperFeature());
                }

            });

            var inputContent = ResourceFile.ReadResource(_assembly, inputFile, sourceFile: true);
            var item = new TestRazorProjectItem("/" + inputFile) { Content = inputContent, };
            var project = new TestRazorProject(new List<RazorProjectItem>()
            {
                item,
            });

            var razorTemplateEngine = new MvcRazorTemplateEngine(engine, project);
            razorTemplateEngine.Options.ImportsFileName = "_ViewImports.cshtml";
            var codeDocument = razorTemplateEngine.CreateCodeDocument(item);
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

        private static void RunDesignTimeTest(string testName, IEnumerable<TagHelperDescriptor> descriptors = null)
        {
            // Arrange
            var inputFile = "TestFiles/Input/" + testName + ".cshtml";
            var outputFile = "TestFiles/Output/DesignTime/" + testName + ".cs";
            var expectedCode = ResourceFile.ReadResource(_assembly, outputFile, sourceFile: false);

            var lineMappingOutputFile = "TestFiles/Output/DesignTime/" + testName + ".mappings.txt";
            var expectedMappings = ResourceFile.ReadResource(_assembly, lineMappingOutputFile, sourceFile: false);

            var engine = RazorEngine.CreateDesignTime(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(GetMetadataReferenceFeature());

                if (descriptors != null)
                {
                    b.AddTagHelpers(descriptors);
                }
                else
                {
                    b.Features.Add(new DefaultTagHelperFeature());
                }
            });

            var inputContent = ResourceFile.ReadResource(_assembly, inputFile, sourceFile: true);
            var item = new TestRazorProjectItem("/" + inputFile) { Content = inputContent, };
            var project = new TestRazorProject(new List<RazorProjectItem>()
            {
                item,
            });

            var razorTemplateEngine = new MvcRazorTemplateEngine(engine, project);
            razorTemplateEngine.Options.ImportsFileName = "_ViewImports.cshtml";
            var codeDocument = razorTemplateEngine.CreateCodeDocument(item);
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

        private static IEnumerable<TagHelperDescriptor> BuildDivDescriptors()
        {
            return new List<TagHelperDescriptor> {
                BuildDescriptor("div", "DivTagHelper", "TestAssembly"),
                BuildDescriptor("a", "UrlResolutionTagHelper", "Microsoft.AspNetCore.Mvc.Razor")
            };
        }

        private static TagHelperDescriptor BuildDescriptor(
            string tagName,
            string typeName,
            string assemblyName)
        {
            return ITagHelperDescriptorBuilder.Create(typeName, assemblyName)
                .TagMatchingRule(ruleBuilder => ruleBuilder.RequireTagName(tagName))
                .Build();
        }

        private static IRazorEngineFeature GetMetadataReferenceFeature()
        {
            var currentAssembly = typeof(CodeGenerationTests).GetTypeInfo().Assembly;
            var dependencyContext = DependencyContext.Load(currentAssembly);

            var references = dependencyContext.CompileLibraries.SelectMany(l => l.ResolveReferencePaths())
                .Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath))
                .ToList<MetadataReference>();

            var syntaxTree = CreateTagHelperSyntaxTree();
            var compilation = CSharpCompilation.Create("Microsoft.AspNetCore.Mvc.Razor", syntaxTree, references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var stream = new MemoryStream();
            var compilationResult = compilation.Emit(stream, options: new EmitOptions());
            stream.Position = 0;

            Assert.True(compilationResult.Success);

            references.Add(MetadataReference.CreateFromStream(stream));

            var feature = new DefaultMetadataReferenceFeature()
            {
                References = references,
            };

            return feature;
        }

        private static IEnumerable<SyntaxTree> CreateTagHelperSyntaxTree()
        {
            var text = $@"
            public class UrlResolutionTagHelper : {typeof(TagHelper).FullName}
            {{

            }}";

            return new SyntaxTree[] { CSharpSyntaxTree.ParseText(text) };
        }
    }
}