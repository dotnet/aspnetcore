// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class JsInteropInteractiveRenderAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new JsInteropInteractiveRenderAnalyzer();
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
        protected Task InvokeAsync(Action workItem) => Task.CompletedTask;
        protected Task InvokeAsync(Func<Task> workItem) => Task.CompletedTask;
    }

    public abstract class Dispatcher
    {
        public abstract Task InvokeAsync(Action workItem);
        public abstract Task InvokeAsync(Func<Task> workItem);
    }

    public sealed class InjectAttribute : Attribute
    {
    }
    
    public sealed class RendererInfo
    {
        public bool IsInteractive { get; } = false;
    }

    public readonly struct EventCallback
    {
        public static EventCallbackFactory Factory { get; } = new();
    }

    public readonly struct EventCallback<T>
    {
    }

    public delegate void RenderFragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder);

    public delegate RenderFragment RenderFragment<TValue>(TValue value);

    public class ChangeEventArgs : EventArgs
    {
        public object Value { get; set; }
    }

    public class EventCallbackFactory
    {
        public EventCallback<T> Create<T>(object receiver, Action callback) => default;
        public EventCallback<T> Create<T>(object receiver, Action<T> callback) => default;
        public EventCallback<T> Create<T>(object receiver, Func<Task> callback) => default;
        public EventCallback<ChangeEventArgs> CreateBinder<T>(object receiver, Action<T> setter, T value) => default;
    }

    namespace CompilerServices
    {
        public static class RuntimeHelpers
        {
            public static T TypeCheck<T>(T value) => value;
        }
    }

    namespace Rendering
    {
        public class RenderTreeBuilder
        {
            public void OpenElement(int sequence, string elementName) { }
            public void CloseElement() { }
            public void AddAttribute<T>(int sequence, string name, T value) { }
            public void AddMarkupContent(int sequence, string markupContent) { }
            public void SetUpdatesAttributeName(string name) { }
            public void AddContent(int sequence, object textContent) { }
            public void AddContent<TValue>(int sequence, RenderFragment<TValue> fragment, TValue value) { }
            public void OpenComponent<T>(int sequence) where T : IComponent { }
            public void AddComponentParameter(int sequence, string name, object value) { }
            public void CloseComponent() { }
        }
    }

    namespace Web
    {
        public class MouseEventArgs { }
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

    public static class DotNetObjectReference
    {
        public static DotNetObjectReference<TValue> Create<TValue>(TValue value) where TValue : class
            => default;
    }

    public sealed class DotNetObjectReference<TValue> : System.IDisposable where TValue : class
    {
        public void Dispose() { }
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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
    public void JSInvokeAfterReassignedVariableCheckShouldThrowWarning()
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
            var available = RendererInfo.IsInteractive;
            available = true;
            if (available)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeBeforeAndAfterReassignedVariableCheckShouldThrowWarning()
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
            var available = RendererInfo.IsInteractive;
            if (available)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }

            available = true;
            if (available)
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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

        protected override async Task OnInitializedAsync()
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
    public void JSInvokeAfterPartialReturnInNonInteractiveBranchShouldThrowWarning()
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
                if (RandomCheck())
                {
                    return;
                }
            }

            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }

        private static bool RandomCheck() => false;
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 23, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeAfterNestedIfElseThatAlwaysReturnsShouldNotThrowWarning()
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
                if (RandomCheck())
                {
                    return;
                }
                else
                {
                    return;
                }
            }

            await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
        }

        private static bool RandomCheck() => true;
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeAfterIfElseIfNotAllBranchesReturnShouldThrowWarning()
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
                if (RandomCheck())
                {
                    return;
                }
                else if (OtherCheck())
                {
                    return;
                }
            }

            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }

        private static bool RandomCheck() => false;
        private static bool OtherCheck() => true;
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 27, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeAfterIfCheckThrowsShouldNotThrowWarning()
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
                throw new InvalidOperationException();
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

        protected override async Task OnInitializedAsync()
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

    [Fact]
    public void JSInvokeInIfCheckShouldThrowWarning()
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
            if (true && await JS.InvokeAsync<bool>(""isElementIntoView""))
            {
                // Do stuff
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 31) }, Id = id, Message = messageInvokeAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData(@"RendererInfo.IsInteractive && await JS.InvokeAsync<bool>(""isElementIntoView"")")]
    [InlineData(@"RendererInfo.IsInteractive || await JS.InvokeAsync<bool>(""isElementIntoView"")")]
    [InlineData(@"!RendererInfo.IsInteractive && await JS.InvokeAsync<bool>(""isElementIntoView"")")]
    [InlineData(@"!RendererInfo.IsInteractive || await JS.InvokeAsync<bool>(""isElementIntoView"")")]
    public void JSInvokeInIfCheckWithIsInteractiveShouldNotThrowWarning(string condition)
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
            if (" + condition + @")
            {
                // Do stuff
            }
        }
    }
}" + BaseComponentDeclarations;

        // We cannot determine outcome of conditionals that involve IsInteractive check, so if present we don't throw a warning.
        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeInMethodCallUsedInOnInitializedShouldThrowWarning()
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
            await SomeOtherMethod();
        }

        protected async Task SomeOtherMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInUncalledLocalFunctionShouldNotThrowWarning()
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

        protected override Task OnInitializedAsync()
        {
            async Task InvokeAsync()
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should not fail!"");
            }

            return Task.CompletedTask;
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeInCalledLocalFunctionShouldThrowWarning()
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
            async Task InvokeAsync()
            {
                await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
            }

            await InvokeAsync();
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInMethodCallWithArgumentUsedInOnInitializedShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override async Task OnInitializedAsync()
        {
            await SomeOtherMethod(42);
        }

        private async Task SomeOtherMethod(int value)
        {
            await JS.InvokeVoidAsync(""console.log"", value);
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInConvertedArgumentUsedInOnInitializedShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSInProcessRuntime JS = default!;

        protected override void OnInitialized()
        {
            Consume((object)JS.Invoke<double>(""getValue""));
        }

        private static void Consume(object value) { }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 29) }, Id = id, Message = messageInvoke, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInReceiverMethodUsedInOnInitializedShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSInProcessRuntime JS = default!;

        protected override void OnInitialized()
        {
            GetHelper().Run();
        }

        private Helper GetHelper()
        {
            JS.Invoke<double>(""getValue"");
            return new Helper();
        }

        private sealed class Helper
        {
            public void Run() { }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 13) }, Id = id, Message = messageInvoke, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInMethodCallNotUsedInOnInitializedShouldNotThrowWarning()
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
            Console.WriteLine(""This should not fail!"");
        }

        protected async Task SomeOtherMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeInMethodInDeeperLevelShouldThrowWarning()
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
            await SomeFirstMethod();
        }

        protected async Task SomeFirstMethod()
        {
            await SomeOtherMethod();
        }

        protected async Task SomeOtherMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 25, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInMethodBeyondMaxDeepthShouldNotThrowWarning()
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
            await SomeFirstMethod();
        }

        protected async Task SomeFirstMethod()
        {
            await SomeSecondMethod();
        }

        protected async Task SomeSecondMethod()
        {
            await SomeInvokeMethod();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInvokeInMethodFromDifferentComponentShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class OtherComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        public async Task SomeOtherMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }

    class TestComponent : ComponentBase
    {
        protected override async Task OnInitializedAsync()
        {
            var otherComponent = new OtherComponent();
            await otherComponent.SomeOtherMethod();
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("AddAttribute")]
    [InlineData("AddComponentParameter")]
    public void JSInvokeInAnonymousEventCallbackHandlersShouldUseIndependentState(string addHandler)
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSInProcessRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            __builder." + addHandler + @"(1, ""guarded"", EventCallback.Factory.Create<object>(this, () =>
            {
                if (!RendererInfo.IsInteractive)
                {
                    return;
                }
            }));
            __builder." + addHandler + @"(2, ""unguarded"", EventCallback.Factory.Create<object>(this, () =>
            {
                JS.Invoke<double>(""getValue"");
            }));
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 17) }, Id = id, Message = messageInvoke, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInAnonymousDelegateHandlersShouldUseIndependentState()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSInProcessRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            __builder.AddAttribute<Action>(1, ""guarded"", () =>
            {
                if (!RendererInfo.IsInteractive)
                {
                    return;
                }
            });
            __builder.AddAttribute<Action>(2, ""unguarded"", () =>
            {
                JS.Invoke<double>(""getValue"");
            });
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 17) }, Id = id, Message = messageInvoke, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("SomeInvokeMethod")]
    [InlineData("() => SomeInvokeMethod()")]
    public void JSInvokeInDomEventHandlerMethodShouldNotThrowWarning(string handler)
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            //  <button @onclick=""SomeInvokeMethod"">Button</button>
            __builder.OpenElement(14, ""button"");
            __builder.AddAttribute(12, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                " + handler + @"
            ));
            __builder.AddContent(16, ""Button"");
            __builder.CloseElement();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This is fine."");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Theory]
    [InlineData("SomeInvokeMethod")]
    [InlineData("() => SomeInvokeMethod()")]
    public void JSInvokeInFormSubmitHandlerMethodShouldThrowWarning(string handler)
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            //  <form method=""post"" @formname=""form"" @onsubmit=""SomeInvokeMethod""></form>
            __builder.OpenElement(14, ""form"");
            __builder.AddAttribute(12, ""onsubmit"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<System.EventArgs>(this,
                " + handler + @"
            ));
            __builder.CloseElement();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 27, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInComponentEventHandlerAddedWithAddAttributeShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            //  <MyComponent OnClick=""SomeInvokeMethod""></MyComponent> (legacy codegen)
            __builder.OpenComponent<ConsoleApplication1.Pages.MyComponent>(14);
            __builder.AddAttribute(78, ""OnClick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, SomeInvokeMethod));
            __builder.CloseComponent();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 25, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("SomeInvokeMethod")]
    [InlineData("() => SomeInvokeMethod()")]
    public void JSInvokeInComponentEventHandlerMethodShouldThrowWarning(string handler)
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            //  <MyComponent OnClick=""@(() => SomeInvokeMethod())""></MyComponent>
            __builder.OpenComponent<ConsoleApplication1.Pages.MyComponent>(14);
            __builder.AddComponentParameter(78, ""OnClick"", Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>>(Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                " + handler + @"
            )));
            __builder.CloseComponent();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 27, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("SomeInvokeMethod")]
    [InlineData("() => SomeInvokeMethod()")]
    public void JSInvokeInDomEventHandlerMethodInChildContentShouldNotThrowWarning(string handler)
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            //  <MyComponent><button @onclick=""SomeInvokeMethod"">Button</button></MyComponent>
            __builder.OpenComponent<ConsoleApplication1.Pages.MyComponent>(0);
            __builder.AddComponentParameter(1, ""ChildContent"", (Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                __builder2.OpenElement(2, ""button"");
                __builder2.AddAttribute(3, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this,
                    " + handler + @"
                ));
                __builder2.CloseElement();
            }));
            __builder.CloseComponent();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This is fine."");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Theory]
    [InlineData("SomeInvokeMethod")]
    [InlineData("() => SomeInvokeMethod()")]
    public void JSInvokeInComponentEventHandlerMethodInChildContentShouldThrowWarning(string handler)
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            //  <MyComponent><MyComponent OnClick=""SomeInvokeMethod""></MyComponent></MyComponent>
            __builder.OpenComponent<ConsoleApplication1.Pages.MyComponent>(0);
            __builder.AddComponentParameter(1, ""ChildContent"", (Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                __builder2.OpenComponent<ConsoleApplication1.Pages.MyComponent>(2);
                __builder2.AddComponentParameter(3, ""OnClick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this,
                    " + handler + @"
                ));
                __builder2.CloseComponent();
            }));
            __builder.CloseComponent();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 31, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInEventHandlersInTemplatedChildContentShouldOnlyWarnForComponentHandler()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            //  <MyComponent><button @onclick=""SomeInvokeMethod"" /><MyComponent OnClick=""SomeInvokeMethod"" /></MyComponent> with RenderFragment<T> ChildContent
            __builder.OpenComponent<ConsoleApplication1.Pages.MyComponent>(0);
            __builder.AddComponentParameter(1, ""ChildContent"", (Microsoft.AspNetCore.Components.RenderFragment<object>)((context) => (__builder2) => {
                __builder2.OpenElement(2, ""button"");
                __builder2.AddAttribute(3, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, SomeInvokeMethod));
                __builder2.CloseElement();
                __builder2.OpenComponent<ConsoleApplication1.Pages.MyComponent>(4);
                __builder2.AddComponentParameter(5, ""OnClick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, SomeInvokeMethod));
                __builder2.CloseComponent();
            }));
            __builder.CloseComponent();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 32, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInEventHandlersInNestedChildContentShouldOnlyWarnForComponentHandler()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;

    class TestComponent : ComponentBase
    {
        private IJSRuntime JS = default!;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            //  <MyComponent><MyComponent Context=""item""><MyComponent>
            //      <button @onclick=""SomeInvokeMethod"" /><MyComponent OnClick=""SomeInvokeMethod"" />
            //  </MyComponent></MyComponent></MyComponent>
            __builder.OpenComponent<ConsoleApplication1.Pages.MyComponent>(0);
            __builder.AddComponentParameter(1, ""ChildContent"", (Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                __builder2.OpenComponent<ConsoleApplication1.Pages.MyComponent>(2);
                __builder2.AddComponentParameter(3, ""ChildContent"", (Microsoft.AspNetCore.Components.RenderFragment<object>)((item) => (__builder3) => {
                    __builder3.OpenComponent<ConsoleApplication1.Pages.MyComponent>(4);
                    __builder3.AddComponentParameter(5, ""ChildContent"", (Microsoft.AspNetCore.Components.RenderFragment)((__builder4) => {
                        __builder4.OpenElement(6, ""button"");
                        __builder4.AddAttribute(7, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => SomeInvokeMethod()));
                        __builder4.CloseElement();
                        __builder4.OpenComponent<ConsoleApplication1.Pages.MyComponent>(8);
                        __builder4.AddComponentParameter(9, ""OnClick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, SomeInvokeMethod));
                        __builder4.CloseComponent();
                    }));
                    __builder3.CloseComponent();
                }));
                __builder2.CloseComponent();
            }));
            __builder.CloseComponent();
        }

        protected async Task SomeInvokeMethod()
        {
            await JS.InvokeVoidAsync(""console.log"", ""This should fail!"");
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 42, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData(@"var result = await JS.InvokeAsync<double>(""getValue"");", 32)]
    [InlineData(@"_result = await JS.InvokeAsync<double>(""getValue"");", 29)]
    [InlineData(@"Result = await JS.InvokeAsync<double>(""getValue"");", 28)]
    [InlineData(@"_result += await JS.InvokeAsync<double>(""getValue"");", 30)]
    public void JSInvokeAssignedToVariableOrMemberShouldThrowWarning(string statement, int column)
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
        private double _result;
        private double Result { get; set; }

        protected override async Task OnInitializedAsync()
        {
            " + statement + @"
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, column) }, Id = id, Message = messageInvokeAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData(@"var result = await JS.InvokeAsync<double>(""getValue"");", true)]
    [InlineData(@"_result = await JS.InvokeAsync<double>(""getValue"");", true)]
    [InlineData(@"Result = await JS.InvokeAsync<double>(""getValue"");", true)]
    [InlineData(@"_result += await JS.InvokeAsync<double>(""getValue"");", true)]
    [InlineData(@"var result = await JS.InvokeAsync<double>(""getValue"");", false)]
    [InlineData(@"_result = await JS.InvokeAsync<double>(""getValue"");", false)]
    public void JSInvokeAssignedToVariableOrMemberAfterIsInteractiveCheckShouldNotThrowWarning(string statement, bool returnEarly)
    {
        var body = returnEarly
            ? @"
            if (!RendererInfo.IsInteractive)
            {
                return;
            }
            " + statement
            : @"
            if (RendererInfo.IsInteractive)
            {
                " + statement + @"
            }";

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
        private double _result;
        private double Result { get; set; }

        protected override async Task OnInitializedAsync()
        {" + body + @"
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    private static string CreateOnInitializedAsyncTest(string statements)
    {
        var body = string.Join(Environment.NewLine, statements.Split('|').Select(statement => "            " + statement));
        return @"
namespace ConsoleApplication1
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private const string Identifier = ""getValue"";
        private IJSRuntime JS = default!;

        private async Task InvokeJSAsync()
        {
            await JS.InvokeVoidAsync(Identifier);
        }

        protected override async Task OnInitializedAsync()
        {
" + body + @"
        }
    }
}" + BaseComponentDeclarations;
    }

    [Theory]
    [InlineData("Func<Task> invokeJS = async () => await JS.InvokeVoidAsync(Identifier);|await invokeJS();", 21, 53)]
    [InlineData("Func<Task> invokeJS = async () => await JS.InvokeVoidAsync(Identifier);|await invokeJS.Invoke();", 21, 53)]
    [InlineData("Func<Task> invokeJS = async () => await JS.InvokeVoidAsync(Identifier);|await (invokeJS?.Invoke() ?? Task.CompletedTask);", 21, 53)]
    [InlineData("Func<Task> invokeJS;|invokeJS = async () => await JS.InvokeVoidAsync(Identifier);|await invokeJS();", 22, 42)]
    [InlineData("Func<Task> invokeJS = async () =>|{|    await JS.InvokeVoidAsync(Identifier);|};|await invokeJS();", 23, 23)]
    [InlineData("Func<Task> invokeJS = InvokeJSAsync;|await invokeJS();", 16, 19)]
    public void JSInvokeInStoredDelegateInvokedWithoutIsInteractiveCheckShouldThrowWarning(string statements, int line, int column)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("Func<Task> invokeJS = async () => await JS.InvokeVoidAsync(Identifier);|if (RendererInfo.IsInteractive)|{|    await invokeJS();|}")]
    [InlineData("Func<Task> invokeJS = async () => await JS.InvokeVoidAsync(Identifier);|if (!RendererInfo.IsInteractive)|{|    return;|}|await invokeJS();")]
    [InlineData("Func<Task> invokeJS = async () => await JS.InvokeVoidAsync(Identifier);")]
    [InlineData("Func<Task> invokeJS = async () =>|{|    if (!RendererInfo.IsInteractive)|    {|        return;|    }|    await JS.InvokeVoidAsync(Identifier);|};|await invokeJS();")]
    [InlineData("Func<Task> invokeJS = async () => await JS.InvokeVoidAsync(Identifier);|invokeJS = () => Task.CompletedTask;|await invokeJS();")]
    [InlineData("Func<Task> invokeJS = InvokeJSAsync;|if (RendererInfo.IsInteractive)|{|    await invokeJS();|}")]
    public void JSInvokeInStoredDelegateNotInvokedWithoutIsInteractiveCheckShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }

    [Fact]
    public void JSInvokeInDelegateAssignedToParameterAndInvokedShouldThrowWarning()
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest("Run(() => { }); void Run(Action work) { work = () => JS.InvokeVoidAsync(Identifier); work(); }"),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 66) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("Run(() => { }); void Run(Action work) { work = () => JS.InvokeVoidAsync(Identifier); }")]
    [InlineData("Run(() => { }); void Run(Action work) { work = () => JS.InvokeVoidAsync(Identifier); work = () => { }; work(); }")]
    public void JSInvokeInDelegateAssignedToParameterNotInvokedShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }

    [Theory]
    [InlineData("if (RendererInfo.IsInteractive == false)|{|    await JS.InvokeVoidAsync(Identifier);|}", 23)]
    [InlineData("if (RendererInfo.IsInteractive != true)|{|    await JS.InvokeVoidAsync(Identifier);|}", 23)]
    [InlineData("if (RendererInfo.IsInteractive is false)|{|    await JS.InvokeVoidAsync(Identifier);|}", 23)]
    [InlineData("if (true == RendererInfo.IsInteractive)|{|}|else|{|    await JS.InvokeVoidAsync(Identifier);|}", 26)]
    public void JSInvokeWhenIsInteractiveComparedToFalseShouldThrowWarning(string statements, int line)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, 23) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("if (RendererInfo.IsInteractive == false)|{|    return;|}|await JS.InvokeVoidAsync(Identifier);")]
    [InlineData("if (RendererInfo.IsInteractive != true)|{|    return;|}|await JS.InvokeVoidAsync(Identifier);")]
    [InlineData("if (false == RendererInfo.IsInteractive)|{|    return;|}|await JS.InvokeVoidAsync(Identifier);")]
    [InlineData("if (RendererInfo.IsInteractive is false)|{|    return;|}|await JS.InvokeVoidAsync(Identifier);")]
    [InlineData("if (RendererInfo.IsInteractive == false)|{|}|else|{|    await JS.InvokeVoidAsync(Identifier);|}")]
    [InlineData("if (RendererInfo.IsInteractive != false)|{|    await JS.InvokeVoidAsync(Identifier);|}")]
    [InlineData("if (RendererInfo.IsInteractive is true)|{|    await JS.InvokeVoidAsync(Identifier);|}")]
    public void JSInvokeWhenIsInteractiveComparedToTrueShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }

    [Theory]
    [InlineData("await InvokeJSAsync();", true)]
    [InlineData("if (!RendererInfo.IsInteractive)|{|    return;|}|await InvokeJSAsync();", false)]
    public void JSInvokeInMethodDeclaredInOtherPartialFileShouldBeAnalyzedFromCaller(string statements, bool expectWarning)
    {
        var body = string.Join(Environment.NewLine, statements.Split('|').Select(statement => "            " + statement));
        var component = @"
namespace ConsoleApplication1
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;

    partial class TestComponent : ComponentBase
    {
        protected override async Task OnInitializedAsync()
        {
" + body + @"
        }
    }
}";
        var codeBehind = @"
