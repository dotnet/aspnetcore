#pragma checksum "EscapedTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "34a293044db8d18ae2c99fc1e4231e9e7ee96137"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class EscapedTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private System.IO.TextWriter __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = new TagHelperRunner();
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private InputTagHelper __InputTagHelper = null;
        private InputTagHelper2 __InputTagHelper2 = null;
        #line hidden
        public EscapedTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(27, 72, true);
            WriteLiteral("\r\n<div class=\"randomNonTagHelperAttribute\">\r\n    <p class=\"Hello World\" ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(102, 12, false);
#line 4 "EscapedTagHelpers.cshtml"
                       Write(DateTime.Now);

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(114, 69, true);
            WriteLiteral(">\r\n        <input type=\"text\" />\r\n        <em>Not a TagHelper: </em> ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", true, "test", async() => {
            }
            , StartWritingScope, EndWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            StartWritingScope();
#line 6 "EscapedTagHelpers.cshtml"
Write(DateTime.Now);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndWritingScope();
            __InputTagHelper.Type = __tagHelperStringValueBuffer.ToString();
            __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            __InputTagHelper2.Type = __InputTagHelper.Type;
#line 6 "EscapedTagHelpers.cshtml"
                                              __InputTagHelper2.Checked = true;

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
            Instrumentation.BeginContext(231, 18, true);
            WriteLiteral("\r\n    </p>\r\n</div>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
