namespace AspNetCore
{
    #line hidden
    using TModel = DateTime;
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class _TestFiles_Input_ModelExpressionTagHelper_cshtml : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<DateTime>
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
DateTime __typeHelper = null;
        }
        ))();
        ((System.Action)(() => {
System.Object __typeHelper = "Microsoft.AspNetCore.Mvc.Razor.Extensions.InputTestTagHelper, Microsoft.AspNetCore.Mvc.Razor.Extensions.Test";
        }
        ))();
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        private global::Microsoft.AspNetCore.Mvc.Razor.Extensions.InputTestTagHelper __Microsoft_AspNetCore_Mvc_Razor_Extensions_InputTestTagHelper = null;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            __Microsoft_AspNetCore_Mvc_Razor_Extensions_InputTestTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.Extensions.InputTestTagHelper>();
#line 5 "/TestFiles/Input/ModelExpressionTagHelper.cshtml"
__Microsoft_AspNetCore_Mvc_Razor_Extensions_InputTestTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.Now);

#line default
#line hidden
            __Microsoft_AspNetCore_Mvc_Razor_Extensions_InputTestTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.Extensions.InputTestTagHelper>();
#line 6 "/TestFiles/Input/ModelExpressionTagHelper.cshtml"
__Microsoft_AspNetCore_Mvc_Razor_Extensions_InputTestTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => Model);

#line default
#line hidden
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<DateTime> Html { get; private set; }
    }
}
