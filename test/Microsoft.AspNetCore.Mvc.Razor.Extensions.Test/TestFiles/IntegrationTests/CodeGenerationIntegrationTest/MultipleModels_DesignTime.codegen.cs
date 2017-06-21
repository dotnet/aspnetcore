[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(null, typeof(AspNetCore.TestFiles_IntegrationTests_CodeGenerationIntegrationTest_MultipleModels_cshtml))]
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
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_MultipleModels_cshtml : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<ThisShouldBeGenerated>
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
ThisShouldBeGenerated __typeHelper = default(ThisShouldBeGenerated);
        }
        ))();
        ((System.Action)(() => {
System.Collections.IEnumerable __typeHelper = default(System.Collections.IEnumerable);
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
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<ThisShouldBeGenerated> Html { get; private set; }
    }
}
