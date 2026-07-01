// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class JSInteropCodeFixProviderTest : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new JSInteropAnalyzer();
    protected override CodeFixProvider GetCSharpCodeFixProvider() => new JSInteropCodeFixProvider();

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

    private static readonly string BlazorComponentDeclarations = @"
    namespace Microsoft.AspNetCore.Components
    {
        using System;
        using System.Threading.Tasks;

        public interface IComponent
        {
        }

        public sealed class InjectAttribute : Attribute
        {
        }

        public abstract class ComponentBase : IComponent
        {
            protected virtual Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
        }
    }
    ";

    [Fact]
    public void WrapsUnguardedInvokeVoidAsyncInTryCatch()
    {
        var oldSource = @"
    namespace BlazorApp1.Components
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
                await JS.InvokeVoidAsync(""initializeChart"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        var newSource = @"
    namespace BlazorApp1.Components
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
            try
            {
                await JS.InvokeVoidAsync(""initializeChart"");
            }
            catch (Exception)
            {
            }
        }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpFix(oldSource, newSource);
    }

    [Fact]
    public void WrapsUnguardedInvokeAsyncExpressionStatementInTryCatch()
    {
        var oldSource = @"
    namespace BlazorApp1.Components
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
                await JS.InvokeAsync<string>(""prompt"", ""Name?"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        var newSource = @"
    namespace BlazorApp1.Components
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
            try
            {
                await JS.InvokeAsync<string>(""prompt"", ""Name?"");
            }
            catch (Exception)
            {
            }
        }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpFix(oldSource, newSource);
    }

    [Fact]
    public void WrapsNonAwaitedCallInTryCatch()
    {
        var oldSource = @"
    namespace BlazorApp1.Components
    {
        using System;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSRuntime JS = default!;

            public void DisplayCustomer()
            {
                JS.InvokeVoidAsync(""console.log"", ""hello"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        var newSource = @"
    namespace BlazorApp1.Components
    {
        using System;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSRuntime JS = default!;

            public void DisplayCustomer()
            {
            try
            {
                JS.InvokeVoidAsync(""console.log"", ""hello"");
            }
            catch (Exception)
            {
            }
        }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpFix(oldSource, newSource);
    }

    [Fact]
    public void DoesNotOfferFixForLocalDeclarationStatement()
    {
        var source = @"
    namespace BlazorApp1.Components
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
                var result = await JS.InvokeAsync<string>(""prompt"", ""Name?"");
                _ = result;
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpFix(source, source);
    }

    [Fact]
    public void DoesNotOfferFixForReturnStatement()
    {
        var source = @"
    namespace BlazorApp1.Components
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSRuntime JS = default!;

            public ValueTask DisplayCustomer()
            {
                return JS.InvokeVoidAsync(""console.log"", $""Customer submitted: {1}, {2}, {3}, {4}, {5}"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpFix(source, source);
    }
}
