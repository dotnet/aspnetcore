#pragma checksum "TagHelpersInSection.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "f228c34bdcee3e7f64bc3de14469d11a61efb825"
namespace TestOutput
{
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
                    WriteLiteral("\r\n            In None ContentBehavior.\r\n            ");
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("nestedtaghelper", false, "test", async() => {
                        WriteLiteral("Some buffered values with ");
#line 11 "TagHelpersInSection.cshtml"
                                 Write(code);

#line default
#line hidden
                    }
                    , StartTagHelperWritingScope, EndTagHelperWritingScope);
                    __NestedTagHelper = CreateTagHelper<NestedTagHelper>();
                    __tagHelperExecutionContext.Add(__NestedTagHelper);
                    __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
                    WriteLiteral(__tagHelperExecutionContext.Output.GenerateStartTag());
                    Write(__tagHelperExecutionContext.Output.GeneratePreContent());
                    if (__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        Write(__tagHelperExecutionContext.Output.GenerateContent());
                    }
                    else if (__tagHelperExecutionContext.ChildContentRetrieved)
                    {
                        Write(__tagHelperExecutionContext.GetChildContentAsync().Result);
                    }
                    else
                    {
                        __tagHelperExecutionContext.ExecuteChildContentAsync().Wait();
                    }
                    Write(__tagHelperExecutionContext.Output.GeneratePostContent());
                    WriteLiteral(__tagHelperExecutionContext.Output.GenerateEndTag());
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    WriteLiteral("\r\n        ");
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __MyTagHelper = CreateTagHelper<MyTagHelper>();
                __tagHelperExecutionContext.Add(__MyTagHelper);
                StartTagHelperWritingScope();
                WriteLiteral("Current Time: ");
#line 9 "TagHelpersInSection.cshtml"
Write(DateTime.Now);

#line default
#line hidden
                __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                __MyTagHelper.BoundProperty = __tagHelperStringValueBuffer.ToString();
                __tagHelperExecutionContext.AddTagHelperAttribute("BoundProperty", __MyTagHelper.BoundProperty);
                StartTagHelperWritingScope();
                WriteLiteral("Current Time: ");
#line 9 "TagHelpersInSection.cshtml"
Write(DateTime.Now);

#line default
#line hidden
                __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                __tagHelperExecutionContext.AddHtmlAttribute("unboundproperty", __tagHelperStringValueBuffer.ToString());
                __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
                WriteLiteralTo(__razor_template_writer, __tagHelperExecutionContext.Output.GenerateStartTag());
                WriteTo(__razor_template_writer, __tagHelperExecutionContext.Output.GeneratePreContent());
                if (__tagHelperExecutionContext.Output.IsContentModified)
                {
                    WriteTo(__razor_template_writer, __tagHelperExecutionContext.Output.GenerateContent());
                }
                else if (__tagHelperExecutionContext.ChildContentRetrieved)
                {
                    WriteTo(__razor_template_writer, __tagHelperExecutionContext.GetChildContentAsync().Result);
                }
                else
                {
                    StartTagHelperWritingScope(__razor_template_writer);
                    __tagHelperExecutionContext.ExecuteChildContentAsync().Wait();
                    EndTagHelperWritingScope();
                }
                WriteTo(__razor_template_writer, __tagHelperExecutionContext.Output.GeneratePostContent());
                WriteLiteralTo(__razor_template_writer, __tagHelperExecutionContext.Output.GenerateEndTag());
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
