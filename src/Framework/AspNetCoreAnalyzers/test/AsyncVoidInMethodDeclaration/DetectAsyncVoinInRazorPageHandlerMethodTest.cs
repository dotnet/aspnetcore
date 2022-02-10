// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public class DetectAsyncVoinInRazorPageHandlerMethodTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new AsyncVoidInMethodDeclarationAnalyzer());

    [Fact]
    public async Task AsyncVoidDiagnosted_RazorPageHandler()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mvc.Pages;

public class IndexModel : PageModel
{
    public IndexModel() {}

    public async void OnGet() {}
}

public class Program { public static void Main() {} }
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostic.Descriptor);
    }

    [Fact]
    public async Task AsyncVoidDiagnosted_RazorPageWithNotOnlyHandlerMethod()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mvc.Pages;

public class IndexModel : PageModel
{
    public IndexModel() {}

    public async void OnGet() {}

    public async void Get() {}
}

public class Program { public static void Main() {} }
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostic.Descriptor);
    }

    [Fact]
    public async Task AsyncVoidDiagnosted_RazorPageWithMultipleHandlers()
    {
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mvc.Pages;

public class IndexModel : PageModel
{
    public IndexModel() {}

    public async void OnGet() {}

    public async Task OnPost() {}
}

public class Program { public static void Main() {} }
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostic.Descriptor);
    }

    [Fact]
    public async Task AsyncVoidDiagnostedMultipleTime_RazorPageHandler()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mvc.Pages;

public class IndexModel : PageModel
{
    public IndexModel() {}

    public async void OnGet() {}

    public async void OnPost() {}

    public async void OnPut() {}

    public async void OnDelete() {}
}

public class Program { public static void Main() {} }
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Equal(4, diagnostics.Length);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[0].Descriptor);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[1].Descriptor);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[2].Descriptor);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[3].Descriptor);
    }

    [Fact]
    public async Task AsyncVoidNotDiagnosted_RazorPageHandler()
    {
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mvc.Pages;

public class IndexModel : PageModel
{
    public IndexModel() {}

    public async Task OnGet() {}

    public async Task OnPost() {}

    public async Task OnPut() {}

    public async Task OnDelete() {}
}

public class Program { public static void Main() {} }
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AsyncVoidNotDiagnosted_RazorPageWithNotOnlyHandlerMethod()
    {
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mvc.Pages;

public class IndexModel : PageModel
{
    public IndexModel() {}

    public async Task OnGet() {}

    public async void Get() {}
}

public class Program { public static void Main() {} }
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AsyncVoidNotDiagnosted_RazorPageHandlerWithNonHandlerAttribute()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mvc.Pages;

public class IndexModel : PageModel
{
    public IndexModel() {}

    [NonHandler]
    public async void OnGet() {}
}

public class Program { public static void Main() {} }
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Empty(diagnostics);
    }
}
