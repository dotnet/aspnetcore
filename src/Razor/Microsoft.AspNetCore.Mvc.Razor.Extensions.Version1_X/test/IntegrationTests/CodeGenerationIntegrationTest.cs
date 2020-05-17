// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X.IntegrationTests
{
    public class CodeGenerationIntegrationTest : IntegrationTestBase
    {
        private readonly static CSharpCompilation DefaultBaseCompilation = MvcShim.BaseCompilation.WithAssemblyName("AppCode");

        public CodeGenerationIntegrationTest()
            : base(generateBaselines: null)
        {
            Configuration = RazorConfiguration.Create(
                RazorLanguageVersion.Version_1_1,
                "MVC-1.1",
                new[] { new AssemblyExtension("MVC-1.1", typeof(ExtensionInitializer).Assembly) });
        }

        protected override CSharpCompilation BaseCompilation => DefaultBaseCompilation;

        protected override RazorConfiguration Configuration { get; }

        [Fact]
        public void InvalidNamespaceAtEOF_DesignTime()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

            var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
            Assert.Equal("RZ1007", Assert.Single(diagnotics).Id);
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }

        [Fact]
        public void InheritsWithViewImports_DesignTime()
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

}");

            AddProjectItemFromText(@"@inherits MyBasePageForViews<TModel>");

            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToAssembly(projectItem, designTime: true);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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
            AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        }
    }
}
