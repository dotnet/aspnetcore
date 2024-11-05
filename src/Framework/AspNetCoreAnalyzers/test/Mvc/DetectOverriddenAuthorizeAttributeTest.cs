// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<Microsoft.AspNetCore.Analyzers.Mvc.MvcAnalyzer>;

namespace Microsoft.AspNetCore.Analyzers.Mvc;

public partial class DetectOverriddenAuthorizeAttributeTest
{
    private const string CommonPrefix = """
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

WebApplication.Create().Run();

""";

    [Fact]
    public async Task AuthorizeOnAction_AllowAnonymousOnController_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyController
{
    [{|#0:Authorize|}]
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyController").WithLocation(0)
        );
    }

    [Fact]
    public async Task AllowAnonymousOnAction_AuthorizeOnController_NoDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[Authorize]
public class MyController
{
    [AllowAnonymous]
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AuthorizeOnAction_AllowAnonymousOnControllerBase_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyControllerBase
{
}

public class MyController : MyControllerBase
{
    [{|#0:Authorize|}]
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBase").WithLocation(0)
        );
    }

    [Fact]
    public async Task AuthorizeOnActionControllerAndAction_AllowAnonymousOnControllerBase_HasMultipleDiagnostics()
    {
        // The closest Authorize attribute to the action reported if multiple could be considered overridden.
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
[{|#0:Authorize|}]
public class MyControllerBase
{
}

[{|#1:Authorize|}]
public class MyController : MyControllerBase
{
    [{|#2:Authorize|}]
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBase").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBase").WithLocation(1),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBase").WithLocation(2)
        );
    }

    [Fact]
    public async Task AuthorizeOnController_AllowAnonymousOnControllerBase_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyControllerBase
{
}

[{|#0:Authorize|}]
public class MyController : MyControllerBase
{
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBase").WithLocation(0)
        );
    }

    [Fact]
    public async Task AuthorizeOnControllerWithMultipleActions_AllowAnonymousOnControllerBase_HasSingleDiagnostic()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyControllerBase
{
}

[{|#0:Authorize|}]
public class MyController : MyControllerBase
{
    public object Get() => new();
    public object AnotherGet() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBase").WithLocation(0)
        );
    }

    [Fact]
    public async Task AuthorizeOnControllerBaseWithMultipleChildren_AllowAnonymousOnControllerBaseBaseType_HasMultipleDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyControllerBaseBase
{
}

[{|#0:Authorize|}]
public class MyControllerBase : MyControllerBaseBase
{
}

public class MyController : MyControllerBase
{
    public object Get() => new();
    public object AnotherGet() => new();
}

public class MyOtherController : MyControllerBase
{
    public object Get() => new();
    public object AnotherGet() => new();
}
""";

        // This is not ideal. If someone comes along and fixes it to be a single diagnostic, feel free to update this test to end with
        // "HasSingleDiagnostic" instead of "HasMultipleDiagnostics". I think fixing it will require disabling parallelization of the
        // entire MvcAnalyzer which I don't think is a good tradeoff. I assume that this scenario is rare enough and that overreporting
        // is benign enough to not warrant the performance hit.
        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase").WithLocation(0)
        );
    }

    [Fact]
    public async Task CustomAuthorizeOnAction_CustomAllowAnonymousOnController_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[MyAllowAnonymous]
public class MyController
{
    [{|#0:MyAuthorize|}]
    public object Get() => new();
}

public class MyAuthorizeAttribute : Attribute, IAuthorizeData
{
    public string? Policy { get; set; }
    public string? Roles { get; set; }
    public string? AuthenticationSchemes { get; set; }
}

public class MyAllowAnonymousAttribute : Attribute, IAllowAnonymous
{
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyController").WithLocation(0)
        );
    }

    [Fact]
    public async Task AuthorizeOnAction_NonInheritableAllowAnonymousOnController_NoDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[MyAllowAnonymous]
public class MyControllerBase
{
}

public class MyController : MyControllerBase
{
    [Authorize]
    public object Get() => new();
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MyAllowAnonymousAttribute : Attribute, IAllowAnonymous
{
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CustomAuthorizeCombinedWithAllowAnonymousOnAction_AllowAnonymousOnController_NoDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyController
{
    [MyAuthorize]
    public object Get() => new();
}

public class MyAuthorizeAttribute : Attribute, IAuthorizeData, IAllowAnonymous
{
    public string? Policy { get; set; }
    public string? Roles { get; set; }
    public string? AuthenticationSchemes { get; set; }
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AuthorizeBeforeAllowAnonymousOnAction_AllowAnonymousOnController_NoDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyController
{
    [Authorize(AuthenticationSchemes = "foo")]
    [AllowAnonymous]
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AuthorizeAfterAllowAnonymousOnAction_NoAttributeOnController_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
public class MyController
{
    [AllowAnonymous]
    [{|#0:Authorize|}]
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyController.Get").WithLocation(0)
        );
    }

    [Fact]
    public async Task NoAttributeOnAction_AuthorizeBeforeAllowAnonymousOnController_NoDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[Authorize(AuthenticationSchemes = "foo")]
[AllowAnonymous]
public class MyController
{
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoAttributeOnAction_AuthorizeAfterAllowAnonymousOnController_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
[{|#0:Authorize|}]
public class MyController
{
    public object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyController").WithLocation(0)
        );
    }

    [Fact]
    public async Task AuthorizeOnAction_AllowAnonymousOnBaseMethod_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
public class MyControllerBase
{
    [AllowAnonymous]
    public virtual object Get() => new();
}

public class MyController : MyControllerBase
{
    [{|#0:Authorize|}]
    public override object Get() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBase.Get").WithLocation(0)
        );
    }

    [Fact]
    public async Task AllowAnonymousOnVirtualBaseActionWithNoOverride_AuthorizeOnController_NoDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
public class MyControllerBase
{
    [AllowAnonymous]
    public virtual object Get() => new();
}

[Authorize]
public class MyController : MyControllerBase
{
}
""";

        // I suspect [AllowAnonymous] on a specific action that never needs auth despite the child controller
        // configuring auth is a fairly common pattern. However, if the action is explicitly overridden,
        // you need to reapply [AllowAnonymous] on the override to silence the warning and clarify your intent.
        // Making people do this when there isn't even an override is a bit onerous.
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AllowAnonymousOnVirtualBaseActionAndOverride_AuthorizeOnController_NoDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
public class MyControllerBase
{
    [AllowAnonymous]
    public virtual object Get() => new();
}

[Authorize]
public class MyController : MyControllerBase
{
    [AllowAnonymous]
    public override object Get() => new();
    public object AnotherGet() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AllowAnonymousOnVirtualBaseBaseActionButNotOverride_AuthorizeOnControllerBase_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
public class MyControllerBaseBase
{
    [AllowAnonymous]
    public virtual object Get() => new();
}

[{|#0:Authorize|}]
public class MyControllerBase : MyControllerBaseBase
{
}

public class MyController : MyControllerBase
{
    public override object Get() => new();
    public object AnotherGet() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase.Get").WithLocation(0)
        );
    }

    [Fact]
    public async Task AllowAnonymousOnVirtualBaseActionButNotOverride_AuthorizeOnController_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
public class MyControllerBase
{
    [AllowAnonymous]
    public virtual object Get() => new();
}

[{|#0:Authorize|}]
public class MyController : MyControllerBase
{
    public override object Get() => new();
    public object AnotherGet() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBase.Get").WithLocation(0)
        );
    }

    [Fact]
    public async Task AllowAnonymousOnBaseBaseController_AuthorizeOnControllerBase_HasSingleDiagnostic()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyControllerBaseBase
{
    public virtual object Get() => new();
}

[{|#0:Authorize|}]
public class MyControllerBase : MyControllerBaseBase
{
}

public class MyController : MyControllerBase
{
    public override object Get() => new();
    public object AnotherGet() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase").WithLocation(0)
        );
    }

    [Fact]
    public async Task AllowAnonymousOnControllerBaseBaseType_AuthorizeOnControllerBaseAndAction_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class MyControllerBaseBase
{
    [{|#2:Authorize|}]
    public virtual object Get() => new();
}

[{|#0:Authorize|}]
public class MyControllerBase : MyControllerBaseBase
{
    [{|#1:Authorize|}]
    public override object Get() => new();
}

public class MyController : MyControllerBase
{
    public override object Get() => new();
    public object AnotherGet() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase").WithLocation(1),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase").WithLocation(2)
        );
    }

    [Fact]
    public async Task AllowAnonymousOnBaseBaseType_AuthorizeOnControllerBaseAndAction_HasDiagnostics()
    {
        var source = $$"""
{{CommonPrefix}}
[Authorize]
public class MyControllerBaseBase
{
    [AllowAnonymous]
    public virtual object Get() => new();
}

[{|#0:Authorize|}]
public class MyControllerBase : MyControllerBaseBase
{
    [{|#1:Authorize|}]
    public override object Get() => new();
}

public class MyController : MyControllerBase
{
    public override object Get() => new();
    public object AnotherGet() => new();
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase.Get").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("MyControllerBaseBase.Get").WithLocation(1)
        );
    }

    [Fact]
    public async Task AttributesOnMultipleActions_AttributesOnMultipleControllers_HasDiagnostics()
    {
        // Copied from https://github.com/dotnet/aspnetcore/issues/43550#issuecomment-1940287150
        var source = $$"""
{{CommonPrefix}}
[AllowAnonymous]
public class OAuthControllerAnon : ControllerBase
{
}

[Authorize]
public class OAuthControllerAuthz : ControllerBase
{
}

[{|#0:Authorize|}] // BUG
public class OAuthControllerInherited : OAuthControllerAnon
{
}

public class OAuthControllerInherited2 : OAuthControllerAnon
{
    [{|#1:Authorize|}] // BUG
    public IActionResult Privacy()
    {
        return null;
    }
}

[AllowAnonymous]
[{|#2:Authorize|}] // BUG
public class OAuthControllerMultiple : ControllerBase
{
}

[AllowAnonymous]
public class OAuthControllerInherited3 : ControllerBase
{
    [{|#3:Authorize|}] // BUG
    public IActionResult Privacy()
    {
        return null;
    }
}
""";

        await VerifyCS.VerifyAnalyzerAsync(source,
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("OAuthControllerAnon").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("OAuthControllerAnon").WithLocation(1),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("OAuthControllerMultiple").WithLocation(2),
            new DiagnosticResult(DiagnosticDescriptors.OverriddenAuthorizeAttribute).WithArguments("OAuthControllerInherited3").WithLocation(3)
        );
    }
}
