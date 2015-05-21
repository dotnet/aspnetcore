#pragma checksum "AttributeTargetingTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "edaa271f830648a99892d01bb55f49e328fa621c"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class AttributeTargetingTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = null;
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private PTagHelper __PTagHelper = null;
        private CatchAllTagHelper __CatchAllTagHelper = null;
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
        #line hidden
        public AttributeTargetingTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(30, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", false, "test", async() => {
                WriteLiteral("\r\n    <p>");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("strong", false, "test", async() => {
                    WriteLiteral("Hello");
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
                __tagHelperExecutionContext.Add(__CatchAllTagHelper);
                __tagHelperExecutionContext.AddHtmlAttribute("catchAll", Html.Raw("hi"));
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("<strong>World</strong></p>\r\n    <input checked=\"true\" />\r\n    ");
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
#line 6 "AttributeTargetingTagHelpers.cshtml"
        __InputTagHelper2.Checked = true;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n    ");
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
#line 7 "AttributeTargetingTagHelpers.cshtml"
        __InputTagHelper2.Checked = true;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
                __tagHelperExecutionContext.Add(__CatchAllTagHelper);
                __tagHelperExecutionContext.AddHtmlAttribute("catchAll", Html.Raw("hi"));
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n");
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("btn"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
