// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.IntegrationTests
{
    public class CodeGenerationIntegrationTest : IntegrationTestBase
    {
        private readonly static CSharpCompilation DefaultBaseCompilation = MvcShim.BaseCompilation.WithAssemblyName("AppCode");

        public CodeGenerationIntegrationTest()
            : base(generateBaselines: null, projectDirectoryHint: "Microsoft.AspNetCore.Mvc.Razor.Extensions")
        {
            Configuration = RazorConfiguration.Create(
                RazorLanguageVersion.Latest,
                "MVC-3.0",
                new[] { new AssemblyExtension("MVC-3.0", typeof(ExtensionInitializer).Assembly) });
        }

        protected override CSharpCompilation BaseCompilation => DefaultBaseCompilation;

        protected override RazorConfiguration Configuration { get; }

        #region Runtime

        [Fact]
        public void UsingDirectives_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false, throwOnFailure: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);

            var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
            Assert.Equal("The using directive for 'System' appeared previously in this namespace", Assert.Single(diagnostics).GetMessage());
        }

        [Fact]
        public void InvalidNamespaceAtEOF_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);

            var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
            Assert.Equal("RZ1014", Assert.Single(diagnotics).Id);
        }

        [Fact]
        public void IncompleteDirectives_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
public class MyService<TModel>
{
    public string Html { get; set; }
}");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);

            // We expect this test to generate a bunch of errors.
            Assert.True(compiled.CodeDocument.GetCSharpDocument().Diagnostics.Count > 0);
        }

        [Fact]
        public void InheritsViewModel_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
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
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void InheritsWithViewImports_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
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

}");
            AddProjectItemFromText(@"@inherits MyPageModel<TModel>");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void AttributeDirectiveWithViewImports_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();
            AddProjectItemFromText(@"
@using System
@attribute [Serializable]");

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false, throwOnFailure: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);

            var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
            Assert.Equal("Duplicate 'Serializable' attribute", Assert.Single(diagnostics).GetMessage());
        }

        [Fact]
        public void MalformedPageDirective_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);

            var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
            Assert.Equal("RZ1016", Assert.Single(diagnotics).Id);
        }

        [Fact]
        public void Basic_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void BasicComponent_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile(fileKind: FileKinds.Component);

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact(Skip = "Reenable after CS1701 errors are resolved")]
        public void Sections_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void _ViewImports_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void Inject_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
public class MyApp
{
    public string MyProperty { get; set; }
}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void InjectWithModel_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
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
}");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void InjectWithSemicolon_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
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
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void Model_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());

            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact(Skip = "Reenable after CS1701 errors are resolved")]
        public void ModelExpressionTagHelper_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact(Skip = "Reenable after CS1701 errors are resolved")]
        public void RazorPages_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void RazorPagesWithRouteTemplate_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact(Skip = "Reenable after CS1701 errors are resolved")]
        public void RazorPagesWithoutModel_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void PageWithNamespace_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void ViewWithNamespace_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact(Skip = "Reenable after CS1701 errors are resolved")]
        public void ViewComponentTagHelper_Runtime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
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

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);
        }

        [Fact]
        public void RazorPageWithNoLeadingPageDirective_Runtime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: false);

            var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
            Assert.Equal("RZ3906", Assert.Single(diagnotics).Id);
        }

        [Fact]
        public void RazorPage_WithCssScope()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
[{typeof(HtmlTargetElementAttribute).FullName}({"\"all\""})]
public class AllTagHelper : {typeof(TagHelper).FullName}
{{
    public string Bar {{ get; set; }}
}}

