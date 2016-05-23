namespace AspNetCore
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using System.Threading.Tasks;

    public class testfiles_input_modelexpressiontaghelper_cshtml : Microsoft.AspNetCore.Mvc.Razor.RazorPage<DateTime>
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = "Microsoft.AspNetCore.Mvc.Razor.InputTestTagHelper, Microsoft.AspNetCore.Mvc.Razor.Host.Test";
#line 1 "testfiles/input/modelexpressiontaghelper.cshtml"
var __modelHelper = default(DateTime);

#line default
#line hidden
            #pragma warning restore 219
        }
        #line hidden
        private global::Microsoft.AspNetCore.Mvc.Razor.InputTestTagHelper __Microsoft_AspNetCore_Mvc_Razor_InputTestTagHelper = null;
        #line hidden
        public testfiles_input_modelexpressiontaghelper_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<DateTime> Html { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __Microsoft_AspNetCore_Mvc_Razor_InputTestTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.InputTestTagHelper>();
#line 5 "testfiles/input/modelexpressiontaghelper.cshtml"
__Microsoft_AspNetCore_Mvc_Razor_InputTestTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.Now);

#line default
#line hidden
            __Microsoft_AspNetCore_Mvc_Razor_InputTestTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.InputTestTagHelper>();
#line 6 "testfiles/input/modelexpressiontaghelper.cshtml"
__Microsoft_AspNetCore_Mvc_Razor_InputTestTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => Model);

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
