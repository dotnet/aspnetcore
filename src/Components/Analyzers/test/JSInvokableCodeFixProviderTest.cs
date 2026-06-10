// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class JSInvokableCodeFixProviderTest : CodeFixVerifier
{
    [Fact]
    public void IgnoresPrivateMethodWithoutJSInvokableAttribute()
    {
        var test = """
            namespace ConsoleApplication1
            {
                class TypeName
                {
                    private string MyMethod() { return null; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void AddsDiagnosticAndFixForPrivateOrInstanceMethodsWithJSInvokableAttribute()
    {
        var test = """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;

                class TypeName
                {
                    [JSInvokable] private static string BadMethod() { return null; }
                    [JSInvokable] private string BadMethod2() { return null; }
                    [JSInvokable] public string BadMethod3() { return null; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test,
            [
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations =
                    [
                        new DiagnosticResultLocation("Test0.cs", 7, 45)
                    ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod2()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations =
                    [
                        new DiagnosticResultLocation("Test0.cs", 8, 38)
                    ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBeStatic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod2()' decorated with [JSInvokable] should be static.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations =
                    [
                        new DiagnosticResultLocation("Test0.cs", 8, 38)
                    ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBeStatic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod3()' decorated with [JSInvokable] should be static.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations =
                    [
                        new DiagnosticResultLocation("Test0.cs", 9, 37)
                    ]
                }
            ]);

        VerifyCSharpFix(test, """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;

                class TypeName
                {
                    [JSInvokable] public static string BadMethod() { return null; }
                    [JSInvokable] public static string BadMethod2() { return null; }
                    [JSInvokable] public static string BadMethod3() { return null; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable);
    }

    [Fact]
    public void AddsDiagnosticAndFixForMultiModifierMethodsWithJSInvokableAttribute()
    {
        var test = """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TypeName
                {
                    [JSInvokable]
                    protected async Task<int> BadMethod() => await Task.FromResult(42);
                    [JSInvokable] private protected static string BadMethod2() { return null; }
                    [JSInvokable] protected string BadMethod3() { return null; }
                }
                class TypeName2 : TypeName
                {
                    [JSInvokable] protected new string BadMethod3() { return null; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test,
            [
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 9, 35) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBeStatic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod()' decorated with [JSInvokable] should be static.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 9, 35) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod2()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 10, 55) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod3()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 11, 40) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBeStatic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod3()' decorated with [JSInvokable] should be static.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 11, 40) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName2.BadMethod3()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 15, 44) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBeStatic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName2.BadMethod3()' decorated with [JSInvokable] should be static.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 15, 44) ]
                }
            ]);

        VerifyCSharpFix(test, """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TypeName
                {
                    [JSInvokable]
                    public static async Task<int> BadMethod() => await Task.FromResult(42);
                    [JSInvokable] public static string BadMethod2() { return null; }
                    [JSInvokable] public static string BadMethod3() { return null; }
                }
                class TypeName2 : TypeName
                {
                    [JSInvokable] public static new string BadMethod3() { return null; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable);
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider()
        => new JSInvokableCodeFixProvider();

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new JSInvokableAnalyzer();
}
