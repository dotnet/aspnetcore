#pragma checksum "SingleTagHelperWithNewlineBeforeAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "95e2e2bab663b8be9934a66150af50a2a6ee08e7"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class SingleTagHelperWithNewlineBeforeAttributes
    {
        #line hidden
        #pragma warning disable 0414
        private TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = null;
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private PTagHelper __PTagHelper = null;
        #line hidden
        public SingleTagHelperWithNewlineBeforeAttributes()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(73, 11, true);
                WriteLiteral("Body of Tag");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("Hello World"));
#line 4 "SingleTagHelperWithNewlineBeforeAttributes.cshtml"
         __PTagHelper.Age = 1337;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(35, 53, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
