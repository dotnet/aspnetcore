#pragma checksum "TagHelpersInSection.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "f23e7b19a8af7b8fe9c6cb784e5a4bc60537c66b"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class TagHelpersInSection
    {
        #line hidden
        #pragma warning disable 0414
        private global::Microsoft.AspNet.Razor.TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = new global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager();
        private global::TestNamespace.MyTagHelper __TestNamespace_MyTagHelper = null;
        private global::TestNamespace.NestedTagHelper __TestNamespace_NestedTagHelper = null;
        #line hidden
        public TagHelpersInSection()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner();
            Instrumentation.BeginContext(31, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 3 "TagHelpersInSection.cshtml"
  
    var code = "some code";

#line default
#line hidden

            Instrumentation.BeginContext(69, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            DefineSection("MySection", async(__razor_template_writer) => {
                Instrumentation.BeginContext(91, 21, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    <div>\r\n        ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("mytaghelper", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                    Instrumentation.BeginContext(215, 52, true);
                    WriteLiteral("\r\n            In None ContentBehavior.\r\n            ");
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("nestedtaghelper", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                        Instrumentation.BeginContext(284, 26, true);
                        WriteLiteral("Some buffered values with ");
                        Instrumentation.EndContext();
                        Instrumentation.BeginContext(311, 4, false);
#line 11 "TagHelpersInSection.cshtml"
                                                  Write(code);

#line default
#line hidden
                        Instrumentation.EndContext();
                    }
                    , StartTagHelperWritingScope, EndTagHelperWritingScope);
                    __TestNamespace_NestedTagHelper = CreateTagHelper<global::TestNamespace.NestedTagHelper>();
                    __tagHelperExecutionContext.Add(__TestNamespace_NestedTagHelper);
                    __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        __tagHelperExecutionContext.Output.Content = await __tagHelperExecutionContext.Output.GetChildContentAsync();
                    }
                    Instrumentation.BeginContext(267, 66, false);
                    Write(__tagHelperExecutionContext.Output);
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    Instrumentation.BeginContext(333, 10, true);
                    WriteLiteral("\r\n        ");
                    Instrumentation.EndContext();
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __TestNamespace_MyTagHelper = CreateTagHelper<global::TestNamespace.MyTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_MyTagHelper);
                StartTagHelperWritingScope(null);
                WriteLiteral("Current Time: ");
#line 9 "TagHelpersInSection.cshtml"
                                      WriteLiteral(DateTime.Now);

#line default
#line hidden
                __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                __TestNamespace_MyTagHelper.BoundProperty = __tagHelperStringValueBuffer.GetContent(HtmlEncoder);
                __tagHelperExecutionContext.AddTagHelperAttribute("boundproperty", __TestNamespace_MyTagHelper.BoundProperty);
                BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unboundproperty", 3);
                AddHtmlAttributeValue("", 186, "Current", 186, 7, true);
                AddHtmlAttributeValue(" ", 193, "Time:", 194, 6, true);
#line 9 "TagHelpersInSection.cshtml"
AddHtmlAttributeValue(" ", 199, DateTime.Now, 200, 14, false);

#line default
#line hidden
                EndAddHtmlAttributeValues(__tagHelperExecutionContext);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    __tagHelperExecutionContext.Output.Content = await __tagHelperExecutionContext.Output.GetChildContentAsync();
                }
                Instrumentation.BeginContext(112, 245, false);
                WriteTo(__razor_template_writer, __tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(357, 14, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    </div>\r\n");
                Instrumentation.EndContext();
            }
            );
        }
        #pragma warning restore 1998
    }
}