[{typeof(HtmlTargetElementAttribute).FullName}({"\"form\""})]
public class FormTagHelper : {typeof(TagHelper).FullName}
{{
}}
");

            // Act
            // This test case attempts to use all syntaxes that might interact with auto-generated attributes
            var generated = CompileToCSharp(@"@page
@addTagHelper *, AppCode
@{
    ViewData[""Title""] = ""Home page"";
}
<div class=""text-center"">
    <h1 class=""display-4"">Welcome</h1>
    <p>Learn about<a href= ""https://docs.microsoft.com/aspnet/core"" > building Web apps with ASP.NET Core</a>.</p>
</div>
<all Bar=""Foo""></all>
<form asp-route=""register"" method=""post"">
  <input name=""regular input"" />
</form>
", cssScope: "TestCssScope");

            // Assert
            var intermediate = generated.CodeDocument.GetDocumentIntermediateNode();
            var csharp = generated.CodeDocument.GetCSharpDocument();
            AssertDocumentNodeMatchesBaseline(intermediate);
            AssertCSharpDocumentMatchesBaseline(csharp);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorView_WithCssScope()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
[{typeof(HtmlTargetElementAttribute).FullName}({"\"all\""})]
public class AllTagHelper : {typeof(TagHelper).FullName}
{{
    public string Bar {{ get; set; }}
}}

[{typeof(HtmlTargetElementAttribute).FullName}({"\"form\""})]
public class FormTagHelper : {typeof(TagHelper).FullName}
{{
}}
");

            // Act
            // This test case attempts to use all syntaxes that might interact with auto-generated attributes
            var generated = CompileToCSharp(@"@addTagHelper *, AppCode
@{
    ViewData[""Title""] = ""Home page"";
}
<div class=""text-center"">
    <h1 class=""display-4"">Welcome</h1>
    <p>Learn about<a href= ""https://docs.microsoft.com/aspnet/core"" > building Web apps with ASP.NET Core</a>.</p>
</div>
<all Bar=""Foo""></all>
<form asp-route=""register"" method=""post"">
  <input name=""regular input"" />
</form>
", cssScope: "TestCssScope");

            // Assert
            var intermediate = generated.CodeDocument.GetDocumentIntermediateNode();
            var csharp = generated.CodeDocument.GetCSharpDocument();
            AssertDocumentNodeMatchesBaseline(intermediate);
            AssertCSharpDocumentMatchesBaseline(csharp);
            CompileToAssembly(generated);
        }

        [Fact]
        public void RazorView_Layout_WithCssScope()
        {
                        // Arrange
            AddCSharpSyntaxTree($@"
[{typeof(HtmlTargetElementAttribute).FullName}({"\"all\""})]
public class AllTagHelper : {typeof(TagHelper).FullName}
{{
    public string Bar {{ get; set; }}
}}
[{typeof(HtmlTargetElementAttribute).FullName}({"\"form\""})]
public class FormTagHelper : {typeof(TagHelper).FullName}
{{
}}
");

            // Act
            // This test case attempts to use all syntaxes that might interact with auto-generated attributes
            var generated = CompileToCSharp(@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>@ViewData[""Title""] - Test layout component</title>
</head>
<body>
    <p>This is a body.</p>
</body>
</html>
", cssScope: "TestCssScope");

            // Assert
            var intermediate = generated.CodeDocument.GetDocumentIntermediateNode();
            var csharp = generated.CodeDocument.GetCSharpDocument();
            AssertDocumentNodeMatchesBaseline(intermediate);
            AssertCSharpDocumentMatchesBaseline(csharp);
            CompileToAssembly(generated);
        }
        #endregion

        #region DesignTime

        [Fact]
        public void UsingDirectives_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true, throwOnFailure: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

            var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
            Assert.Equal("The using directive for 'System' appeared previously in this namespace", Assert.Single(diagnostics).GetMessage());
        }

        [Fact]
        public void InvalidNamespaceAtEOF_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

            var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
            Assert.Equal("RZ1014", Assert.Single(diagnotics).Id);
        }

        [Fact]
        public void IncompleteDirectives_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
public class MyService<TModel>
{
    public string Html { get; set; }
}");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

            // We expect this test to generate a bunch of errors.
            Assert.True(compiled.CodeDocument.GetCSharpDocument().Diagnostics.Count > 0);
        }

        [Fact]
        public void InheritsViewModel_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
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
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void InheritsWithViewImports_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
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

}");

            AddProjectItemFromText(@"@inherits MyPageModel<TModel>");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void AttributeDirectiveWithViewImports_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();
            AddProjectItemFromText(@"
@using System
@attribute [Serializable]");

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true, throwOnFailure: false);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

            var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
            Assert.Equal("Duplicate 'Serializable' attribute", Assert.Single(diagnostics).GetMessage());
        }

        [Fact]
        public void MalformedPageDirective_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

            var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
            Assert.Equal("RZ1016", Assert.Single(diagnotics).Id);
        }

        [Fact]
        public void Basic_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void BasicComponent_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile(fileKind: FileKinds.Component);

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void Sections_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void _ViewImports_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void Inject_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
public class MyApp
{
    public string MyProperty { get; set; }
}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void InjectWithModel_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
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
}");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void InjectWithSemicolon_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
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
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void Model_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void MultipleModels_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree(@"
public class ThisShouldBeGenerated
{

}");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

            var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
            Assert.Equal("RZ2001", Assert.Single(diagnotics).Id);
        }

        [Fact]
        public void ModelExpressionTagHelper_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {typeof(TagHelper).FullName}
{{
    public ModelExpression For {{ get; set; }}
}}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void RazorPages_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void RazorPagesWithRouteTemplate_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void RazorPagesWithoutModel_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
public class DivTagHelper : {typeof(TagHelper).FullName}
{{

}}
");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void PageWithNamespace_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void ViewWithNamespace_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void ViewComponentTagHelper_DesignTime()
        {
            // Arrange
            AddCSharpSyntaxTree($@"
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

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void RazorPageWithNoLeadingPageDirective_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertHtmlDocumentMatchesBaseline(compiled.CodeDocument.GetHtmlDocument());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertLinePragmas(compiled.CodeDocument, designTime: true);
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

            var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
            Assert.Equal("RZ3906", Assert.Single(diagnotics).Id);
        }

        #endregion
    }
}
