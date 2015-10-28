#pragma checksum "EnumTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "57102d182f8d5da659bb113653552ea18f42bb76"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class EnumTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private global::Microsoft.AspNet.Razor.TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = new global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager();
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.CatchAllTagHelper __TestNamespace_CatchAllTagHelper = null;
        #line hidden
        public EnumTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner();
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 3 "EnumTagHelpers.cshtml"
  
    var enumValue = MyEnum.MyValue;

#line default
#line hidden

            Instrumentation.BeginContext(79, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
#line 7 "EnumTagHelpers.cshtml"
__TestNamespace_InputTagHelper.Value = MyEnum.MyValue;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("value", __TestNamespace_InputTagHelper.Value);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(81, 33, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(114, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "class", 1);
#line 8 "EnumTagHelpers.cshtml"
AddHtmlAttributeValue("", 130, MyEnum.MySecondValue, 130, 21, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(116, 39, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(155, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
#line 9 "EnumTagHelpers.cshtml"
__TestNamespace_InputTagHelper.Value = Microsoft.AspNet.Razor.Test.Generator.MyEnum.MyValue;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("value", __TestNamespace_InputTagHelper.Value);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(157, 25, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(182, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
#line 10 "EnumTagHelpers.cshtml"
__TestNamespace_InputTagHelper.Value = Microsoft.AspNet.Razor.Test.Generator.MyEnum.MySecondValue;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("value", __TestNamespace_InputTagHelper.Value);
#line 10 "EnumTagHelpers.cshtml"
__TestNamespace_CatchAllTagHelper.CatchAll = Microsoft.AspNet.Razor.Test.Generator.MyEnum.MyValue;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("catch-all", __TestNamespace_CatchAllTagHelper.CatchAll);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(184, 50, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(234, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            __TestNamespace_CatchAllTagHelper = CreateTagHelper<global::TestNamespace.CatchAllTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_CatchAllTagHelper);
#line 11 "EnumTagHelpers.cshtml"
__TestNamespace_InputTagHelper.Value = enumValue;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("value", __TestNamespace_InputTagHelper.Value);
#line 11 "EnumTagHelpers.cshtml"
__TestNamespace_CatchAllTagHelper.CatchAll = enumValue;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("catch-all", __TestNamespace_CatchAllTagHelper.CatchAll);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(236, 51, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(287, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
