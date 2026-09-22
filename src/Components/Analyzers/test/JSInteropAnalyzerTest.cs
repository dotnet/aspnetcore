// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class JSInteropAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new JSInteropAnalyzer();

    private static readonly string JSInteropDeclarations = @"
    namespace Microsoft.JSInterop
    {
        using System.Threading;
        using System.Threading.Tasks;

        public interface IJSRuntime
        {
            ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
            ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args);
            ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object[] args)
                => default;
            ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, CancellationToken cancellationToken, object[] args)
                => default;
            ValueTask<TValue> GetValueAsync<TValue>(string identifier)
                => default;
            ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken)
                => default;
            ValueTask SetValueAsync<TValue>(string identifier, TValue value)
                => default;
            ValueTask SetValueAsync<TValue>(string identifier, TValue value, CancellationToken cancellationToken)
                => default;
        }

        public interface IJSObjectReference : System.IAsyncDisposable
        {
            ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
            ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args);
            ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object[] args)
                => default;
            ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, CancellationToken cancellationToken, object[] args)
                => default;
            ValueTask<TValue> GetValueAsync<TValue>(string identifier)
                => default;
            ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken)
                => default;
            ValueTask SetValueAsync<TValue>(string identifier, TValue value)
                => default;
            ValueTask SetValueAsync<TValue>(string identifier, TValue value, CancellationToken cancellationToken)
                => default;
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
    public void UnguardedJSRuntimeExtensionsCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await JS.InvokeVoidAsync(""initializeComponent"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeVoidAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedJSObjectReferenceExtensionsCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSObjectReference Module = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await Module.InvokeVoidAsync(""showModal"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeVoidAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSRuntimeCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await JS.InvokeAsync<string>(""prompt"", new object[] { ""Name?"" });
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSRuntimeGetValueAsyncCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await JS.GetValueAsync<string>(""document.title"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'GetValueAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSRuntimeSetValueAsyncCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await JS.SetValueAsync(""document.title"", ""My title"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'SetValueAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSRuntimeInvokeConstructorAsyncCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await JS.InvokeConstructorAsync(""window.SomeClass"", new object[] { ""arg"" });
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeConstructorAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSObjectReferenceCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSObjectReference Module = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await Module.InvokeAsync<string>(""getTitle"", new object[] { ""document"" });
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSObjectReferenceGetValueAsyncCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSObjectReference Module = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await Module.GetValueAsync<string>(""title"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'GetValueAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSObjectReferenceSetValueAsyncCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSObjectReference Module = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await Module.SetValueAsync(""title"", ""My title"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'SetValueAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSObjectReferenceInvokeConstructorAsyncCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSObjectReference Module = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await Module.InvokeConstructorAsync(""SomeClass"", new object[] { ""arg"" });
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeConstructorAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            });
    }

    [Fact]
    public void UnguardedIJSInProcessRuntimeCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSInProcessRuntime JSInProcess { get; set; } = default!;

            public void TriggerInterop()
            {
                JSInProcess.Invoke<string>(""getValue"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'Invoke' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 17) }
            });
    }

    [Fact]
    public void UnguardedIJSInProcessObjectReferenceCall_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            private IJSInProcessObjectReference Module = default!;

            public void TriggerInterop()
            {
                Module.Invoke<string>(""focusElement"");
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'Invoke' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 17) }
            });
    }

    [Fact]
    public void JSInteropCallInsideTryCatch_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                try
                {
                    await JS.InvokeVoidAsync(""initializeComponent"");
                }
                catch (Exception)
                {
                }
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInteropCallNestedInsideTryCatch_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                try
                {
                    if (firstRender)
                    {
                        await JS.InvokeVoidAsync(""initializeComponent"");
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void JSInteropCallInsideCatch_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                try
                {
                }
                catch (Exception)
                {
                    await JS.InvokeVoidAsync(""initializeComponent"");
                }
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeVoidAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 27) }
            });
    }

    [Fact]
    public void JSInteropCallInsideFinally_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                try
                {
                }
                finally
                {
                    await JS.InvokeVoidAsync(""initializeComponent"");
                }
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeVoidAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 27) }
            });
    }

    [Fact]
    public void JSInteropCallInsideTryWithoutCatch_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                try
                {
                    await JS.InvokeVoidAsync(""initializeComponent"");
                }
                finally
                {
                }
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeVoidAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 27) }
            });
    }

    [Fact]
    public void UnguardedConcreteTypeImplementingIJSRuntime_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class CustomJsRuntime : IJSRuntime
        {
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
                => default;

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
                => default;
        }

        class TestComponent : ComponentBase
        {
            [Inject] public CustomJsRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                await JS.InvokeAsync<string>(""prompt"", new object[] { ""Name?"" });
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 23) }
            });
    }

    [Fact]
    public void JSInteropCallInLambdaInsideTryCatch_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                try
                {
                    Func<Task> action = async () => await JS.InvokeVoidAsync(""initializeComponent"");
                    await action();
                }
                catch (Exception)
                {
                }
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeVoidAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 59) }
            });
    }

    [Fact]
    public void JSInteropCallInLocalFunctionInsideTryCatch_ReportsDiagnostic()
    {
        var test = @"
    namespace BlazorApp1.Components
    {
        using System;
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;
        using Microsoft.JSInterop;

        class TestComponent : ComponentBase
        {
            [Inject] public IJSRuntime JS { get; set; } = default!;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                try
                {
                    async Task InitAsync() => await JS.InvokeVoidAsync(""initializeComponent"");
                    await InitAsync();
                }
                catch (Exception)
                {
                }
            }
        }
    }" + BlazorComponentDeclarations + JSInteropDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnguardedJSInteropCall.Id,
                Message = "JS interop call 'InvokeVoidAsync' is not guarded with a try/catch block.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 53) }
            });
    }

    [Fact]
    public void InvokeMethodOnUnrelatedJSRuntimeExtensionsType_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;

        class FakeRuntime
        {
        }

        class TestClass
        {
            public async Task TestMethod(FakeRuntime runtime)
            {
                await Microsoft.JSInterop.JSRuntimeExtensions.InvokeVoidAsync(runtime, ""myFunction"");
            }
        }
    }

    namespace Microsoft.JSInterop
    {
        using System.Threading.Tasks;

        public static class JSRuntimeExtensions
        {
            public static ValueTask InvokeVoidAsync(global::ConsoleApplication1.FakeRuntime runtime, string identifier)
                => default;
        }
    }";

        VerifyCSharpDiagnostic(test);
    }
}
