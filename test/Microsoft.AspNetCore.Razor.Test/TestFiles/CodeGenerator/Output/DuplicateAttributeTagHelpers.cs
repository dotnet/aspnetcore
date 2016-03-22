#pragma checksum "DuplicateAttributeTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "84ecb9053de09ef4f2a69927fed3e41e188c730b"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class DuplicateAttributeTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private global::Microsoft.AspNetCore.Razor.TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager();
        private global::TestNamespace.PTagHelper __TestNamespace_PTagHelper = null;
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("AGE", new global::Microsoft.AspNetCore.Html.HtmlEncodedString("40"));
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("Age", new global::Microsoft.AspNetCore.Html.HtmlEncodedString("500"));
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("TYPE", new global::Microsoft.AspNetCore.Html.HtmlEncodedString("checkbox"));
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", new global::Microsoft.AspNetCore.Html.HtmlEncodedString("checkbox"));
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("checked", new global::Microsoft.AspNetCore.Html.HtmlEncodedString("false"));
        #line hidden
        public DuplicateAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner();
            Instrumentation.BeginContext(31, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(63, 6, true);
                WriteLiteral("\r\n    ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                __TestNamespace_InputTagHelper.Type = "button";
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __TestNamespace_InputTagHelper.Type);
                __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(69, 39, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(108, 6, true);
                WriteLiteral("\r\n    ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                __TestNamespace_InputTagHelper.Type = "button";
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __TestNamespace_InputTagHelper.Type);
                __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 5 "DuplicateAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = true;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_3);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_4);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(114, 70, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(184, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
#line 3 "DuplicateAttributeTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = 3;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                __tagHelperExecutionContext.Output.Content = await __tagHelperExecutionContext.Output.GetChildContentAsync();
            }
            Instrumentation.BeginContext(33, 157, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
