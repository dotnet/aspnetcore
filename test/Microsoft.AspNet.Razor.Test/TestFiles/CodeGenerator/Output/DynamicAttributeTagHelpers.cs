#pragma checksum "DynamicAttributeTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "107e341010aad754fc5c952722dbfdc7e33fc38e"
namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class DynamicAttributeTagHelpers
    {
        #line hidden
        #pragma warning disable 0414
        private global::Microsoft.AspNet.Razor.TagHelperContent __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner __tagHelperRunner = null;
        private global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager __tagHelperScopeManager = new global::Microsoft.AspNet.Razor.Runtime.TagHelperScopeManager();
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        #line hidden
        public DynamicAttributeTagHelpers()
        {
        }

        #pragma warning disable 1998
        public override async Task ExecuteAsync()
        {
            __tagHelperRunner = __tagHelperRunner ?? new global::Microsoft.AspNet.Razor.Runtime.TagHelperRunner();
            Instrumentation.BeginContext(31, 2, true);
            WriteLiteral("\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 2);
            AddHtmlAttributeValue("", 49, "prefix", 49, 6, true);
#line 3 "DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 55, DateTime.Now, 56, 14, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(33, 40, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(73, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 2);
            AddHtmlAttributeValue("", 93, new Template(async(__razor_attribute_value_writer) => {
#line 5 "DynamicAttributeTagHelpers.cshtml"
                 if (true) { 

#line default
#line hidden

                Instrumentation.BeginContext(107, 12, false);
#line 5 "DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
                Instrumentation.EndContext();
#line 5 "DynamicAttributeTagHelpers.cshtml"
                                           } else { 

#line default
#line hidden

                Instrumentation.BeginContext(130, 5, false);
#line 5 "DynamicAttributeTagHelpers.cshtml"
             WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
                Instrumentation.EndContext();
#line 5 "DynamicAttributeTagHelpers.cshtml"
                                                           }

#line default
#line hidden

            }
            ), 93, 44, false);
            AddHtmlAttributeValue(" ", 137, "suffix", 138, 7, true);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(77, 71, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(148, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            StartTagHelperWritingScope(null);
            WriteLiteral("prefix ");
#line 7 "DynamicAttributeTagHelpers.cshtml"
         WriteLiteral(DateTime.Now);

#line default
#line hidden
            WriteLiteral(" suffix");
            __tagHelperStringValueBuffer = EndTagHelperWritingScope();
            __TestNamespace_InputTagHelper.Bound = __tagHelperStringValueBuffer.GetContent(HtmlEncoder);
            __tagHelperExecutionContext.AddTagHelperAttribute("bound", __TestNamespace_InputTagHelper.Bound);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 3);
            AddHtmlAttributeValue("", 204, "prefix", 204, 6, true);
#line 7 "DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 210, DateTime.Now, 211, 14, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 224, "suffix", 225, 7, true);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(152, 83, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(235, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            StartTagHelperWritingScope(null);
#line 9 "DynamicAttributeTagHelpers.cshtml"
  WriteLiteral(long.MinValue);

#line default
#line hidden
            WriteLiteral(" ");
#line 9 "DynamicAttributeTagHelpers.cshtml"
                              if (true) { 

#line default
#line hidden

#line 9 "DynamicAttributeTagHelpers.cshtml"
                              WriteLiteral(string.Empty);

#line default
#line hidden
#line 9 "DynamicAttributeTagHelpers.cshtml"
                                                        } else { 

#line default
#line hidden

#line 9 "DynamicAttributeTagHelpers.cshtml"
                                                     WriteLiteral(false);

#line default
#line hidden
#line 9 "DynamicAttributeTagHelpers.cshtml"
                                                                        }

#line default
#line hidden

            WriteLiteral(" ");
#line 9 "DynamicAttributeTagHelpers.cshtml"
                                                              WriteLiteral(int.MaxValue);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndTagHelperWritingScope();
            __TestNamespace_InputTagHelper.Bound = __tagHelperStringValueBuffer.GetContent(HtmlEncoder);
            __tagHelperExecutionContext.AddTagHelperAttribute("bound", __TestNamespace_InputTagHelper.Bound);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 3);
#line 10 "DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue("", 345, long.MinValue, 345, 14, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 359, new Template(async(__razor_attribute_value_writer) => {
#line 10 "DynamicAttributeTagHelpers.cshtml"
                                if (true) { 

#line default
#line hidden

                Instrumentation.BeginContext(374, 12, false);
#line 10 "DynamicAttributeTagHelpers.cshtml"
     WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
                Instrumentation.EndContext();
#line 10 "DynamicAttributeTagHelpers.cshtml"
                                                          } else { 

#line default
#line hidden

                Instrumentation.BeginContext(397, 5, false);
#line 10 "DynamicAttributeTagHelpers.cshtml"
                            WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
                Instrumentation.EndContext();
#line 10 "DynamicAttributeTagHelpers.cshtml"
                                                                          }

#line default
#line hidden

            }
            ), 360, 45, false);
#line 10 "DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 404, int.MaxValue, 405, 14, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(239, 183, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(422, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 5);
#line 12 "DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue("", 442, long.MinValue, 442, 14, false);

#line default
#line hidden
#line 12 "DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 456, DateTime.Now, 457, 14, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 470, "static", 471, 7, true);
            AddHtmlAttributeValue("    ", 477, "content", 481, 11, true);
#line 12 "DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 488, int.MaxValue, 489, 14, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(426, 80, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            Instrumentation.BeginContext(506, 4, true);
            WriteLiteral("\r\n\r\n");
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNet.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            , StartTagHelperWritingScope, EndTagHelperWritingScope);
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 1);
            AddHtmlAttributeValue("", 526, new Template(async(__razor_attribute_value_writer) => {
#line 14 "DynamicAttributeTagHelpers.cshtml"
                 if (true) { 

#line default
#line hidden

                Instrumentation.BeginContext(540, 12, false);
#line 14 "DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
                Instrumentation.EndContext();
#line 14 "DynamicAttributeTagHelpers.cshtml"
                                           } else { 

#line default
#line hidden

                Instrumentation.BeginContext(563, 5, false);
#line 14 "DynamicAttributeTagHelpers.cshtml"
             WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
                Instrumentation.EndContext();
#line 14 "DynamicAttributeTagHelpers.cshtml"
                                                           }

#line default
#line hidden

            }
            ), 526, 44, false);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.Output = await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            Instrumentation.BeginContext(510, 64, false);
            Write(__tagHelperExecutionContext.Output);
            Instrumentation.EndContext();
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
