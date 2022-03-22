// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class ComponentParametersShouldBePublicCodeFixProviderTest : CodeFixVerifier
{
    [Fact]
    public void IgnoresPrivatePropertiesWithoutParameterAttribute()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private string MyProperty { get; set; }
        }
    }" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void AddsDiagnosticAndFixForPrivatePropertiesWithParameterAttribute()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using " + typeof(ParameterAttribute).Namespace + @";

        class TypeName
        {
            [Parameter] private string BadProperty1 { get; set; }
        }
    }" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParametersShouldBePublic.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.BadProperty1' should be public.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 8, 40)
                }
            });

        VerifyCSharpFix(test, @"
    namespace ConsoleApplication1
    {
        using " + typeof(ParameterAttribute).Namespace + @";

        class TypeName
        {
            [Parameter] public string BadProperty1 { get; set; }
        }
    }" + ComponentsTestDeclarations.Source);
    }

    [Fact]
    public void IgnoresPublicPropertiesWithNonPublicSetterWithParameterAttribute()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using " + typeof(ParameterAttribute).Namespace + @";

        class TypeName
        {
            [Parameter] public string MyProperty1 { get; private set; }
            [Parameter] public object MyProperty2 { get; protected set; }
            [Parameter] public object MyProperty3 { get; internal set; }
        }
    }" + ComponentsTestDeclarations.Source;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParameterSettersShouldBePublic.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty1' should have a public setter.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 8, 39)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParameterSettersShouldBePublic.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty2' should have a public setter.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 9, 39)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ComponentParameterSettersShouldBePublic.Id,
                Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty3' should have a public setter.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                        new DiagnosticResultLocation("Test0.cs", 10, 39)
                }
            });
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider()
    {
        return new ComponentParametersShouldBePublicCodeFixProvider();
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ComponentParameterAnalyzer();
    }
}
