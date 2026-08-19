// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class JsInteropUsageAnalyzersIntegrationTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer[] GetMultipleCSharpDiagnosticAnalyzers() => new DiagnosticAnalyzer[]
    {
        new InvokeAsyncOfObjectAnalyzer(),
        new JSInteropAnalyzer(),
        new JsInteropUsageWithoutCheckAnalyzer()
    };
    private static readonly string BaseComponentDeclarations = @"
namespace Microsoft.AspNetCore.Components
{
    using System;
    using System.Threading.Tasks;

    public interface IComponent { }

    public abstract class ComponentBase : IComponent
    {
        protected RendererInfo RendererInfo = new();
        protected virtual void OnAfterRender(bool firstRender) {}
        protected virtual Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
    }

    public sealed class InjectAttribute : Attribute
    {
    }
    
    public sealed class RendererInfo
    {
        public bool IsInteractive { get; } = false;
    }
}

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
    public void JSInvokeOnInitializedShouldThrowAllMultipleTypesOfWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override async Task OnInitializedAsync()
        {
            await JS.InvokeAsync<object>(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyMultipleCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 19) },
                Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
                Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
                Severity = DiagnosticSeverity.Warning,
            },
            new DiagnosticResult {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 19) },
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
            },
            new DiagnosticResult {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 19) },
                Id = DiagnosticDescriptors.JsInteropUsageWithoutIsInteractiveCheck.Id,
                Message = "JS interop call 'InvokeAsync' is used outside of OnAfterRender/OnAfterRenderAsync without checking RendererInfo.IsInteractive.",
                Severity = DiagnosticSeverity.Warning,
            }
        );
    }

    [Fact]
    public void JSInvokeOnInitializedInTryCatchShouldThrowAllMultipleTypesOfWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await JS.InvokeAsync<object>(""console.log"", ""This should fail!"");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyMultipleCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 23) },
                Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
                Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
                Severity = DiagnosticSeverity.Warning,
            },
            new DiagnosticResult
            {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 23) },
                Id = DiagnosticDescriptors.JsInteropUsageWithoutIsInteractiveCheck.Id,
                Message = "JS interop call 'InvokeAsync' is used outside of OnAfterRender/OnAfterRenderAsync without checking RendererInfo.IsInteractive.",
                Severity = DiagnosticSeverity.Warning,
            }
        );
    }

    [Fact]
    public void JSInvokeInteractiveWithoutTryCatchShouldThrowAllMultipleTypesOfWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override async Task OnInitializedAsync()
        {
            if (!RendererInfo.IsInteractive)
            {
                return;
            }

            await JS.InvokeAsync<object>(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyMultipleCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 19) },
                Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
                Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
                Severity = DiagnosticSeverity.Warning,
            },
            new DiagnosticResult
            {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 19) },
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
            }
        );
    }

    [Fact]
    public void JSInvokeInteractiveWithTryCatchShouldThrowOnlyBL0010Warning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override async Task OnInitializedAsync()
        {
            if (!RendererInfo.IsInteractive)
            {
                return;
            }

            try
            {
                await JS.InvokeAsync<object>(""console.log"", ""This should fail!"");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyMultipleCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 22, 23) },
                Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
                Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
                Severity = DiagnosticSeverity.Warning,
            }
        );
    }

    [Fact]
    public void JSInvokeInAfterRenderWithoutTryCatchShouldThrowMultipleTypesOfWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeAsync<object>(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyMultipleCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 19) },
                Id = DiagnosticDescriptors.UseInvokeVoidAsyncForObjectReturn.Id,
                Message = "Use 'InvokeVoidAsync' instead of 'InvokeAsync<object>'. Return values of type 'object' cannot be deserialized and may cause serialization errors if the JavaScript function returns a non-serializable value.",
                Severity = DiagnosticSeverity.Warning,
            },
            new DiagnosticResult
            {
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 19) },
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
            }
        );
    }
}
