namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_TagHelpersWithTemplate_DesignTime
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
global::System.Object __typeHelper = "*, TestAssembly";
        }
        ))();
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        private global::DivTagHelper __DivTagHelper = null;
        private global::InputTagHelper __InputTagHelper = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 13 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersWithTemplate.cshtml"
      
        RenderTemplate(
            "Template: ",
            

#line default
#line hidden
            item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersWithTemplate.cshtml"
                                  __o = item;

#line default
#line hidden
                __InputTagHelper = CreateTagHelper<global::InputTagHelper>();
                __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
            }
            )
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/TagHelpersWithTemplate.cshtml"
                                                                                               );
    

#line default
#line hidden
            __DivTagHelper = CreateTagHelper<global::DivTagHelper>();
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
