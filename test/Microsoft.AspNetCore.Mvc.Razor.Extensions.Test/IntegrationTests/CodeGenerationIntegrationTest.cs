// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        public void UsingDirectives_Runtime()
        {
            var compilation = BaseCompilation;

            RunRuntimeTest(compilation, new[] { "The using directive for 'System' appeared previously in this namespace" });
        }

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

        [Fact]
        public void RazorPageWithNoLeadingPageDirective_Runtime()
        {
            var compilation = BaseCompilation;

            RunRuntimeTest(compilation);
        }
        #endregion

        #region DesignTime

        [Fact]
        public void UsingDirectives_DesignTime()
        {
            var compilation = BaseCompilation;

            RunDesignTimeTest(compilation, new[] { "The using directive for 'System' appeared previously in this namespace" });
        }

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

        [Fact]
        public void RazorPageWithNoLeadingPageDirective_DesignTime()
        {
            var compilation = BaseCompilation;

            RunDesignTimeTest(compilation);
        }
        #endregion

        private void RunRuntimeTest(
            CSharpCompilation baseCompilation,
            IEnumerable<string> expectedWarnings = null)
        {
            Assert.Empty(baseCompilation.GetDiagnostics());

            // Arrange
            var engine = CreateEngine(baseCompilation);
            var projectItem = CreateProjectItem();

            // Act
            var document = engine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
            AssertDocumentCompiles(document, baseCompilation, expectedWarnings);
        }

        private void RunDesignTimeTest(
            CSharpCompilation baseCompilation,
            IEnumerable<string> expectedWarnings = null)
        {
            Assert.Empty(baseCompilation.GetDiagnostics());

            // Arrange
            var engine = CreateEngine(baseCompilation);
            var projectItem = CreateProjectItem();

            // Act
            var document = engine.ProcessDesignTime(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(document.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(document);
            AssertDocumentCompiles(document, baseCompilation, expectedWarnings);
        }

        private void AssertDocumentCompiles(
            RazorCodeDocument document,
            CSharpCompilation baseCompilation,
            IEnumerable<string> expectedWarnings = null)
        {
            var cSharp = document.GetCSharpDocument().GeneratedCode;

            var syntaxTree = CSharpSyntaxTree.ParseText(cSharp);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var references = baseCompilation.References.Concat(new[] { baseCompilation.ToMetadataReference() });
            var compilation = CSharpCompilation.Create("CodeGenerationTestAssembly", new[] { syntaxTree }, references, options);

            var diagnostics = compilation.GetDiagnostics();

            var warnings = diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning);

            if (expectedWarnings == null)
            {
                Assert.Empty(warnings);
            }
            else
            {
                Assert.Equal(expectedWarnings, warnings.Select(e => e.GetMessage()));
            }
        }

        protected RazorProjectEngine CreateEngine(CSharpCompilation compilation)
        {
            var references = compilation.References.Concat(new[] { compilation.ToMetadataReference() });

            return CreateProjectEngine(b =>
            {
                RazorExtensions.Register(b);

                var existingImportFeature = b.Features.OfType<IImportProjectFeature>().Single();
                b.SetImportFeature(new NormalizedDefaultImportFeature(existingImportFeature));

                b.Features.Add(GetMetadataReferenceFeature(references));
                b.Features.Add(new CompilationTagHelperFeature());
            });
        }

        private static IRazorEngineFeature GetMetadataReferenceFeature(IEnumerable<MetadataReference> references)
        {
            return new DefaultMetadataReferenceFeature()
            {
                References = references.ToList()
            };
        }

        private class NormalizedDefaultImportFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
        {
            private IImportProjectFeature _existingFeature;

            public NormalizedDefaultImportFeature(IImportProjectFeature existingFeature)
            {
                _existingFeature = existingFeature;
            }

            protected override void OnInitialized()
            {
                _existingFeature.ProjectEngine = ProjectEngine;
            }

            public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
            {
                var normalizedImports = new List<RazorProjectItem>();
                var imports = _existingFeature.GetImports(projectItem);
                foreach (var import in imports)
                {
                    var text = string.Empty;
                    using (var stream = import.Read())
                    using (var reader = new StreamReader(stream))
                    {
                        text = reader.ReadToEnd().Trim();
                    }

                    // It's important that we normalize the newlines in the default imports. The default imports will
                    // be created with Environment.NewLine, but we need to normalize to `\r\n` so that the indices
                    // are the same on xplat.
                    var normalizedText = Regex.Replace(text, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
                    var normalizedImport = new TestRazorProjectItem(import.FilePath, import.PhysicalPath, import.RelativePhysicalPath, import.BasePath)
                    {
                        Content = normalizedText
                    };

                    normalizedImports.Add(normalizedImport);
                }

                return normalizedImports;
            }
        }
    }
}
