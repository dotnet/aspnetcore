#pragma checksum "TagHelpersInSection.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "b6492c68360b85d4993de94eeac547b7fb012a26"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class TagHelpersInSection
    {
        #line hidden
        #pragma warning disable 0414
        private TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = null;
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private MyTagHelper __MyTagHelper = null;
        private NestedTagHelper __NestedTagHelper = null;
        #line hidden
        public TagHelpersInSection()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 3 "TagHelpersInSection.cshtml"
  
    var code = "some code";

#line default
#line hidden

            Instrumentation.BeginContext(69, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            DefineSection("MySection", async(__razor_template_writer) => {
                Instrumentation.BeginContext(93, 21, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    <div>\r\n        ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("mytaghelper", false, "test", async() => {
                    Instrumentation.BeginContext(217, 52, true);
                    WriteLiteral("\r\n            In None ContentBehavior.\r\n            ");
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("nestedtaghelper", false, "test", async() => {
                        Instrumentation.BeginContext(286, 26, true);
                        WriteLiteral("Some buffered values with ");
                        Instrumentation.EndContext();
                        Instrumentation.BeginContext(313, 4, false);
#line 11 "TagHelpersInSection.cshtml"
                                 Write(code);

#line default
#line hidden
                        Instrumentation.EndContext();
                    }
                    , StartTagHelperWritingScope, EndTagHelperWritingScope);
                    __NestedTagHelper = CreateTagHelper<NestedTagHelper>();
                    __tagHelperExecutionContext.Add(__NestedTagHelper);
                    __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    Instrumentation.BeginContext(269, 66, false);
                    await WriteTagHelperAsync(__tagHelperExecutionContext);
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    Instrumentation.BeginContext(335, 10, true);
                    WriteLiteral("\r\n        ");
                    Instrumentation.EndContext();
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __MyTagHelper = CreateTagHelper<MyTagHelper>();
                __tagHelperExecutionContext.Add(__MyTagHelper);
                StartTagHelperWritingScope();
                WriteLiteral("Current Time: ");
#line 9 "TagHelpersInSection.cshtml"
WriteLiteral(DateTime.Now);

#line default
#line hidden
                __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                __MyTagHelper.BoundProperty = __tagHelperStringValueBuffer.ToString();
                __tagHelperExecutionContext.AddTagHelperAttribute("boundproperty", __MyTagHelper.BoundProperty);
                StartTagHelperWritingScope();
                WriteLiteral("Current Time: ");
#line 9 "TagHelpersInSection.cshtml"
Write(DateTime.Now);

#line default
#line hidden
                __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                __tagHelperExecutionContext.AddHtmlAttribute("unboundproperty", Html.Raw(__tagHelperStringValueBuffer.ToString()));
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(114, 245, false);
                await WriteTagHelperToAsync(__razor_template_writer, __tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(359, 14, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    </div>\r\n");
                Instrumentation.EndContext();
            }
            );
        }
        #pragma warning restore 1998
    }
}
