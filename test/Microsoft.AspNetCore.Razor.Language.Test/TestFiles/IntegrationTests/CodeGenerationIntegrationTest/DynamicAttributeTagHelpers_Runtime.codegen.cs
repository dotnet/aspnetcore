#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "e044ca9442dd9f93d8ce7f93a79c46a542221f1e"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
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
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::TestNamespace.InputTagHelper __TestNamespace_InputTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 51, "prefix", 51, 6, true);
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 57, DateTime.Now, 58, 13, false);

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
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 95, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
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
            ), 95, 44, false);
            AddHtmlAttributeValue(" ", 139, "suffix", 140, 7, true);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
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
            __tagHelperExecutionContext.AddTagHelperAttribute("bound", __TestNamespace_InputTagHelper.Bound, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 3, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 206, "prefix", 206, 6, true);
#line 7 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 212, DateTime.Now, 213, 13, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 226, "suffix", 227, 7, true);
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
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
            __tagHelperExecutionContext.AddTagHelperAttribute("bound", __TestNamespace_InputTagHelper.Bound, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 3, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue("", 347, long.MinValue, 347, 14, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 361, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
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
            ), 362, 44, false);
#line 10 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 406, int.MaxValue, 407, 13, false);

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
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 5, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue("", 444, long.MinValue, 444, 14, false);

#line default
#line hidden
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 458, DateTime.Now, 459, 13, false);

#line default
#line hidden
            AddHtmlAttributeValue(" ", 472, "static", 473, 7, true);
            AddHtmlAttributeValue("    ", 479, "content", 483, 11, true);
#line 12 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/DynamicAttributeTagHelpers.cshtml"
AddHtmlAttributeValue(" ", 490, int.MaxValue, 491, 13, false);

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
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
            }
            );
            __TestNamespace_InputTagHelper = CreateTagHelper<global::TestNamespace.InputTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_InputTagHelper);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unbound", 1, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 528, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
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
            ), 528, 44, false);
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
