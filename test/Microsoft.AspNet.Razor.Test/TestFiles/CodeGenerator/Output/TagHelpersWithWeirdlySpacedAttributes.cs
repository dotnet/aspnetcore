#pragma checksum "TagHelpersWithWeirdlySpacedAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "61ec0611b60d88357a6c3b6551513ebf6223f6ee"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class TagHelpersWithWeirdlySpacedAttributes
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
        public TagHelpersWithWeirdlySpacedAttributes()
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
                Instrumentation.BeginContext(105, 11, true);
                WriteLiteral("Body of Tag");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("Hello World"));
#line 6 "TagHelpersWithWeirdlySpacedAttributes.cshtml"
  __PTagHelper.Age = 1337;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            StartTagHelperWritingScope();
#line 7 "TagHelpersWithWeirdlySpacedAttributes.cshtml"
             Write(true);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndTagHelperWritingScope();
            __tagHelperExecutionContext.AddHtmlAttribute("data-content", Html.Raw(__tagHelperStringValueBuffer.GetContent(HtmlEncoder)));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(35, 85, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(120, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            __InputTagHelper.Type = "text";
            __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __tagHelperExecutionContext.AddHtmlAttribute("data-content", Html.Raw("hello"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(124, 47, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(171, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
#line 11 "TagHelpersWithWeirdlySpacedAttributes.cshtml"
__PTagHelper.Age = 1234;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.AddHtmlAttribute("data-content", Html.Raw("hello2"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(175, 46, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(221, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __InputTagHelper = CreateTagHelper<InputTagHelper>();
            __tagHelperExecutionContext.Add(__InputTagHelper);
            __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
            __tagHelperExecutionContext.Add(__InputTagHelper2);
            __InputTagHelper.Type = "password";
            __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
            __InputTagHelper2.Type = __InputTagHelper.Type;
            __tagHelperExecutionContext.AddHtmlAttribute("data-content", Html.Raw("blah"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(225, 51, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
