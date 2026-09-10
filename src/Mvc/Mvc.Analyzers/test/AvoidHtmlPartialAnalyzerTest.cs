// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

public class AvoidHtmlPartialAnalyzerTest
{
    private static readonly DiagnosticDescriptor DiagnosticDescriptor = DiagnosticDescriptors.MVC1000_HtmlHelperPartialShouldBeAvoided;

    [Fact]
    public Task NoDiagnosticsAreReturned_ForNonUseOfHtmlPartial()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class NoDiagnosticsAreReturned_ForNonUseOfHtmlPartial : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 25, true);
            WriteLiteral(""Hello world"");
            EndContext();
            EndContext();
        }
#pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";

        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_ForUseOfHtmlPartialAsync()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class NoDiagnosticsAreReturned_ForUseOfHtmlPartialAsync : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            await Html.PartialAsync(""Some - Partial"");
        }
#pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591";

        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfHtmlPartial()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfHtmlPartial : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 25, true);
            Write({|#0:Html.Partial(""Some - Partial"")|});
            EndContext();
            EndContext();
        }
#pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0)
            .WithArguments("Partial");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfHtmlPartial_WithAdditionalParameters()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfHtmlPartial_WithAdditionalParameters : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 25, true);
            Write({|#0:Html.Partial(""Some - Partial"", new object())|});
            EndContext();
            EndContext();
        }
#pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0)
            .WithArguments("Partial");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfHtmlPartial_InSections()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfHtmlPartial_InSections : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 25, true);
            DefineSection(""name"", async () => {
                Write({|#0:Html.Partial(""Test"")|});
        });
            EndContext();
        EndContext();
    }
#pragma warning restore 1998
    [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
    public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
    [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
    public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
    [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
    public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
    [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
    public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
    [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
    public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
}
}
#pragma warning restore 1591";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0)
            .WithArguments("Partial");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_ForUseOfRenderPartialAsync()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class NoDiagnosticsAreReturned_ForUseOfRenderPartialAsync : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 25, true);
            await Html.RenderPartialAsync(""Some - Partial"");
            EndContext();
            EndContext();
        }
#pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591";

        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfRenderPartial()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfRenderPartial : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 25, true);
            {|#0:Html.RenderPartial(""Some - Partial"")|};
            EndContext();
            EndContext();
        }
#pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0)
            .WithArguments("RenderPartial");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfRenderPartial_WithAdditionalParameters()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfRenderPartial_WithAdditionalParameters : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 25, true);
            {|#0:Html.RenderPartial(""Some-Partial"", new object())|};
            EndContext();
            EndContext();
        }
#pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0)
            .WithArguments("RenderPartial");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForUseOfRenderPartial_InSections()
    {
        var source = @"
namespace AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class DiagnosticsAreReturned_ForUseOfHtmlRenderPartial_InSections : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
#pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 25, true);
            DefineSection(""name"", async () => {
                {|#0:Html.RenderPartial(""Test"")|};
            });
            EndContext();
            EndContext();
        }
#pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptor)
            .WithLocation(0)
            .WithArguments("RenderPartial");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new AvoidHtmlPartialCSharpAnalyzerTest(TestReferences.MetadataReferences)
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    internal sealed class AvoidHtmlPartialCSharpAnalyzerTest : CSharpAnalyzerTest<AvoidHtmlPartialAnalyzer, DefaultVerifier>
    {
        public AvoidHtmlPartialCSharpAnalyzerTest(ImmutableArray<MetadataReference> metadataReferences)
        {
            TestState.AdditionalReferences.AddRange(metadataReferences);
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { new AvoidHtmlPartialAnalyzer() };
    }
}
