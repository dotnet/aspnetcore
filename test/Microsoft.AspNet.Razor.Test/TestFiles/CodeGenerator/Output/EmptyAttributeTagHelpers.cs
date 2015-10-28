#pragma checksum "EmptyAttributeTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "cdf68f4aae781fadf4445e465c23df6d758fcd81"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class EmptyAttributeTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private global::Microsoft.AspNet.Razor.TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = new global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager();
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        private global::TestNamespace.PTagHelper __TestNamespace_PTagHelper = null;
        #line hidden
        public EmptyAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner();
            Instrumentation.BeginContext(27, 13, true);
            WriteLiteral("\r\n<div>\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
            __TestNamespace_InputTagHelper.Type = "";
            __tagHelperExecutionContext.AddTagHelperAttribute("type", __TestNamespace_InputTagHelper.Type);
            __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 4 "EmptyAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = ;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw(""));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(40, 34, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(74, 6, true);
            WriteLiteral("\r\n    ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(90, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                __TestNamespace_InputTagHelper.Type = "";
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __TestNamespace_InputTagHelper.Type);
                __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 6 "EmptyAttributeTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = ;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked);
                __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw(""));
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(100, 34, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(134, 6, true);
                WriteLiteral("\r\n    ");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
#line 5 "EmptyAttributeTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = ;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(80, 64, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(144, 8, true);
            WriteLiteral("\r\n</div>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
