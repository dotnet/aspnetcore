// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Microsoft.AspNetCore.Components.Analyzers.Test;

public class ForLoopIteratorInClosureAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ForLoopIteratorInClosureAnalyzer();

    [Fact]
    public void IteratorVariablesButCachedInLambdaShouldNotThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (var i = 0; i < stringArray.Length; i++)
        {
            var index = i;
            SelectItem(index);

            //  <button @onclick=""@(() => SelectItem(i))"">Item @i</button>
            __builder.OpenElement(14, ""button"");
            __builder.AddAttribute(15, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
        () => SelectItem(index)

            ));
            __builder.AddContent(16, ""Item "");
                __builder.AddContent(17, i
            );
            __builder.CloseElement();
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
    public void SelectItem(int index) { }
}";

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IteratorVariablesInLambdaWithArgsShouldThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (var i = 0; i < stringArray.Length; i++)
        {
            SelectItem(i);

            // <button @onclick=""@(e => UpdateHeading(e, i))"">Button #@i</button>
            __builder.OpenElement(19, ""button"");
            __builder.AddAttribute(20, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                e => UpdateHeading(e, i)
            ));
            __builder.AddContent(21, ""Button #"");
                __builder.AddContent(22, i
            );
            __builder.CloseElement();
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
    public void UpdateHeading(MouseEventArgs e, int index) { }
}";

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 15, 39)
                }
            });
    }

    [Fact]
    public void IteratorVariablesInBindingIndexedVariableShouldThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (var i = 0; i < stringArray.Length; i++)
        {
            SelectItem(i);

            //  <input type=""text"" @bind=""stringArray[i]"" />
            __builder.AddMarkupContent(18, ""\r\n    "");
            __builder.OpenElement(19, ""input"");
            __builder.AddAttribute(20, ""type"", ""text"");
            __builder.AddAttribute(21, ""value"", global::Microsoft.AspNetCore.Components.BindConverter.FormatValue(
                stringArray[i]
            ));
            __builder.AddAttribute(22, ""onchange"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.CreateBinder(this, __value => stringArray[i] = __value, stringArray[i]));
            __builder.SetUpdatesAttributeName(""value"");
            __builder.CloseElement();
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
}";

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 17, 29)
                }
            });
    }

    [Fact]
    public void IteratorVariablesNestedIfsInLambdaShouldThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
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

    public string[] stringArray = [""test1"", ""test2""];
    public void SelectItem(int index) { }
}";

    VerifyCSharpDiagnostic(test,
        new DiagnosticResult
        {
            Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
            Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 19, 34)
            }
        });
    }

    [Fact]
    public void IteratorVariablesNestedForEachInLambdaShouldThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (var i = 0; i < stringArray.Length; i++)
        {
            var index = i;
            SelectItem(i);

            if (i == 1) {
                foreach (var item in items)
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

    public string[] stringArray = [""test1"", ""test2""];
    public void SelectItem(int index) { }
}";

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                new DiagnosticResultLocation("Test0.cs", 19, 34)
                }
            });
    }

    [Fact]
    public void NonIncrementedVariablesInLambdaShouldNotThrowWarnings()
    {
        // Shouldn't throw because 'j' is a copy of 'i' at the start with value 0 and won't be iterated on.
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (int i = 0, j = i; i < stringArray.Length; i++)
        {
            //  <button @onclick=""@(() => SelectItem(j))"">Item @j</button>
            __builder.OpenElement(14, ""button"");
            __builder.AddAttribute(15, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
        () => SelectItem(j)

            ));
            __builder.AddContent(16, ""Item "");
                __builder.AddContent(17, j
            );
            __builder.CloseElement();
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
    public void SelectItem(int index) { }
}";

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void NonIncrementedVariablesButLaterIncrementedShouldThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (int i = 0, j = i; i < stringArray.Length; i++)
        {
            //  <button @onclick=""@(() => SelectItem(j))"">Item @j</button>
            __builder.OpenElement(14, ""button"");
            __builder.AddAttribute(15, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
        () => SelectItem(j)

            ));
            __builder.AddContent(16, ""Item "");
                __builder.AddContent(17, j
            );
            __builder.CloseElement();

            if (i > 0) {
                j++;
            }
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
    public void SelectItem(int index) { }
}";

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'j' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                new DiagnosticResultLocation("Test0.cs", 13, 26)
                }
            });
    }

    [Fact]
    public void NonIncrementedVariablesButLaterAssignedShouldThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (int i = 0, j = i; i < stringArray.Length; i++)
        {
            //  <button @onclick=""@(() => SelectItem(j))"">Item @j</button>
            __builder.OpenElement(14, ""button"");
            __builder.AddAttribute(15, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
        () => SelectItem(j)

            ));
            __builder.AddContent(16, ""Item "");
                __builder.AddContent(17, j
            );
            __builder.CloseElement();

            if (i > 0) {
                j = i;
            }
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
    public void SelectItem(int index) { }
}";

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'j' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                new DiagnosticResultLocation("Test0.cs", 13, 26)
                }
            });
    }

    [Fact]
    public void IteratorVariablesInComponentChildContentShouldThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (var i = 0; i < stringArray.Length; i++)
        {
            SelectItem(i);

            //  <MyComponent>Count: @i</MyComponent>
            __builder.OpenComponent<global::BlazorUnitedApp.Pages.MyComponent>(23);
            __builder.AddAttribute(24, ""ChildContent"", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                __builder2.AddContent(25, ""Count: "");
                __builder2.AddContent(26, i
                );
            }
            ));
            __builder.CloseComponent();
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
}";

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 16, 43)
                }
            });
    }

    [Fact]
    public void IteratorVariablesInComponentParameterShouldThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        for (var i = 0; i < stringArray.Length; i++)
        {
            SelectItem(i);

            //  <MyComponent OnClick=""@(() => SelectItem(i))""></MyComponent>
            __builder.OpenComponent<global::BlazorUnitedApp.Pages.AddressEditor>(14);
            __builder.AddComponentParameter(15, nameof(global::BlazorUnitedApp.Pages.AddressEditor.
                OnClick
            ), global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::Microsoft.AspNetCore.Components.EventCallback<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>>(global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                () => SelectItem(i)
            )));
            __builder.CloseComponent();
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
}";

        VerifyCSharpDiagnostic(test,
            new DiagnosticResult
            {
                Id = DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id,
                Message = "For loop iterator 'i' that is being incremented is used in a closure or RenderFragment/ChildContent. This can lead to unexpected runtime behavior.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 17, 34)
                }
            });
    }

    [Fact]
    public void IteratorVariablesNotInClosuresShouldNotThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
