// <auto-generated/>
#pragma warning disable 1591
namespace AspNetCore
{
    #line hidden
    #pragma warning disable 8019
    using TModel = global::System.Object;
    #pragma warning restore 8019
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
    using Microsoft.AspNetCore.Mvc;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using Microsoft.AspNetCore.Mvc.Rendering;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    #pragma warning restore 8019
#nullable restore
#line 4 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/RazorPagesWithRouteTemplate.cshtml"
using Microsoft.AspNetCore.Mvc.RazorPages;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute("RouteTemplate", "/About")]
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_RazorPagesWithRouteTemplate : global::Microsoft.AspNetCore.Mvc.RazorPages.Page
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
#nullable restore
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/RazorPagesWithRouteTemplate.cshtml"
global::System.Object __typeHelper = "/About";

#line default
#line hidden
#nullable disable
        }
        ))();
        ((System.Action)(() => {
#nullable restore
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/RazorPagesWithRouteTemplate.cshtml"
NewModel __typeHelper = default!;

#line default
#line hidden
#nullable disable
        }
        ))();
        }
        #pragma warning restore 219
        #pragma warning disable 0414
        private static System.Object __o = null;
        #pragma warning restore 0414
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 13 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/RazorPagesWithRouteTemplate.cshtml"
            __o = Model.Name;

#line default
#line hidden
#nullable disable
        }
        #pragma warning restore 1998
#nullable restore
#line 6 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/RazorPagesWithRouteTemplate.cshtml"
            
    public class NewModel : PageModel
    {
        public string Name { get; set; }
    }

#line default
#line hidden
#nullable disable
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
#pragma warning restore 1591
