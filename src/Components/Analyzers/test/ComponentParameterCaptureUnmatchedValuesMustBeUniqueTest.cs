// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class ComponentParameterCaptureUnmatchedValuesMustBeUniqueTest : DiagnosticVerifier
{
    [Fact]
    public void IgnoresPropertiesWithCaptureUnmatchedValuesFalse()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using System.Collections.Generic;
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureUnmatchedValues = false)] public string MyProperty {{ get; set; }}
            [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> MyOtherProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void AddsDiagnosticForMultipleCaptureUnmatchedValuesProperties()
    {
        var test = $@"
    namespace ConsoleApplication1
    {{
        using System.Collections.Generic;
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> MyProperty {{ get; set; }}
            [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> MyOtherProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

        var message = @"Component type 'ConsoleApplication1.TypeName' defines properties multiple parameters with CaptureUnmatchedValues. Properties: " + Environment.NewLine +
"ConsoleApplication1.TypeName.MyOtherProperty" + Environment.NewLine +
"ConsoleApplication1.TypeName.MyProperty";

        VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesMustBeUnique.Id,
                    Message = message,
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 6, 15)
                    }
                });
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ComponentParameterAnalyzer();
    }
}
