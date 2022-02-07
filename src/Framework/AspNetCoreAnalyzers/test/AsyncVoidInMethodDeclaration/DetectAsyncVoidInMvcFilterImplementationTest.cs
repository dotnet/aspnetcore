// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public class DetectAsyncVoidInMvcFilterImplementationTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new AsyncVoidInMethodDeclarationAnalyzer());

    [Fact]
    public async Task AsyncVoidDiagnosted_ControllerDetectedBySuffix()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mvc.Controllers;

public class SomeFilter : IActionFilter
{
    public async void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public async void OnActionExecuting(ActionExecutingContext context)
    {
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Equal(2, diagnostics.Count());
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[0].Descriptor);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[1].Descriptor);
    }
}
