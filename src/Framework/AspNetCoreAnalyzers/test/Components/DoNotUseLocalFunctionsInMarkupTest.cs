// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

public class DoNotUseLocalFunctionsInMarkupTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new DoNotUseLocalFunctionsInMarkupAnalyzer());

    [Fact]
    public async Task LocalFunctionWithRenderTreeBuilderCall_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void /*MM*/LocalFunction()
        {
            builder.OpenElement(0, ""div"");
            builder.CloseElement();
        }

        LocalFunction();
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        Assert.Equal("ASP0039", analyzerDiagnostic.Id);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
        Assert.StartsWith("Local function 'LocalFunction' accesses RenderTreeBuilder from parent scope", analyzerDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task LocalFunctionWithMultipleRenderTreeBuilderCalls_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void /*MM*/LocalFunction()
        {
            builder.OpenElement(0, ""div"");
            builder.AddContent(1, ""text"");
            builder.CloseElement();
        }

        LocalFunction();
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
        Assert.StartsWith("Local function 'LocalFunction' accesses RenderTreeBuilder from parent scope", analyzerDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task LocalFunctionWithoutRenderTreeBuilderCall_NoDiagnostic()
    {
        var source = @"
void LocalFunction()
{
    var x = 5;
    System.Console.WriteLine(x);
}

LocalFunction();
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithParameterRenderTreeBuilder_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void LocalFunction(RenderTreeBuilder builderParam)
        {
            builderParam.OpenElement(0, ""div"");
            builderParam.CloseElement();
        }

        LocalFunction(builder);
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithLocalRenderTreeBuilder_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void LocalFunction()
        {
            var localBuilder = new RenderTreeBuilder();
            localBuilder.OpenElement(0, ""div"");
            localBuilder.CloseElement();
        }

        LocalFunction();
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithOwningBuilderAlias_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var capturedBuilder = builder;

        void /*MM*/LocalFunction()
        {
            capturedBuilder.OpenElement(0, ""div"");
            capturedBuilder.CloseElement();
        }

        LocalFunction();
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
    }

    [Fact]
    public async Task LocalFunctionWithReassignedOwningBuilderAlias_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var childBuilder = new RenderTreeBuilder();
        var alias = builder;
        alias = childBuilder;

        void LocalFunction()
        {
            alias.OpenElement(0, ""div"");
            alias.CloseElement();
        }

        LocalFunction();
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithAliasReassignedToOwningBuilder_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var alias = new RenderTreeBuilder();
        alias = builder;

        void /*MM*/LocalFunction()
        {
            alias.OpenElement(0, ""div"");
            alias.CloseElement();
        }

        LocalFunction();
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
    }

    [Fact]
    public async Task LocalFunctionWithChainedOwningBuilderAlias_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var alias = new RenderTreeBuilder();
        var another = alias = builder;

        void /*MM*/LocalFunction()
        {
            another.OpenElement(0, ""div"");
            another.CloseElement();
        }

        LocalFunction();
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
    }

    [Fact]
    public async Task LocalFunctionUsedAsRenderFragmentMethodGroup_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void /*MM*/LocalFunction(RenderTreeBuilder childBuilder)
        {
            builder.OpenElement(0, ""div"");
            builder.CloseElement();
        }

        RenderFragment fragment = LocalFunction;
        builder.AddContent(0, fragment);
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
    }

    [Fact]
    public async Task LocalFunctionInIndependentSwitchCaseWithOwningBuilderAlias_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var childBuilder = new RenderTreeBuilder();
        var alias = builder;
        var value = 2;

        switch (value)
        {
            case 1:
                alias = childBuilder;
                break;
            case 2:
                void /*MM*/LocalFunction()
                {
                    alias.OpenElement(0, ""div"");
                    alias.CloseElement();
                }

                LocalFunction();
                break;
        }
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
    }

    [Fact]
    public async Task NestedLocalFunctionWithFreshCapturedBuilder_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void Outer()
        {
            var scratch = new RenderTreeBuilder();

            void Inner()
            {
                scratch.OpenElement(0, ""div"");
                scratch.CloseElement();
            }

            Inner();
        }

        Outer();
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithNestedLambdaBuilderParameter_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        RenderFragment LocalFunction() => childBuilder =>
        {
            childBuilder.OpenElement(0, ""div"");
            childBuilder.CloseElement();
        };

        builder.AddContent(0, LocalFunction());
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithNestedLambdaCapturedRenderTreeBuilder_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        RenderFragment /*MM*/LocalFunction() => childBuilder =>
        {
            builder.OpenElement(0, ""div"");
            builder.CloseElement();
        };

        builder.AddContent(0, LocalFunction());
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
    }

    [Fact]
    public async Task NestedLocalFunctionWithRenderTreeBuilderCall_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void OuterFunction()
        {
            void /*MM*/InnerFunction()
            {
                builder.OpenElement(0, ""div"");
                builder.CloseElement();
            }

            InnerFunction();
        }

        OuterFunction();
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup).ToList();
        var innerFunctionDiagnostic = Assert.Single(analyzerDiagnostics);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, innerFunctionDiagnostic.Location);
        Assert.StartsWith("Local function 'InnerFunction' accesses RenderTreeBuilder from parent scope", innerFunctionDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task StaticLocalFunction_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        static void LocalFunction(RenderTreeBuilder builderParam)
        {
            builderParam.OpenElement(0, ""div"");
            builderParam.CloseElement();
        }

        LocalFunction(builder);
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithMethodInvocation_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void /*MM*/LocalFunction()
        {
            builder.AddMarkupContent(0, ""<div>Hello</div>"");
        }

        LocalFunction();
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
        Assert.StartsWith("Local function 'LocalFunction' accesses RenderTreeBuilder from parent scope", analyzerDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task LocalFunctionOutsideComponentBase_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components.Rendering;

public class NotAComponent
{
    public void SomeMethod()
    {
        var builder = new RenderTreeBuilder();

        void LocalFunction()
        {
            builder.OpenElement(0, ""div"");
            builder.CloseElement();
        }

        LocalFunction();
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionInBuildRenderTreeOverload_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    private void BuildRenderTree()
    {
        var builder = new RenderTreeBuilder();

        void LocalFunction()
        {
            builder.OpenElement(0, ""div"");
            builder.CloseElement();
        }

        LocalFunction();
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionInGeneratedBuildRenderTree_ProducesDiagnostic()
    {
        var source = TestSource.Read(@"
using System.CodeDom.Compiler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

[GeneratedCode(""Razor"", ""1.0"")]
public class TestComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        void /*MM*/LocalFunction()
        {
            builder.OpenElement(0, ""div"");
            builder.CloseElement();
        }

        LocalFunction();
    }
}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
    }

    [Fact]
    public async Task LocalFunctionInRazorGeneratedBuildRenderTree_ProducesDiagnostic()
    {
        var razorSource = """
@using Microsoft.AspNetCore.Components
@inherits ComponentBase
@{
    void RenderTree(int depth, int maxDepth)
    {
        if (depth >= maxDepth)
        {
            return;
        }

        <FluentTreeItem Text="item">
            @{ RenderTree(depth + 1, maxDepth); }
        </FluentTreeItem>
    }

    RenderTree(0, 2);
}
""";
        var projectEngine = RazorProjectEngine.Create(
            RazorConfiguration.Default,
            RazorProjectFileSystem.Create("/"),
            builder => builder.SetRootNamespace("Test"));
        var codeDocument = projectEngine.Process(
            RazorSourceDocument.Create(razorSource, "/IssueSample.razor"),
            FileKinds.Component,
            Array.Empty<RazorSourceDocument>(),
            Array.Empty<TagHelperDescriptor>());
        var generatedCode = codeDocument.GetCSharpDocument().GeneratedCode;

        Assert.Contains("void RenderTree(int depth, int maxDepth)", generatedCode);
        Assert.Contains("__builder.OpenElement", generatedCode);

        var diagnostics = await Runner.GetDiagnosticsAsync(generatedCode);

        var analyzerDiagnostic = Assert.Single(diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup));
        Assert.Equal("/IssueSample.razor", analyzerDiagnostic.Location.GetMappedLineSpan().Path);
    }

    [Fact]
    public async Task LocalFunctionInNonBuildRenderTreeMethod_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

public class TestComponent : ComponentBase
{
    private void SomeOtherMethod()
    {
        var builder = new RenderTreeBuilder();

        void LocalFunction()
        {
            builder.OpenElement(0, ""div"");
            builder.CloseElement();
        }

        LocalFunction();
    }
}
";
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }
}
