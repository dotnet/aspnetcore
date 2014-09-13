#pragma checksum "TagHelpersInSection.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "bfe8d61279682f87dc473aa71134e86af554f55e"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class TagHelpersInSection
    {
        #line hidden
        private System.IO.TextWriter __tagHelperStringValueBuffer = null;
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = new TagHelperRunner();
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
#line 1 "TagHelpersInSection.cshtml"
  
    var code = "some code";

#line default
#line hidden

            Instrumentation.BeginContext(34, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            DefineSection("MySection", new Template((__razor_template_writer) => {
                Instrumentation.BeginContext(58, 21, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    <div>\r\n        ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("mytaghelper");
                __MyTagHelper = CreateTagHelper<MyTagHelper>();
                __tagHelperExecutionContext.Add(__MyTagHelper);
                StartWritingScope();
                WriteLiteral("Current Time: ");
#line 7 "TagHelpersInSection.cshtml"
Write(DateTime.Now);

#line default
#line hidden
                __tagHelperStringValueBuffer = EndWritingScope();
                __MyTagHelper.BoundProperty = __tagHelperStringValueBuffer.ToString();
                __tagHelperExecutionContext.AddTagHelperAttribute("BoundProperty", __MyTagHelper.BoundProperty);
                StartWritingScope();
                WriteLiteral("Current Time: ");
#line 7 "TagHelpersInSection.cshtml"
Write(DateTime.Now);

#line default
#line hidden
                __tagHelperStringValueBuffer = EndWritingScope();
                __tagHelperExecutionContext.AddHtmlAttribute("unboundproperty", __tagHelperStringValueBuffer.ToString());
                __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
                WriteLiteralTo(__razor_template_writer, __tagHelperExecutionContext.Output.GenerateStartTag());
                Instrumentation.BeginContext(182, 52, true);
                WriteLiteralTo(__razor_template_writer, "\r\n            In None ContentBehavior.\r\n            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("nestedtaghelper");
                __NestedTagHelper = CreateTagHelper<NestedTagHelper>();
                __tagHelperExecutionContext.Add(__NestedTagHelper);
                StartWritingScope();
                WriteLiteral("Some buffered values with ");
#line 9 "TagHelpersInSection.cshtml"
                                 Write(code);

#line default
#line hidden
                __tagHelperStringValueBuffer = EndWritingScope();
                __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext, __tagHelperStringValueBuffer).Result;
                WriteLiteralTo(__razor_template_writer, __tagHelperExecutionContext.Output.GenerateStartTag());
                WriteLiteralTo(__razor_template_writer, __tagHelperExecutionContext.Output.GenerateContent());
                WriteLiteralTo(__razor_template_writer, __tagHelperExecutionContext.Output.GenerateEndTag());
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(300, 10, true);
                WriteLiteralTo(__razor_template_writer, "\r\n        ");
                Instrumentation.EndContext();
                WriteLiteralTo(__razor_template_writer, __tagHelperExecutionContext.Output.GenerateEndTag());
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(324, 14, true);
                WriteLiteralTo(__razor_template_writer, "\r\n    </div>\r\n");
                Instrumentation.EndContext();
            }
            ));
        }
        #pragma warning restore 1998
    }
}
