// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

public class AttributesShouldNotBeAppliedToPageModelAnalyzerTest
{
    [Fact]
    public Task NoDiagnosticsAreReturned_ForControllerBaseActions()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class NoDiagnosticsAreReturned_ForControllerBaseActions : ControllerBase
    {
        [Authorize]
        public IActionResult AuthorizeAttribute() => null;

        [ServiceFilter(typeof(object))]
        public IActionResult ServiceFilter() => null;
    }
}";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_ForControllerActions()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class NoDiagnosticsAreReturned_ForControllerActions : Controller
    {
        [Authorize]
        public IActionResult AuthorizeAttribute() => null;

        [ServiceFilter(typeof(object))]
        public IActionResult ServiceFilter() => null;
    }
}";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_ForPageHandlersWithNonFilterAttributes()
    {
        var source = @"
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class NoDiagnosticsAreReturned_ForPageHandlersWithNonFilterAttributes : PageModel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnGet()
        {
        }
    }
}
";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_IfFiltersAreAppliedToPageModel()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [ServiceFilter(typeof(object))]
    public class NoDiagnosticsAreReturned_IfFiltersAreAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageModel()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [Authorize]
    public class NoDiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageModel()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [AllowAnonymous]
    public class NoDiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturned_ForNonHandlerMethodsWithAttributes()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class NoDiagnosticsAreReturned_ForNonHandlerMethodsWithAttributes : PageModel
    {
        [Authorize]
        private void OnGetPrivate() { }

        [TypeFilter(typeof(object))]
        internal IActionResult OnPost() => null;

        [AllowAnonymous]
        public void OnGet<T>() { }

        [ServiceFilter(typeof(object))]
        public static void OnPostStatic() { }
    }
}";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethod()
    {
        var source = @"
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethod : PageModel
    {
        [{|#0:ServiceFilter(typeof(object))|}]
        public void OnGet()
        {
        }
    }
}";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods)
            .WithLocation(0)
            .WithArguments("ServiceFilterAttribute");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethodDerivingFromCustomModel()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [PageModel]
    public abstract class CustomPageModel
    {

    }

    public class DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethodDerivingFromCustomModel : CustomPageModel
    {
        [{|#0:ServiceFilter(typeof(object))|}]
        public void OnGet()
        {
        }
    }
}";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods)
           .WithLocation(0)
           .WithArguments("ServiceFilterAttribute");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageHandlerMethod()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class DiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageHandlerMethod : PageModel
    {
        [{|#0:Authorize|}]
        public void OnPost()
        {
        }
    }
}";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods)
           .WithLocation(0)
           .WithArguments("AuthorizeAttribute");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethodForTypeWithPageModelAttribute()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [PageModel]
    public class DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethodForTypeWithPageModelAttribute
    {
        [{|#0:ServiceFilter(typeof(object))|}]
        public void OnGet()
        {
        }
    }
}";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods)
          .WithLocation(0)
          .WithArguments("ServiceFilterAttribute");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfAttributeIsAppliedToBaseType()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [PageModel]
    public abstract class DiagnosticsAreReturned_IfAttributeIsAppliedToBaseTypeBase
    {
        [{|#0:Authorize|}]
        public void OnGet() { }
    }

    public class DiagnosticsAreReturned_IfAttributeIsAppliedToBaseType : DiagnosticsAreReturned_IfAttributeIsAppliedToBaseTypeBase
    {
    }
}
";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods)
         .WithLocation(0)
         .WithArguments("AuthorizeAttribute");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfRouteAttributesAreAppliedToPageHandlerMethod()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [{|#0:Route(""/mypage"")|}]
    public class DiagnosticsAreReturned_IfRouteAttribute_IsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MVC1003_RouteAttributesShouldNotBeAppliedToPageModels)
         .WithLocation(0)
         .WithArguments("RouteAttribute");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageHandlerMethod()
    {
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class DiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageHandlerMethod : PageModel
    {
        [{|#0:AllowAnonymous|}]
        public void OnGet()
        {

        }

        public void OnPost()
        {
        }
    }
}";
        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods)
         .WithLocation(0)
         .WithArguments("AllowAnonymousAttribute");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfRouteAttribute_IsAppliedToPageModel()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class DiagnosticsAreReturned_IfRouteAttributesAreAppliedToPageHandlerMethod : PageModel
    {
        [{|#0:HttpHead|}]
        public void OnGet()
        {
        }
    }
}";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.MVC1002_RouteAttributesShouldNotBeAppliedToPageHandlerMethods)
         .WithLocation(0)
         .WithArguments("HttpHeadAttribute");

        return VerifyAnalyzerAsync(source, diagnosticResult);
    }

    private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new AttributesShouldNotBeAppliedToPageModelCSharpAnalyzerTest(TestReferences.MetadataReferences)
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    private sealed class AttributesShouldNotBeAppliedToPageModelCSharpAnalyzerTest : CSharpAnalyzerTest<AttributesShouldNotBeAppliedToPageModelAnalyzer, DefaultVerifier>
    {
        public AttributesShouldNotBeAppliedToPageModelCSharpAnalyzerTest(ImmutableArray<MetadataReference> metadataReferences)
        {
            TestState.AdditionalReferences.AddRange(metadataReferences);
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { new AttributesShouldNotBeAppliedToPageModelAnalyzer() };
    }
}
