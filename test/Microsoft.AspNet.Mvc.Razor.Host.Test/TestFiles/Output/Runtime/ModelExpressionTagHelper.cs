#pragma checksum "TestFiles/Input/ModelExpressionTagHelper.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "0ff475dc725132bad90d86e9458bacc73dce3df3"
namespace Asp
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.Rendering;
    using System.Threading.Tasks;

    public class ASPV_TestFiles_Input_ModelExpressionTagHelper_cshtml : Microsoft.AspNet.Mvc.Razor.RazorPage<DateTime>
    {
        #line hidden
        #pragma warning disable 0414
        private global::Microsoft.AspNet.Razor.TagHelpers.TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager = new global::Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperScopeManager();
        private global::Microsoft.AspNet.Mvc.Razor.InputTestTagHelper __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = null;
        #line hidden
        public ASPV_TestFiles_Input_ModelExpressionTagHelper_cshtml()
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
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelperRunner();
            BeginContext(118, 2, true);
            WriteLiteral("\r\n");
            EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input-test", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = CreateTagHelper<global::Microsoft.AspNet.Mvc.Razor.InputTestTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper);
#line 5 "TestFiles/Input/ModelExpressionTagHelper.cshtml"
__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For = CreateModelExpression(__model => __model.Now);

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("for", __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            BeginContext(120, 24, false);
            Write(__tagHelperExecutionContext.Output);
            EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            BeginContext(144, 2, true);
            WriteLiteral("\r\n");
            EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input-test", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper = CreateTagHelper<global::Microsoft.AspNet.Mvc.Razor.InputTestTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper);
#line 6 "TestFiles/Input/ModelExpressionTagHelper.cshtml"
__Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For = CreateModelExpression(__model => Model);

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("for", __Microsoft_AspNet_Mvc_Razor_InputTestTagHelper.For);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            BeginContext(146, 27, false);
            Write(__tagHelperExecutionContext.Output);
            EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
