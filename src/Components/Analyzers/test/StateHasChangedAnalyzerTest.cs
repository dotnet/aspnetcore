// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class StateHasChangedAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new StateHasChangedAnalyzer();

    private static readonly string ComponentDeclarations = @"
    namespace Microsoft.AspNetCore.Components
    {
        using System;
        using System.Threading.Tasks;

        public struct ParameterView
        {
        }

        public sealed class ParameterAttribute : Attribute
        {
            public bool CaptureUnmatchedValues { get; set; }
        }

        public sealed class CascadingParameterAttribute : Attribute
        {
        }

        public interface IComponent { }

        public struct EventCallback
        {
            public EventCallback(Func<Task> callback) { }
        }

        public sealed class EventCallbackFactory
        {
            public EventCallback Create(object receiver, Action callback) => default;

            public EventCallback Create(object receiver, Func<Task> callback) => default;

            public EventCallback Create<T>(object receiver, Action<T> callback) => default;
        }

        public abstract class ComponentBase : IComponent
        {
            public EventCallbackFactory EventCallbackFactory => default;

            protected virtual void OnInitialized() { }

            protected virtual Task OnInitializedAsync() => Task.CompletedTask;

            protected virtual void OnParametersSet() { }

            protected virtual Task OnParametersSetAsync() => Task.CompletedTask;

            public virtual Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;

            protected Task InvokeAsync(Action workItem) => Task.CompletedTask;

            protected void StateHasChanged() { }
        }
    }
";

    [Fact]
    public void OnLifecycleEvents_WithStateHasChanged_ReportsDiagnostic()
    {

        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override void OnInitialized()
            {
                StateHasChanged();
            }

            protected override void OnParametersSet()
            {
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnInitialized' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnParametersSet' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 17) }
            });
    }

    [Fact]
    public void OnAsyncLifecycleEvents_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            private int _refreshCount;

            protected override async Task OnInitializedAsync()
            {
                _refreshCount++;
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                StateHasChanged();
            }

            protected override async Task OnParametersSetAsync()
            {
                _refreshCount++;
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnInitializedAsync' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnInitializedAsync' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnParametersSetAsync' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 30, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnParametersSetAsync' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 40, 17) }
            });
    }

    [Fact]
    public void EventHandler_ReportsRedundantStateHasChangedCalls()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private int _refreshCount;

            public EventCallback OnRefresh => EventCallbackFactory.Create(this, OnRefreshClicked);

            private async Task OnRefreshClicked()
            {
                _refreshCount++;
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                StateHasChanged();

                await Task.Delay(1);

                _refreshCount++;
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnRefreshClicked' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 17) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnRefreshClicked' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 17) }
            });
    }

    [Fact]
    public void ExpressionBodyLifecycleMethod_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override void OnInitialized() => StateHasChanged();

            protected override void OnParametersSet() => StateHasChanged();
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnInitialized' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 56) }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnParametersSet' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 58) }
            });
    }

    [Fact]
    public void SyncEventHandlerAction_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public EventCallback OnRefresh => EventCallbackFactory.Create(this, OnRefreshClicked);

            private void OnRefreshClicked()
            {
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnRefreshClicked' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 17) }
            });
    }

    [Fact]
    public void SingleAwaitLifecycleMethod_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            protected override async Task OnInitializedAsync()
            {
                StateHasChanged();

                await Task.Delay(1);
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnInitializedAsync' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 17) }
            });
    }

    [Fact]
    public void SingleAwaitEventHandler_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public EventCallback OnRefresh => EventCallbackFactory.Create(this, OnRefreshClicked);

            private async Task OnRefreshClicked()
            {
                StateHasChanged();

                await Task.Delay(1);
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnRefreshClicked' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 17) }
            });
    }

    [Fact]
    public void ExpressionBodiedEventHandler_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public EventCallback OnRefresh => EventCallbackFactory.Create(this, OnRefreshClicked);

            private void OnRefreshClicked() => StateHasChanged();
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnRefreshClicked' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 48) }
            });
    }

    [Fact]
    public void MultiLevelInheritance_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class MyBase : ComponentBase
        {
        }

        class TestComponent : MyBase
        {
            protected override void OnInitialized()
            {
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnInitialized' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 17) }
            });
    }

    [Fact]
    public void ExplicitThisQualifier_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override void OnInitialized()
            {
                this.StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnInitialized' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            });
    }

    [Fact]
    public void SyncEventHandlerActionOfT_WithStateHasChanged_ReportsDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public EventCallback OnCountChangedCallback => EventCallbackFactory.Create<int>(this, OnCountChanged);

            private void OnCountChanged(int value)
            {
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(
            test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id,
                Message = "StateHasChanged is unnecessary in method 'OnCountChanged' and can be removed.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 17) }
            });
    }

    [Fact]
    public void StateHasChangedBetweenTwoAwaits_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override async Task OnInitializedAsync()
            {
                await Task.Delay(1);

                StateHasChanged();

                await Task.Delay(1);
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NonComponentBaseClassWithOnInitialized_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        class TestComponent
        {
            protected void OnInitialized()
            {
                StateHasChanged();
            }

            protected void StateHasChanged()
            {
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void StateHasChangedInsideNestedFunctionLike_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            protected override void OnInitialized()
            {
                void Refresh() => StateHasChanged();
                System.Action refresh = () => StateHasChanged();

                Refresh();
                refresh();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void StateHasChangedInInvokeAsyncLambda_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            protected override async Task OnInitializedAsync()
            {
                await InvokeAsync(() => StateHasChanged());
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void PrivateMethodThatIsNotEventHandler_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private void Refresh()
            {
                StateHasChanged();
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void SetParametersAsync_WithStateHasChanged_DoesNotReportDiagnostic()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using System.Threading.Tasks;
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public override Task SetParametersAsync(ParameterView parameters)
            {
                StateHasChanged();

                return Task.CompletedTask;
            }
        }
    }" + ComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }
}
