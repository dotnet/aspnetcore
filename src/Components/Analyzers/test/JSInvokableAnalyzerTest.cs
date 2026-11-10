// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class JSInvokableAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new JSInvokableAnalyzer();

    [Fact]
    public void NoDiagnosticForMethodsWithoutAttribute()
    {
        var test = /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TestClass
                {
                    private static Task<int> ReturnIntAsync() =>
                        Task.FromResult(42);

                    protected Task<int> ReturnIntAsync2() =>
                        Task.FromResult(42);
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnosticForPublicMethods()
    {
        var test = /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TestClass
                {
                    [JSInvokable]
                    public static Task<int> ReturnIntAsync() => Task.FromResult(42);

                    [JSInvokable]
                    public Task<int> ReturnIntAsync2() => Task.FromResult(42);
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void DiagnosticForNonPublicJSInvokableMethods()
    {
        var test = /* lang=c#-test */ """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TestClass
                {
                    [JSInvokable]
                    private static Task<int> ReturnIntAsync() =>
                        Task.FromResult(42);
                    [JSInvokable] protected static void ReturnIntAsync2() { return; }

                    [JSInvokable]
                    private Task<int> ReturnIntAsync3() =>
                        Task.FromResult(42);
                    [JSInvokable] protected void ReturnIntAsync4() { return; }
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                Message = "Method 'ConsoleApplication1.TestClass.ReturnIntAsync()' decorated with [JSInvokable] should be public.",
                Severity = DiagnosticSeverity.Warning,
                Locations = [ new DiagnosticResultLocation("Test0.cs", 9, 34) ]
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                Message = "Method 'ConsoleApplication1.TestClass.ReturnIntAsync2()' decorated with [JSInvokable] should be public.",
                Severity = DiagnosticSeverity.Warning,
                Locations = [ new DiagnosticResultLocation("Test0.cs", 11, 45) ]
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                Message = "Method 'ConsoleApplication1.TestClass.ReturnIntAsync3()' decorated with [JSInvokable] should be public.",
                Severity = DiagnosticSeverity.Warning,
                Locations = [ new DiagnosticResultLocation("Test0.cs", 14, 27) ]
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                Message = "Method 'ConsoleApplication1.TestClass.ReturnIntAsync4()' decorated with [JSInvokable] should be public.",
                Severity = DiagnosticSeverity.Warning,
                Locations = [ new DiagnosticResultLocation("Test0.cs", 16, 38) ]
            });
    }
}
