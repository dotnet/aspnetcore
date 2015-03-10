#pragma checksum "TagHelpersInHelper.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "5f28fe84901bdeb20db8296b1da1e9a1f1da1023"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class TagHelpersInHelper
    {
public static Template 
#line 3 "TagHelpersInHelper.cshtml"
MyHelper(string val)
{

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 4 "TagHelpersInHelper.cshtml"
 

#line default
#line hidden

            Instrumentation.BeginContext(68, 19, true);
            WriteLiteralTo(__razor_helper_writer, "    <div>\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("mytaghelper", false, "test", async() => {
                WriteLiteral("\r\n            In None ContentBehavior.\r\n            ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("nestedtaghelper", false, "test", async() => {
                    WriteLiteral("Some buffered values with a value of ");
#line 8 "TagHelpersInHelper.cshtml"
                                            Write(val);

#line default
#line hidden
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __NestedTagHelper = CreateTagHelper<NestedTagHelper>();
                __tagHelperExecutionContext.Add(__NestedTagHelper);
                __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
                WriteTagHelperAsync(__tagHelperExecutionContext).Wait();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n        ");
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __MyTagHelper = CreateTagHelper<MyTagHelper>();
            __tagHelperExecutionContext.Add(__MyTagHelper);
            StartTagHelperWritingScope();
            WriteLiteral("Current Time: ");
#line 6 "TagHelpersInHelper.cshtml"
Write(DateTime.Now);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndTagHelperWritingScope();
            __MyTagHelper.BoundProperty = __tagHelperStringValueBuffer.ToString();
            __tagHelperExecutionContext.AddTagHelperAttribute("BoundProperty", __MyTagHelper.BoundProperty);
            StartTagHelperWritingScope();
            WriteLiteral("Current Time: ");
#line 6 "TagHelpersInHelper.cshtml"
Write(DateTime.Now);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndTagHelperWritingScope();
            __tagHelperExecutionContext.AddHtmlAttribute("unboundproperty", __tagHelperStringValueBuffer.ToString());
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteTagHelperToAsync(__razor_helper_writer, __tagHelperExecutionContext).Wait();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(342, 14, true);
            WriteLiteralTo(__razor_helper_writer, "\r\n    </div>\r\n");
            Instrumentation.EndContext();
#line 11 "TagHelpersInHelper.cshtml"

#line default
#line hidden

        }
        );
#line 11 "TagHelpersInHelper.cshtml"
}

#line default
#line hidden

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
        public TagHelpersInHelper()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("mytaghelper", false, "test", async() => {
#line 12 "TagHelpersInHelper.cshtml"
Write(MyHelper(item => new Template((__razor_template_writer) => {
    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("nestedtaghelper", false, "test", async() => {
        WriteLiteral("Custom Value");
    }
    , StartTagHelperWritingScope, EndTagHelperWritingScope);
    __NestedTagHelper = CreateTagHelper<NestedTagHelper>();
    __tagHelperExecutionContext.Add(__NestedTagHelper);
    __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
    WriteTagHelperToAsync(__razor_template_writer, __tagHelperExecutionContext).Wait();
    __tagHelperExecutionContext = __tagHelperScopeManager.End();
}
)
));

#line default
#line hidden
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __MyTagHelper = CreateTagHelper<MyTagHelper>();
            __tagHelperExecutionContext.Add(__MyTagHelper);
            __tagHelperExecutionContext.Output = __tagHelperRunner.RunAsync(__tagHelperExecutionContext).Result;
            WriteTagHelperAsync(__tagHelperExecutionContext).Wait();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(445, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
