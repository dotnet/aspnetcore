// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers;

public class ElementReferenceUsageAnalyzerTest : DiagnosticVerifier
{
    private static readonly string TestDeclarations = $@"
    namespace {typeof(ParameterAttribute).Namespace}
    {{
        public class {typeof(ParameterAttribute).Name} : System.Attribute
        {{
            public bool CaptureUnmatchedValues {{ get; set; }}
        }}

        public class {typeof(CascadingParameterAttribute).Name} : System.Attribute
        {{
        }}

        public interface {typeof(IComponent).Name}
        {{
        }}

        public abstract class ComponentBase : {typeof(IComponent).Name}
        {{
            protected virtual void OnInitialized() {{ }}
            protected virtual System.Threading.Tasks.Task OnInitializedAsync() => System.Threading.Tasks.Task.CompletedTask;
            protected virtual void OnParametersSet() {{ }}
            protected virtual System.Threading.Tasks.Task OnParametersSetAsync() => System.Threading.Tasks.Task.CompletedTask;
            protected virtual void OnAfterRender(bool firstRender) {{ }}
            protected virtual System.Threading.Tasks.Task OnAfterRenderAsync(bool firstRender) => System.Threading.Tasks.Task.CompletedTask;
        }}

        public struct ElementReference
        {{
            public string Id {{ get; }}
            public ElementReference(string id) {{ Id = id; }}
            public System.Threading.Tasks.ValueTask FocusAsync() => new System.Threading.Tasks.ValueTask();
        }}
    }}
";
    [Fact]
    public void ElementReferenceUsageInOnAfterRenderAsync_DoesNotWarn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                if (firstRender)
                {
                    await myElement.FocusAsync();
                }
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ElementReferenceUsageInOnAfterRender_DoesNotWarn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            protected override void OnAfterRender(bool firstRender)
            {
                if (firstRender)
                {
                    var elementId = myElement.Id;
                }
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ElementReferenceUsageInOnInitialized_Warns()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            protected override void OnInitialized()
            {
                var elementId = myElement.Id;
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync.Id,
                Message = "ElementReference 'myElement' should only be accessed within OnAfterRenderAsync or OnAfterRender",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 33)
                }
            });
    }

    [Fact]
    public void ElementReferenceUsageInEventHandler_Warns()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            private async Task HandleClick()
            {
                await myElement.FocusAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync.Id,
                Message = "ElementReference 'myElement' should only be accessed within OnAfterRenderAsync or OnAfterRender",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 13, 23)
                }
            });
    }

    [Fact]
    public void ElementReferencePropertyUsage_Warns()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            public ElementReference MyElement { get; set; }

            protected override void OnInitialized()
            {
                var elementId = MyElement.Id;
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync.Id,
                Message = "ElementReference 'MyElement' should only be accessed within OnAfterRenderAsync or OnAfterRender",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 33)
                }
            });
    }

    [Fact]
    public void ElementReferenceUsageInOnParametersSet_Warns()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            protected override void OnParametersSet()
            {
                var elementId = myElement.Id;
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync.Id,
                Message = "ElementReference 'myElement' should only be accessed within OnAfterRenderAsync or OnAfterRender",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 33)
                }
            });
    }

    [Fact]
    public void ElementReferenceUsageInHelperMethodCalledFromOnAfterRender_DoesNotWarn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                if (firstRender)
                {
                    await Helper();
                }
            }

            private async Task Helper()
            {
                await myElement.FocusAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ElementReferenceUsageInHelperMethodCalledFromEventHandler_Warns()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            private async Task HandleClick()
            {
                await Helper();
            }

            private async Task Helper()
            {
                await myElement.FocusAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ElementReferenceUsageInMethodCalledOutsideOnAfterRender.Id,
                Message = "Method 'Helper' accesses ElementReference 'myElement' and is being invoked outside of OnAfterRenderAsync or OnAfterRender",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 18, 23)
                }
            });
    }

    [Fact]
    public void ElementReferenceUsageInHelperMethodCalledFromBothSafeAndUnsafeContexts_Warns()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                if (firstRender)
                {
                    await Helper(); // Safe call
                }
            }

            private async Task HandleClick()
            {
                await Helper(); // Unsafe call
            }

            private async Task Helper()
            {
                await myElement.FocusAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ElementReferenceUsageInMethodCalledOutsideOnAfterRender.Id,
                Message = "Method 'Helper' accesses ElementReference 'myElement' and is being invoked outside of OnAfterRenderAsync or OnAfterRender",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 26, 23)
                }
            });
    }

    [Fact]
    public void ElementReferenceUsageInChainedHelperMethods_DoesNotWarn()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;
        using System.Threading.Tasks;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                if (firstRender)
                {
                    await HelperLevel1();
                }
            }

            private async Task HelperLevel1()
            {
                await HelperLevel2();
            }

            private async Task HelperLevel2()
            {
                await myElement.FocusAsync();
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void ElementReferenceUsageInRegularMethod_StillWarns()
    {
        var test = @"
    namespace ConsoleApplication1
    {
        using Microsoft.AspNetCore.Components;

        class TestComponent : ComponentBase
        {
            private ElementReference myElement;

            public void SomePublicMethod()
            {
                var elementId = myElement.Id; // Should warn with standard diagnostic
            }
        }
    }" + TestDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ElementReferenceShouldOnlyBeAccessedInOnAfterRenderAsync.Id,
                Message = "ElementReference 'myElement' should only be accessed within OnAfterRenderAsync or OnAfterRender",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 12, 33)
                }
            });
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ElementReferenceUsageAnalyzer();
    }
}