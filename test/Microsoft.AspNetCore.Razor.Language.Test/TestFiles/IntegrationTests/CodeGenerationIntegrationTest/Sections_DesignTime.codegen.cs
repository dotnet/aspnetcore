namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_Sections_DesignTime
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
global::System.Object Section2 = null;
        }
        ))();
        ((System.Action)(() => {
global::System.Object Section1 = null;
        }
        ))();
        ((System.Action)(() => {
global::System.Object NestedDelegates = null;
        }
        ))();
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 1 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
  
    Layout = "_SectionTestLayout.cshtml"

#line default
#line hidden
            DefineSection("Section2", async (__razor_section_writer) => {
#line 8 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
                __o = thing;

#line default
#line hidden
            });
            DefineSection("Section1", async (__razor_section_writer) => {
            });
            DefineSection("NestedDelegates", async (__razor_section_writer) => {
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
       Func<dynamic, object> f = 

#line default
#line hidden
            item => new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_template_writer) => {
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
                                   __o = item;

#line default
#line hidden
            }
            )
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/Sections.cshtml"
                                                    ; 

#line default
#line hidden
            });
        }
        #pragma warning restore 1998
    }
}
