#pragma checksum "BasicTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "02f53fe4a386cdc0885235e5fd3f5d6d317dd4d6"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class BasicTagHelpers
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
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("data-delay", new global::Microsoft.AspNetCore.Html.HtmlString("1000"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("data", new global::Microsoft.AspNetCore.Html.HtmlString("-delay1000"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", "text", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", "checkbox", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        public BasicTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner();
            __tagHelperScopeManager = __tagHelperScopeManager ?? new global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
            Instrumentation.BeginContext(33, 71, true);
            WriteLiteral("\r\n<div data-animation=\"fade\" class=\"randomNonTagHelperAttribute\">\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(145, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                }
                );
                __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(155, 25, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(180, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagOnly, "test", async() => {
                }
                );
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                BeginWriteTagHelperAttribute();
                WriteLiteral("2000 + ");
#line 6 "BasicTagHelpers.cshtml"
                                Write(ViewBag.DefaultInterval);

#line default
#line hidden
                WriteLiteral(" + 1");
                __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
                __tagHelperExecutionContext.AddHtmlAttribute("data-interval", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                __TestNamespace_InputTagHelper.Type = (string)__tagHelperAttribute_3.Value;
                __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_3);
                __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(190, 71, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(261, 10, true);
                WriteLiteral("\r\n        ");
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
#line 7 "BasicTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = true;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(271, 39, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(310, 6, true);
                WriteLiteral("\r\n    ");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(104, 216, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(320, 8, true);
            WriteLiteral("\r\n</div>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
