#pragma checksum "BasicTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "ef59308f8b0456ad2f49a08604e8ba36ca11a4b2"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System.Threading.Tasks;

    public class BasicTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private System.IO.TextWriter __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = new TagHelperRunner();
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private PTagHelper __PTagHelper = null;
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
        #line hidden
        public BasicTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(33, 49, true);
            WriteLiteral("\r\n<div class=\"randomNonTagHelperAttribute\">\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", false, "test", async() => {
                WriteLiteral("\r\n        ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", false, "test", async() => {
                }
                , StartWritingScope, EndWritingScope);
                __PTagHelper = CreateTagHelper<PTagHelper>();
                __tagHelperExecutionContext.Add(__PTagHelper);
                __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
                WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
                WriteLiteral(__tagHelperExecutionContext.Output.GeneratePreContent());
                if (__tagHelperExecutionContext.Output.ContentSet)
                {
                    WriteLiteral(__tagHelperExecutionContext.Output.GenerateContent());
                }
                else if (__tagHelperExecutionContext.ChildContentRetrieved)
                {
                    WriteLiteral(__tagHelperExecutionContext.GetChildContentAsync().Result);
                }
                else
                {
                    __tagHelperExecutionContext.ExecuteChildContentAsync().Wait();
                }
                WriteLiteral(__tagHelperExecutionContext.Output.GeneratePostContent());
                WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n        ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
                }
                , StartWritingScope, EndWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper.Type = **From custom attribute code renderer**: "text";
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
                __InputTagHelper2.Type = __InputTagHelper.Type;
                __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
                WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
                WriteLiteral(__tagHelperExecutionContext.Output.GeneratePreContent());
                if (__tagHelperExecutionContext.Output.ContentSet)
                {
                    WriteLiteral(__tagHelperExecutionContext.Output.GenerateContent());
                }
                else if (__tagHelperExecutionContext.ChildContentRetrieved)
                {
                    WriteLiteral(__tagHelperExecutionContext.GetChildContentAsync().Result);
                }
                else
                {
                    __tagHelperExecutionContext.ExecuteChildContentAsync().Wait();
                }
                WriteLiteral(__tagHelperExecutionContext.Output.GeneratePostContent());
                WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n        ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
                }
                , StartWritingScope, EndWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper.Type = **From custom attribute code renderer**: "checkbox";
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
                __InputTagHelper2.Type = __InputTagHelper.Type;
#line 7 "BasicTagHelpers.cshtml"
            __InputTagHelper2.Checked = **From custom attribute code renderer**: true;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
                WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
                WriteLiteral(__tagHelperExecutionContext.Output.GeneratePreContent());
                if (__tagHelperExecutionContext.Output.ContentSet)
                {
                    WriteLiteral(__tagHelperExecutionContext.Output.GenerateContent());
                }
                else if (__tagHelperExecutionContext.ChildContentRetrieved)
                {
                    WriteLiteral(__tagHelperExecutionContext.GetChildContentAsync().Result);
                }
                else
                {
                    __tagHelperExecutionContext.ExecuteChildContentAsync().Wait();
                }
                WriteLiteral(__tagHelperExecutionContext.Output.GeneratePostContent());
                WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n    ");
            }
            , StartWritingScope, EndWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", "Hello World");
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            WriteLiteral(__tagHelperExecutionContext.Output.GeneratePreContent());
            if (__tagHelperExecutionContext.Output.ContentSet)
            {
                WriteLiteral(__tagHelperExecutionContext.Output.GenerateContent());
            }
            else if (__tagHelperExecutionContext.ChildContentRetrieved)
            {
                WriteLiteral(__tagHelperExecutionContext.GetChildContentAsync().Result);
            }
            else
            {
                __tagHelperExecutionContext.ExecuteChildContentAsync().Wait();
            }
            WriteLiteral(__tagHelperExecutionContext.Output.GeneratePostContent());
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(212, 8, true);
            WriteLiteral("\r\n</div>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
