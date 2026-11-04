// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class InvokeAsyncOfObjectAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new InvokeAsyncOfObjectAnalyzer();

    private static readonly string JSInteropDeclarations = @"
    namespace Microsoft.JSInterop
    {
        using System.Threading;
        using System.Threading.Tasks;

        public interface IJSRuntime
        {
            ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
            ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args);
        }

        public interface IJSObjectReference : System.IAsyncDisposable
        {
            ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
            ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args);
        }

        public interface IJSInProcessRuntime : IJSRuntime
        {
            TValue Invoke<TValue>(string identifier, params object[] args);
        }

        public interface IJSInProcessObjectReference : IJSObjectReference
        {
            TValue Invoke<TValue>(string identifier, params object[] args);
        }

        public static class JSRuntimeExtensions
        {
            public static ValueTask InvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, params object[] args)
                => default;
            public static ValueTask<TValue> InvokeAsync<TValue>(this IJSRuntime jsRuntime, string identifier, params object[] args)
                => default;
            public static ValueTask<TValue> InvokeAsync<TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object[] args)
                => default;
            public static ValueTask InvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object[] args)
                => default;
        }

        public static class JSObjectReferenceExtensions
        {
            public static ValueTask InvokeVoidAsync(this IJSObjectReference jsObjectReference, string identifier, params object[] args)
                => default;
            public static ValueTask<TValue> InvokeAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, params object[] args)
                => default;
            public static ValueTask<TValue> InvokeAsync<TValue>(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, params object[] args)
                => default;
            public static ValueTask InvokeVoidAsync(this IJSObjectReference jsObjectReference, string identifier, CancellationToken cancellationToken, params object[] args)
                => default;
        }
    }
";

    [Fact]
    public void NoDiagnosticForInvokeVoidAsync()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task TestMethod()
            {
                await _jsRuntime.InvokeVoidAsync(""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnosticForInvokeAsyncWithTypedReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task<string> TestMethod()
            {
                return await _jsRuntime.InvokeAsync<string>(""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void DiagnosticForInvokeAsyncWithObjectReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task TestMethod()
            {
                await _jsRuntime.InvokeAsync<object>(""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 23)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void DiagnosticForExtensionMethodInvokeAsyncWithObjectReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task TestMethod()
            {
                await JSRuntimeExtensions.InvokeAsync<object>(_jsRuntime, ""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 23)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void DiagnosticForIJSObjectReferenceInvokeAsyncWithObjectReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSObjectReference _jsObjectReference;

            public async Task TestMethod()
            {
                await _jsObjectReference.InvokeAsync<object>(""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 23)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void DiagnosticForInvokeAsyncWithCancellationTokenAndObjectReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task TestMethod(CancellationToken ct)
            {
                await _jsRuntime.InvokeAsync<object>(""myFunction"", ct, null);
            }
        }
    }" + JSInteropDeclarations;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 14, 23)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void NoDiagnosticForNonJSInteropInvokeAsync()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;

        class TestClass
        {
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
                => default;

            public async Task TestMethod()
            {
                await InvokeAsync<object>(""myFunction"", null);
            }
        }
    }";

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnosticForInvokeAsyncWithIntReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task<int> TestMethod()
            {
                return await _jsRuntime.InvokeAsync<int>(""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NoDiagnosticForInvokeAsyncWithCustomClassReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class MyResult { }

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task<MyResult> TestMethod()
            {
                return await _jsRuntime.InvokeAsync<MyResult>(""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void DiagnosticForIJSInProcessRuntimeInvokeAsyncWithObjectReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSInProcessRuntime _jsRuntime;

            public async Task TestMethod()
            {
                await _jsRuntime.InvokeAsync<object>(""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 23)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void DiagnosticForIJSInProcessObjectReferenceInvokeAsyncWithObjectReturn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSInProcessObjectReference _jsObjectRef;

            public async Task TestMethod()
            {
                await _jsObjectRef.InvokeAsync<object>(""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 23)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void MultipleInvocationsReportMultipleDiagnostics()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task TestMethod()
            {
                await _jsRuntime.InvokeAsync<object>(""myFunction1"");
                await _jsRuntime.InvokeAsync<object>(""myFunction2"");
            }
        }
    }" + JSInteropDeclarations;

        var expected1 = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 23)
            }
        };

        var expected2 = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 14, 23)
            }
        };

        VerifyCSharpDiagnostic(test, expected1, expected2);
    }

    [Fact]
    public void DiagnosticForJSObjectReferenceExtensionMethod()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSObjectReference _jsObjectRef;

            public async Task TestMethod()
            {
                await JSObjectReferenceExtensions.InvokeAsync<object>(_jsObjectRef, ""myFunction"");
            }
        }
    }" + JSInteropDeclarations;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 23)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }

    [Fact]
    public void DiagnosticForInvokeAsyncWithObjectReturnAssignedToVariable()
    {
        // This test confirms that the diagnostic still fires when the result is assigned to a variable.
        // Using InvokeAsync<object> is problematic because 'object' cannot be properly deserialized from JSON -
        // the result will either be null or cause serialization errors if JavaScript returns a non-serializable value.
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.JSInterop;
        using System.Threading.Tasks;

        class TestClass
        {
            private IJSRuntime _jsRuntime;

            public async Task TestMethod()
            {
                var result = await _jsRuntime.InvokeAsync<object>(""myFunction"");
                System.Console.WriteLine(result);
            }
        }
    }" + JSInteropDeclarations;

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
            Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 13, 36)
            }
        };

        VerifyCSharpDiagnostic(test, expected);
    }
}
