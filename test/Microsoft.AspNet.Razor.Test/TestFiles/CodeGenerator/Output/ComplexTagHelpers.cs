#pragma checksum "ComplexTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "7e06587198159a7cdea48c42e64a766a79a12cf7"
namespace TestOutput
{
    using Microsoft.AspNet.Razor.Runtime.TagHelpers;
    using System;
    using System.Threading.Tasks;

    public class ComplexTagHelpers
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
        public ComplexTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new TagHelperRunner();
            Instrumentation.BeginContext(33, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
#line 3 "ComplexTagHelpers.cshtml"
 if (true)
{
    var checkbox = "checkbox";


#line default
#line hidden

            Instrumentation.BeginContext(84, 55, true);
            WriteLiteral("    <div class=\"randomNonTagHelperAttribute\">\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(177, 34, true);
                WriteLiteral("\r\n            <h1>Set Time:</h1>\r\n");
                Instrumentation.EndContext();
#line 10 "ComplexTagHelpers.cshtml"
            

#line default
#line hidden

#line 10 "ComplexTagHelpers.cshtml"
             if (false)
            {

#line default
#line hidden

                Instrumentation.BeginContext(251, 16, true);
                WriteLiteral("                ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                    Instrumentation.BeginContext(270, 10, true);
                    WriteLiteral("New Time: ");
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.SelfClosing, "test", async() => {
                    }
                    , StartTagHelperWritingScope, EndTagHelperWritingScope);
                    __InputTagHelper = CreateTagHelper<InputTagHelper>();
                    __tagHelperExecutionContext.Add(__InputTagHelper);
                    __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                    __tagHelperExecutionContext.Add(__InputTagHelper2);
                    __InputTagHelper.Type = "text";
                    __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
                    __InputTagHelper2.Type = __InputTagHelper.Type;
                    __tagHelperExecutionContext.AddHtmlAttribute("value", Html.Raw(""));
                    __tagHelperExecutionContext.AddHtmlAttribute("placeholder", Html.Raw("Enter in a new time..."));
                    __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    Instrumentation.BeginContext(280, 66, false);
                    await WriteTagHelperAsync(__tagHelperExecutionContext);
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __PTagHelper = CreateTagHelper<PTagHelper>();
                __tagHelperExecutionContext.Add(__PTagHelper);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(267, 83, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(350, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
#line 13 "ComplexTagHelpers.cshtml"
            }
            else
            {

#line default
#line hidden

                Instrumentation.BeginContext(400, 16, true);
                WriteLiteral("                ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                    Instrumentation.BeginContext(419, 14, true);
                    WriteLiteral("Current Time: ");
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.SelfClosing, "test", async() => {
                    }
                    , StartTagHelperWritingScope, EndTagHelperWritingScope);
                    __InputTagHelper = CreateTagHelper<InputTagHelper>();
                    __tagHelperExecutionContext.Add(__InputTagHelper);
                    __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                    __tagHelperExecutionContext.Add(__InputTagHelper2);
                    StartTagHelperWritingScope();
#line 16 "ComplexTagHelpers.cshtml"
WriteLiteral(checkbox);

#line default
#line hidden
                    __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                    __InputTagHelper.Type = __tagHelperStringValueBuffer.GetContent(HtmlEncoder);
                    __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
                    __InputTagHelper2.Type = __InputTagHelper.Type;
#line 16 "ComplexTagHelpers.cshtml"
                                   __InputTagHelper2.Checked = true;

#line default
#line hidden
                    __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                    __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    Instrumentation.BeginContext(433, 37, false);
                    await WriteTagHelperAsync(__tagHelperExecutionContext);
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __PTagHelper = CreateTagHelper<PTagHelper>();
                __tagHelperExecutionContext.Add(__PTagHelper);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(416, 58, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(474, 18, true);
                WriteLiteral("\r\n                ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.SelfClosing, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
                StartTagHelperWritingScope();
#line 17 "ComplexTagHelpers.cshtml"
WriteLiteral(true ? "checkbox" : "anything");

#line default
#line hidden
                __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                __InputTagHelper.Type = __tagHelperStringValueBuffer.GetContent(HtmlEncoder);
                __tagHelperExecutionContext.AddTagHelperAttribute("tYPe", __InputTagHelper.Type);
                __InputTagHelper2.Type = __InputTagHelper.Type;
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(492, 50, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(542, 18, true);
                WriteLiteral("\r\n                ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.StartTagOnly, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
                StartTagHelperWritingScope();
#line 18 "ComplexTagHelpers.cshtml"
if(true) {

#line default
#line hidden

                WriteLiteral("checkbox");
#line 18 "ComplexTagHelpers.cshtml"
 

#line default
#line hidden

#line 18 "ComplexTagHelpers.cshtml"
} else {

#line default
#line hidden

                WriteLiteral("anything");
#line 18 "ComplexTagHelpers.cshtml"
 

#line default
#line hidden

#line 18 "ComplexTagHelpers.cshtml"
}

#line default
#line hidden

                __tagHelperStringValueBuffer = EndTagHelperWritingScope();
                __InputTagHelper.Type = __tagHelperStringValueBuffer.GetContent(HtmlEncoder);
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __InputTagHelper.Type);
                __InputTagHelper2.Type = __InputTagHelper.Type;
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(560, 79, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(641, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
#line 19 "ComplexTagHelpers.cshtml"
            }

#line default
#line hidden

                Instrumentation.BeginContext(658, 8, true);
                WriteLiteral("        ");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "time", 3);
            AddHtmlAttributeValue("", 148, "Current", 148, 7, true);
            AddHtmlAttributeValue(" ", 155, "Time:", 156, 6, true);
            AddHtmlAttributeValue(" ", 161, DateTime.Now, 162, 14, false);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(139, 529, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(670, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(767, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
#line 22 "ComplexTagHelpers.cshtml"
            

#line default
#line hidden

#line 22 "ComplexTagHelpers.cshtml"
               var @object = false;

#line default
#line hidden

                Instrumentation.BeginContext(807, 12, true);
                WriteLiteral("            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.StartTagOnly, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
#line 23 "ComplexTagHelpers.cshtml"
__InputTagHelper2.Checked = (@object);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("ChecKED", __InputTagHelper2.Checked);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(819, 28, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(847, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute("unbound", Html.Raw("first value"));
#line 21 "ComplexTagHelpers.cshtml"
                   __PTagHelper.Age = DateTimeOffset.Now.Year - 1970;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.AddHtmlAttribute("unbound", Html.Raw("second value"));
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(680, 181, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(861, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(913, 14, true);
                WriteLiteral("\r\n            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.SelfClosing, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
                __tagHelperExecutionContext.AddHtmlAttribute("unbound", Html.Raw("hello"));
                __tagHelperExecutionContext.AddHtmlAttribute("unbound", Html.Raw("world"));
#line 26 "ComplexTagHelpers.cshtml"
                                __InputTagHelper2.Checked = (DateTimeOffset.Now.Year > 2014);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(927, 85, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(1012, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
#line 25 "ComplexTagHelpers.cshtml"
__PTagHelper.Age = -1970 + @DateTimeOffset.Now.Year;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(871, 155, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(1026, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(1076, 14, true);
                WriteLiteral("\r\n            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.StartTagOnly, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
#line 29 "ComplexTagHelpers.cshtml"
__InputTagHelper2.Checked = DateTimeOffset.Now.Year > 2014;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(1090, 48, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(1138, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
#line 28 "ComplexTagHelpers.cshtml"
__PTagHelper.Age = DateTimeOffset.Now.Year - 1970;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(1036, 116, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(1152, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(1204, 14, true);
                WriteLiteral("\r\n            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.SelfClosing, "test", async() => {
                }
                , StartTagHelperWritingScope, EndTagHelperWritingScope);
                __InputTagHelper = CreateTagHelper<InputTagHelper>();
                __tagHelperExecutionContext.Add(__InputTagHelper);
                __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
                __tagHelperExecutionContext.Add(__InputTagHelper2);
#line 32 "ComplexTagHelpers.cshtml"
__InputTagHelper2.Checked =    @(  DateTimeOffset.Now.Year  ) > 2014   ;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
                __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(1218, 63, false);
                await WriteTagHelperAsync(__tagHelperExecutionContext);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(1281, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __PTagHelper = CreateTagHelper<PTagHelper>();
            __tagHelperExecutionContext.Add(__PTagHelper);
#line 31 "ComplexTagHelpers.cshtml"
__PTagHelper.Age = ("My age is this long.".Length);

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(1162, 133, false);
            await WriteTagHelperAsync(__tagHelperExecutionContext);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(1295, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(1306, 11, false);
#line 34 "ComplexTagHelpers.cshtml"
   Write(someMethod(item => new Template(async(__razor_template_writer) => {
    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", TagMode.StartTagAndEndTag, "test", async() => {
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", TagMode.SelfClosing, "test", async() => {
        }
        , StartTagHelperWritingScope, EndTagHelperWritingScope);
        __InputTagHelper = CreateTagHelper<InputTagHelper>();
        __tagHelperExecutionContext.Add(__InputTagHelper);
        __InputTagHelper2 = CreateTagHelper<InputTagHelper2>();
        __tagHelperExecutionContext.Add(__InputTagHelper2);
#line 34 "ComplexTagHelpers.cshtml"
                                   __InputTagHelper2.Checked = checked;

#line default
#line hidden
        __tagHelperExecutionContext.AddTagHelperAttribute("checked", __InputTagHelper2.Checked);
        __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
        Instrumentation.BeginContext(1345, 26, false);
        await WriteTagHelperAsync(__tagHelperExecutionContext);
        Instrumentation.EndContext();
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
    }
    , StartTagHelperWritingScope, EndTagHelperWritingScope);
    __PTagHelper = CreateTagHelper<PTagHelper>();
    __tagHelperExecutionContext.Add(__PTagHelper);
#line 34 "ComplexTagHelpers.cshtml"
          __PTagHelper.Age = 123;

#line default
#line hidden
    __tagHelperExecutionContext.AddTagHelperAttribute("age", __PTagHelper.Age);
    __tagHelperExecutionContext.AddHtmlAttribute("class", Html.Raw("hello"));
    __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
    Instrumentation.BeginContext(1318, 57, false);
    await WriteTagHelperToAsync(__razor_template_writer, __tagHelperExecutionContext);
    Instrumentation.EndContext();
    __tagHelperExecutionContext = __tagHelperScopeManager.End();
}
)
));

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(1376, 14, true);
            WriteLiteral("\r\n    </div>\r\n");
            Instrumentation.EndContext();
#line 36 "ComplexTagHelpers.cshtml"
}

#line default
#line hidden

        }
        #pragma warning restore 1998
    }
}
