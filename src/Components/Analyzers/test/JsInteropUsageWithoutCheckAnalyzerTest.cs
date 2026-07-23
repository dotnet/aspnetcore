// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class JsInteropUsageWithoutCheckAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new JsInteropUsageWithoutCheckAnalyzer();
    private static readonly string id = DiagnosticDescriptors.JsInteropUsageWithoutIsInteractiveCheck.Id;
    private static readonly string messageInvoke = "JS interop call 'Invoke' is used outside of OnAfterRender/OnAfterRenderAsync without checking RendererInfo.IsInteractive.";
    private static readonly string messageInvokeAsync = "JS interop call 'InvokeAsync' is used outside of OnAfterRender/OnAfterRenderAsync without checking RendererInfo.IsInteractive.";
    private static readonly string messageInvokeVoidAsync = "JS interop call 'InvokeVoidAsync' is used outside of OnAfterRender/OnAfterRenderAsync without checking RendererInfo.IsInteractive.";
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
    
    public sealed class RendererInfo()
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
    public void JSInvokeVariantsInOnAfterRenderShouldNotThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;
        private IJSObjectReference JSObj = default!;
        private IJSInProcessRuntime JSInProcess = default!;
        private IJSInProcessObjectReference JSInProcessObj = default!;

        protected override void OnAfterRender(bool firstRender)
        {
            JSInProcess.Invoke<double>(""scrollElementIntoView"");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync(""console.log"", ""message"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeVariantsShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;
        private IJSObjectReference JSObj = default!;
        private IJSInProcessRuntime JSInProcess = default!;
        private IJSInProcessObjectReference JSInProcessObj = default!;

        protected async Task OnInitializedAsync()
        {
            // IJSRuntime interface overloads
            await JS.InvokeAsync<double>(""scrollElementIntoView"", Array.Empty<object>());

            // IJSRuntime extension methods (JSRuntimeExtensions)
            await JS.InvokeAsync<double>(""scrollElementIntoView"");
            await JS.InvokeVoidAsync(""console.log"", CancellationToken.None, ""message"");

            // IJSObjectReference interface overloads
            await JSObj.InvokeAsync<double>(""scrollElementIntoView"", Array.Empty<object>());

            // IJSObjectReference extension methods (JSObjectReferenceExtensions)
            await JSObj.InvokeAsync<double>(""scrollElementIntoView"");
            await JSObj.InvokeVoidAsync(""console.log"", ""message"");

            // IJSInProcessRuntime (synchronous)
            JSInProcess.Invoke<double>(""scrollElementIntoView"");

            // IJSInProcessObjectReference (synchronous)
            JSInProcessObj.Invoke<double>(""scrollElementIntoView"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 19) }, Id = id, Message = messageInvokeAsync, Severity = DiagnosticSeverity.Warning, },
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 23, 19) }, Id = id, Message = messageInvokeAsync, Severity = DiagnosticSeverity.Warning, },
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, },
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 27, 19) }, Id = id, Message = messageInvokeAsync, Severity = DiagnosticSeverity.Warning, },
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 30, 19) }, Id = id, Message = messageInvokeAsync, Severity = DiagnosticSeverity.Warning, },
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 31, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, },
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 34, 13) }, Id = id, Message = messageInvoke, Severity = DiagnosticSeverity.Warning, },
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 37, 13) }, Id = id, Message = messageInvoke, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInIfOfNonInteractiveConditionShouldThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            var someFlag = true;
            if (someFlag)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInElseOfNonInteractiveConditionShouldThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            var someFlag = true;
            if (someFlag)
            {
                // someFlag is unrelated to RendererInfo.IsInteractive
            }
            else
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 22, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeAfterBasicIfCheckShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (RendererInfo.IsInteractive)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeAfterBasicTernaryCheckShouldThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            var bCheck = true;
            var result = bCheck ? await JS.InvokeAsync<double>(""scrollElementIntoView"") : null;
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 41) }, Id = id, Message = messageInvokeAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeAfterBasicTernaryCheckShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            var result = RendererInfo.IsInteractive ? await JS.InvokeAsync<double>(""scrollElementIntoView"") : null;
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeAfterBasicVariableCheckShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            var isInteractive = RendererInfo.IsInteractive;
            if (isInteractive)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeAfterComplexVariableCheckShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            var isInteractive = true || RendererInfo.IsInteractive;
            if (isInteractive)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeAfterVariableWithTernaryConditionReturningCheckShouldThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            var test = true;

            // Unsupported scenario, since we cannot evaluate the result of the condition!
            var resultCheck = test ? RendererInfo.IsInteractive : false;
            if (resultCheck)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeAfterVariableWithCheckButNotBooleanShouldThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            var result = RendererInfo.IsInteractive ? GetValue() : 0;
            if (result)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }
        }

        protected int GetValue()
        {
            return 1;
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInElseOfBasicIfCheckShouldThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (RendererInfo.IsInteractive)
            {
                var x = RendererInfo.IsInteractive;
            }
            else
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInElseOfBasicIfNegatedCheckShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (!RendererInfo.IsInteractive)
            {
                var x = RendererInfo.IsInteractive;
            }
            else
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeInElseIfOfBasicIfNegatedCheckShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (false)
            {
                var x = RendererInfo.IsInteractive;
            }
            else if (RendererInfo.IsInteractive)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeInTrueComplexIfCheckShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (RandomTrueCheck() && (RandomFalseCheck() || RendererInfo.IsInteractive))
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }
        }

        protected bool RandomTrueCheck()
        {
            return true;
        }

        protected bool RandomFalseCheck()
        {
            return false;
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeInElseOfComplexIfCheckShouldThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (RandomTrueCheck() && (RandomFalseCheck() || RendererInfo.IsInteractive))
            {
                var x = RendererInfo.IsInteractive;
            }
            else
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
            }
        }

        protected bool RandomTrueCheck()
        {
            return true;
        }

        protected bool RandomFalseCheck()
        {
            return false;
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeAfterIfCheckReturnsShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (!RendererInfo.IsInteractive)
            {
                // This counts for the rest of the method, so we can safely call JS interop after that.
                return;
            }

            await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");   
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeAfterIfElseCheckReturnsShouldNotThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (RendererInfo.IsInteractive)
            {
                // Do stuff
            }
            else
            {
                return;
            }

            await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");   
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeAfterIfElseNonInteractiveCheckReturnsShouldThrowWarning()
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

        protected async Task OnInitializedAsync()
        {
            if (false)
            {
                // Do stuff
            }
            else
            {
                return;
            }

            await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }
}
