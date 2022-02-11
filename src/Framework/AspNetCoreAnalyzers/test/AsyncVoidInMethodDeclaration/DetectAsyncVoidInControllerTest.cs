// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public class DetectAsyncVoidInControllerTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new AsyncVoidInMethodDeclarationAnalyzer());

    [Fact]
    public async Task AsyncVoidDiagnosted_ControllerDetectedBySuffix()
    {
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Mvc;

public class HomeController : Base
{
    public async void Index()
    {}
}

public class Base
{}

public class Program { public static void Main() {}}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostic.Descriptor);
    }

    [Theory]
    [InlineData("Controller")]
    [InlineData("ControllerBase")]
    [InlineData("MyCustomController")]
    public async Task AsyncVoidDiagnosted_ControllerDetectedByAncestor(string ancestor)
    {
        var source = TestSource.Read($@"
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public class Home : {ancestor}
{{
    public async void Index()
    {{
    }}
}}

public abstract class MyCustomController {{}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostic.Descriptor);
    }

    [Theory]
    [InlineData("[Controller]")]
    [InlineData("[ApiController]")]
    [InlineData("[Route(\"/api\")]\n[Controller]")]
    public async Task AsyncVoidDiagnosted_ControllerDetectedByAttribute(string attribute)
    {
        var source = TestSource.Read($@"
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

{attribute}
public class Home : Base
{{
    public async void Index()
    {{
    }}
}}

public class Base
{{
}}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostic.Descriptor);
    }

    [Fact]
    public async Task AsyncVoidNotDiagnosted_Controller()
    {
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[Route(""/api"")]
[Controller]
public class Home : Base
{
    public async Task Index()
    {}
}

public class Base : Controller
{
    public async Task Get()
    {}
}

public class AuthController : Base
{
    public async Task Auth()
    {}
}

public class Program { public static void Main() {}}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AsyncVoidDiagnosted_ControllerWithVariousMethodSignatures()
    {
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Base
{
    public async Task Index()
    {}

    protected new async void GenerateCheckpoint()
    {}

    protected override async void CheckToken()
    {}

    private async void GetImpl()
    {}
}

public abstract class Base
{
    protected abstract void CheckToken();
    protected void GenerateCheckpoint(){}
}

public class Program { public static void Main() {}}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Equal(3, diagnostics.Length);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[0].Descriptor);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[1].Descriptor);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[2].Descriptor);
    }
}
