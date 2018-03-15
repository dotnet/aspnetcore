// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.IntegrationTests
{
    public class CodeGenerationIntegrationTest : IntegrationTestBase
    {
        private static readonly RazorSourceDocument DefaultImports = MvcRazorTemplateEngine.GetDefaultImports();

        private CSharpCompilation BaseCompilation => MvcShim.BaseCompilation.WithAssemblyName("AppCode");

        #region Runtime
        [Fact]
        public void InvalidNamespaceAtEOF_Runtime()
        {
            var compilation = BaseCompilation;
            RunRuntimeTest(compilation);
        }

        [Fact]
        public void IncompleteDirectives_Runtime()
        {
            var appCode = @"
public class MyService<TModel>
{
    public string Html { get; set; }
}";
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void MalformedPageDirective_Runtime()
        {
            var compilation = BaseCompilation;

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void Basic_Runtime()
        {
            var compilation = BaseCompilation;

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void Sections_Runtime()
        {
            var appCode = $@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
";

            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void _ViewImports_Runtime()
        {
            var compilation = BaseCompilation;

            RunRuntimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void Model_Runtime()
        {
            var compilation = BaseCompilation;

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void ModelExpressionTagHelper_Runtime()
        {
            var appCode = $@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
";
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void RazorPages_Runtime()
        {
            var appCode = $@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
";
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void RazorPagesWithRouteTemplate_Runtime()
        {
            var compilation = BaseCompilation;

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void RazorPagesWithoutModel_Runtime()
        {
            var appCode = $@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
";
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunRuntimeTest(compilation);
        }

        [Fact]
        public void PageWithNamespace_Runtime()
        {
            var compilation = BaseCompilation;
            RunRuntimeTest(compilation);
        }

        [Fact]
        public void ViewWithNamespace_Runtime()
        {
            var compilation = BaseCompilation;
            RunRuntimeTest(compilation);
        }

        [Fact]
        public void ViewComponentTagHelper_Runtime()
        {
            var appCode = $@"
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
";

            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));
            
            RunRuntimeTest(compilation);
        }
        #endregion

        #region DesignTime
        [Fact]
        public void InvalidNamespaceAtEOF_DesignTime()
        {
            var compilation = BaseCompilation;
            RunDesignTimeTest(compilation);
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

            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
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

            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void MalformedPageDirective_DesignTime()
        {
            var compilation = BaseCompilation;
            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void Basic_DesignTime()
        {
            var compilation = BaseCompilation;
            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void Sections_DesignTime()
        {
            var appCode = $@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
";

            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void _ViewImports_DesignTime()
        {
            var compilation = BaseCompilation;
            RunDesignTimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
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
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void Model_DesignTime()
        {
            var compilation = BaseCompilation;
            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void MultipleModels_DesignTime()
        {
            var appCode = @"
public class ThisShouldBeGenerated
{

}";

            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
        }
        
        [Fact]
        public void ModelExpressionTagHelper_DesignTime()
        {
            var appCode = $@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
";

            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void RazorPages_DesignTime()
        {
            var appCode = $@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
";
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void RazorPagesWithRouteTemplate_DesignTime()
        {
            var compilation = BaseCompilation;

            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void RazorPagesWithoutModel_DesignTime()
        {
            var appCode = $@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
";
            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));

            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void PageWithNamespace_DesignTime()
        {
            var compilation = BaseCompilation;
            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void ViewWithNamespace_DesignTime()
        {
            var compilation = BaseCompilation;
            RunDesignTimeTest(compilation);
        }

        [Fact]
        public void ViewComponentTagHelper_DesignTime()
        {
            var appCode = $@"
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
";

            var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(appCode));
            
            RunDesignTimeTest(compilation);
        }
        #endregion

        private void RunRuntimeTest(
            CSharpCompilation baseCompilation,
            IEnumerable<string> expectedErrors = null)
        {
            Assert.Empty(baseCompilation.GetDiagnostics());

            // Arrange
            var engine = CreateRuntimeEngine(baseCompilation);
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
            AssertDocumentCompiles(document, baseCompilation, expectedErrors);
        }

        private void RunDesignTimeTest(
            CSharpCompilation baseCompilation,
            IEnumerable<string> expectedErrors = null)
        {
            Assert.Empty(baseCompilation.GetDiagnostics());

            // Arrange
            var engine = CreateDesignTimeEngine(baseCompilation);
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(document);
            AssertDocumentCompiles(document, baseCompilation, expectedErrors);
        }

        private void AssertDocumentCompiles(
            RazorCodeDocument document,
            CSharpCompilation baseCompilation,
            IEnumerable<string> expectedErrors = null)
        {
            var cSharp = document.GetCSharpDocument().GeneratedCode;

            var syntaxTree = CSharpSyntaxTree.ParseText(cSharp);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var references = baseCompilation.References.Concat(new[] { baseCompilation.ToMetadataReference() });
            var compilation = CSharpCompilation.Create("CodeGenerationTestAssembly", new[] { syntaxTree }, references, options);

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

        protected RazorEngine CreateDesignTimeEngine(CSharpCompilation compilation)
        {
            var references = compilation.References.Concat(new[] { compilation.ToMetadataReference() });

            return RazorEngine.CreateDesignTime(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(GetMetadataReferenceFeature(references));
                b.Features.Add(new CompilationTagHelperFeature());
            });
        }

        protected RazorEngine CreateRuntimeEngine(CSharpCompilation compilation)
        {
            var references = compilation.References.Concat(new[] { compilation.ToMetadataReference() });

            return RazorEngine.Create(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(GetMetadataReferenceFeature(references));
                b.Features.Add(new CompilationTagHelperFeature());
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
    }
}
