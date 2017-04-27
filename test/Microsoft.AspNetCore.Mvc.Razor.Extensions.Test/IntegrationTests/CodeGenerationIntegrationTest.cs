// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.IntegrationTests
{
    public class CodeGenerationIntegrationTest : IntegrationTestBase
    {
        private static readonly RazorSourceDocument DefaultImports = MvcRazorTemplateEngine.GetDefaultImports();

        #region Runtime
        [Fact]
        public void IncompleteDirectives_Runtime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InheritsViewModel_Runtime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InheritsWithViewImports_Runtime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void MalformedPageDirective_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Basic_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void _ViewImports_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Inject_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InjectWithModel_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InjectWithSemicolon_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Model_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ModelExpressionTagHelper_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void RazorPages_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine(BuildDivDescriptors());
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void RazorPagesWithoutModel_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine(BuildDivDescriptors());
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void PageWithNamespace_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ViewWithNamespace_Runtime()
        {
            // Arrange
            var engine = CreateRuntimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }
        #endregion

        #region DesignTime
        [Fact]
        public void IncompleteDirectives_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InheritsViewModel_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InheritsWithViewImports_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void MalformedPageDirective_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Basic_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void _ViewImports_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Inject_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InjectWithModel_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void InjectWithSemicolon_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void Model_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void MultipleModels_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ModelExpressionTagHelper_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void RazorPages_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine(BuildDivDescriptors());
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void RazorPagesWithoutModel_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine(BuildDivDescriptors());
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void PageWithNamespace_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }

        [Fact]
        public void ViewWithNamespace_DesignTime()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
        }
        #endregion

        protected RazorEngine CreateDesignTimeEngine(IEnumerable<TagHelperDescriptor> descriptors = null)
        {
            return RazorEngine.CreateDesignTime(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(GetMetadataReferenceFeature());

                if (descriptors != null)
                {
                    b.AddTagHelpers(descriptors);
                }
                else
                {
                    b.Features.Add(new CompilationTagHelperFeature());
                    b.Features.Add(new DefaultTagHelperDescriptorProvider() { DesignTime = true });
                    b.Features.Add(new ViewComponentTagHelperDescriptorProvider());
                }
            });
        }

        protected RazorEngine CreateRuntimeEngine(IEnumerable<TagHelperDescriptor> descriptors = null)
        {
            return RazorEngine.Create(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(GetMetadataReferenceFeature());

                if (descriptors != null)
                {
                    b.AddTagHelpers(descriptors);
                }
                else
                {
                    b.Features.Add(new CompilationTagHelperFeature());
                    b.Features.Add(new DefaultTagHelperDescriptorProvider());
                    b.Features.Add(new ViewComponentTagHelperDescriptorProvider());
                }
            });
        }

        protected override void OnCreatingCodeDocument(ref RazorSourceDocument source, IList<RazorSourceDocument> imports)
        {
            // It's important that we normalize the newlines in the default imports. The default imports will
            // be created with Environment.NewLine, but we need to normalize to `\r\n` so that the indices
            // are the same on xplat.
            var buffer = new char[DefaultImports.Length];
            DefaultImports.CopyTo(0, buffer, 0, DefaultImports.Length);

            var text = new string(buffer);
            text = Regex.Replace(text, "(?<!\r)\n", "\r\n");

            imports.Add(RazorSourceDocument.Create(text, DefaultImports.FileName, DefaultImports.Encoding));
        }

        private static IEnumerable<TagHelperDescriptor> BuildDivDescriptors()
        {
            return new List<TagHelperDescriptor>
            {
                BuildDescriptor("div", "DivTagHelper", "TestAssembly"),
                BuildDescriptor("a", "UrlResolutionTagHelper", "Microsoft.AspNetCore.Mvc.Razor")
            };
        }

        private static TagHelperDescriptor BuildDescriptor(
            string tagName,
            string typeName,
            string assemblyName)
        {
            return TagHelperDescriptorBuilder.Create(typeName, assemblyName)
                .TagMatchingRule(ruleBuilder => ruleBuilder.RequireTagName(tagName))
                .Build();
        }

        private static IMetadataReferenceFeature GetMetadataReferenceFeature()
        {
            var currentAssembly = typeof(CodeGenerationIntegrationTest).GetTypeInfo().Assembly;
            var dependencyContext = DependencyContext.Load(currentAssembly);

            var references = dependencyContext.CompileLibraries.SelectMany(l => l.ResolveReferencePaths())
                .Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath))
                .ToList<MetadataReference>();

            var syntaxTree = CreateTagHelperSyntaxTree();
            var compilation = CSharpCompilation.Create(
                "Microsoft.AspNetCore.Mvc.Razor",
                syntaxTree,
                references,
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
