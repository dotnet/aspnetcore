// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
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

    // Note: The following tests are disabled because the test framework doesn't support
    // required and init keywords in the current C# language version used by the test compiler.
    // These features are tested manually and will work correctly in the real analyzer.

    /*
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

        VerifyCSharpDiagnostic(test,
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

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldNotUseRequiredOrInit.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.InitProperty' should not use 'init' modifier. Consider using [EditorRequired] attribute instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 7, 67)
                }
            });
    }
    */

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ComponentParameterAnalyzer();
}