#pragma checksum "TransitionsInTagHelperAttributes.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "050ce5cabf326eaa117aa12f5a1a10dbf82a8917"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class TransitionsInTagHelperAttributes
    {
        #line hidden
        #pragma warning disable 0414
        private TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private TagHelperExecutionContext __tagHelperExecutionContext = null;
        private TagHelperRunner __tagHelperRunner = null;
        private TagHelperScopeManager __tagHelperScopeManager = new TagHelperScopeManager();
        private PTagHelper __PTagHelper = null;
        #line hidden
        public TransitionsInTagHelperAttributes()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
#line 2 "TransitionsInTagHelperAttributes.cshtml"
   
    var @class = "container-fluid";
    var @int = 1;

#line default
#line hidden

            Instrumentation.BeginContext(97, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(128, 11, true);
                WriteLiteral("Body of Tag");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            AddHtmlAttributeValues("class", __tagHelperExecutionContext, 
            Tuple.Create(Tuple.Create("", 109), Tuple.Create<System.Object, System.Int32>(new Template((__razor_attribute_value_writer) => {
            }
            ), 109), false));
#line 7 "TransitionsInTagHelperAttributes.cshtml"
    __PTagHelper.Age = 1337;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(99, 44, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(143, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            AddHtmlAttributeValues("class", __tagHelperExecutionContext, 
            Tuple.Create(Tuple.Create("", 155), Tuple.Create<System.Object, System.Int32>(@class, 155), false));
#line 8 "TransitionsInTagHelperAttributes.cshtml"
       __PTagHelper.Age = 42;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(145, 34, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(179, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("test"));
#line 9 "TransitionsInTagHelperAttributes.cshtml"
  __PTagHelper.Age = 42 + @int;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(181, 36, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(217, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("test"));
#line 10 "TransitionsInTagHelperAttributes.cshtml"
  __PTagHelper.Age = int;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(219, 31, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(250, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("test"));
#line 11 "TransitionsInTagHelperAttributes.cshtml"
  __PTagHelper.Age = (@int);

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(252, 34, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(286, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            AddHtmlAttributeValues("class", __tagHelperExecutionContext, Tuple.Create(Tuple.Create("", 298), Tuple.Create("custom-", 298), true), 
            Tuple.Create(Tuple.Create("", 305), Tuple.Create<System.Object, System.Int32>(@class, 305), false));
#line 12 "TransitionsInTagHelperAttributes.cshtml"
              __PTagHelper.Age = 4 * @(@int + 2);

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(288, 54, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(342, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
        }
        #pragma warning restore 1998
    }
}
