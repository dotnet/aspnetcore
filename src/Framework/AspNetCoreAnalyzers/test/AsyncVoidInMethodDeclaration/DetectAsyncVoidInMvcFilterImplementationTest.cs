// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public class DetectAsyncVoidInMvcFilterImplementationTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new AsyncVoidInMethodDeclarationAnalyzer());

    [Theory]
    [InlineData("async void", 2)]
    [InlineData("void", 0)]
    public async Task AsyncVoidDetection_FilterImplementsIActionFilter(string returnClause, short expectedDiagnosticsNumber)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mvc.Filters;

public class SomeFilter : IActionFilter
{{
    public {returnClause} OnActionExecuted(ActionExecutedContext context)
    {{}}

    public {returnClause} OnActionExecuting(ActionExecutingContext context)
    {{}}
}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        DoAssert(expectedDiagnosticsNumber, diagnostics);
    }

    [Theory]
    [InlineData("async void", 1)]
    [InlineData("void", 0)]
    public async Task AsyncVoidDetection_FilterImplementsIAuthorizationFilter(string returnClause, short expectedDiagnosticsNumber)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mvc.Filters;

public class SomeFilter : IAuthorizationFilter
{{
    public {returnClause} OnAuthorization(AuthorizationFilterContext context)
    {{}}
}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        DoAssert(expectedDiagnosticsNumber, diagnostics);
    }

    [Theory]
    [InlineData("async void", 1)]
    [InlineData("void", 0)]
    public async Task AsyncVoidDetection_FilterImplementsIExceptionFilter(string returnClause, short expectedDiagnosticsNumber)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mvc.Filters;

public class SomeFilter : IExceptionFilter
{{
    public {returnClause} OnException(ExceptionContext context)
    {{}}
}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        DoAssert(expectedDiagnosticsNumber, diagnostics);
    }

    [Theory]
    [InlineData("async void", 2)]
    [InlineData("void", 0)]
    public async Task AsyncVoidDetection_FilterImplementsIResourceFilter(string returnClause, short expectedDiagnosticsNumber)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mvc.Filters;

public class SomeFilter : IResourceFilter
{{
    public {returnClause} OnResourceExecuted(ResourceExecutedContext context)
    {{}}

    public {returnClause} OnResourceExecuting(ResourceExecutingContext context)
    {{}}
}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        DoAssert(expectedDiagnosticsNumber, diagnostics);
    }

    [Theory]
    [InlineData("async void", 2)]
    [InlineData("void", 0)]
    public async Task AsyncVoidDetection_FilterImplementsIResultFilter(string returnClause, short expectedDiagnosticsNumber)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mvc.Filters;

public class SomeFilter : IResultFilter
{{
    public {returnClause} OnResultExecuted(ResultExecutedContext context)
    {{}}

    public {returnClause} OnResultExecuting(ResultExecutingContext context)
    {{}}
}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        DoAssert(expectedDiagnosticsNumber, diagnostics);
    }

    [Theory]
    [InlineData("async void", 4)]
    [InlineData("void", 0)]
    public async Task AsyncVoidDetection_FilterImplementsMultipleInterfaces(string returnClause, short expectedDiagnosticsNumber)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mvc.Filters;

public class SomeFilter : IResultFilter, IActionFilter
{{
    public {returnClause} OnResultExecuted(ResultExecutedContext context)
    {{}}

    public {returnClause} OnResultExecuting(ResultExecutingContext context)
    {{}}

    public {returnClause} OnActionExecuted(ActionExecutedContext context)
    {{}}

    public {returnClause} OnActionExecuting(ActionExecutingContext context)
    {{}}
}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        DoAssert(expectedDiagnosticsNumber, diagnostics);
    }

    [Theory]
    [InlineData("async void", 5)]
    [InlineData("void", 0)]
    public async Task AsyncVoidDetection_FilterHasNotOnlyInterfaceMethods(string returnClause, short expectedDiagnosticsNumber)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mvc.Filters;

public class SomeFilter : IActionFilter
{{
    public {returnClause} OnActionExecuted(ActionExecutedContext context)
    {{}}

    public {returnClause} OnActionExecuting(ActionExecutingContext context)
    {{}}

    public {returnClause} GetData()
    {{}}

    protected {returnClause} Validate()
    {{}}

    private {returnClause} Serialize()
    {{}}
}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        DoAssert(expectedDiagnosticsNumber, diagnostics);
    }

    private void DoAssert(int expectedDiagnosticsNumber, CodeAnalysis.Diagnostic[] resultedDisgnostic)
    {
        Assert.Equal(expectedDiagnosticsNumber, resultedDisgnostic.Length);
        for (int i = 0; i < expectedDiagnosticsNumber; i++)
        {
            Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, resultedDisgnostic[i].Descriptor);
        }
    }
}
