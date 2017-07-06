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
        private const string CurrentMvcShim = "Microsoft.AspNetCore.Razor.Test.MvcShim.dll";
        private static readonly RazorSourceDocument DefaultImports = MvcRazorTemplateEngine.GetDefaultImports();

        #region Runtime
        [Fact]
        public void InvalidNamespaceAtEOF_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunRuntimeTest(references);
        }

        [Fact]
        public void IncompleteDirectives_Runtime()
        {
            var appCode = @"
public class MyService<TModel>
{
    public string Html { get; set; }
}";
            var compilationReferences = CreateCompilationReferences(CurrentMvcShim, appCode);

            RunRuntimeTest(compilationReferences);
        }

        [Fact]
        public void InheritsViewModel_Runtime()
        {
            var appCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;

public class MyBasePageForViews<TModel> : RazorPage
{
    public override Task ExecuteAsync()
    {
        throw new System.NotImplementedException();
    }
}
public class MyModel
{

}
";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);

            RunRuntimeTest(references);
        }

        [Fact]
        public void InheritsWithViewImports_Runtime()
        {
            var appCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

public abstract class MyPageModel<T> : Page
{
    public override Task ExecuteAsync()
    {
        throw new System.NotImplementedException();
    }
}

public class MyModel
{

}";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);

            RunRuntimeTest(references);
        }

        [Fact]
        public void MalformedPageDirective_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunRuntimeTest(references);
        }

        [Fact]
        public void Basic_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunRuntimeTest(references);
        }

        [Fact]
        public void _ViewImports_Runtime()
        {
            var error = "'TestFiles_IntegrationTests_CodeGenerationIntegrationTest__ViewImports_cshtml.Model' " + 
                "hides inherited member 'RazorPage<dynamic>.Model'. Use the new keyword if hiding was intended.";

            var references = CreateCompilationReferences(CurrentMvcShim);
            RunRuntimeTest(references, new[] { error, });
        }

        [Fact]
        public void Inject_Runtime()
        {
            var appCode = @"
public class MyApp
{
    public string MyProperty { get; set; }
}
";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);

            RunRuntimeTest(references);
        }

        [Fact]
        public void InjectWithModel_Runtime()
        {
            var appCode = @"
public class MyModel
{

}

public class MyService<TModel>
{
    public string Html { get; set; }
}

public class MyApp
{
    public string MyProperty { get; set; }
}";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);

            RunRuntimeTest(references);
        }

        [Fact]
        public void InjectWithSemicolon_Runtime()
        {
            var appCode = @"
public class MyModel
{

}

public class MyApp
{
    public string MyProperty { get; set; }
}

public class MyService<TModel>
{
    public string Html { get; set; }
}
";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);

            RunRuntimeTest(references);
        }

        [Fact]
        public void Model_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);

            RunRuntimeTest(references);
        }

        [Fact]
        public void ModelExpressionTagHelper_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim, appCode: $@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
");
            RunRuntimeTest(references);
        }

        [Fact]
        public void RazorPages_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim, appCode: $@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
");
            RunRuntimeTest(references);
        }

        [Fact]
        public void RazorPagesWithoutModel_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim, appCode: $@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
");
            RunRuntimeTest(references);
        }

        [Fact]
        public void PageWithNamespace_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunRuntimeTest(references);
        }

        [Fact]
        public void ViewWithNamespace_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunRuntimeTest(references);
        }

        [Fact]
        public void ViewComponentTagHelper_Runtime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim, appCode: $@"
public class TestViewComponent
{{
    public string Invoke(string firstName)
    {{
        return firstName;
    }}
}}

