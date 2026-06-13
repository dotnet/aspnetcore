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
        var test = """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TestClass
                {
                    private static Task<int> ReturnIntAsync() =>
                        Task.FromResult(42);

                    public Task<int> ReturnIntAsync2() =>
                        Task.FromResult(42);
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnosticForPublicStaticMethod()
    {
        var test = """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TestClass
                {
                    [JSInvokable]
                    public static Task<int> ReturnIntAsync() =>
                        Task.FromResult(42);
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void DiagnosticForPrivateStaticJSInvokableMethod()
    {
        var test = """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TestClass
                {
                    [JSInvokable]
                    private static Task<int> ReturnIntAsync() =>
                        Task.FromResult(42);
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
            Message = "Method 'ConsoleApplication1.TestClass.ReturnIntAsync()' decorated with [JSInvokable] should be public.",
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", 9, 34)
            ]
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void DiagnosticForPublicJSInvokableMethod()
    {
        var test = """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TestClass
                {
                    [JSInvokable]
                    public Task<int> ReturnIntAsync() =>
                        Task.FromResult(42);
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.JSInvokableMethodShouldBeStatic.Id,
            Message = "Method 'ConsoleApplication1.TestClass.ReturnIntAsync()' decorated with [JSInvokable] should be static.",
            Severity = DiagnosticSeverity.Warning,
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", 9, 26)
            ]
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void DiagnosticForPrivateJSInvokableMethod()
    {
        var test = """
            namespace ConsoleApplication1
            {
                using Microsoft.JSInterop;
                using System.Threading.Tasks;

                class TestClass
                {
                    [JSInvokable]
                    private Task<int> ReturnIntAsync() =>
                        Task.FromResult(42);
                }
            }
            """ + ComponentsTestDeclarations.SourceWithJSInvokable;

        var expected = new[] {
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id,
                Message = "Method 'ConsoleApplication1.TestClass.ReturnIntAsync()' decorated with [JSInvokable] should be public.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", 9, 27)
                ]
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.JSInvokableMethodShouldBeStatic.Id,
                Message = "Method 'ConsoleApplication1.TestClass.ReturnIntAsync()' decorated with [JSInvokable] should be static.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                [
                    new DiagnosticResultLocation("Test0.cs", 9, 27)
                ]
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }
}
