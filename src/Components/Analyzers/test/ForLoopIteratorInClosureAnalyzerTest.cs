// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class ForLoopIteratorInClosureAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ForLoopIteratorInClosureAnalyzer();

    private static readonly string BaseComponentDeclarations = @"
namespace Microsoft.AspNetCore.Components
{
    using System;

    public interface IComponent { }

    public abstract class ComponentBase : IComponent
    {
        protected virtual void BuildRenderTree(Rendering.RenderTreeBuilder __builder) { }
    }
    public static class BindConverter
    {
        public static T FormatValue<T>(T value) => value;
    }

    public readonly struct EventCallback
    {
        public static EventCallbackFactory Factory { get; } = new();
    }

    public readonly struct EventCallback<T>
    {
    }

    public class EventCallbackFactory
    {
        public EventCallback<T> Create<T>(object receiver, Action callback) => default;
        public EventCallback<T> Create<T>(object receiver, Action<T> callback) => default;
        public object CreateBinder<T>(object receiver, Action<T> setter, T value) => null;
    }

    public delegate void RenderFragment(Rendering.RenderTreeBuilder builder);
    public delegate RenderFragment RenderFragment<in TValue>(TValue value);

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

namespace ConsoleApplication1
{
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.AspNetCore.Components.CompilerServices;

    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        public string[] stringArray = [""test1"", ""test2""];
        public void SelectItem(int index) { }
        public void UpdateHeading(Microsoft.AspNetCore.Components.Web.MouseEventArgs e, int index) { }
    }

