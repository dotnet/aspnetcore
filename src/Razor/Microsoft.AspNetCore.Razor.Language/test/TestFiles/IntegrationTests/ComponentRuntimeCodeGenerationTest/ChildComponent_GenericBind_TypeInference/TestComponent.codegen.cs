// <auto-generated/>
#pragma warning disable 1591
namespace Test
{
    #line hidden
    #pragma warning disable 8019
    using System;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using System.Collections.Generic;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using System.Linq;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using System.Threading.Tasks;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using Microsoft.AspNetCore.Components;
    #pragma warning restore 8019
    public partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            __Blazor.Test.TestComponent.TypeInference.CreateMyComponent_0(__builder, 0, 1, 
#nullable restore
#line 1 "x:\dir\subdir\Test\TestComponent.cshtml"
                        Value

#line default
#line hidden
#nullable disable
            , 2, __value => Value = __value);
            __builder.AddMarkupContent(3, "\r\n");
            __Blazor.Test.TestComponent.TypeInference.CreateMyComponent_1(__builder, 4, 5, 
#nullable restore
#line 2 "x:\dir\subdir\Test\TestComponent.cshtml"
                        Value

#line default
#line hidden
#nullable disable
            , 6, __value => Value = __value);
        }
        #pragma warning restore 1998
#nullable restore
#line 3 "x:\dir\subdir\Test\TestComponent.cshtml"
       
    string Value;

#line default
#line hidden
#nullable disable
    }
}
namespace __Blazor.Test.TestComponent
{
    #line hidden
    internal static class TypeInference
    {
        public static void CreateMyComponent_0<TItem>(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder, int seq, int __seq0, TItem __arg0, int __seq1, global::System.Action<TItem> __arg1)
        {
        __builder.OpenComponent<global::Test.MyComponent<TItem>>(seq);
        __builder.AddAttribute(__seq0, "Item", __arg0);
        __builder.AddAttribute(__seq1, "ItemChanged", __arg1);
        __builder.CloseComponent();
        }
        public static void CreateMyComponent_1<TItem>(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder, int seq, int __seq0, TItem __arg0, int __seq1, global::System.Action<TItem> __arg1)
        {
        __builder.OpenComponent<global::Test.MyComponent<TItem>>(seq);
        __builder.AddAttribute(__seq0, "Item", __arg0);
        __builder.AddAttribute(__seq1, "ItemChanged", __arg1);
        __builder.CloseComponent();
        }
    }
}
#pragma warning restore 1591
