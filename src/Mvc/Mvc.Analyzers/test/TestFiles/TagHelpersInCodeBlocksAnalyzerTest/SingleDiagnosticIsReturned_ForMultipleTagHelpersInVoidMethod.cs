namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    public class SingleDiagnosticIsReturned_ForMultipleTagHelpersInVoidMethod : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
  
    SometMethod();

            WriteLiteral("\r\n");
        }
        #pragma warning restore 1998
            
    /*MM*/void SometMethod()
    {

        WriteLiteral("        ");
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin("cache", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "a49ad3383d0802d134e8f9ec401bad4c2f47d14d3250", async() => {
            WriteLiteral("\r\n            <p>The current time is ");
                              Write(DateTime.Now);

            WriteLiteral("</p>\r\n        ");
        }
        );
        __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
        __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
        BeginWriteTagHelperAttribute();
        __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
        __tagHelperExecutionContext.AddHtmlAttribute("asp-vary-by-user", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
        await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
        if (!__tagHelperExecutionContext.Output.IsContentModified)
        {
            await __tagHelperExecutionContext.SetOutputContentAsync();
        }
        Write(__tagHelperExecutionContext.Output);
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
        WriteLiteral("\r\n");
        WriteLiteral("        ");
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin("cache", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "a49ad3383d0802d134e8f9ec401bad4c2f47d14d4821", async() => {
            WriteLiteral("\r\n            <p>The current time is ");
                              Write(DateTime.Now);

            WriteLiteral("</p>\r\n        ");
        }
        );
        __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
        __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
        BeginWriteTagHelperAttribute();
        __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
        __tagHelperExecutionContext.AddHtmlAttribute("asp-vary-by-user", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
        await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
        if (!__tagHelperExecutionContext.Output.IsContentModified)
        {
            await __tagHelperExecutionContext.SetOutputContentAsync();
        }
        Write(__tagHelperExecutionContext.Output);
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
        WriteLiteral("\r\n");
        WriteLiteral("        ");
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin("cache", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "a49ad3383d0802d134e8f9ec401bad4c2f47d14d6392", async() => {
            WriteLiteral("\r\n            <p>The current time is ");
                              Write(DateTime.Now);

            WriteLiteral("</p>\r\n        ");
        }
        );
        __Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.CacheTagHelper>();
        __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_CacheTagHelper);
        BeginWriteTagHelperAttribute();
        __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
        __tagHelperExecutionContext.AddHtmlAttribute("asp-vary-by-user", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
        await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
        if (!__tagHelperExecutionContext.Output.IsContentModified)
        {
            await __tagHelperExecutionContext.SetOutputContentAsync();
        }
        Write(__tagHelperExecutionContext.Output);
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
        WriteLiteral("\r\n");
    }

        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
