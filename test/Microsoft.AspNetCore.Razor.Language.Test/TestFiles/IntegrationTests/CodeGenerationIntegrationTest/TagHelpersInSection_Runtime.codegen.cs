#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersInSection.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "97d0dc6305d8a47fd3d81a127119f47417862f1a"
namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_TagHelpersInSection_Runtime
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
        private global::TestNamespace.MyTagHelper __TestNamespace_MyTagHelper = null;
        private global::TestNamespace.NestedTagHelper __TestNamespace_NestedTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
#line 3 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersInSection.cshtml"
  
    var code = "some code";

#line default
#line hidden
            WriteLiteral("\r\n");
            DefineSection("MySection", async () => {
            WriteLiteral("\r\n    <div>\r\n        ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("mytaghelper", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                WriteLiteral("\r\n            In None ContentBehavior.\r\n            ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("nestedtaghelper", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "test", async() => {
                    WriteLiteral("Some buffered values with ");
#line 11 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersInSection.cshtml"
                                                  Write(code);

#line default
#line hidden
                }
                );
                __TestNamespace_NestedTagHelper = CreateTagHelper<global::TestNamespace.NestedTagHelper>();
                __tagHelperExecutionContext.Add(__TestNamespace_NestedTagHelper);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n        ");
            }
            );
            __TestNamespace_MyTagHelper = CreateTagHelper<global::TestNamespace.MyTagHelper>();
            __tagHelperExecutionContext.Add(__TestNamespace_MyTagHelper);
            BeginWriteTagHelperAttribute();
            WriteLiteral("Current Time: ");
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersInSection.cshtml"
                                      WriteLiteral(DateTime.Now);

#line default
#line hidden
            __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
            __TestNamespace_MyTagHelper.BoundProperty = __tagHelperStringValueBuffer;
            __tagHelperExecutionContext.AddTagHelperAttribute("boundproperty", __TestNamespace_MyTagHelper.BoundProperty, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "unboundproperty", 3, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 188, "Current", 188, 7, true);
            AddHtmlAttributeValue(" ", 195, "Time:", 196, 6, true);
#line 9 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersInSection.cshtml"
AddHtmlAttributeValue(" ", 201, DateTime.Now, 202, 13, false);

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
            WriteLiteral("\r\n    </div>\r\n");
            });
        }
        #pragma warning restore 1998
    }
}
