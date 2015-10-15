#pragma checksum "NestedScriptTagTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "9e6bc8d09df124eda650118b208b7c5e6e058f6b"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class NestedScriptTagTagHelpers
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
        public NestedScriptTagTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(33, 106, true);
            WriteLiteral("\r\n<script type=\"text/html\">\r\n    <div data-animation=\"fade\" class=\"randomNonTagHe" +
"lperAttribute\">\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(180, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
#line 6 "NestedScriptTagTagHelpers.cshtml"
            

#line default
#line hidden

#line 6 "NestedScriptTagTagHelpers.cshtml"
             for(var i = 0; i < 5; i++) {

#line default
#line hidden

                Instrumentation.BeginContext(225, 84, true);
                WriteLiteral("                <script id=\"nestedScriptTag\" type=\"text/html\">\r\n                 " +
"   ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.StartTagOnly, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
                StartTagHelperWritingScope();
                WriteLiteral("2000 + ");
#line 8 "NestedScriptTagTagHelpers.cshtml"
                                            Write(ViewBag.DefaultInterval);

#line default
#line hidden
                WriteLiteral(" + 1");
                __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                __tagHelperExecutionContext.AddHtmlAttribute("data-interval", Html.Raw(__tagHelperStringValueBuffer.GetContent(HtmlEncoder)));
                __InputTagHelper.Type = "text";
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
                __InputTagHelper2.Type = __InputTagHelper.Type;
#line 8 "NestedScriptTagTagHelpers.cshtml"
                                                                        __InputTagHelper2.Checked = true;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(309, 86, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(395, 29, true);
                WriteLiteral("\r\n                </script>\r\n");
                Instrumentation.EndContext();
#line 10 "NestedScriptTagTagHelpers.cshtml"
            }

#line default
#line hidden

                Instrumentation.BeginContext(439, 129, true);
                WriteLiteral("            <script type=\"text/javascript\">\r\n                var tag = \'<input ch" +
"ecked=\"true\">\';\r\n            </script>\r\n        ");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("Hello World"));
            __tagHelperExecutionContext.AddHtmlAttribute("data-delay", Html.Raw("1000"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(139, 433, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(572, 23, true);
            WriteLiteral("\r\n    </div>\r\n</script>");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
