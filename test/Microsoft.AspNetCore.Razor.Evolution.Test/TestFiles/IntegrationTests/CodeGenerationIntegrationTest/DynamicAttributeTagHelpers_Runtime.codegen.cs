#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "107e341010aad754fc5c952722dbfdc7e33fc38e"
namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_DynamicAttributeTagHelpers_Runtime
    {
        #line hidden
        #pragma warning disable 0414
        private string __tagHelperStringValueBuffer = null;
        #pragma warning restore 0414
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 2, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 49, "prefix", 49, 6, true);
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 55, DateTime.Now, 56, 13, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 2, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 93, new HelperResult(async(__razor_attribute_value_writer) => {
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                 if (true) { 

#line default
#line hidden
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                           } else { 

#line default
#line hidden
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
             WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
#line 5 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                                           }

#line default
#line hidden
            }
            ), 93, 44, false);
            AddHtmlAttributeValue(" ", 137, "suffix", 138, 7, true);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginWriteTagHelperAttribute();
            WriteLiteral("prefix ");
#line 7 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
         WriteLiteral(DateTime.Now);

#line default
#line hidden
            WriteLiteral(" suffix");
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __TestNamespace_InputTagHelper.Bound = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("bound", __TestNamespace_InputTagHelper.Bound, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 3, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 204, "prefix", 204, 6, true);
#line 7 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 210, DateTime.Now, 211, 13, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 224, "suffix", 225, 7, true);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginWriteTagHelperAttribute();
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
  WriteLiteral(long.MinValue);

#line default
#line hidden
            WriteLiteral(" ");
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                              if (true) { 

#line default
#line hidden
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                              WriteLiteral(string.Empty);

#line default
#line hidden
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                                        } else { 

#line default
#line hidden
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                                     WriteLiteral(false);

#line default
#line hidden
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                                                        }

#line default
#line hidden
            WriteLiteral(" ");
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                                              WriteLiteral(int.MaxValue);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __TestNamespace_InputTagHelper.Bound = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("bound", __TestNamespace_InputTagHelper.Bound, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 3, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue("", 345, long.MinValue, 345, 14, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 359, new HelperResult(async(__razor_attribute_value_writer) => {
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                if (true) { 

#line default
#line hidden
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
     WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                                          } else { 

#line default
#line hidden
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                            WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                                                          }

#line default
#line hidden
            }
            ), 360, 44, false);
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 404, int.MaxValue, 405, 13, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 5, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue("", 442, long.MinValue, 442, 14, false);

#line default
#line hidden
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 456, DateTime.Now, 457, 13, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 470, "static", 471, 7, true);
            AddHtmlAttributeValue("    ", 477, "content", 481, 11, true);
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 488, int.MaxValue, 489, 13, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.Evolution.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 1, global::Microsoft.AspNetCore.Razor.Evolution.Legacy.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 526, new HelperResult(async(__razor_attribute_value_writer) => {
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                 if (true) { 

#line default
#line hidden
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
WriteTo(__razor_attribute_value_writer, string.Empty);

#line default
#line hidden
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                           } else { 

#line default
#line hidden
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
             WriteTo(__razor_attribute_value_writer, false);

#line default
#line hidden
#line 14 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
                                                           }

#line default
#line hidden
            }
            ), 526, 44, false);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
        }
        #pragma warning restore 1998
    }
}
