#pragma checksum "SingleTagHelper.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "60ad52f4a16ebc0499d729dfd4b79358331907f4"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class SingleTagHelper
    {
        #line hidden
        #pragma warning disable 0414
        private System.IO.TextWriter __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = new TagHelperRunner();
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private PTagHelper __PTagHelper = null;
        #line hidden
        public SingleTagHelper()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", false, "test", async() => {
                WriteLiteral("Body of Tag");
            }
            , StartWritingScope, EndWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
#line 3 "SingleTagHelper.cshtml"
         __PTagHelper.Age = 1337;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
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
        }
        #pragma warning restore 1998
    }
}
