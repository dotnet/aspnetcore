#pragma checksum "TestFiles/Input/ModelExpressionTagHelper.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "11088b573392b1db9fe4cac4fff3a9ce68a72c40"
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
        #line hidden
        private System.IO.TextWriter __tagHelperStringValueBuffer = null;
        private Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperRunner();
        private Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager = new Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperScopeManager();
        private Microsoft.AspNet.Mvc.Razor.InputTestTagHelper __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = null;
        #line hidden
        public ASPV_TestFiles_Input_ModelExpressionTagHelper_cshtml()
        {
        }
        #line hidden
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<DateTime> Html { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.IViewComponentHelper Component { get; private set; }
        [Microsoft.AspNet.Mvc.ActivateAttribute]
        public Microsoft.AspNet.Mvc.IUrlHelper Url { get; private set; }

        #line hidden

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("inputTest");
            __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = CreateTagHelper<Microsoft.AspNet.Mvc.Razor.InputTestTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper);
            __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For = CreateModelExpression(__model => __model.Now);
            __tagHelperExecutionContext.AddTagHelperAttribute("For", __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For);
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
