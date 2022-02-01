// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class ComponentParameterCaptureUnmatchedValuesHasWrongTypeTest : DiagnosticVerifier
{
    [Theory]
    [InlineData("System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>")]
    [InlineData("System.Collections.Generic.Dictionary<string, object>")]
    [InlineData("System.Collections.Generic.IDictionary<string, object>")]
    [InlineData("System.Collections.Generic.IReadOnlyDictionary<string, object>")]
    public void IgnoresPropertiesWithSupportedType(string propertyType)
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureUnmatchedValues = true)] public {propertyType} MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IgnoresPropertiesWithCaptureUnmatchedValuesFalse()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureUnmatchedValues = false)] public string MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void AddsDiagnosticForInvalidType()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureUnmatchedValues = true)] public string MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesHasWrongType.Id,
                    Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty' defines CaptureUnmatchedValues but has an unsupported type 'string'. Use a type assignable from 'System.Collections.Generic.Dictionary<string, object>'.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 7, 70)
                    }
                });
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ComponentParameterAnalyzer();
    }
}