for (var i = 0; i < items.Length; i++)
{
    var index = i;
            __builder.OpenElement(14, ""button"");
            __builder.AddAttribute(15, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
() => SelectItem(index)
            ));
            __builder.AddContent(16, ""Item "");
__builder.AddContent(17, index
            );
            __builder.CloseElement();
            __builder.AddMarkupContent(18, ""\r\n    "");
            __builder.OpenElement(19, ""button"");
            __builder.AddAttribute(20, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
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

    public string[] stringArray = [""test1"", ""test2""];
}";

        VerifyCSharpDiagnostic(test);
    }

    [Fact]
    public void IteratorVariablesInForEachShouldNotThrowWarnings()
    {
        var test = @"
using System;

class C : global::Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        foreach (var item in items)
        {
            // <button @onclick=""@(() => SelectItem(item))"">@item</button>

            __builder.OpenElement(27, ""button"");
            __builder.AddAttribute(28, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
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
            __builder.AddAttribute(31, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
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
            __builder.AddAttribute(35, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
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
            __builder.AddAttribute(38, ""onclick"", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
                () => SelectItem(entry.index)
            ));
                __builder.AddContent(39, entry.item
            );
            __builder.CloseElement();
        }
    }

    public string[] stringArray = [""test1"", ""test2""];
}";

        VerifyCSharpDiagnostic(test);
    }
}
