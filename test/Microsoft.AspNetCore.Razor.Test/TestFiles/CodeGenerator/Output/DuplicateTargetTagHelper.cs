#pragma checksum "DuplicateTargetTagHelper.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "1014725b9048d825ce97b0e5e260ac35f057fe0a"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class DuplicateTargetTagHelper
    {
        #line hidden
        #pragma warning disable 0414
        private string __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = null;
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.CatchAllTagHelper __TestNamespace_CatchAllTagHelper = null;
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", "checkbox", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        public DuplicateTargetTagHelper()
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
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            __TestNamespace_InputTagHelper.Type = (string)__tagHelperAttribute_0.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
            __TestNamespace_CatchAllTagHelper.Type = __TestNamespace_InputTagHelper.Type;
#line 3 "DuplicateTargetTagHelper.cshtml"
__TestNamespace_InputTagHelper.Checked = true;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper.Checked, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            __TestNamespace_CatchAllTagHelper.Checked = __TestNamespace_InputTagHelper.Checked;
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(33, 40, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
