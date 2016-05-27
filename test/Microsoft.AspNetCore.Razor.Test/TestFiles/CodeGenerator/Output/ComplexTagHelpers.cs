#pragma checksum "ComplexTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "3cc5f5ed458e4e33874c4242798b195a31ab065c"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ComplexTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private string __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = null;
        private global::TestNamespace.PTagHelper __TestNamespace_PTagHelper = null;
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        private global::TestNamespace.InputTagHelper2 __TestNamespace_InputTagHelper2 = null;
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", "text", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("value", new global::Microsoft.AspNetCore.Html.HtmlString(""), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("placeholder", new global::Microsoft.AspNetCore.Html.HtmlString("Enter in a new time..."), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("unbound", new global::Microsoft.AspNetCore.Html.HtmlString("first value"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("unbound", new global::Microsoft.AspNetCore.Html.HtmlString("second value"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_5 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("unbound", new global::Microsoft.AspNetCore.Html.HtmlString("hello"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_6 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("unbound", new global::Microsoft.AspNetCore.Html.HtmlString("world"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_7 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("hello"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        public ComplexTagHelpers()
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
#line 3 "ComplexTagHelpers.cshtml"
 if (true)
{
    var checkbox = "checkbox";


#line default
#line hidden

            Instrumentation.BeginContext(82, 55, true);
            WriteLiteral("    <div class=\"randomNonTagHelperAttribute\">\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(175, 34, true);
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

                Instrumentation.BeginContext(249, 16, true);
                WriteLiteral("                ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                    Instrumentation.BeginContext(268, 10, true);
                    WriteLiteral("New Time: ");
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                    }
                    );
                    __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                    __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                    __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                    __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                    __TestNamespace_InputTagHelper.Type = (string)__tagHelperAttribute_0.Value;
                    __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
                    __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
                    __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
                    __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    Instrumentation.BeginContext(278, 66, false);
                    Write(__tagHelperExecutionContext.Output);
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                }
                );
                __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Instrumentation.BeginContext(265, 83, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(348, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
#line 13 "ComplexTagHelpers.cshtml"
            }
            else
            {

#line default
#line hidden

                Instrumentation.BeginContext(398, 16, true);
                WriteLiteral("                ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                    Instrumentation.BeginContext(417, 14, true);
                    WriteLiteral("Current Time: ");
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                    }
                    );
                    __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                    __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                    __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                    __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                    BeginWriteTagHelperAttribute();
#line 16 "ComplexTagHelpers.cshtml"
                                 WriteLiteral(checkbox);

#line default
#line hidden
                    __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
                    __TestNamespace_InputTagHelper.Type = __tagHelperStringValueBuffer;
                    __tagHelperExecutionContext.AddTagHelperAttribute("type", __TestNamespace_InputTagHelper.Type, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                    __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
#line 16 "ComplexTagHelpers.cshtml"
                     __TestNamespace_InputTagHelper2.Checked = true;

#line default
#line hidden
                    __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    Instrumentation.BeginContext(431, 37, false);
                    Write(__tagHelperExecutionContext.Output);
                    Instrumentation.EndContext();
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                }
                );
                __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Instrumentation.BeginContext(414, 58, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(472, 18, true);
                WriteLiteral("\r\n                ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                }
                );
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                BeginWriteTagHelperAttribute();
#line 17 "ComplexTagHelpers.cshtml"
                  WriteLiteral(true ? "checkbox" : "anything");

#line default
#line hidden
                __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
                __TestNamespace_InputTagHelper.Type = __tagHelperStringValueBuffer;
                __tagHelperExecutionContext.AddTagHelperAttribute("tYPe", __TestNamespace_InputTagHelper.Type, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.SingleQuotes);
                __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(490, 50, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(540, 18, true);
                WriteLiteral("\r\n                ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagOnly, "test", async() => {
                }
                );
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                BeginWriteTagHelperAttribute();
#line 18 "ComplexTagHelpers.cshtml"
                              if(true) {

#line default
#line hidden

                WriteLiteral("checkbox");
#line 18 "ComplexTagHelpers.cshtml"
                                                              } else {

#line default
#line hidden

                WriteLiteral("anything");
#line 18 "ComplexTagHelpers.cshtml"
                                                                                            }

#line default
#line hidden

                __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
                __TestNamespace_InputTagHelper.Type = __tagHelperStringValueBuffer;
                __tagHelperExecutionContext.AddTagHelperAttribute("type", __TestNamespace_InputTagHelper.Type, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.SingleQuotes);
                __TestNamespace_InputTagHelper2.Type = __TestNamespace_InputTagHelper.Type;
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(558, 79, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(639, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
#line 19 "ComplexTagHelpers.cshtml"
            }

#line default
#line hidden

                Instrumentation.BeginContext(656, 8, true);
                WriteLiteral("        ");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "time", 3, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 146, "Current", 146, 7, true);
            AddHtmlAttributeValue(" ", 153, "Time:", 154, 6, true);
#line 8 "ComplexTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 159, DateTime.Now, 160, 14, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(137, 529, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(668, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(765, 2, true);
                WriteLiteral("\r\n");
                Instrumentation.EndContext();
#line 22 "ComplexTagHelpers.cshtml"
            

#line default
#line hidden

#line 22 "ComplexTagHelpers.cshtml"
               var @object = false;

#line default
#line hidden

                Instrumentation.BeginContext(805, 12, true);
                WriteLiteral("            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagOnly, "test", async() => {
                }
                );
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
#line 23 "ComplexTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = (@object);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("ChecKED", __TestNamespace_InputTagHelper2.Checked, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(817, 28, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(845, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_3);
#line 21 "ComplexTagHelpers.cshtml"
     __TestNamespace_PTagHelper.Age = DateTimeOffset.Now.Year - 1970;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_4);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(678, 181, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(859, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(911, 14, true);
                WriteLiteral("\r\n            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                }
                );
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_5);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_6);
#line 26 "ComplexTagHelpers.cshtml"
                  __TestNamespace_InputTagHelper2.Checked = (DateTimeOffset.Now.Year > 2014);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(925, 85, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(1010, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
#line 25 "ComplexTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = -1970 + @DateTimeOffset.Now.Year;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(869, 155, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(1024, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(1074, 14, true);
                WriteLiteral("\r\n            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagOnly, "test", async() => {
                }
                );
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
#line 29 "ComplexTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked = DateTimeOffset.Now.Year > 2014;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(1088, 48, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(1136, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
#line 28 "ComplexTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = DateTimeOffset.Now.Year - 1970;

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(1034, 116, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(1150, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                Instrumentation.BeginContext(1202, 14, true);
                WriteLiteral("\r\n            ");
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                }
                );
                __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
                __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
                __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
#line 32 "ComplexTagHelpers.cshtml"
__TestNamespace_InputTagHelper2.Checked =    @(  DateTimeOffset.Now.Year  ) > 2014   ;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                Instrumentation.BeginContext(1216, 63, false);
                Write(__tagHelperExecutionContext.Output);
                Instrumentation.EndContext();
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                Instrumentation.BeginContext(1279, 10, true);
                WriteLiteral("\r\n        ");
                Instrumentation.EndContext();
            }
            );
            __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
