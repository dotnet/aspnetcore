namespace AspNetCore
{
    #line hidden
    using TModel = global::System.Object;
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
#line 1 "/TestFiles/Input/Inject.cshtml"
using MyNamespace;

#line default
#line hidden
    public class _TestFiles_Input_Inject_cshtml : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
MyApp __typeHelper = null;
        }
        ))();
        ((System.Action)(() => {
System.Object MyPropertyName = null;
        }
        ))();
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
        }
        #pragma warning restore 1998
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
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
