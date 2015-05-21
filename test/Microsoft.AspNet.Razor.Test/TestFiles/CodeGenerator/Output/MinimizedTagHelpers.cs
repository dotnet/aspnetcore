#pragma checksum "MinimizedTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "07839be4304797e30b19b50b95e2247c93cdff06"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class MinimizedTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = null;
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private CatchAllTagHelper __CatchAllTagHelper = null;
        private InputTagHelper __InputTagHelper = null;
        #line hidden
        public MinimizedTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", false, "test", async() => {
                WriteLiteral("\r\n    <input nottaghelper />\r\n    ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
                __tagHelperExecutionContext.Add(__CatchAllTagHelper);
                __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("btn"));
                __tagHelperExecutionContext.AddMinimizedHtmlAttribute("catchall-unbound-required");
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n    ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper.BoundRequiredString = "hello";
                __tagHelperExecutionContext.AddTagHelperAttribute("input-bound-required-string", __InputTagHelper.BoundRequiredString);
                __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
                __tagHelperExecutionContext.Add(__CatchAllTagHelper);
                __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("btn"));
                __tagHelperExecutionContext.AddMinimizedHtmlAttribute("catchall-unbound-required");
                __tagHelperExecutionContext.AddMinimizedHtmlAttribute("input-unbound-required");
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n    ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper.BoundRequiredString = "hello2";
                __tagHelperExecutionContext.AddTagHelperAttribute("input-bound-required-string", __InputTagHelper.BoundRequiredString);
                __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
                __tagHelperExecutionContext.Add(__CatchAllTagHelper);
                __CatchAllTagHelper.BoundRequiredString = "world";
                __tagHelperExecutionContext.AddTagHelperAttribute("catchall-bound-string", __CatchAllTagHelper.BoundRequiredString);
                __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("btn"));
                __tagHelperExecutionContext.AddMinimizedHtmlAttribute("catchall-unbound-required");
                __tagHelperExecutionContext.AddMinimizedHtmlAttribute("input-unbound-required");
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n    ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper.BoundRequiredString = "world";
                __tagHelperExecutionContext.AddTagHelperAttribute("input-bound-required-string", __InputTagHelper.BoundRequiredString);
                __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
                __tagHelperExecutionContext.Add(__CatchAllTagHelper);
                __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("btn"));
                __tagHelperExecutionContext.AddHtmlAttribute("catchall-unbound-required", Html.Raw("hello"));
                __tagHelperExecutionContext.AddHtmlAttribute("input-unbound-required", Html.Raw("hello2"));
                __tagHelperExecutionContext.AddMinimizedHtmlAttribute("catchall-unbound-required");
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n");
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __CatchAllTagHelper = CreateTagHelper<CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__CatchAllTagHelper);
            __tagHelperExecutionContext.AddMinimizedHtmlAttribute("catchall-unbound-required");
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