[{typeof(HtmlTargetElementAttribute).FullName}]
public class AllTagHelper : {typeof(TagHelper).FullName}
{{
    public string Bar {{ get; set; }}
}}
");
            RunRuntimeTest(references);
        }
        #endregion

        #region DesignTime
        [Fact]
        public void InvalidNamespaceAtEOF_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void IncompleteDirectives_DesignTime()
        {
            var appCode = @"
public class MyService<TModel>
{
    public string Html { get; set; }
}
";

            var references = CreateCompilationReferences(CurrentMvcShim, appCode);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void InheritsViewModel_DesignTime()
        {
            var appCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;

public class MyBasePageForViews<TModel> : RazorPage
{
    public override Task ExecuteAsync()
    {
        throw new System.NotImplementedException();
    }
}

public class MyModel
{

}
";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void InheritsWithViewImports_DesignTime()
        {
            var appCode = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class MyModel
{

}

public abstract class MyPageModel<T> : Page
{
    public override Task ExecuteAsync()
    {
        throw new System.NotImplementedException();
    }
}
";

            var references = CreateCompilationReferences(CurrentMvcShim, appCode);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void MalformedPageDirective_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void Basic_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void _ViewImports_DesignTime()
        {
            var error = "'TestFiles_IntegrationTests_CodeGenerationIntegrationTest__ViewImports_cshtml.Model' " +
                "hides inherited member 'RazorPage<dynamic>.Model'. Use the new keyword if hiding was intended.";

            var references = CreateCompilationReferences(CurrentMvcShim);
            RunDesignTimeTest(references, new[] { error, });
        }

        [Fact]
        public void Inject_DesignTime()
        {
            var appCode = @"
public class MyApp
{
    public string MyProperty { get; set; }
}
";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void InjectWithModel_DesignTime()
        {
            var appCode = @"
public class MyModel
{

}

public class MyService<TModel>
{
    public string Html { get; set; }
}

public class MyApp
{
    public string MyProperty { get; set; }
}
";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void InjectWithSemicolon_DesignTime()
        {
            var appCode = @"
public class MyModel
{

}

public class MyService<TModel>
{
    public string Html { get; set; }
}

public class MyApp
{
    public string MyProperty { get; set; }
}
";
            var references = CreateCompilationReferences(CurrentMvcShim, appCode);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void Model_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void MultipleModels_DesignTime()
        {
            var appCode = @"
public class ThisShouldBeGenerated
{

}";

            var references = CreateCompilationReferences(CurrentMvcShim, appCode);
            RunDesignTimeTest(references);
        }
        
        [Fact]
        public void ModelExpressionTagHelper_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim, appCode: $@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
");
            RunDesignTimeTest(references);
        }

        [Fact]
        public void RazorPages_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim, appCode: $@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
");
            RunDesignTimeTest(references);
        }

        [Fact]
        public void RazorPagesWithoutModel_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim, appCode: $@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
");
            RunDesignTimeTest(references);
        }

        [Fact]
        public void PageWithNamespace_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void ViewWithNamespace_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim);
            RunDesignTimeTest(references);
        }

        [Fact]
        public void ViewComponentTagHelper_DesignTime()
        {
            var references = CreateCompilationReferences(CurrentMvcShim, appCode: $@"
public class TestViewComponent
{{
    public string Invoke(string firstName)
    {{
        return firstName;
    }}
}}

[{typeof(HtmlTargetElementAttribute).FullName}]
public class AllTagHelper : {typeof(TagHelper).FullName}
{{
    public string Bar {{ get; set; }}
}}
");
            RunDesignTimeTest(references);
        }
        #endregion

        private void RunRuntimeTest(
            IEnumerable<MetadataReference> compilationReferences,
            IEnumerable<string> expectedErrors = null)
        {
            // Arrange
            var engine = CreateRuntimeEngine(compilationReferences);
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
            AssertDocumentCompiles(document, compilationReferences, expectedErrors);
        }

        private void RunDesignTimeTest(
            IEnumerable<MetadataReference> compilationReferences,
            IEnumerable<string> expectedErrors = null)
        {
            // Arrange
            var engine = CreateDesignTimeEngine(compilationReferences);
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(document);
            AssertDocumentCompiles(document, compilationReferences, expectedErrors);
        }

        private static IEnumerable<MetadataReference> CreateCompilationReferences(string mvcShimName, string appCode = null)
        {
            var shimReferences = CreateMvcShimReferences(mvcShimName);
            return CreateAppCodeReferences(appCode, shimReferences);
        }

        private void AssertDocumentCompiles(
            RazorCodeDocument document,
            IEnumerable<MetadataReference> compilationReferences,
            IEnumerable<string> expectedErrors = null)
        {
            var cSharp = document.GetCSharpDocument().GeneratedCode;

            var syntaxTree = CSharpSyntaxTree.ParseText(cSharp);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create("CodeGenerationTestAssembly", new[] { syntaxTree }, compilationReferences, options);

            var diagnostics = compilation.GetDiagnostics();

            var errors = diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning);

            if (expectedErrors == null)
            {
                Assert.Empty(errors.Select(e => e.GetMessage()));
            }
            else
            {
                Assert.Equal(expectedErrors, errors.Select(e => e.GetMessage()));
            }
        }

        protected RazorEngine CreateDesignTimeEngine(IEnumerable<MetadataReference> references)
        {
            return RazorEngine.CreateDesignTime(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(GetMetadataReferenceFeature(references));
                b.Features.Add(new CompilationTagHelperFeature());
                b.Features.Add(new DefaultTagHelperDescriptorProvider() { DesignTime = true });
                b.Features.Add(new ViewComponentTagHelperDescriptorProvider() { ForceEnabled = true });
            });
        }

        protected RazorEngine CreateRuntimeEngine(IEnumerable<MetadataReference> references)
        {
            return RazorEngine.Create(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(GetMetadataReferenceFeature(references));
                b.Features.Add(new CompilationTagHelperFeature());
                b.Features.Add(new DefaultTagHelperDescriptorProvider() { DesignTime = true });
                b.Features.Add(new ViewComponentTagHelperDescriptorProvider() { ForceEnabled = true });
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

            imports.Add(RazorSourceDocument.Create(text, DefaultImports.FilePath, DefaultImports.Encoding));
        }

        private static MetadataReference BuildDynamicAssembly(
            string text,
            IEnumerable<MetadataReference> references,
            string assemblyName)
        {
            var syntaxTree = new SyntaxTree[] { CSharpSyntaxTree.ParseText(text) };

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTree,
                references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var stream = new MemoryStream();
            var compilationResult = compilation.Emit(stream, options: new EmitOptions());
            stream.Position = 0;

            Assert.True(compilationResult.Success);

            return MetadataReference.CreateFromStream(stream);
        }

        private static IRazorEngineFeature GetMetadataReferenceFeature(IEnumerable<MetadataReference> references)
        {
            return new DefaultMetadataReferenceFeature()
            {
                References = references.ToList()
            };
        }

        private static IEnumerable<MetadataReference> CreateAppCodeReferences(string appCode, IEnumerable<MetadataReference> shimReferences)
        {
            var references = new List<MetadataReference>(shimReferences);

            if (appCode != null)
            {
                var appCodeSyntaxTrees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(appCode) };

                var compilation = CSharpCompilation.Create(
                    "AppCode",
                    appCodeSyntaxTrees,
                    shimReferences,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                var stream = new MemoryStream();
                var compilationResult = compilation.Emit(stream, options: new EmitOptions());
                stream.Position = 0;

                var diagString = string.Join(";", compilationResult.Diagnostics.Where(s => s.Severity == DiagnosticSeverity.Error).Select(s => s.ToString()));
                Assert.True(compilationResult.Success, string.Format("Application code needed for tests didn't compile!: {0}", diagString));

                references.Add(MetadataReference.CreateFromStream(stream));
            }

            return references;
        }

        private static IEnumerable<MetadataReference> CreateMvcShimReferences(string mvcShimName)
        {
            var dllPath = Path.Combine(Directory.GetCurrentDirectory(), mvcShimName);
            var assembly = Assembly.LoadFile(dllPath);
            var assemblyDependencyContext = DependencyContext.Load(assembly);

            var assemblyReferencePaths = assemblyDependencyContext.CompileLibraries.SelectMany(l => l.ResolveReferencePaths());

            var references = assemblyReferencePaths
                .Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath))
                .ToList<MetadataReference>();

            Assert.NotEmpty(references);

            return references;
        }
    }
}
