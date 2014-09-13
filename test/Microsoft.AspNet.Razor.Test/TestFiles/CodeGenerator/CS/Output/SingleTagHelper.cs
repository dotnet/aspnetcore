#pragma checksum "SingleTagHelper.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "2c9b5f2ce383fe784f68f84cbb669ab04077c417"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class SingleTagHelper
    {
        #line hidden
        private System.IO.TextWriter __tagHelperStringValueBuffer = null;
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
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p");
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __PTagHelper.Foo = 1337;
            __tagHelperExecutionContext.AddTagHelperAttribute("foo", __PTagHelper.Foo);
            __tagHelperExecutionContext.AddHtmlAttribute("class", "Hello World");
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
            Instrumentation.BeginContext(34, 11, true);
            WriteLiteral("Body of Tag");
            Instrumentation.EndContext();
            WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
