namespace AspNetCore
{
    #line hidden
    using TModel = NewModel;
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
#line 5 "/TestFiles/Input/RazorPages.cshtml"
using Microsoft.AspNetCore.Mvc.RazorPages;

#line default
#line hidden
    public class _TestFiles_Input_RazorPages_cshtml : global::Microsoft.AspNetCore.Mvc.RazorPages.Page
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
NewModel __typeHelper = null;
        }
        ))();
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
#line 29 "/TestFiles/Input/RazorPages.cshtml"
                                         __o = Name;

#line default
#line hidden
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
        }
        #pragma warning restore 1998
#line 7 "/TestFiles/Input/RazorPages.cshtml"
            
    public class NewModel : PageModel
    {
        public IActionResult OnPost(Customer customer)
        {
            Name = customer.Name;
            return Redirect("~/customers/inlinepagemodels/");
        }

        public string Name { get; set; }
    }

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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<NewModel> Html { get; private set; }
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<NewModel> ViewData => (global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<NewModel>)PageContext?.ViewData;
        public NewModel Model => ViewData.Model;
    }
}
