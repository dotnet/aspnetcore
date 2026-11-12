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
        var test = /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;

                class TypeName
                {
                    private string MyMethod() { return null; }
                    [JSInvokable] private void BadMethod() { return; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test, new DiagnosticResult
        {
            Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
            Message = "Method 'ConsoleApplication1.TypeName.BadMethod()' decorated with [JSInvokable] should be public.",
            Severity = DiagnosticSeverity.Warning,
            Locations = [new DiagnosticResultLocation("Test0.cs", 8, 36)]
        });

        VerifyCSharpFix(test, /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;

                class TypeName
                {
                    private string MyMethod() { return null; }
                    [JSInvokable] public void BadMethod() { return; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable);
    }

    [Fact]
    public void AddsDiagnosticAndFixForNonPublicMethodsWithJSInvokableAttribute()
    {
        var test = /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;

                class TypeName
                {
                    [JSInvokable] private static string BadMethod() { return null; }
                    [JSInvokable]
                    protected string BadMethod2() { return null; }
                    [JSInvokable] internal string BadMethod3() { return null; }
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
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 7, 45) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod2()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 9, 26) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName.BadMethod3()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 10, 39) ]
                }
            ]);

        VerifyCSharpFix(test, /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;

                class TypeName
                {
                    [JSInvokable] public static string BadMethod() { return null; }
                    [JSInvokable]
                    public string BadMethod2() { return null; }
                    [JSInvokable] public string BadMethod3() { return null; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable);
    }

    [Fact]
    public void AddsDiagnosticAndFixForMultiModifierMethodsWithJSInvokableAttribute()
    {
        var test = /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TypeName
                {
                    [JSInvokable]
                    protected async Task<int> BadMethod() => await Task.FromResult(42);
                    [JSInvokable] private protected static string BadMethod2() { return null; }
                    [JSInvokable] protected internal string BadMethod3() { return null; }
                }
                class TypeName2 : TypeName
                {
                    [JSInvokable] protected new internal string BadMethod3() { return null; }
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
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 11, 49) ]
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                    Message = "Method 'ConsoleApplication1.TypeName2.BadMethod3()' decorated with [JSInvokable] should be public.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = [ new DiagnosticResultLocation("Test0.cs", 15, 53) ]
                },
            ]);

        VerifyCSharpFix(test, /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TypeName
                {
                    [JSInvokable]
                    public async Task<int> BadMethod() => await Task.FromResult(42);
                    [JSInvokable] public static string BadMethod2() { return null; }
                    [JSInvokable] public string BadMethod3() { return null; }
                }
                class TypeName2 : TypeName
                {
                    [JSInvokable] public new string BadMethod3() { return null; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable);
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider()
        => new JSInvokableCodeFixProvider();

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new JSInvokableAnalyzer();
}
