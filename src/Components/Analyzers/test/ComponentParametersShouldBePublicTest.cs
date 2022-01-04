// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers;

public class ComponentParametersShouldBePublicTest : DiagnosticVerifier
{
    [Fact]
    public void IgnoresPublicProperties()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter] public string MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresPrivateNonParameterProperties()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            private string MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ErrorsForNonPublicParameters()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter] string MyProperty1 {{ get; set; }}
            [Parameter] private string MyProperty2 {{ get; set; }}
            [Parameter] protected string MyProperty3 {{ get; set; }}
            [Parameter] internal string MyProperty4 {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldBePublic.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty1' should be public.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 7, 32)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldBePublic.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty2' should be public.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 8, 40)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldBePublic.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty3' should be public.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 9, 42)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldBePublic.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty4' should be public.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 10, 41)
                }
            });
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ComponentParameterAnalyzer();
}
