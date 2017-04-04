namespace AspNetCore
{
    #line hidden
    using TModel = _TestFiles_Input_RazorPagesWithoutModel_cshtml;
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#line 4 "/TestFiles/Input/RazorPagesWithoutModel.cshtml"
using Microsoft.AspNetCore.Mvc.RazorPages;

#line default
#line hidden
    public class _TestFiles_Input_RazorPagesWithoutModel_cshtml : global::Microsoft.AspNetCore.Mvc.RazorPages.Page
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
System.Object __typeHelper = "*, TestAssembly";
        }
        ))();
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        private global::DivTagHelper __DivTagHelper = null;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
#line 25 "/TestFiles/Input/RazorPagesWithoutModel.cshtml"
                                         __o = Name;

#line default
#line hidden
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
        }
        #pragma warning restore 1998
#line 6 "/TestFiles/Input/RazorPagesWithoutModel.cshtml"
            
    public IActionResult OnPost(Customer customer)
    {
        Name = customer.Name;
        return Redirect("~/customers/inlinepagemodels/");
    }

    public string Name { get; set; }

    public class Customer
    {
        public string Name { get; set; }
    }

#line default
#line hidden
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<_TestFiles_Input_RazorPagesWithoutModel_cshtml> Html { get; private set; }
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<_TestFiles_Input_RazorPagesWithoutModel_cshtml> ViewData => (global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<_TestFiles_Input_RazorPagesWithoutModel_cshtml>)PageContext?.ViewData;
        public _TestFiles_Input_RazorPagesWithoutModel_cshtml Model => ViewData.Model;
    }
}
