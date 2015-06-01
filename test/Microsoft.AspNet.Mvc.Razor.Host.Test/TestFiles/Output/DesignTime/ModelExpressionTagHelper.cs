namespace Asp
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.Rendering;
    using System.Threading.Tasks;

    public class ASPV_TestFiles_Input_ModelExpressionTagHelper_cshtml : Microsoft.AspNet.Mvc.Razor.RazorPage<
#line 1 "TestFiles/Input/ModelExpressionTagHelper.cshtml"
       DateTime

#line default
#line hidden
    >
    {
        private static object @__o;
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            string __tagHelperDirectiveSyntaxHelper = null;
            __tagHelperDirectiveSyntaxHelper = 
#line 3 "TestFiles/Input/ModelExpressionTagHelper.cshtml"
              "Microsoft.AspNet.Mvc.Razor.InputTestTagHelper, Microsoft.AspNet.Mvc.Razor.Host.Test"

#line default
#line hidden
            ;
            #pragma warning restore 219
        }
        #line hidden
        private Microsoft.AspNet.Mvc.Razor.InputTestTagHelper __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = null;
        #line hidden
        public ASPV_TestFiles_Input_ModelExpressionTagHelper_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<DateTime> Html { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute]
        public Microsoft.AspNet.Mvc.IUrlHelper Url { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = CreateTagHelper<Microsoft.AspNet.Mvc.Razor.InputTestTagHelper>();
#line 5 "TestFiles/Input/ModelExpressionTagHelper.cshtml"
__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For = CreateModelExpression(__model => __model.Now);

#line default
#line hidden
            __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = CreateTagHelper<Microsoft.AspNet.Mvc.Razor.InputTestTagHelper>();
#line 6 "TestFiles/Input/ModelExpressionTagHelper.cshtml"
__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For = CreateModelExpression(__model => Model);

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
