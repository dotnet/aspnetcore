#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersWithTemplate.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "f1ffadb1ad9c279ac40218b45a1688ac2f3ea857"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_TagHelpersWithTemplate_Runtime
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", new global::Microsoft.AspNetCore.Html.HtmlString("checkbox"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("checked", new global::Microsoft.AspNetCore.Html.HtmlString("true"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("condition", new global::Microsoft.AspNetCore.Html.HtmlString("true"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
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
        private global::DivTagHelper __DivTagHelper = null;
        private global::InputTagHelper __InputTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("div", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                WriteLiteral("\r\n");
#line 13 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersWithTemplate.cshtml"
      
        RenderTemplate(
            "Template: ",
            

#line default
#line hidden
                item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("div", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                        WriteLiteral("<h3>");
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersWithTemplate.cshtml"
                                  Write(item);

#line default
#line hidden
                        WriteLiteral("</h3>");
                        __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "test", async() => {
                        }
                        );
                        __InputTagHelper = CreateTagHelper<global::InputTagHelper>();
                        __tagHelperExecutionContext.Add(__InputTagHelper);
                        __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
                        __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
                        await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                        if (!__tagHelperExecutionContext.Output.IsContentModified)
                        {
                            await __tagHelperExecutionContext.SetOutputContentAsync();
                        }
                        Write(__tagHelperExecutionContext.Output);
                        __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    }
                    );
                    __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
                    __tagHelperExecutionContext.Add(__DivTagHelper);
                    __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        await __tagHelperExecutionContext.SetOutputContentAsync();
                    }
                    WriteTo(__razor_template_writer, __tagHelperExecutionContext.Output);
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                }
                )
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersWithTemplate.cshtml"
                                                                                               );
    

#line default
#line hidden
            }
            );
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            __tagHelperExecutionContext.Add(__DivTagHelper);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n\r\n");
        }
        #pragma warning restore 1998
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersWithTemplate.cshtml"
            
    public void RenderTemplate(string title, Func<string, HelperResult> template)
    {
        Output.WriteLine("<br /><p><em>Rendering Template:</em></p>");
        var helperResult = template(title);
        helperResult.WriteTo(Output, HtmlEncoder);
    }

#line default
#line hidden
    }
}
