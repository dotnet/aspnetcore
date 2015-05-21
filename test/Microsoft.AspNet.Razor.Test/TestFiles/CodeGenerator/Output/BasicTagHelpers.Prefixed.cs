#pragma checksum "BasicTagHelpers.Prefixed.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "da1aeb71c15bd7443662536f58df99382b0a47f2"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class BasicTagHelpers.Prefixed
    {
        #line hidden
        #pragma warning disable 0414
        private TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = null;
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private PTagHelper __PTagHelper = null;
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
        #line hidden
        public BasicTagHelpers.Prefixed()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(57, 52, true);
            WriteLiteral("\r\n<THSdiv class=\"randomNonTagHelperAttribute\">\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", false, "test", async() => {
                WriteLiteral("\r\n        <p></p>\r\n        <input type=\"text\" />\r\n        ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper.Type = "checkbox";
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
                __InputTagHelper2.Type = __InputTagHelper.Type;
#line 8 "BasicTagHelpers.Prefixed.cshtml"
               __InputTagHelper2.Checked = true;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n    ");
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("Hello World"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(249, 11, true);
            WriteLiteral("\r\n</THSdiv>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