#line 31 "ComplexTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = ("My age is this long.".Length);

#line default
#line hidden
            __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Instrumentation.BeginContext(1160, 133, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(1293, 10, true);
            WriteLiteral("\r\n        ");
            Instrumentation.EndContext();
            Instrumentation.BeginContext(1304, 11, false);
#line 34 "ComplexTagHelpers.cshtml"
   Write(someMethod(item => new Template(async(__razor_template_writer) => {
    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("p", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
        __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
        }
        );
        __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
        __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
        __TestNamespace_InputTagHelper2 = CreateTagHelper<global::TestNamespace.InputTagHelper2>();
        __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper2);
#line 34 "ComplexTagHelpers.cshtml"
                     __TestNamespace_InputTagHelper2.Checked = checked;

#line default
#line hidden
        __tagHelperExecutionContext.AddTagHelperAttribute("checked", __TestNamespace_InputTagHelper2.Checked, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
        Instrumentation.BeginContext(1343, 26, false);
        Write(__tagHelperExecutionContext.Output);
        Instrumentation.EndContext();
        __tagHelperExecutionContext = __tagHelperScopeManager.End();
    }
    );
    __TestNamespace_PTagHelper = CreateTagHelper<global::TestNamespace.PTagHelper>();
    __tagHelperExecutionContext.Add(__TestNamespace_PTagHelper);
#line 34 "ComplexTagHelpers.cshtml"
__TestNamespace_PTagHelper.Age = 123;

#line default
#line hidden
    __tagHelperExecutionContext.AddTagHelperAttribute("age", __TestNamespace_PTagHelper.Age, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
    __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_7);
    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
    if (!__tagHelperExecutionContext.Output.IsContentModified)
    {
        await __tagHelperExecutionContext.SetOutputContentAsync();
    }
    Instrumentation.BeginContext(1316, 57, false);
    WriteTo(__razor_template_writer, __tagHelperExecutionContext.Output);
    Instrumentation.EndContext();
    __tagHelperExecutionContext = __tagHelperScopeManager.End();
}
)
));

#line default
#line hidden
            Instrumentation.EndContext();
            Instrumentation.BeginContext(1374, 14, true);
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
