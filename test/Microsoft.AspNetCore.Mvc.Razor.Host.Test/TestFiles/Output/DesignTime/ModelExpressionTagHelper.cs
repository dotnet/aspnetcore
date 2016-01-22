namespace Asp
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.Rendering;
    using System.Threading.Tasks;

    public class ASPV_testfiles_input_modelexpressiontaghelper_cshtml : Microsoft.AspNet.Mvc.Razor.RazorPage<DateTime>
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = "Microsoft.AspNet.Mvc.Razor.InputTestTagHelper, Microsoft.AspNet.Mvc.Razor.Host.Test";
#line 1 "testfiles/input/modelexpressiontaghelper.cshtml"
var __modelHelper = default(DateTime);

#line default
#line hidden
            #pragma warning restore 219
        }
        #line hidden
        private global::Microsoft.AspNet.Mvc.Razor.InputTestTagHelper __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = null;
        #line hidden
        public ASPV_testfiles_input_modelexpressiontaghelper_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.IUrlHelper Url { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<DateTime> Html { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = CreateTagHelper<global::Microsoft.AspNet.Mvc.Razor.InputTestTagHelper>();
#line 5 "testfiles/input/modelexpressiontaghelper.cshtml"
__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For = CreateModelExpression(__model => __model.Now);

#line default
#line hidden
            __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = CreateTagHelper<global::Microsoft.AspNet.Mvc.Razor.InputTestTagHelper>();
#line 6 "testfiles/input/modelexpressiontaghelper.cshtml"
__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For = CreateModelExpression(__model => Model);

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