namespace ConsoleApplication1
{
    using System.Threading.Tasks;
    using Microsoft.JSInterop;

    partial class TestComponent
    {
        private IJSRuntime JS = default!;

        private async Task InvokeJSAsync()
        {
            await JS.InvokeVoidAsync(""getValue"");
        }
    }
}" + BaseComponentDeclarations;

        var expected = expectWarning
            ? new[] { new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test1.cs", 13, 19) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, } }
            : Array.Empty<DiagnosticResult>();

        VerifyCSharpDiagnostic(new[] { component, codeBehind }, expected);
    }

    [Theory]
    [InlineData("var value = !RendererInfo.IsInteractive ? throw new InvalidOperationException() : 0; await JS.InvokeVoidAsync(Identifier);")]
    [InlineData("var value = RendererInfo.IsInteractive ? 0 : throw new InvalidOperationException(); await JS.InvokeVoidAsync(Identifier);")]
    public void JSInvokeAfterTernaryThatThrowsWhenNotInteractiveShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }

    [Fact]
    public void JSInvokeAfterTernaryThatDoesNotThrowShouldThrowWarning()
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest("var value = !RendererInfo.IsInteractive ? 1 : 0; await JS.InvokeVoidAsync(Identifier);"),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 68) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("try { if (!RendererInfo.IsInteractive) return; } finally { } await JS.InvokeVoidAsync(Identifier);")]
    [InlineData("try { if (!RendererInfo.IsInteractive) return; } catch (InvalidOperationException) { return; } await JS.InvokeVoidAsync(Identifier);")]
    [InlineData("try { if (!RendererInfo.IsInteractive) return; } catch (InvalidOperationException) { if (!RendererInfo.IsInteractive) return; } await JS.InvokeVoidAsync(Identifier);")]
    public void JSInvokeAfterTryWithIsInteractiveCheckOnEveryCompletingPathShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }

    [Theory]
    [InlineData("try { if (!RendererInfo.IsInteractive) return; } catch (InvalidOperationException) { } await JS.InvokeVoidAsync(Identifier);", 106)]
    [InlineData("try { } catch (InvalidOperationException) { if (!RendererInfo.IsInteractive) return; } await JS.InvokeVoidAsync(Identifier);", 106)]
    [InlineData("try { if (!RendererInfo.IsInteractive) return; } finally { await JS.InvokeVoidAsync(Identifier); }", 78)]
    public void JSInvokeAfterTryWithoutIsInteractiveCheckOnEveryCompletingPathShouldThrowWarning(string statements, int column)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, column) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("var available = RendererInfo.IsInteractive; Replace(ref available); if (available) { await JS.InvokeVoidAsync(Identifier); } static void Replace(ref bool value) => value = true;", 104)]
    [InlineData("var available = RendererInfo.IsInteractive; Replace(out available); if (available) { await JS.InvokeVoidAsync(Identifier); } static void Replace(out bool value) => value = true;", 104)]
    [InlineData("var available = RendererInfo.IsInteractive; Replace(ref available); if (!available) { return; } await JS.InvokeVoidAsync(Identifier); static void Replace(ref bool value) => value = true;", 115)]
    public void JSInvokeAfterIsInteractiveVariablePassedByRefOrOutShouldThrowWarning(string statements, int column)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, column) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("var available = RendererInfo.IsInteractive; Read(available); if (available) { await JS.InvokeVoidAsync(Identifier); } static void Read(bool value) { }")]
    [InlineData("var available = RendererInfo.IsInteractive; Read(in available); if (available) { await JS.InvokeVoidAsync(Identifier); } static void Read(in bool value) { }")]
    [InlineData("var available = RendererInfo.IsInteractive; Replace(ref available); available = RendererInfo.IsInteractive; if (available) { await JS.InvokeVoidAsync(Identifier); } static void Replace(ref bool value) => value = true;")]
    [InlineData("var available = false; Replace(out available); available = RendererInfo.IsInteractive; if (available) { await JS.InvokeVoidAsync(Identifier); } static void Replace(out bool value) => value = true;")]
    public void JSInvokeAfterIsInteractiveVariableNotReplacedByCalleeShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }

    [Theory]
    [InlineData(@"JS.Invoke<string>(""getValue"");", 20, 13)]
    [InlineData(@"__builder.AddContent(1, JS.Invoke<string>(""getValue""));", 20, 37)]
    [InlineData(@"__builder.AddContent<Action>(1, item => b => JS.Invoke<string>(""getValue""), () => { });", 20, 58)]
    [InlineData(@"if (RendererInfo.IsInteractive) { } else { __builder.AddComponentParameter(1, ""OnClick"", EventCallback.Factory.Create<object>(this, () => JS.Invoke<string>(""getValue""))); }", 20, 151)]
    [InlineData(@"if (RendererInfo.IsInteractive) { } else { __builder.AddComponentParameter(1, ""OnClick"", EventCallback.Factory.Create<object>(this, InvokeJS)); }", 15, 13)]
    public void JSInvokeInBuildRenderTreeWithoutIsInteractiveCheckShouldThrowWarning(string statements, int line, int column)
    {
        VerifyCSharpDiagnostic(CreateBuildRenderTreeTest(statements),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }, Id = id, Message = messageInvoke, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData(@"if (RendererInfo.IsInteractive) { JS.Invoke<string>(""getValue""); }")]
    [InlineData(@"if (RendererInfo.IsInteractive) { __builder.AddContent(1, JS.Invoke<string>(""getValue"")); }")]
    [InlineData(@"if (!RendererInfo.IsInteractive) { return; } JS.Invoke<string>(""getValue"");")]
    [InlineData(@"if (RendererInfo.IsInteractive) { __builder.AddComponentParameter(1, ""OnClick"", EventCallback.Factory.Create<object>(this, () => JS.Invoke<string>(""getValue""))); }")]
    [InlineData(@"if (RendererInfo.IsInteractive) { __builder.AddComponentParameter(1, ""OnClick"", EventCallback.Factory.Create<object>(this, InvokeJS)); }")]
    [InlineData(@"var interactive = RendererInfo.IsInteractive; if (interactive) { __builder.AddComponentParameter(1, ""OnClick"", EventCallback.Factory.Create<object>(this, InvokeJS)); }")]
    [InlineData(@"if (RendererInfo.IsInteractive) { __builder.AddComponentParameter(1, ""ChildContent"", (RenderFragment)((__builder2) => { __builder2.AddComponentParameter(2, ""OnClick"", EventCallback.Factory.Create<object>(this, InvokeJS)); })); }")]
    [InlineData(@"__builder.AddAttribute(1, ""onclick"", EventCallback.Factory.Create<object>(this, InvokeJS));")]
    public void JSInvokeInBuildRenderTreeWithIsInteractiveCheckShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateBuildRenderTreeTest(statements));
    }

    [Theory]
    [InlineData(@"_store.AddAttribute(1, ""onclick"", JS.Invoke<string>(""getValue""));", 47)]
    [InlineData(@"_store.AddAttribute(JS.Invoke<int>(""getValue""), ""OnClick"", EventCallback.Factory.Create<object>(this, () => { }));", 33)]
    public void JSInvokeInAddAttributeArgumentsOfOtherReceiverShouldThrowWarning(string statement, int column)
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.JSInterop;

    class AttributeStore
    {
        public void AddAttribute(int sequence, string name, object value) { }
    }

    class TestComponent : ComponentBase
    {
        private IJSInProcessRuntime JS = default!;
        private readonly AttributeStore _store = new();

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            " + statement + @"
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, column) }, Id = id, Message = messageInvoke, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("await InvokeAsync(async () => await JS.InvokeVoidAsync(Identifier));", 21, 49)]
    [InlineData("await InvokeAsync(InvokeJSAsync);", 16, 19)]
    [InlineData("Func<Task> invokeJS = async () => await JS.InvokeVoidAsync(Identifier); await InvokeAsync(invokeJS);", 21, 53)]
    [InlineData("Dispatcher dispatcher = default!; await dispatcher.InvokeAsync(async () => await JS.InvokeVoidAsync(Identifier));", 21, 94)]
    [InlineData("new System.Collections.Generic.List<int> { 1 }.ForEach(_ => JS.InvokeVoidAsync(Identifier));", 21, 73)]
    [InlineData("Array.ForEach(new[] { 1 }, _ => JS.InvokeVoidAsync(Identifier));", 21, 45)]
    [InlineData("await RunAsync(async () => await JS.InvokeVoidAsync(Identifier)); static async Task RunAsync(Func<Task> work) => await work();", 21, 46)]
    [InlineData("new System.Collections.Generic.List<int> { 1 }.ForEach(_ => { if (!RendererInfo.IsInteractive) { return; } }); await JS.InvokeVoidAsync(Identifier);", 21, 130)]
    public void JSInvokeInDelegateArgumentInvokedByCalleeShouldThrowWarning(string statements, int line, int column)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Theory]
    [InlineData("if (!RendererInfo.IsInteractive) { return; } await InvokeAsync(async () => await JS.InvokeVoidAsync(Identifier));")]
    [InlineData("var callbacks = new System.Collections.Generic.List<Func<ValueTask>>(); callbacks.Add(() => JS.InvokeVoidAsync(Identifier));")]
    [InlineData("Store(() => JS.InvokeVoidAsync(Identifier)); static void Store(Func<ValueTask> work) => _ = work;")]
    [InlineData("var lazy = new Lazy<ValueTask>(() => JS.InvokeVoidAsync(Identifier));")]
    [InlineData("AppDomain.CurrentDomain.ProcessExit += (sender, args) => JS.InvokeVoidAsync(Identifier);")]
    public void JSInvokeInDelegateArgumentNotInvokedWithoutIsInteractiveCheckShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }

    [Theory]
    [InlineData("var callbacks = new System.Collections.Generic.List<Action>(); callbacks.Add(Identifier.Length > 0 ? null : () => JS.InvokeVoidAsync(Identifier));")]
    [InlineData("var callbacks = new System.Collections.Generic.List<Action>(); Action handler = null; callbacks.Add(handler ?? (() => JS.InvokeVoidAsync(Identifier)));")]
    [InlineData("var actions = new Action[] { () => JS.InvokeVoidAsync(Identifier) };")]
    [InlineData("Action action = Identifier.Length > 0 ? null : () => JS.InvokeVoidAsync(Identifier);")]
    [InlineData("var pair = (Callback: (Action)(() => JS.InvokeVoidAsync(Identifier)), Count: 1);")]
    public void JSInvokeInLambdaNestedInExpressionNotInvokedShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }

    [Fact]
    public void JSInvokeInReturnedLambdaInvokedByCallerShouldThrowWarning()
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest("Make()(); Action Make() => () => JS.InvokeVoidAsync(Identifier);"),
            new DiagnosticResult { Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 46) }, Id = id, Message = messageInvokeVoidAsync, Severity = DiagnosticSeverity.Warning, }
        );
    }

    [Fact]
    public void JSInvokeInDelegateStoredByUnrelatedAddAttributeShouldNotThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    using System;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.JSInterop;

    class CallbackStore
    {
        private Action _callback = default!;

        public void AddAttribute(int sequence, string name, Action value) => _callback = value;
    }

    class TestComponent : ComponentBase
    {
        private IJSInProcessRuntime JS = default!;
        private readonly CallbackStore _store = new();

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            _store.AddAttribute(1, ""OnClick"", () => JS.Invoke<string>(""getValue""));
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    private static string CreateBuildRenderTreeTest(string statements)
    {
        var body = string.Join(Environment.NewLine, statements.Split('|').Select(statement => "            " + statement));
        return @"
namespace ConsoleApplication1
{
    using System;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.JSInterop;

    class TestComponent : ComponentBase
    {
        private IJSInProcessRuntime JS = default!;

        private void InvokeJS()
        {
            JS.Invoke<string>(""getValue"");
        }

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
" + body + @"
        }
    }
}" + BaseComponentDeclarations;
    }

    [Theory]
    [InlineData("var reference = DotNetObjectReference.Create(this);")]
    [InlineData("DotNetObjectReference.Create(this).Dispose();")]
    public void DotNetObjectReferenceUsageShouldNotThrowWarning(string statements)
    {
        VerifyCSharpDiagnostic(CreateOnInitializedAsyncTest(statements));
    }
}