    namespacePages
    {
        class AddressEditor : Microsoft.AspNetCore.Components.IComponent { }
    }
}
";

    [Fact]
    public void IteratorVariableDifferentIncrementationsUsedInLambdaShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var incremented = 0; incremented < stringArray.Length; incremented++)
            {
                //  <button @onclick=""@(() => SelectItem(incremented))"">Item @incremented</button>
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
            () => SelectItem(incremented)

                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, incremented
                );
                __builder.CloseElement();
            }

            for (var decremented = stringArray.Length - 1; decremented >= 0; decremented--)
            {
                //  <button @onclick=""@(() => SelectItem(decremented))"">Item @decremented</button>
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
            () => SelectItem(decremented)

                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, decremented
                );
                __builder.CloseElement();
            }

            for (var assigned = 0; assigned < stringArray.Length; assigned = assigned + 1)
            {
                //  <button @onclick=""@(() => SelectItem(assigned))"">Item @assigned</button>
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
            () => SelectItem(assigned)

                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, assigned
                );
                __builder.CloseElement();
            }

            for (var compoundAssigned = 0; compoundAssigned < stringArray.Length; compoundAssigned += 1)
            {
                //  <button @onclick=""@(() => SelectItem(compoundAssigned))"">Item @compoundAssigned</button>
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
            () => SelectItem(compoundAssigned)

                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, compoundAssigned
                );
                __builder.CloseElement();
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'incremented' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 13, 30)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'decremented' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 27, 30)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'assigned' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 41, 30)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'compoundAssigned' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 55, 30)
                }
            });
    }

    [Fact]
    public void IteratorVariableButCachedUsedInLambdaShouldNotThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i++)
            {
                var index = i;
                SelectItem(index);

                //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
            () => SelectItem(index)

                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, i
                );
                __builder.CloseElement();
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IteratorVariablesInLambdaWithArgsShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i++)
            {
                SelectItem(i);

                // <button @onclick=""@(e => UpdateHeading(e, i))"">Button #@i</button>
                __builder.OpenElement(19, ""button"");
                __builder.AddAttribute(20, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    e => UpdateHeading(e, i)
                ));
                __builder.AddContent(21, ""Button #"");
                    __builder.AddContent(22, i
                );
                __builder.CloseElement();
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 15, 43)
                }
            });
    }

    [Fact]
    public void IteratorVariablesInBindingIndexedVariableShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i++)
            {
                SelectItem(i);

                //  <input type=""text"" @bind=""stringArray[i]"" />
                __builder.AddMarkupContent(18, ""\r\n    "");
                __builder.OpenElement(19, ""input"");
                __builder.AddAttribute(20, ""type"", ""text"");
                __builder.AddAttribute(21, ""value"", Microsoft.AspNetCore.Components.BindConverter.FormatValue(
                    stringArray[i]
                ));
                __builder.AddAttribute(22, ""onchange"", Microsoft.AspNetCore.Components.EventCallback.Factory.CreateBinder(this, __value => stringArray[i] = __value, stringArray[i]));
                __builder.SetUpdatesAttributeName(""value"");
                __builder.CloseElement();
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 17, 33)
                }
            });
    }

    [Fact]
    public void IteratorVariablesNestedIfsInLambdaShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i++)
            {
                var index = i;
                SelectItem(i);

                if (i == 1) {
                    for (var j = 0; j < stringArray.Length; j++)
                    {
                        //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                        __builder.OpenElement(14, ""button"");
                        __builder.AddAttribute(15, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(i)

                        ));
                        __builder.AddContent(16, ""Item "");
                            __builder.AddContent(17, i
                        );
                        __builder.CloseElement();
                    }
                }
            }
        }
    }
}" + BaseComponentDeclarations;

    VerifyCSharpDiagnostic(test,
        new DiagnosticResult
        {
            Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
            Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 19, 38)
            }
        });
    }

    [Fact]
    public void IteratorVariablesNestedForEachInLambdaShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i++)
            {
                var index = i;
                SelectItem(i);

                if (i == 1)
                {
                    foreach (var item in items)
                    {
                        //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                        __builder.OpenElement(14, ""button"");
                        __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(i)

                        ));
                        __builder.AddContent(16, ""Item "");
                            __builder.AddContent(17, i
                        );
                        __builder.CloseElement();
                    }
                }
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                new DiagnosticResultLocation("Test0.cs", 20, 38)
                }
            });
    }

    [Fact]
    public void NonIncrementedVariablesInLambdaShouldNotThrowWarnings()
    {
        // Shouldn't throw because 'j' is a copy of 'i' at the start with value 0 and won't be iterated on.
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (int i = 0, j = i; i < stringArray.Length; i++)
            {
                //  <button @onclick=""@(() => SelectItem(j))"">Item @j</button>
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
            () => SelectItem(j)

                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, j
                );
                __builder.CloseElement();
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NonIncrementedVariablesButLaterIncrementedShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (int i = 0, j = i; i < stringArray.Length; i++)
            {
                //  <button @onclick=""@(() => SelectItem(j))"">Item @j</button>
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
            () => SelectItem(j)

                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, j
                );
                __builder.CloseElement();

                if (i > 0)
                {
                    j++;
                }
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'j' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                new DiagnosticResultLocation("Test0.cs", 13, 30)
                }
            });
    }

    [Fact]
    public void NonIncrementedVariablesButLaterAssignedShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (int i = 0, j = i; i < stringArray.Length; i++)
            {
                //  <button @onclick=""@(() => SelectItem(j))"">Item @j</button>
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
            () => SelectItem(j)

                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, j
                );
                __builder.CloseElement();

                if (i > 0)
                {
                    j = i;
                }
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'j' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                new DiagnosticResultLocation("Test0.cs", 13, 30)
                }
            });
    }

    [Fact]
    public void IteratorVariablesInComponentChildContentShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i++)
            {
                SelectItem(i);

                //  <MyComponent>Count: @i</MyComponent>
                __builder.OpenComponent<ConsoleApplication1.Pages.MyComponent>(23);
                __builder.AddAttribute(24, ""ChildContent"", (Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                    __builder2.AddContent(25, ""Count: "");
                    __builder2.AddContent(26, i
                    );
                }
                ));
                __builder.CloseComponent();
            }
        }

        public string[] stringArray = [""test1"", ""test2""];
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 16, 47)
                }
            });
    }

    [Fact]
    public void IteratorVariablesInComponentParameterShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i++)
            {
                SelectItem(i);

                //  <MyComponent OnClick=""@(() => SelectItem(i))""></MyComponent>
                __builder.OpenComponent<ConsoleApplication1.Pages.AddressEditor>(14);
                __builder.AddComponentParameter(78, ""OnClick"", Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>>(Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(i)
                )));
                __builder.CloseComponent();
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 15, 38)
                }
            });
    }

    [Fact]
    public void IteratorVariablesNotInClosuresShouldNotThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < items.Length; i++)
            {
                var index = i;
                __builder.OpenElement(14, ""button"");
                __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(index)
                ));
                __builder.AddContent(16, ""Item "");
                    __builder.AddContent(17, index
                );
                __builder.CloseElement();
                __builder.AddMarkupContent(18, ""\r\n    "");
                __builder.OpenElement(19, ""button"");
                __builder.AddAttribute(20, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    HandleClick
                ));
                __builder.AddContent(21, ""Item "");
                    __builder.AddContent(22, i
                );
                __builder.CloseElement();
                __builder.AddMarkupContent(23, ""\r\n    "");
                __builder.OpenElement(24, ""span"");
                __builder.AddContent(25, ""Item "");
                    __builder.AddContent(26, i
                );
                __builder.CloseElement();
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IteratorVariableUsedInsideNestedForEachShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i += 1)
            {
                foreach (var item in items)
                {
                    //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                    __builder.OpenElement(14, ""button"");
                    __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                () => SelectItem(i)

                    ));
                    __builder.AddContent(16, ""Item "");
                        __builder.AddContent(17, i
                    );
                    __builder.CloseElement();
                }
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 15, 34)
                }
            });
    }

    [Fact]
    public void IteratorVariablesInForEachShouldNotThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            foreach (var item in items)
            {
                // <button @onclick=""@(() => SelectItem(item))"">@item</button>

                __builder.OpenElement(27, ""button"");
                __builder.AddAttribute(28, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(item)
                ));
                    __builder.AddContent(29, item
                );
                __builder.CloseElement();
            }

            foreach (var i in Enumerable.Range(0, items.Length))
            {
                // <button @onclick=""@(() => SelectItem(i))"">Item @i</button>

                __builder.OpenElement(30, ""button"");
                __builder.AddAttribute(31, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(i)
                ));
                __builder.AddContent(32, ""Item "");
                    __builder.AddContent(33, i
                );
                __builder.CloseElement();
            }

            foreach (var (index, item) in items.Index())
            {
                // <button @onclick=""@(() => SelectItem(index))"">@item</button>

                __builder.OpenElement(34, ""button"");
                __builder.AddAttribute(35, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(index)
                ));
                    __builder.AddContent(36, item
                );
                __builder.CloseElement();
            }

            foreach (var entry in items.Select((item, index) => (item, index)))
            {
                // <button @onclick=""@(() => SelectItem(entry.index))"">@entry.item</button>

                __builder.OpenElement(37, ""button"");
                __builder.AddAttribute(38, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(entry.index)
                ));
                    __builder.AddContent(39, entry.item
                );
                __builder.CloseElement();
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IteratorVariableUsedInsideNestedTryCatchFinallyShouldThrowWarnings()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i += 1)
            {
                try {

                    //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                    __builder.OpenElement(14, ""button"");
                    __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                () => SelectItem(i)

                    ));
                    __builder.AddContent(16, ""Item "");
                        __builder.AddContent(17, i
                    );
                    __builder.CloseElement();

                } catch (Exception ex) {

                    //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                    __builder.OpenElement(14, ""button"");
                    __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                () => SelectItem(i)

                    ));
                    __builder.AddContent(16, ""Item "");
                        __builder.AddContent(17, i
                    );
                    __builder.CloseElement();

                } finally {

                    //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                    __builder.OpenElement(14, ""button"");
                    __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                () => SelectItem(i)

                    ));
                    __builder.AddContent(16, ""Item "");
                        __builder.AddContent(17, i
                    );
                    __builder.CloseElement();
                }
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 15, 34)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 28, 34)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 41, 34)
                }
            });
    }

    [Fact]
    public void IteratorVariableUsedInsideSwitchStatementShouldThrowWarning()
    {
        var test = @"
namespace ConsoleApplication1
{
    partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            for (var i = 0; i < stringArray.Length; i += 1)
            {
                switch (i) {
                    case 0:
                        //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                        __builder.OpenElement(14, ""button"");
                        __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(i)

                        ));
                        __builder.AddContent(16, ""Item "");
                            __builder.AddContent(17, i
                        );
                        __builder.CloseElement();
                        break;
                    case 1:
                        //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
                        __builder.OpenElement(14, ""button"");
                        __builder.AddAttribute(15, ""onclick"", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                    () => SelectItem(i)
                        ));
                        __builder.AddContent(16, ""Item "");
                            __builder.AddContent(17, i
                        );
                        __builder.CloseElement();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}" + BaseComponentDeclarations;

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 15, 38)
                }
            },
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 27, 38)
                }
            });
    }

}
