#pragma checksum "/TestFiles/Input/InjectWithModel.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "1f010500f93116162444110956e512df61642f4e"
namespace AspNetCore
{
    #line hidden
#line 1 ""
using System;

#line default
#line hidden
    using System.Threading.Tasks;
#line 2 ""
using System.Linq;

#line default
#line hidden
#line 3 ""
using System.Collections.Generic;

#line default
#line hidden
#line 4 ""
using Microsoft.AspNetCore.Mvc;

#line default
#line hidden
#line 5 ""
using Microsoft.AspNetCore.Mvc.Rendering;

#line default
#line hidden
#line 6 ""
using Microsoft.AspNetCore.Mvc.ViewFeatures;

#line default
#line hidden
    public class _TestFiles_Input_InjectWithModel_cshtml : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<MyModel>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public MyService<MyModel> Html { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public MyApp MyPropertyName { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
    }
}
