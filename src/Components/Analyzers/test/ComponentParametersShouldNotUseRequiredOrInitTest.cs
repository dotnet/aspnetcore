// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers;

public class ComponentParametersShouldNotUseRequiredOrInitTest : DiagnosticVerifier
{
    [Fact]
    public void IgnoresNonParameterProperties()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            public string RegularProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresParametersWithoutRequiredOrInit()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter] public string NormalProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void WarnsForRequiredParameter()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter] public required string RequiredProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp11);
        VerifyCSharpDiagnostic(test, parseOptions,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldNotUseRequiredOrInit.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.RequiredProperty' should not use 'required' modifier. Consider using [EditorRequired] attribute instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 7, 32)
                }
            });
    }

    [Fact]
    public void WarnsForInitParameter()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter] public string InitProperty {{ get; init; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp9);
        VerifyCSharpDiagnostic(test, parseOptions,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldNotUseRequiredOrInit.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.InitProperty' should not use 'init' modifier. Consider using [EditorRequired] attribute instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 7, 59)
                }
            });
    }

    [Fact]
    public void WarnsForBothRequiredAndInit()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter] public required string RequiredProperty {{ get; set; }}
            [Parameter] public string InitProperty {{ get; init; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp11);
        VerifyCSharpDiagnostic(test, parseOptions,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldNotUseRequiredOrInit.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.RequiredProperty' should not use 'required' modifier. Consider using [EditorRequired] attribute instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 7, 32)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldNotUseRequiredOrInit.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.InitProperty' should not use 'init' modifier. Consider using [EditorRequired] attribute instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 8, 59)
                }
            });
    }

    [Fact]
    public void IgnoresNonParameterPropertiesWithRequiredAndInit()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            public required string RequiredNonParameter {{ get; set; }}
            public string InitNonParameter {{ get; init; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp11);
        VerifyCSharpDiagnostic(test, parseOptions);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ComponentParameterAnalyzer();
}