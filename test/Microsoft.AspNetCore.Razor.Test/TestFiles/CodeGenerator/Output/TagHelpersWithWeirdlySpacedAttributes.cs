#pragma checksum "TagHelpersWithWeirdlySpacedAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "44db1fa170a6845ec719f4200e4bb1f639830b49"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class TagHelpersWithWeirdlySpacedAttributes
    {
        #line hidden
        #pragma warning disable 0414
        private string __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = null;
        private global::TestNamespace.PTagHelper __TestNamespace_PTagHelper = null;
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("Hello World"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", "text", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.SingleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("data-content", new global::Microsoft.AspNetCore.Html.HtmlString("hello"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("data-content", new global::Microsoft.AspNetCore.Html.HtmlString("hello2"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.SingleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", "password", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_5 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("data-content", new global::Microsoft.AspNetCore.Html.HtmlString("blah"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        public TagHelpersWithWeirdlySpacedAttributes()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner();
            __tagHelperScopeManager = __tagHelperScopeManager ?? new global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
            Instrumentation.BeginContext(31, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(103, 11, true);
                WriteLiteral("Body of Tag");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
#line 6 "TagHelpersWithWeirdlySpacedAttributes.cshtml"
__TestNamespace_PTagHelper.Age = 1337;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            BeginWriteTagHelperAttribute();
#line 7 "TagHelpersWithWeirdlySpacedAttributes.cshtml"
             Write(true);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __tagHelperExecutionContext.AddHtmlAttribute("data-content", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(33, 85, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(118, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
            __TestNamespace_InputTagHelper.Type = (string)__tagHelperAttribute_1.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_1);
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(122, 47, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(169, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
            }
            );
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
#line 11 "TagHelpersWithWeirdlySpacedAttributes.cshtml"
__TestNamespace_PTagHelper.Age = 1234;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_3);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(173, 46, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(219, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
            __TestNamespace_InputTagHelper.Type = (string)__tagHelperAttribute_4.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_4);
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_5);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(223, 51, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
