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
#nullable restore
#line 1 "x:\dir\subdir\Test\TestComponent.cshtml"
using Microsoft.AspNetCore.Components.Web;

#line default
#line hidden
#nullable disable
    public partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            __builder.OpenComponent<Test.MyComponent>(0);
            __builder.AddAttribute(1, "OnClick", Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>>(Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, 
#nullable restore
#line 2 "x:\dir\subdir\Test\TestComponent.cshtml"
                       Increment

#line default
#line hidden
#nullable disable
            )));
            __builder.CloseComponent();
        }
        #pragma warning restore 1998
#nullable restore
#line 4 "x:\dir\subdir\Test\TestComponent.cshtml"
       
    private int counter;
    private void Increment(MouseEventArgs e) {
        counter++;
    }

#line default
#line hidden
#nullable disable
    }
}
#pragma warning restore 1591
