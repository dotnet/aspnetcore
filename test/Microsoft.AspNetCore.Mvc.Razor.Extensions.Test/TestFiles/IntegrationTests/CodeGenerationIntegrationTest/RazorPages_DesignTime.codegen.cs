namespace AspNetCore
{
    #line hidden
    using TModel = global::System.Object;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/RazorPages.cshtml"
using Microsoft.AspNetCore.Mvc.RazorPages;

#line default
#line hidden
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_RazorPages_cshtml : global::Microsoft.AspNetCore.Mvc.RazorPages.Page
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
NewModel __typeHelper = default(NewModel);
        }
        ))();
        ((System.Action)(() => {
global::System.Object __typeHelper = "*, AppCode";
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
#line 29 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/RazorPages.cshtml"
                                         __o = Model.Name;

#line default
#line hidden
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
        }
        #pragma warning restore 1998
#line 7 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/RazorPages.cshtml"
            
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
